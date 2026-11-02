using System;
using UnityEngine;
using PrimeTween;
using MatchThemAll.Scripts;
using Random = UnityEngine.Random;

namespace MatchThemAll.Scripts.Power_Ups
{
    /// <summary>
    /// Spring pad: a random item hops out of its slot onto the pad, the pad squashes and springs back,
    /// and the item is launched in a visible arc (with a trail) onto a free spot of the board, where it
    /// lands with a squash, a dust puff and a thud, then drops back into the physics pile.
    /// </summary>
    [Serializable]
    public class SpringEffect : PowerupEffect
    {
        [Header("Launch")]
        [Tooltip("Trail attached to the item while it flies.")]
        public TrailRenderer trailPrefab;
        public float hopTime = .2f;
        public float flightTime = .55f;
        [Tooltip("How far the arc bulges up the screen, in world units.")]
        public float arcHeight = 1.4f;
        [Tooltip("Landing area on the board, in viewport coordinates.")]
        public Vector2 landViewportMin = new(.25f, .42f);
        public Vector2 landViewportMax = new(.75f, .7f);
        [Tooltip("World height while on the pad (above the item spots so it stays visible) and where it lands (above the pile).")]
        public float padHeight = 11f;
        public float landHeight = 3f;
        public Vector2 springSpinSpeedRange = new(5f, 12f);

        [NonSerialized] private TrailRenderer _trail;

        public override bool CanActivate(PowerupContext ctx) =>
            ctx.ItemSpots != null && ctx.ItemSpots.GetRandomOccupiedSpot();

        public override void Activate(PowerupContext ctx)
        {
            ItemSpot spot = ctx.ItemSpots.GetRandomOccupiedSpot();
            var cam = Camera.main;
            if (!spot || !cam) return;

            ctx.SetBusy(true);

            Item item = spot.Item;
            spot.Clear();
            item.UnassignSpot();
            item.transform.parent = LevelManager.Instance.ItemParent;
            item.DisablePhysics(); // kinematic + no colliders while it flies (can't be tapped mid-air)

            var pad = ctx.Source;
            Vector3 onPad = pad ? PadTop(pad, cam) : item.transform.position;
            onPad.y = padHeight;
            Vector3 land = cam.ViewportToWorldPoint(new Vector3(Random.Range(landViewportMin.x, landViewportMax.x),
                Random.Range(landViewportMin.y, landViewportMax.y), cam.transform.position.y - landHeight));

            // 1. Hop onto the pad.
            Tween.Position(item.transform, onPad, hopTime, Ease.OutQuad);
            Tween.Scale(item.transform, Vector3.one, hopTime);
            // 2. The pad squashes under it, then springs back.
            if (pad)
            {
                var s = pad.localScale;
                Tween.Scale(pad, new Vector3(s.x * 1.15f, s.y * .55f, s.z * 1.15f), .08f, Ease.OutQuad, startDelay: hopTime)
                    .OnComplete(pad, p => Tween.Scale(p, s, .4f, Ease.OutElastic));
            }
            // 3. Launch as the pad releases.
            Tween.Delay(hopTime + .08f, () => Launch(ctx, item, onPad, land, cam.transform.up));
        }

        private void Launch(PowerupContext ctx, Item item, Vector3 from, Vector3 land, Vector3 screenUp)
        {
            if (!item) { ctx.SetBusy(false); return; }

            if (ctx.ActivateVfx != null && VfxPool.Instance != null)
                VfxPool.Instance.Play(ctx.ActivateVfx, from);

            if (!_trail && trailPrefab) _trail = UnityEngine.Object.Instantiate(trailPrefab);
            if (_trail)
            {
                _trail.transform.SetParent(item.transform, false);
                _trail.transform.localPosition = Vector3.zero;
                _trail.Clear();
                _trail.emitting = true;
            }

            var startRot = item.transform.rotation;
            var spinAxis = Random.onUnitSphere;
            float spin = Random.Range(springSpinSpeedRange.x, springSpinSpeedRange.y) * flightTime * Mathf.Rad2Deg;
            Tween.Custom(item.transform, 0f, 1f, flightTime, (t, p) =>
            {
                float bulge = Mathf.Sin(p * Mathf.PI);
                t.position = Vector3.Lerp(from, land, p) + screenUp * (bulge * arcHeight);
                t.localScale = Vector3.one * (1f + .35f * bulge); // reads as height under the top-down camera
                t.rotation = Quaternion.AngleAxis(spin * p, spinAxis) * startRot;
            }).OnComplete(() => Land(ctx, item, land));
        }

        private void Land(PowerupContext ctx, Item item, Vector3 at)
        {
            if (_trail)
            {
                _trail.emitting = false;
                _trail.transform.SetParent(null, true); // keep the fading tail; never destroyed with the item
            }
            if (!item) { ctx.SetBusy(false); return; }

            item.transform.localScale = Vector3.one;
            Tween.PunchScale(item.transform, new Vector3(.3f, -.3f, .3f), .3f);
            if (ctx.EndVfx != null && VfxPool.Instance != null)
                VfxPool.Instance.Play(ctx.EndVfx, at);
            SoundManager.Sfx(SoundEvent.SpringLand);

            item.EnablePhysics();
            var rb = item.GetComponent<Rigidbody>();
            if (rb) rb.linearVelocity = new Vector3(Random.Range(-1f, 1f), 1.5f, Random.Range(-1f, 1f));
            ctx.OnItemBackToGame?.Invoke(item);
            ctx.SetBusy(false);
        }

        // Top of the pad as seen on screen (the camera looks straight down, so "up the screen" is cam.up).
        private static Vector3 PadTop(Transform pad, Camera cam)
        {
            var r = pad.GetComponentInChildren<Renderer>();
            if (!r) return pad.position;
            var up = cam.transform.up;
            var e = r.bounds.extents;
            return r.bounds.center + up * (Mathf.Abs(up.x) * e.x + Mathf.Abs(up.y) * e.y + Mathf.Abs(up.z) * e.z);
        }
    }
}
