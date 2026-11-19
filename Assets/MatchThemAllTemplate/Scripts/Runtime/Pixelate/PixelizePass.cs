using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace MatchThemAll.Scripts.Pixelate
{
    /// <summary>
    /// Pixel-art look in two point-filtered bits: camera color → a texture 1/pixelSize the size
    /// → back onto the camera color. Nearest-neighbor sampling both ways gives hard square blocks.
    /// </summary>
    public class PixelizePass : ScriptableRenderPass
    {
        private static readonly Vector4 FullScaleBias = new(1f, 1f, 0f, 0f);

        private readonly PixelizeFeature.CustomPassSettings _settings;

        private class BlitData { public TextureHandle From; }

        public PixelizePass(PixelizeFeature.CustomPassSettings settings)
        {
            _settings = settings;
            renderPassEvent = settings.renderPassEvent;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            int block = _settings.pixelSize;
            if (!PixelizeFeature.IsActive || block < 2) return; // a 1-pixel block is the unmodified image

            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer) return; // we need an intermediate colo r texture to read

            var desc = frameData.Get<UniversalCameraData>().cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.width = Mathf.Max(1, desc.width / block);
            desc.height = Mathf.Max(1, desc.height / block);

            var camera = resources.activeColorTexture;
            var lowRes = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_PixelizeLowRes", false);

            PointBlit(renderGraph, "Pixelize ↓", camera, lowRes);
            PointBlit(renderGraph, "Pixelize ↑", lowRes, camera);
        }

        private static void PointBlit(RenderGraph renderGraph, string name, TextureHandle from, TextureHandle to)
        {
            using var builder = renderGraph.AddRasterRenderPass<BlitData>(name, out var data);
            data.From = from;
            builder.UseTexture(from);
            builder.SetRenderAttachment(to, 0);
            builder.SetRenderFunc(static (BlitData d, RasterGraphContext ctx) =>
                Blitter.BlitTexture(ctx.cmd, d.From, FullScaleBias, 0, false)); // bilinear: false → nearest
        }
    }
}
