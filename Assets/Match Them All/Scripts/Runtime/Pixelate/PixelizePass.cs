using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace MatchThemAll.Scripts.Pixelate
{
    public class PixelizePass : ScriptableRenderPass
    {
        private static readonly int PixelizeSize = Shader.PropertyToID("_PixelizeSize");
        private readonly PixelizeFeature.CustomPassSettings _settings;

        public PixelizePass(PixelizeFeature.CustomPassSettings settings)
        {
            _settings = settings;
            renderPassEvent = settings.renderPassEvent;
        }

        // ── Render Graph pass data ────────────────────────────────────────────────
        private class PassData { public TextureHandle Src; }

        // ── URP 17 entry point ────────────────────────────────────────────────────
        // Crispness strategy: downscale to (resolution ÷ pixelSize) with POINT
        // sampling → upscale back to full res with POINT sampling.
        // Hardware nearest-neighbor gives perfectly hard block edges —
        // no UV-snapping math needed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!PixelizeFeature.IsActive)
            {
                Shader.SetGlobalFloat(PixelizeSize, 1.0f);
                return;
            }

            var ps = Mathf.Max(2, _settings.pixelSize); // pixelSize=1 = no visible effect, skip
            Shader.SetGlobalFloat(PixelizeSize, ps);

            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) return;

            var cameraData = frameData.Get<UniversalCameraData>();
            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            // Small texture at exact pixel-block resolution
            var smallDesc = desc;
            smallDesc.width  = Mathf.Max(1, desc.width  / ps);
            smallDesc.height = Mathf.Max(1, desc.height / ps);

            var source   = resourceData.activeColorTexture;
            var smallTex = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, smallDesc, "_PixelizeSmall", false);

            // ── Pass 1: Downscale (point) → block resolution ─────────────────────
            // Point sampling lands at each block's center UV automatically,
            // because the fragment UVs tile perfectly into the source blocks.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Pixelize Downscale", out var passData))
            {
                passData.Src = source;
                builder.UseTexture(source);
                builder.SetRenderAttachment(smallTex, 0);
                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    Blitter.BlitTexture(ctx.cmd, data.Src,
                        new Vector4(1f, 1f, 0f, 0f), 0, false));   // bilinear=false → nearest
            }

            // ── Pass 2: Upscale (point) → screen resolution ──────────────────────
            // Nearest upscale magnifies each block pixel uniformly → hard square edges.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Pixelize Upscale", out var passData))
            {
                passData.Src = smallTex;
                builder.UseTexture(smallTex);
                builder.SetRenderAttachment(source, 0);
                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    Blitter.BlitTexture(ctx.cmd, data.Src,
                        new Vector4(1f, 1f, 0f, 0f), 0, false));   // bilinear=false → nearest
            }
        }

        // Nothing to dispose — no material created in this pass.
        public void Dispose() { }
    }
}
