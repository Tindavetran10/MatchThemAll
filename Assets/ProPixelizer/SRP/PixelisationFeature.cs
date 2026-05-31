// Copyright Elliot Bentine, 2018-
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace ProPixelizer
{
    public class PixelisationFeature : ScriptableRendererFeature
    {
        [FormerlySerializedAs("DepthTestOutlines")]
        [Tooltip("Perform depth testing for outlines where object IDs differ. This prevents outlines appearing when one object intersects another, but requires an extra depth sample.")]
        public bool UseDepthTestingForIDOutlines = true;

        [Tooltip("The threshold value used when depth comparing outlines.")]
        public float DepthTestThreshold = 0.001f;

        [Tooltip("Use normals for edge detection. This will analyse pixelated screen normals to determine where edges occur within an objects silhouette.")]
        public bool UseNormalsForEdgeDetection = true;

        public float NormalEdgeDetectionSensitivity = 1f;

        [Tooltip("Generates warnings if the pipeline state is incompatible with ProPixelizer.")]
        public bool GenerateWarnings = true;

        [HideInInspector, SerializeField]
        PixelizationPass.ShaderResources PixelizationShaders;
        [HideInInspector, SerializeField]
        OutlineDetectionPassRG.ShaderResources OutlineShaders;

        PixelizationPass _PixelisationPass;
        OutlineDetectionPassRG _OutlinePass;

        public override void Create()
        {
            PixelizationShaders = new PixelizationPass.ShaderResources().Load();
            OutlineShaders = new OutlineDetectionPassRG.ShaderResources().Load();
            _OutlinePass = new OutlineDetectionPassRG(OutlineShaders)
            {
                DepthTestOutlines = UseDepthTestingForIDOutlines,
                DepthTestThreshold = DepthTestThreshold,
                UseNormalsForEdgeDetection = UseNormalsForEdgeDetection,
                NormalEdgeDetectionSensitivity = NormalEdgeDetectionSensitivity
            };
            _PixelisationPass = new PixelizationPass(PixelizationShaders, _OutlinePass);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            _PixelisationPass.ConfigureInput(ScriptableRenderPassInput.Color);
            renderer.EnqueuePass(_PixelisationPass);
            renderer.EnqueuePass(_OutlinePass);

            if (GenerateWarnings)
                ProPixelizerVerification.GenerateWarnings();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _OutlinePass.Dispose();
                _PixelisationPass.Dispose();
            }
        }
    }
}