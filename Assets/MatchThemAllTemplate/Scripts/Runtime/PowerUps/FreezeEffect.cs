using System;
using PrimeTween;
using UnityEngine;
using MatchThemAll.Scripts;

namespace MatchThemAll.Scripts.Power_Ups
{
    /// <summary>
    /// Freeze gun: the gun lifts, fires a laser into the board, an ice burst plays where it hits, then the
    /// level timer freezes (TimerManager). The lasting frozen look (frost, ice spheres, bar under the clock)
    /// is FreezeFx, driven by TimerFrozenEvent / TimerUnfrozenEvent.
    /// </summary>
    [Serializable]
    public class FreezeEffect : PowerupEffect
    {
        [Tooltip("Laser beam (LineRenderer prefab) fired from the gun into the board.")]
        public LineRenderer laserPrefab;
        [Tooltip("Viewport point the laser hits (the board centre).")]
        public Vector2 laserTarget = new(.5f, .55f);
        [Tooltip("World height of the plane the laser hits (above the item pile).")]
        public float boardHeight = 4f;
        [Tooltip("While shooting, the gun lifts by this much (tray-local Y) and comes this much toward the camera (tray-local -Z) so the item spots and the board never cover it.")]
        public Vector2 shootLift = new(.45f, 2f);

        [NonSerialized] private LineRenderer _laser;

        public override bool CanActivate(PowerupContext ctx) =>
            ctx.Timer != null && !ctx.Timer.IsFrozen;

        public override void Activate(PowerupContext ctx)
        {
            var gun = ctx.Source;
            if (!gun || !Camera.main)
            {
                Hit(ctx, ctx.FreezeMuzzle ? ctx.FreezeMuzzle.position : Vector3.zero);
                return;
            }

            ctx.SetBusy(true);
            var cam = Camera.main;
            var hit = cam.ViewportToWorldPoint(new Vector3(laserTarget.x, laserTarget.y, cam.transform.position.y - boardHeight));
            var basePos = gun.localPosition;
            var baseRot = gun.localRotation;
            var liftPos = basePos + new Vector3(0f, shootLift.x, -shootLift.y);
            // 1. Lift the gun up and in front of everything while turning the barrel onto the target, 2. fire, 3. settle back.
            Tween.LocalPosition(gun, liftPos, .2f, Ease.OutBack);
            Tween.LocalRotation(gun, AimRotation(gun, liftPos, cam, hit), .2f, Ease.OutQuad);
            Tween.Delay(.22f, () => Fire(ctx, gun, basePos, baseRot, hit));
        }

        // Local rotation that, from the lifted position, points the barrel (the gun's +Z) from the Muzzle at the target,
        // as seen by the camera (rotation about the view axis only, so the gun keeps its side-on look).
        private static Quaternion AimRotation(Transform gun, Vector3 liftedLocalPos, Camera cam, Vector3 target)
        {
            Vector3 savedPos = gun.localPosition;
            Quaternion savedRot = gun.localRotation;
            gun.localPosition = liftedLocalPos;
            var muzzle = gun.Find("Muzzle");
            var axis = cam.transform.forward;
            for (int i = 0; i < 2; i++) // second pass corrects for the muzzle moving as the gun turns
            {
                var from = muzzle ? muzzle.position : gun.position;
                float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(gun.forward, axis), Vector3.ProjectOnPlane(target - from, axis), axis);
                gun.rotation = Quaternion.AngleAxis(angle, axis) * gun.rotation;
            }
            var aimed = gun.localRotation;
            gun.localPosition = savedPos;
            gun.localRotation = savedRot;
            return aimed;
        }

        private void Fire(PowerupContext ctx, Transform gun, Vector3 basePos, Quaternion baseRot, Vector3 hit)
        {
            var cam = Camera.main;
            if (!gun || !cam) { ctx.SetBusy(false); return; }
            // The laser leaves the barrel tip ("Muzzle" child of the gun); fall back to the top of the gun's bounds.
            var muzzle = gun.Find("Muzzle");
            var r = gun.GetComponentInChildren<Renderer>();
            var start = muzzle ? muzzle.position
                : r ? r.bounds.center + cam.transform.up * r.bounds.extents.magnitude * .4f : gun.position;

            if (!_laser && laserPrefab) _laser = UnityEngine.Object.Instantiate(laserPrefab);
            if (_laser)
            {
                _laser.SetPosition(0, start);
                _laser.SetPosition(1, hit);
                _laser.enabled = true;
                Tween.Custom(_laser, 1f, 0f, .35f, (l, v) => l.widthMultiplier = .08f + .5f * v, Ease.InQuad)
                    .OnComplete(_laser, l => l.enabled = false);
            }

            // Hold the aim while the beam fades, then settle back into the slot.
            Tween.LocalRotation(gun, baseRot, .35f, Ease.OutQuad, startDelay: .3f);
            Tween.LocalPosition(gun, basePos, .35f, Ease.OutQuad, startDelay: .3f)
                .OnComplete(() => ctx.SetBusy(false));

            Hit(ctx, hit);
        }

        private static void Hit(PowerupContext ctx, Vector3 at)
        {
            if (ctx.ActivateVfx != null && VfxPool.Instance != null)
                VfxPool.Instance.Play(ctx.ActivateVfx, at);
            // The level may have ended during the wind-up; only freeze a running game.
            if (ctx.Timer != null && GameManager.Instance && GameManager.Instance.IsGame())
                ctx.Timer.FreezeTimer();
            if (!ctx.Source) ctx.SetBusy(false);
        }
    }
}
