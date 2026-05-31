// Copyright Elliot Bentine, 2018-
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProPixelizer
{
    /// <summary>
    /// Performs the outline rendering and detection pass.
    /// </summary>
    public class OutlineDetectionPass : ProPixelizerPass
    {
        public OutlineDetectionPass(ShaderResources resources)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            Materials = new MaterialLibrary(resources);
        }

        private MaterialLibrary Materials;

        /// <summary>
        /// Shader resources used by the OutlineDetectionPass.
        /// </summary>
        [Serializable]
        public sealed class ShaderResources
        {
            public Shader OutlineDetection;

            public ShaderResources Load()
            {
                OutlineDetection = Shader.Find(OutlineDetectionShader);
                return this;
            }
        }

        /// <summary>
        /// Materials used by the OutlineDetectionPass.
        /// </summary>
        private sealed class MaterialLibrary
        {
            private ShaderResources Resources;
            public Material OutlineDetection
            {
                get
                {
                    if (_OutlineDetection == null)
                        _OutlineDetection = new Material(Resources.OutlineDetection);
                    return _OutlineDetection;
                }
            }
            private Material _OutlineDetection;

            public MaterialLibrary(ShaderResources resources)
            {
                Resources = resources;
            }
        }


        public bool DepthTestOutlines;
        public float DepthTestThreshold;
        public bool UseNormalsForEdgeDetection = true;
        public float NormalEdgeDetectionSensitivity = 1f;

public RTHandle _OutlineObjectBuffer;
public RTHandle _OutlineObjectBuffer_Depth;
public RTHandle _OutlineBuffer;

        static ShaderTagId ProPixelizerShaderTagID = new ShaderTagId(PROPIXELIZER_SHADER_TAG);
        private static readonly int OutlineDepthTestThreshold = Shader.PropertyToID("_OutlineDepthTestThreshold");
        private static readonly int EdgeDetectionSensitivity = Shader.PropertyToID("_NormalEdgeDetectionSensitivity");
        private static readonly int ProPixelizerPixelScale = Shader.PropertyToID("_ProPixelizer_Pixel_Scale");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexDepth = Shader.PropertyToID("_MainTex_Depth");
        private static readonly int Size = Shader.PropertyToID("_TexelSize");
        private static readonly int ProPixelizerOutlines = Shader.PropertyToID(OUTLINE_BUFFER);

        private const string OutlineDetectionShader = "Hidden/ProPixelizer/SRP/OutlineDetection";

        private Vector4 TexelSize;
        public const string PROPIXELIZER_OBJECT_BUFFER = "ProPixelizerMetadata";
        public const string OUTLINE_BUFFER = "_ProPixelizerOutlines";
        private const string PROPIXELIZER_SHADER_TAG = "ProPixelizer";

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

            TexelSize = new Vector4(
                1f / cameraTextureDescriptor.width,
                1f / cameraTextureDescriptor.height,
                cameraTextureDescriptor.width,
                cameraTextureDescriptor.height
            );

// NOTE: ConfigureTarget is obsolete in newer URP versions. The preferred approach is to use the Render Graph API.
// For compatibility, we rely on the automatic handling of RTHandles without manually calling ConfigureTarget.
        }
#pragma warning restore 618, 672

        public override void FrameCleanup(CommandBuffer cmd)
        {
            // RTHandles are managed by the renderer and will be automatically released
        }

public void Dispose()
        {
            _OutlineObjectBuffer?.Release();
            _OutlineObjectBuffer_Depth?.Release();
            _OutlineBuffer?.Release();
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // set externally managed RTHandle references to null.
        }

        public const string PROFILER_TAG = "ProPixelizerOutlines";

        #pragma warning disable 618, 672
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Prepare(cmd, ref renderingData);
        }
#pragma warning restore 618, 672

        #pragma warning disable 618, 672
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (DepthTestOutlines)
            {
                Materials.OutlineDetection.EnableKeyword("DEPTH_TEST_OUTLINES_ON");
                Materials.OutlineDetection.SetFloat(OutlineDepthTestThreshold, DepthTestThreshold);
            }
            else
                Materials.OutlineDetection.DisableKeyword("DEPTH_TEST_OUTLINES_ON");

            if (UseNormalsForEdgeDetection)
            {
                Materials.OutlineDetection.SetFloat(EdgeDetectionSensitivity, NormalEdgeDetectionSensitivity);
            }

            CommandBuffer buffer = CommandBufferPool.Get(PROFILER_TAG);
            buffer.name = "ProPixelizer Outline Pass";
            Prepare(buffer, ref renderingData);

            // Preview cameras must unfortunately disable dither expansion
            buffer.SetGlobalFloat(ProPixelizerPixelScale,
                renderingData.cameraData.camera.cameraType == CameraType.Preview ? 0.01f : 1f);

            if (UseNormalsForEdgeDetection)
                buffer.EnableShaderKeyword("NORMAL_EDGE_DETECTION_ON");
            else
                buffer.DisableShaderKeyword("NORMAL_EDGE_DETECTION_ON");


            // Set up matrices for rendering outlines.
#if CAMERADATA_MATRICES
            buffer.SetViewMatrix(renderingData.cameraData.GetViewMatrix());
            buffer.SetProjectionMatrix(renderingData.cameraData.GetProjectionMatrix());
#else
            buffer.SetViewMatrix(renderingData.cameraData.camera.worldToCameraMatrix);
            buffer.SetProjectionMatrix(renderingData.cameraData.camera.projectionMatrix);
#endif

            // Render outlines into a render target.
            buffer.SetRenderTarget(_OutlineObjectBuffer, _OutlineObjectBuffer_Depth);
            buffer.ClearRenderTarget(true, true, Color.white);
            context.ExecuteCommandBuffer(buffer);
            CommandBufferPool.Release(buffer);

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

            // Perform outline detection
            buffer = CommandBufferPool.Get(PROFILER_TAG);
            buffer.name = "ProPixelizer Outline Detection";
            buffer.SetGlobalTexture(MainTex, _OutlineObjectBuffer);
#if URP_13
            buffer.SetGlobalTexture(MainTexDepth, _OutlineObjectBuffer_Depth);//, RenderTextureSubElement.Depth);
#else
            buffer.SetGlobalTexture("_MainTex_Depth", _OutlineObjectBuffer_Depth, RenderTextureSubElement.Depth);
#endif
            buffer.SetGlobalVector(Size, TexelSize);

#if BLIT_API
            Blitter.BlitCameraTexture(buffer, _OutlineObjectBuffer, _OutlineBuffer, Materials.OutlineDetection, 0);
#else
            Blit(buffer, _OutlineObjectBuffer, _OutlineBuffer, Materials.OutlineDetection);
#endif

            buffer.SetGlobalTexture(ProPixelizerOutlines, _OutlineBuffer);

            context.ExecuteCommandBuffer(buffer);
            CommandBufferPool.Release(buffer);
        }
#pragma warning restore 618, 672
    }
}