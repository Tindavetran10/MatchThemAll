// Copyright Elliot Bentine, 2018-
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace ProPixelizer
{
    /// <summary>
    /// Render-Graph-only implementation of the ProPixelizer outline detection pass for URP 17+.
    /// Uses AddUnsafePass / RecordRenderGraph exclusively — no legacy Execute/Configure path.
    /// </summary>
    public class OutlineDetectionPassRG : ProPixelizerPass
    {
        // ----- Constants -----
        private const string OutlineDetectionShaderName  = "Hidden/ProPixelizer/SRP/OutlineDetection";
        public  const string PROPIXELIZER_OBJECT_BUFFER  = "ProPixelizerMetadata";
        public  const string OUTLINE_BUFFER              = "_ProPixelizerOutlines";
        public  const string PROFILER_TAG                = "ProPixelizerOutlines";
        private const string PROPIXELIZER_SHADER_TAG     = "ProPixelizer";

        // ----- Shader property IDs -----
        private static readonly int OutlineDepthTestThreshold  = Shader.PropertyToID("_OutlineDepthTestThreshold");
        private static readonly int EdgeDetectionSensitivity   = Shader.PropertyToID("_NormalEdgeDetectionSensitivity");
        private static readonly int ProPixelizerPixelScale     = Shader.PropertyToID("_ProPixelizer_Pixel_Scale");
        private static readonly int MainTex                    = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexDepth               = Shader.PropertyToID("_MainTex_Depth");
        private static readonly int Size                       = Shader.PropertyToID("_TexelSize");
        private static readonly int ProPixelizerOutlines       = Shader.PropertyToID(OUTLINE_BUFFER);
        private static readonly ShaderTagId ProPixelizerShaderTagID = new ShaderTagId(PROPIXELIZER_SHADER_TAG);

        // ----- Public configuration -----
        public bool  DepthTestOutlines;
        public float DepthTestThreshold;
        public bool  UseNormalsForEdgeDetection  = true;
        public float NormalEdgeDetectionSensitivity = 1f;

        // ----- RTHandles (public so PixelizationPass can import them) -----
        public RTHandle _OutlineObjectBuffer;
        public RTHandle _OutlineObjectBuffer_Depth;
        public RTHandle _OutlineBuffer;

        // ----- Private -----
        private readonly MaterialLibrary _materials;

        // -----------------------------------------------------------------------
        //  Shader resources
        // -----------------------------------------------------------------------
        [Serializable]
        public sealed class ShaderResources
        {
            public Shader OutlineDetection;

            public ShaderResources Load()
            {
                OutlineDetection = Shader.Find(OutlineDetectionShaderName);
                return this;
            }
        }

        // -----------------------------------------------------------------------
        //  Material library
        // -----------------------------------------------------------------------
        private sealed class MaterialLibrary
        {
            private readonly ShaderResources _resources;
            private Material _outlineDetection;

            public Material OutlineDetection
            {
                get
                {
                    if (_outlineDetection == null)
                        _outlineDetection = new Material(_resources.OutlineDetection);
                    return _outlineDetection;
                }
            }

            public MaterialLibrary(ShaderResources resources) { _resources = resources; }
        }

        // -----------------------------------------------------------------------
        //  Constructor
        // -----------------------------------------------------------------------
        public OutlineDetectionPassRG(ShaderResources resources)
        {
            _materials = new MaterialLibrary(resources);
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        // -----------------------------------------------------------------------
        //  Pass data
        // -----------------------------------------------------------------------
        private static readonly int ProPixelizerRenderTargetInfo = Shader.PropertyToID("_ProPixelizer_RenderTargetInfo");

        private class PassData
        {
            // Config
            public bool  DepthTestOutlines;
            public float DepthTestThreshold;
            public bool  UseNormals;
            public float NormalSensitivity;
            public float PixelScale;
            public Vector4 TexelSize;
            public Vector4 RenderTargetInfo;
            public Matrix4x4 ViewMatrix;
            public Matrix4x4 ProjectionMatrix;

            // Materials
            public MaterialLibrary Materials;

            // RTHandles (for raw cmd access inside the render func)
            public RTHandle OutlineObjectBuffer;
            public RTHandle OutlineObjectBuffer_Depth;
            public RTHandle OutlineBuffer;

            // Renderer list declared to the graph
            public RendererListHandle RendererList;
        }

        // -----------------------------------------------------------------------
        //  RecordRenderGraph  (URP 17 RG path)
        // -----------------------------------------------------------------------
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData   = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();

            // Allocate / reallocate persistent RTHandles.
            var desc = cameraData.cameraTargetDescriptor;
            desc.useMipMap    = false;
            desc.colorFormat  = RenderTextureFormat.ARGB32;
            desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;

            var depthDesc = desc;
            depthDesc.colorFormat  = RenderTextureFormat.Depth;
            desc.depthBufferBits   = 0;

            RenderingUtils.ReAllocateHandleIfNeeded(ref _OutlineObjectBuffer,       desc,      name: PROPIXELIZER_OBJECT_BUFFER);
            RenderingUtils.ReAllocateHandleIfNeeded(ref _OutlineObjectBuffer_Depth, depthDesc, name: PROPIXELIZER_OBJECT_BUFFER + "_Depth");
            RenderingUtils.ReAllocateHandleIfNeeded(ref _OutlineBuffer,             desc,      name: OUTLINE_BUFFER);

            var texelSize = new Vector4(
                1f / cameraData.cameraTargetDescriptor.width,
                1f / cameraData.cameraTargetDescriptor.height,
                cameraData.cameraTargetDescriptor.width,
                cameraData.cameraTargetDescriptor.height);

            // Import RTHandles into the render graph.
            var hObjectBuffer      = renderGraph.ImportTexture(_OutlineObjectBuffer);
            var hObjectBufferDepth = renderGraph.ImportTexture(_OutlineObjectBuffer_Depth);
            var hOutlineBuffer     = renderGraph.ImportTexture(_OutlineBuffer);

            // Build the renderer list for drawing ProPixelizer-tagged objects.
            var sort           = new SortingSettings(cameraData.camera);
            var drawSettings   = new DrawingSettings(ProPixelizerShaderTagID, sort);
            var filterSettings = new FilteringSettings(RenderQueueRange.all);
            var listParams     = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
            var rendererList   = renderGraph.CreateRendererList(listParams);

            using var builder = renderGraph.AddUnsafePass<PassData>(PROFILER_TAG, out var passData);
            // Populate pass data.
            passData.DepthTestOutlines        = DepthTestOutlines;
            passData.DepthTestThreshold       = DepthTestThreshold;
            passData.UseNormals               = UseNormalsForEdgeDetection;
            passData.NormalSensitivity        = NormalEdgeDetectionSensitivity;
            passData.PixelScale               = cameraData.cameraType == CameraType.Preview ? 0.01f : 1f;
            passData.TexelSize                = texelSize;
            var hp = RTHandles.rtHandleProperties;
            passData.RenderTargetInfo         = new Vector4(
                cameraData.cameraTargetDescriptor.width,
                cameraData.cameraTargetDescriptor.height,
                hp.rtHandleScale.x,
                hp.rtHandleScale.y);
            passData.ViewMatrix               = cameraData.GetViewMatrix();
            passData.ProjectionMatrix         = cameraData.GetProjectionMatrix();
            passData.Materials                = _materials;
            passData.OutlineObjectBuffer      = _OutlineObjectBuffer;
            passData.OutlineObjectBuffer_Depth = _OutlineObjectBuffer_Depth;
            passData.OutlineBuffer            = _OutlineBuffer;
            passData.RendererList             = rendererList;

            // Declare resource accesses.
            builder.UseTexture(hObjectBuffer,      AccessFlags.ReadWrite);
            builder.UseTexture(hObjectBufferDepth, AccessFlags.ReadWrite);
            builder.UseTexture(hOutlineBuffer,     AccessFlags.ReadWrite);
            builder.UseRendererList(rendererList);

            builder.AllowGlobalStateModification(true);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                // Set render target info global (replaces Prepare() from legacy path).
                cmd.SetGlobalVector(ProPixelizerRenderTargetInfo, data.RenderTargetInfo);

                if (data.DepthTestOutlines)
                {
                    data.Materials.OutlineDetection.EnableKeyword("DEPTH_TEST_OUTLINES_ON");
                    data.Materials.OutlineDetection.SetFloat(OutlineDepthTestThreshold, data.DepthTestThreshold);
                }
                else
                {
                    data.Materials.OutlineDetection.DisableKeyword("DEPTH_TEST_OUTLINES_ON");
                }

                if (data.UseNormals)
                {
                    data.Materials.OutlineDetection.SetFloat(EdgeDetectionSensitivity, data.NormalSensitivity);
                    cmd.EnableShaderKeyword("NORMAL_EDGE_DETECTION_ON");
                }
                else
                {
                    cmd.DisableShaderKeyword("NORMAL_EDGE_DETECTION_ON");
                }

                // Camera matrices & global pixel scale.
                cmd.SetGlobalFloat(ProPixelizerPixelScale, data.PixelScale);
                cmd.SetViewMatrix(data.ViewMatrix);
                cmd.SetProjectionMatrix(data.ProjectionMatrix);

                // Clear and render ProPixelizer object metadata.
                cmd.SetRenderTarget(data.OutlineObjectBuffer, data.OutlineObjectBuffer_Depth);
                cmd.ClearRenderTarget(true, true, Color.white);

                // Draw all objects tagged with the ProPixelizer shader tag.
                cmd.DrawRendererList(data.RendererList);

                // Detect outlines by blitting through the outline detection shader.
                cmd.SetGlobalTexture(MainTex,     data.OutlineObjectBuffer);
                cmd.SetGlobalTexture(MainTexDepth, data.OutlineObjectBuffer_Depth);
                cmd.SetGlobalVector(Size, data.TexelSize);
                Blitter.BlitCameraTexture(cmd, data.OutlineObjectBuffer, data.OutlineBuffer,
                    data.Materials.OutlineDetection, 0);

                // Expose the outline buffer as a global shader texture.
                cmd.SetGlobalTexture(ProPixelizerOutlines, data.OutlineBuffer);
            });
        }

        // -----------------------------------------------------------------------
        //  Cleanup
        // -----------------------------------------------------------------------
        public void Dispose()
        {
            _OutlineObjectBuffer?.Release();
            _OutlineObjectBuffer_Depth?.Release();
            _OutlineBuffer?.Release();
        }
    }
}
