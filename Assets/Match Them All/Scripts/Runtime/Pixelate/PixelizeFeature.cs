using UnityEngine.Rendering.Universal;

namespace MatchThemAll.Scripts.Pixelate
{
    public class PixelizeFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class CustomPassSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            [UnityEngine.Range(1, 32)]
            [UnityEngine.Tooltip("Size of each pixel block in screen pixels. Higher = chunkier pixelation.")]
            public int pixelSize = 4;
        }

        [UnityEngine.SerializeField] private CustomPassSettings settings = new CustomPassSettings();
        private PixelizePass _customPass;

        /// <summary>Toggled at runtime by PixelizeController to match game state.</summary>
        public static bool IsActive { get; set; } = true;

        public override void Create()
        {
            _customPass = new PixelizePass(settings);
        }

        // URP 17: use the parameterless override — RenderingData is gone.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera) return;
#endif
            renderer.EnqueuePass(_customPass);
        }

        protected override void Dispose(bool disposing) => _customPass?.Dispose();
    }
}