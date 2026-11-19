using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MatchThemAll.Scripts.Pixelate
{
    /// <summary>URP renderer feature that adds <see cref="PixelizePass"/> to game cameras (never the Scene view).</summary>
    public class PixelizeFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class CustomPassSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            [Range(1, 32)]
            [Tooltip("Size of each pixel block in screen pixels. Higher = chunkier pixelation.")]
            public int pixelSize = 4;
        }

        [SerializeField] private CustomPassSettings settings = new();

        /// <summary>Global on/off switch, e.g. flipped by PixelizeController from the game state.</summary>
        public static bool IsActive { get; set; } = true;

        private PixelizePass _pass;

        public override void Create() => _pass = new PixelizePass(settings);

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.SceneView) return;
            renderer.EnqueuePass(_pass);
        }
    }
}
