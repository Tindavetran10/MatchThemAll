// Copyright Elliot Bentine, 2018-
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace ProPixelizer
{
    /// <summary>
    /// Performs the ProPixelizer pixelization pass (URP 17 RenderGraph-compatible).
    /// Uses AddUnsafePass with RecordRenderGraph for the RenderGraph path, and
    /// retains the legacy Execute method as a compatibility fallback when RG is disabled.
    /// </summary>
    public class PixelizationPass : ProPixelizerPass
    {
        public PixelizationPass(ShaderResources shaders, OutlineDetectionPassRG outlines)
        {
            // Insert just before AfterRenderingOpaques so we can still write the depth buffer.
            // -1 prevents SSAO's forced depth copy from running before us.
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques - 1;
            Materials = new MaterialLibrary(shaders);
            OutlinePass = outlines;
        }

        private MaterialLibrary Materials;
        private OutlineDetectionPassRG OutlinePass;

        // ----- Profiler tag -----
        public const string PROFILER_TAG = "PIXELISATION";

        // ----- Shader names -----
        private const string CopyDepthShaderName             = "Hidden/ProPixelizer/SRP/BlitCopyDepth";
        private const string CopyMainTexAndDepthShaderName   = "Hidden/ProPixelizer/SRP/BlitCopyMainTexAndDepth";
        private const string PixelizationMapShaderName       = "Hidden/ProPixelizer/SRP/Pixelization Map";
        private const string ApplyPixelizationMapShaderName  = "Hidden/ProPixelizer/SRP/ApplyPixelizationMap";

        // Global keyword cached once per instance.
        private readonly GlobalKeyword ApplyPixelMapDepthOutputKeyword =
            GlobalKeyword.Create("PIXELMAP_DEPTH_OUTPUT_ON");

        // ----- Shader resources -----
        [Serializable]
        public sealed class ShaderResources
        {
            public Shader PixelizationMap;
            public Shader CopyDepth;
            public Shader CopyMainTexAndDepth;
            public Shader ApplyPixelizationMap;

            public ShaderResources Load()
            {
                PixelizationMap      = Shader.Find(PixelizationMapShaderName);
                CopyDepth            = Shader.Find(CopyDepthShaderName);
                ApplyPixelizationMap = Shader.Find(ApplyPixelizationMapShaderName);
                CopyMainTexAndDepth  = Shader.Find(CopyMainTexAndDepthShaderName);
                return this;
            }
        }

        // ----- Material library -----
        public sealed class MaterialLibrary
        {
            private readonly ShaderResources Resources;
            private Material _PixelizationMap;
            private Material _CopyDepth;
            private Material _CopyMainTexAndDepth;
            private Material _ApplyPixelizationMap;

            public Material PixelizationMap
            {
                get { if (_PixelizationMap == null) _PixelizationMap = new Material(Resources.PixelizationMap); return _PixelizationMap; }
            }
            public Material CopyDepth
            {
                get { if (_CopyDepth == null) _CopyDepth = new Material(Resources.CopyDepth); return _CopyDepth; }
            }
            public Material CopyMainTexAndDepth
            {
                get { if (_CopyMainTexAndDepth == null) _CopyMainTexAndDepth = new Material(Resources.CopyMainTexAndDepth); return _CopyMainTexAndDepth; }
            }
            public Material ApplyPixelizationMap
            {
                get { if (_ApplyPixelizationMap == null) _ApplyPixelizationMap = new Material(Resources.ApplyPixelizationMap); return _ApplyPixelizationMap; }
            }

            public MaterialLibrary(ShaderResources resources) { Resources = resources; }
        }

        // ----- Persistent RTHandles (allocated in Configure / RecordRenderGraph) -----
        private RTHandle _PixelizationMapRT;
        private RTHandle _OriginalScene;
        private RTHandle _PixelatedScene;
        private RTHandle _PixelatedScene_Depth;
        private RTHandle _CameraDepthAttachmentTemp;



        // =======================================================================
        //  RENDER GRAPH PATH (URP 17)
        // =======================================================================

        /// <summary>Pass data shared between RecordRenderGraph and the render func.</summary>
        private class PassData
        {
            public MaterialLibrary Materials;
            public GlobalKeyword   DepthOutputKeyword;
            public bool            IsOrthographic;
            public bool            IsOverlay;
            public CameraType      CameraType;

            // Imported RTHandles — used via GetNativeCommandBuffer
            public RTHandle OriginalScene;
            public RTHandle PixelizationMapRT;
            public RTHandle PixelatedScene;
            public RTHandle PixelatedScene_Depth;
            public RTHandle CameraDepthAttachmentTemp;
            public RTHandle OutlineObjectBuffer;
            public RTHandle OutlineObjectBuffer_Depth;
            public RTHandle OutlineBuffer;

            // Camera color / depth TextureHandles (declared to the graph)
            public TextureHandle CameraColor;
            public TextureHandle CameraDepth;

            // View / Projection matrices for restoration
            public Matrix4x4 ViewMatrix;
            public Matrix4x4 ProjectionMatrix;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData   = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            // Skip preview cameras.
            if (cameraData.cameraType == CameraType.Preview)
                return;

            // Ensure persistent RTHandles are allocated for this frame's descriptor.
            var desc = cameraData.cameraTargetDescriptor;
            desc.useMipMap          = false;
            desc.depthBufferBits    = 0;

            var depthDesc = cameraData.cameraTargetDescriptor;
            depthDesc.useMipMap      = false;
            depthDesc.colorFormat    = RenderTextureFormat.Depth;

            var pixMapDesc = desc;
            pixMapDesc.colorFormat   = RenderTextureFormat.ARGB32;
            pixMapDesc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
            pixMapDesc.depthBufferBits = 0;

            RenderingUtils.ReAllocateHandleIfNeeded(ref _PixelizationMapRT,       pixMapDesc, name: "ProP_PixelizationMap");
            RenderingUtils.ReAllocateHandleIfNeeded(ref _OriginalScene,           desc,       name: "ProP_OriginalScene");
            RenderingUtils.ReAllocateHandleIfNeeded(ref _PixelatedScene,          desc,       name: "ProP_PixelatedScene");
            RenderingUtils.ReAllocateHandleIfNeeded(ref _PixelatedScene_Depth,    depthDesc,  name: "ProP_PixelatedScene_Depth");
            RenderingUtils.ReAllocateHandleIfNeeded(ref _CameraDepthAttachmentTemp, depthDesc, name: "ProP_CameraDepthAttachmentTemp");

            var texelSize = new Vector4(
                1f / cameraData.cameraTargetDescriptor.width,
                1f / cameraData.cameraTargetDescriptor.height,
                cameraData.cameraTargetDescriptor.width,
                cameraData.cameraTargetDescriptor.height);

            // Import persistent RTHandles so the graph can track dependencies.
            var hOriginalScene           = renderGraph.ImportTexture(_OriginalScene);
            var hPixelizationMap         = renderGraph.ImportTexture(_PixelizationMapRT);
            var hPixelatedScene          = renderGraph.ImportTexture(_PixelatedScene);
            var hPixelatedScene_Depth    = renderGraph.ImportTexture(_PixelatedScene_Depth);
            var hDepthAttachmentTemp     = renderGraph.ImportTexture(_CameraDepthAttachmentTemp);
            var hOutlineObject           = renderGraph.ImportTexture(OutlinePass._OutlineObjectBuffer);
            var hOutlineObjectDepth      = renderGraph.ImportTexture(OutlinePass._OutlineObjectBuffer_Depth);
            var hOutline                 = renderGraph.ImportTexture(OutlinePass._OutlineBuffer);

            bool isOverlay = cameraData.renderType == CameraRenderType.Overlay;

            using (var builder = renderGraph.AddUnsafePass<PassData>(PROFILER_TAG, out var passData))
            {
                // Populate pass data
                passData.Materials               = Materials;
                passData.DepthOutputKeyword      = ApplyPixelMapDepthOutputKeyword;
                passData.IsOrthographic          = cameraData.camera.orthographic;
                passData.IsOverlay               = isOverlay;
                passData.CameraType              = cameraData.cameraType;
                passData.ViewMatrix              = cameraData.GetViewMatrix();
                passData.ProjectionMatrix        = cameraData.GetProjectionMatrix();
                // RTHandles (accessed via native cmd inside the render func)
                passData.OriginalScene           = _OriginalScene;
                passData.PixelizationMapRT       = _PixelizationMapRT;
                passData.PixelatedScene          = _PixelatedScene;
                passData.PixelatedScene_Depth    = _PixelatedScene_Depth;
                passData.CameraDepthAttachmentTemp = _CameraDepthAttachmentTemp;
                passData.OutlineObjectBuffer     = OutlinePass._OutlineObjectBuffer;
                passData.OutlineObjectBuffer_Depth = OutlinePass._OutlineObjectBuffer_Depth;
                passData.OutlineBuffer           = OutlinePass._OutlineBuffer;
                passData.CameraColor             = resourceData.activeColorTexture;
                passData.CameraDepth             = resourceData.activeDepthTexture;

                // Declare resource accesses to the graph.
                builder.UseTexture(hOutlineObject,        AccessFlags.Read);
                builder.UseTexture(hOutlineObjectDepth,   AccessFlags.Read);
                builder.UseTexture(hOutline,              AccessFlags.Read);
                builder.UseTexture(hOriginalScene,        AccessFlags.ReadWrite);
                builder.UseTexture(hPixelizationMap,      AccessFlags.ReadWrite);
                builder.UseTexture(hPixelatedScene,       AccessFlags.ReadWrite);
                builder.UseTexture(hPixelatedScene_Depth, AccessFlags.ReadWrite);
                builder.UseTexture(hDepthAttachmentTemp,  AccessFlags.ReadWrite);
                builder.UseTexture(passData.CameraColor,  AccessFlags.ReadWrite);
                builder.UseTexture(passData.CameraDepth,  AccessFlags.ReadWrite);

                // Allow setting global shader state inside the pass.
                builder.AllowGlobalStateModification(true);
                // This pass must never be culled by the graph.
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    ExecutePixelizationCommands(cmd, data);
                });
            }
        }

        // =======================================================================
        //  SHARED RENDERING LOGIC (used by both RG and legacy paths)
        // =======================================================================

        private static void ExecutePixelizationCommands(CommandBuffer cmd, PassData data)
        {
            // --- Material keywords ---
            if (data.IsOrthographic)
                data.Materials.PixelizationMap.EnableKeyword("ORTHO_PROJECTION");
            else
                data.Materials.PixelizationMap.DisableKeyword("ORTHO_PROJECTION");

            if (data.IsOverlay)
                data.Materials.PixelizationMap.EnableKeyword("OVERLAY_CAMERA");
            else
                data.Materials.PixelizationMap.DisableKeyword("OVERLAY_CAMERA");

            // 1. Preserve original scene colours (guarantees point-sampled source).
            Blitter.BlitCameraTexture(cmd, data.CameraColor, data.OriginalScene);

            // 2. Build pixelization map from the outline object metadata buffer.
            cmd.SetGlobalTexture("_MainTex",           data.OutlineObjectBuffer);
            cmd.SetGlobalTexture("_SourceDepthTexture", data.OutlineObjectBuffer_Depth,
                                 RenderTextureSubElement.Depth);
            cmd.SetGlobalTexture("_SceneDepthTexture", data.CameraDepth);
            Blitter.BlitCameraTexture(cmd, data.OriginalScene, data.PixelizationMapRT,
                                      data.Materials.PixelizationMap, 0);

            // 3. Pixelate color into _PixelatedScene.
            cmd.SetGlobalTexture("_MainTex",        data.OriginalScene);
            cmd.SetGlobalTexture("_PixelizationMap", data.PixelizationMapRT);
            cmd.DisableKeyword(data.DepthOutputKeyword);
            Blitter.BlitCameraTexture(cmd, data.OriginalScene, data.PixelatedScene,
                                      data.Materials.ApplyPixelizationMap, 0);

            // 4. Pixelate depth into _PixelatedScene_Depth.
            cmd.EnableKeyword(data.DepthOutputKeyword);
            Blitter.BlitCameraTexture(cmd, data.CameraDepth, data.PixelatedScene_Depth,
                                      data.Materials.ApplyPixelizationMap, 0);

            // 5. Write pixelated color + depth back to the camera targets.
            cmd.SetGlobalTexture("_MainTex",  data.PixelatedScene);
            cmd.SetGlobalTexture("_SourceTex", data.PixelatedScene);
            cmd.SetGlobalTexture("_Depth",     data.PixelatedScene_Depth);
            Blitter.BlitCameraTexture(cmd, data.PixelatedScene, data.CameraColor);
            Blitter.BlitCameraTexture(cmd, data.PixelatedScene_Depth, data.CameraDepth,
                                      data.Materials.CopyDepth, 0);

            // 6. Copy pixelated depth into a temp buffer, then restore it on both camera targets.
            Blitter.BlitCameraTexture(cmd, data.PixelatedScene_Depth, data.CameraDepthAttachmentTemp,
                                      data.Materials.CopyDepth, 0);

            if (data.CameraType != CameraType.Preview)
                Blitter.BlitCameraTexture(cmd, data.CameraDepthAttachmentTemp, data.CameraDepth,
                                          data.Materials.CopyDepth, 0);

            // 7. Restore camera matrices so subsequent passes are not affected.
            cmd.SetViewMatrix(data.ViewMatrix);
            cmd.SetProjectionMatrix(data.ProjectionMatrix);
        }

        // =======================================================================
        //  CLEANUP
        // =======================================================================

        public void Dispose()
        {
            _PixelatedScene?.Release();
            _PixelatedScene_Depth?.Release();
            _OriginalScene?.Release();
            _CameraDepthAttachmentTemp?.Release();
            _PixelizationMapRT?.Release();
        }
    }
}