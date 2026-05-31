// Copyright Elliot Bentine, 2018-
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProPixelizer
{
    /// <summary>
    /// Render-Graph compatible implementation of the outline detection pass for URP 17+.
    /// Uses the ScriptableRenderPass compatibility (legacy Execute) path, which is fully
    /// supported in URP 17 via the Compatibility Mode API.
    /// The RenderGraph branch from the original file has been removed because
    /// cameraData.renderGraph no longer exists in URP 17; the proper RG integration
    /// requires RecordRenderGraph() which is a separate, larger refactor.
    /// </summary>
    public class OutlineDetectionPassRG : ProPixelizerPass
    {
        // ----- Constants -----
        private const string OutlineDetectionShaderName = "Hidden/ProPixelizer/SRP/OutlineDetection";
        public const string PROPIXELIZER_OBJECT_BUFFER = "ProPixelizerMetadata";
        public const string OUTLINE_BUFFER = "_ProPixelizerOutlines";
        public const string PROFILER_TAG = "ProPixelizerOutlines";
        private const string PROPIXELIZER_SHADER_TAG = "ProPixelizer";

        // ----- Shader property IDs -----
        private static readonly int OutlineDepthTestThreshold = Shader.PropertyToID("_OutlineDepthTestThreshold");
        private static readonly int EdgeDetectionSensitivity = Shader.PropertyToID("_NormalEdgeDetectionSensitivity");
        private static readonly int ProPixelizerPixelScale = Shader.PropertyToID("_ProPixelizer_Pixel_Scale");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexDepth = Shader.PropertyToID("_MainTex_Depth");
        private static readonly int Size = Shader.PropertyToID("_TexelSize");
        private static readonly int ProPixelizerOutlines = Shader.PropertyToID(OUTLINE_BUFFER);
        private static readonly ShaderTagId ProPixelizerShaderTagID = new ShaderTagId(PROPIXELIZER_SHADER_TAG);

        // ----- Public configuration -----
        public bool DepthTestOutlines;
        public float DepthTestThreshold;
        public bool UseNormalsForEdgeDetection = true;
        public float NormalEdgeDetectionSensitivity = 1f;

        // ----- RTHandles (public so PixelizationPass can read them) -----
        public RTHandle _OutlineObjectBuffer;
        public RTHandle _OutlineObjectBuffer_Depth;
        public RTHandle _OutlineBuffer;

        // ----- Private -----
        private MaterialLibrary _materials;
        private Vector4 _texelSize;

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

            public MaterialLibrary(ShaderResources resources)
            {
                _resources = resources;
            }
        }

        // -----------------------------------------------------------------------
        //  Constructor
        // -----------------------------------------------------------------------
        public OutlineDetectionPassRG(ShaderResources resources)
        {
            _materials = new MaterialLibrary(resources);
            // This pass must run before opaque objects are rendered.
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        // -----------------------------------------------------------------------
        //  Configure — allocate RTHandles
        //  Note: ReAllocateHandleIfNeeded in URP 17 takes 'ref RenderTextureDescriptor'
        // -----------------------------------------------------------------------
#pragma warning disable 618, 672
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var outlineDescriptor = cameraTextureDescriptor;
            outlineDescriptor.useMipMap = false;
            outlineDescriptor.colorFormat = RenderTextureFormat.ARGB32;
            outlineDescriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;

            var depthDescriptor = outlineDescriptor;
            depthDescriptor.colorFormat = RenderTextureFormat.Depth;
            outlineDescriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateHandleIfNeeded(ref _OutlineObjectBuffer, outlineDescriptor, name: PROPIXELIZER_OBJECT_BUFFER);
            RenderingUtils.ReAllocateHandleIfNeeded(ref _OutlineObjectBuffer_Depth, depthDescriptor, name: PROPIXELIZER_OBJECT_BUFFER);
            RenderingUtils.ReAllocateHandleIfNeeded(ref _OutlineBuffer, outlineDescriptor, name: OUTLINE_BUFFER);

            _texelSize = new Vector4(
                1f / cameraTextureDescriptor.width,
                1f / cameraTextureDescriptor.height,
                cameraTextureDescriptor.width,
                cameraTextureDescriptor.height);
        }
#pragma warning restore 618, 672

        // -----------------------------------------------------------------------
        //  OnCameraSetup
        // -----------------------------------------------------------------------
#pragma warning disable 618, 672
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Prepare(cmd, ref renderingData);
        }
#pragma warning restore 618, 672

        // -----------------------------------------------------------------------
        //  Execute
        // -----------------------------------------------------------------------
#pragma warning disable 618, 672
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // --- Configure outline detection material keywords ---
            if (DepthTestOutlines)
            {
                _materials.OutlineDetection.EnableKeyword("DEPTH_TEST_OUTLINES_ON");
                _materials.OutlineDetection.SetFloat(OutlineDepthTestThreshold, DepthTestThreshold);
            }
            else
            {
                _materials.OutlineDetection.DisableKeyword("DEPTH_TEST_OUTLINES_ON");
            }

            if (UseNormalsForEdgeDetection)
                _materials.OutlineDetection.SetFloat(EdgeDetectionSensitivity, NormalEdgeDetectionSensitivity);

            CommandBuffer buffer = CommandBufferPool.Get(PROFILER_TAG);
            buffer.name = "ProPixelizer Outline Pass";
            Prepare(buffer, ref renderingData);

            // Preview cameras use a reduced pixel scale to suppress dither expansion artifacts.
            buffer.SetGlobalFloat(ProPixelizerPixelScale,
                renderingData.cameraData.camera.cameraType == CameraType.Preview ? 0.01f : 1f);

            if (UseNormalsForEdgeDetection)
                buffer.EnableShaderKeyword("NORMAL_EDGE_DETECTION_ON");
            else
                buffer.DisableShaderKeyword("NORMAL_EDGE_DETECTION_ON");

            // Set up camera matrices for rendering.
#if CAMERADATA_MATRICES
            buffer.SetViewMatrix(renderingData.cameraData.GetViewMatrix());
            buffer.SetProjectionMatrix(renderingData.cameraData.GetProjectionMatrix());
#else
            buffer.SetViewMatrix(renderingData.cameraData.camera.worldToCameraMatrix);
            buffer.SetProjectionMatrix(renderingData.cameraData.camera.projectionMatrix);
#endif

            // Clear and render object metadata into the outline buffer.
            buffer.SetRenderTarget(_OutlineObjectBuffer, _OutlineObjectBuffer_Depth);
            buffer.ClearRenderTarget(true, true, Color.white);
            context.ExecuteCommandBuffer(buffer);
            CommandBufferPool.Release(buffer);

            // Draw all objects that use the ProPixelizer shader tag.
            var sort = new SortingSettings(renderingData.cameraData.camera);
            var drawingSettings = new DrawingSettings(ProPixelizerShaderTagID, sort);
            var filteringSettings = new FilteringSettings(RenderQueueRange.all);

#if UNITY_2023_2_OR_NEWER
            buffer = CommandBufferPool.Get(PROFILER_TAG);
            buffer.name = "ProPixelizer Outline Pass (Render Objects)";
            var renderListParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
            var renderList = context.CreateRendererList(ref renderListParams);
            buffer.DrawRendererList(renderList);
            context.ExecuteCommandBuffer(buffer);
            CommandBufferPool.Release(buffer);
#else
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
#endif

            // Detect outlines by blitting through the outline detection shader.
            buffer = CommandBufferPool.Get(PROFILER_TAG);
            buffer.name = "ProPixelizer Outline Detection";
            buffer.SetGlobalTexture(MainTex, _OutlineObjectBuffer);
            buffer.SetGlobalTexture(MainTexDepth, _OutlineObjectBuffer_Depth);
            buffer.SetGlobalVector(Size, _texelSize);

#if BLIT_API
            Blitter.BlitCameraTexture(buffer, _OutlineObjectBuffer, _OutlineBuffer, _materials.OutlineDetection, 0);
#else
            Blit(buffer, _OutlineObjectBuffer, _OutlineBuffer, _materials.OutlineDetection);
#endif

            buffer.SetGlobalTexture(ProPixelizerOutlines, _OutlineBuffer);
            context.ExecuteCommandBuffer(buffer);
            CommandBufferPool.Release(buffer);
        }
#pragma warning restore 618, 672

        // -----------------------------------------------------------------------
        //  Cleanup
        // -----------------------------------------------------------------------
        public override void FrameCleanup(CommandBuffer cmd)
        {
            // RTHandles are managed manually; nothing to release per-frame.
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // Externally managed RTHandle references — do not nullify here.
        }

        public void Dispose()
        {
            _OutlineObjectBuffer?.Release();
            _OutlineObjectBuffer_Depth?.Release();
            _OutlineBuffer?.Release();
        }
    }
}
