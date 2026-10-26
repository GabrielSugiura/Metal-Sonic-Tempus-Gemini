using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MetalSonic.EditorTools
{
    /// <summary>
    /// Headless verification of the Metal Sonic physics and GDD mechanics.
    ///
    /// Play-mode behaviour (Start/Update, Input, Time.deltaTime) does NOT run
    /// under -batchmode, so every check here drives the pure static state
    /// machines and the controller's public Tick() directly with explicit
    /// arguments and a stubbed tilemap. Run via -executeMethod.
    /// </summary>
    public static class MetalSonicTest
    {
        private static int _pass;
        private static int _fail;

        [MenuItem("Project/Metal Sonic/Run Tests")]
        public static void Run()
        {
            _pass = 0;
            _fail = 0;

            PhysicsConstants();
            GroundMovement();
            AirMovement();
            Jump();
            SlopeFactor();
            Spindash();
            Teleport();
            Tempus();
            RingsAndShield();
            DimensionSwap();
            AnimationTiming();
            SpriteBank();
            LevelStructure();

            Debug.Log(string.Format("MetalSonicTest RESULT pass={0} fail={1}", _pass, _fail));
        }

        private static void Check(bool ok, string label)
        {
            if (ok) { _pass++; Debug.Log("PASS  " + label); }
            else { _fail++; Debug.Log("FAIL  " + label); }
        }

        private static void Near(float a, float b, float tol, string label)
        {
            Check(Mathf.Abs(a - b) <= tol, string.Format("{0}  (got {1:0.######}, want {2:0.######})", label, a, b));
        }

        // ------------------------------------------------------------------
        private static void PhysicsConstants()
        {
            Check(SonicPhysics.Acceleration == 12f / 256f, "SPG acceleration is 12 subpx");
            Check(SonicPhysics.Deceleration == 128f / 256f, "SPG deceleration is 128 subpx");
            Check(SonicPhysics.TopSpeed == 6f, "SPG top speed is 6 px/frame");
            Check(SonicPhysics.JumpForce == 6f + 128f / 256f, "SPG jump force is 6.5 px/frame");
            Check(SonicPhysics.Gravity == 56f / 256f, "SPG gravity is 56 subpx");
            Check(SonicPhysics.SlopeFactor == 32f / 256f, "SPG slope factor is 32 subpx");
            Check(SonicPhysics.AnimationFramesPerSecond == 24, "animation runs at 24 fps");
        }

        // ------------------------------------------------------------------
        private static void GroundMovement()
        {
            // accelerate from standstill
            float g = 0f;
            g = SonicMovement.UpdateGroundSpeed(g, 1f, false);
            Near(g, 12f / 256f, 1e-6f, "first acceleration step");

            // reaches top speed and stops climbing
            float gTop = 0f;
            for (int i = 0; i < 200; i++) gTop = SonicMovement.UpdateGroundSpeed(gTop, 1f, false);
            Near(gTop, SonicPhysics.TopSpeed, 1e-4f, "top speed converges to 6 px/frame");

            // deceleration: reversing flips through the +/-0.5 quirk
            float rev = -6f;
            float after = SonicMovement.UpdateGroundSpeed(rev, 1f, false);
            Check(after > rev, "deceleration pushes ground speed back toward zero");

            // friction brings a coasting player to a stop
            float coast = 0.5f;
            for (int i = 0; i < 200; i++) coast = SonicMovement.UpdateGroundSpeed(coast, 0f, false);
            Near(coast, 0f, 1e-6f, "friction stops a coasting player");

            // rolling ignores acceleration entirely
            float rolling = SonicMovement.UpdateGroundSpeed(3f, 1f, true);
            Check(rolling < 3f, "rolling applies friction, not acceleration");
        }

        // ------------------------------------------------------------------
        private static void AirMovement()
        {
            float y = 0f;
            SonicMovement.UpdateAir(0f, ref y, 1f, false, out float nx);
            Near(y, SonicPhysics.Gravity, 1e-6f, "air gravity applied");
            Near(nx, SonicPhysics.AirAcceleration, 1e-6f, "air acceleration applied");

            // air speed is capped at top speed
            float y2 = 0f;
            float xa = 6f;
            for (int i = 0; i < 50; i++)
            {
                SonicMovement.UpdateAir(xa, ref y2, 1f, false, out xa);
            }
            Check(xa <= SonicPhysics.TopSpeed + 1e-4f, "air speed capped at top speed");
        }

        // ------------------------------------------------------------------
        private static void Jump()
        {
            float xs = 0f, ys = 0f;
            SonicMovement.ApplyJump(0f, ref xs, ref ys, SonicPhysics.JumpForce);
            Near(ys, -SonicPhysics.JumpForce, 1e-6f, "flat jump sets upward speed");

            // variable jump height: releasing the button caps upward speed at -4
            float y = -6f;
            SonicMovement.UpdateAir(0f, ref y, 0f, false, out float _);
            Check(y <= SonicPhysics.JumpReleaseCap, "jump release caps upward speed at -4");

            // jump along a slope preserves momentum direction
            float sx = 3f, sy = 0f;
            SonicMovement.ApplyJump(Mathf.Atan2(0.5f, 1f), ref sx, ref sy, SonicPhysics.JumpForce);
            Check(sx < 3f, "sloped jump redirects X speed");
        }

        // ------------------------------------------------------------------
        private static void SlopeFactor()
        {
            float flat = SonicMovement.SlopeFactorFor(0f, false);
            Near(flat, SonicPhysics.SlopeFactor, 1e-6f, "flat slope factor");

            float rolling = SonicMovement.SlopeFactorFor(0.4f, true);
            Check(rolling > 0f, "rolling slope factor is a real value");
        }

        // ------------------------------------------------------------------
        private static void Spindash()
        {
            Near(SonicMovement.SpindashReleaseSpeed(0), 8f, 1e-6f, "spindash release at rev 0 is 8");
            Near(SonicMovement.SpindashReleaseSpeed(8), 12f, 1e-6f, "spindash release at rev 8 is 12");
            Check(SonicMovement.SpindashReleaseSpeed(999) <= 12f, "spindash release capped at 12");
            // SPG: rev -= (rev div 8) / 256 -- integer math, so the drag only
            // becomes visible at high rev values.
            Check(SonicMovement.SpinRevDrag(10000) < 10000, "spindash rev decays under drag");
        }

        // ------------------------------------------------------------------
        private static void Teleport()
        {
            Check(!MetalSonicMechanics.CanTeleport(0.5f), "teleport blocked when bar is not full");
            Check(MetalSonicMechanics.CanTeleport(1f), "teleport allowed when bar is full");

            float spent = MetalSonicMechanics.SpendTeleport(1f);
            Near(spent, 0f, 1e-6f, "successful teleport empties the bar");

            float regen = MetalSonicMechanics.RegenTeleport(0f, 4f);
            Near(regen, 1f, 0.01f, "bar refills after 4 seconds");

            float charged = MetalSonicMechanics.AddRingEnergy(0f, 10);
            Check(charged >= 1f, "10 rings fully charge the teleport bar");
        }

        // ------------------------------------------------------------------
        private static void Tempus()
        {
            Near(MetalSonicMechanics.DrainTempus(99f, 1f), 98f, 1e-6f, "tempus drains 1 per second");
            Near(MetalSonicMechanics.ChargeTempusSwap(99f), 94f, 1e-6f, "dimension swap costs 5 tempus");
            Near(MetalSonicMechanics.ApplyTempusCapsule(50f), 60f, 1e-6f, "capsule restores 10 tempus");
            Check(MetalSonicMechanics.ApplyTempusCapsule(95f) <= MetalSonicMechanics.TempusMax,
                "capsule cannot exceed the tempus cap");
        }

        // ------------------------------------------------------------------
        private static void RingsAndShield()
        {
            // shield absorbs one hit and preserves rings (GDD 7.5)
            int after;
            bool absorbed = MetalSonicMechanics.ResolveHit(12, true, out after);
            Check(absorbed, "shield absorbs the hit");
            Near(after, 12f, 1e-6f, "shield preserves the rings");

            bool absorbed2 = MetalSonicMechanics.ResolveHit(12, false, out after);
            Check(!absorbed2, "no shield means the hit lands");
            Near(after, 0f, 1e-6f, "rings scatter to zero on a clean hit");

            // shield does not stack
            Check(MetalSonicMechanics.CollectShield(true), "collecting a shield while shielded does not stack");
        }

        // ------------------------------------------------------------------
        private static void DimensionSwap()
        {
            float tempusAfter;
            var ok = MetalSonicMechanics.TrySwapDimension(false, 99f, out tempusAfter);
            Check(ok == MetalSonicMechanics.SwapResult.Success, "swap succeeds with free destination");
            Near(tempusAfter, 94f, 1e-6f, "successful swap charges the cost");

            var blocked = MetalSonicMechanics.TrySwapDimension(true, 99f, out tempusAfter);
            Check(blocked == MetalSonicMechanics.SwapResult.BlockedByWall, "swap blocked by a wall");
            Near(tempusAfter, 99f, 1e-6f, "blocked swap deducts nothing");

            var broke = MetalSonicMechanics.TrySwapDimension(false, 2f, out tempusAfter);
            Check(broke == MetalSonicMechanics.SwapResult.NotEnoughTempus, "swap refused without tempus");
            Near(tempusAfter, 2f, 1e-6f, "refused swap deducts nothing");
        }

        // ------------------------------------------------------------------
        private static void AnimationTiming()
        {
            // SPG: a subimage lasts duration+1 frames because the counter
            // resets on the frame it reaches -1.
            Check(SonicPhysics.IdleFrameDuration == 24, "idle subimage duration is 24 frames");
            Check(SonicPhysics.WalkFrameDuration == 6, "walk subimage duration is 6 frames");
            Check(SonicPhysics.SpindashFrameDuration == 1, "spindash advances every frame");
            Check(SonicPhysics.RollFrameDuration == 4, "roll subimage duration is 4 frames");

            // sheet row map
            Check((int)MetalSonicAnim.Idle == 0 && (int)MetalSonicAnim.Hurt == 7,
                "animation rows map to the sheet order");
        }

        // ------------------------------------------------------------------
        private static void SpriteBank()
        {
            // every row must resolve a real sprite for every frame it advertises
            foreach (MetalSonicAnim anim in System.Enum.GetValues(typeof(MetalSonicAnim)))
            {
                string key = anim.ToString().ToLowerInvariant();
                int expected = anim == MetalSonicAnim.Roll ? 6
                    : anim == MetalSonicAnim.Ball ? 7 : 8;
                bool allFound = true;
                for (int i = 0; i < expected; i++)
                {
                    if (MetalSonicSpriteBank.Get(key, i) == null) { allFound = false; break; }
                }
                Check(allFound, string.Format("sprite bank resolves all {0} {1} frames", expected, key));
            }
        }

        // ------------------------------------------------------------------
        private static void LevelStructure()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/TestLevel.unity", OpenSceneMode.Additive);
            Check(scene.isLoaded, "test level loads");

            var cam = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Check(cam.Length > 0, "level contains a camera");

            var players = Object.FindObjectsByType<MetalSonicController>(FindObjectsSortMode.None);
            Check(players.Length == 1, "level contains exactly one Metal Sonic controller");
            if (players.Length == 1)
            {
                var c = players[0];
                Check(c.GetComponent<Rigidbody2D>() != null, "player has a Rigidbody2D");
                Check(c.GetComponent<MetalSonicAnimator>() != null, "player has an animator");
                Check(Mathf.Approximately(c.topSpeed, SonicPhysics.TopSpeed), "player top speed is the SPG value");
            }

            var bridge = Object.FindObjectsByType<HeightfieldBridge>(FindObjectsSortMode.None);
            Check(bridge.Length == 1, "level contains the world bridge");
            if (bridge.Length == 1)
            {
                Check(bridge[0].heights != null && bridge[0].heights.Length > 0, "world bridge has a heightfield");
            }

            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
