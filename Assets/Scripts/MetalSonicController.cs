using UnityEngine;

namespace MetalSonic
{
    /// <summary>
    /// Metal Sonic movement, abilities and resources.
    ///
    /// Physics runs at a fixed 60 Hz through an accumulator so the per-frame
    /// constants stay exact regardless of render frame rate. Gameplay reads
    /// through <see cref="IMetalSonicInput"/> so tests can drive it directly.
    ///
    /// Terrain is a column heightfield queried by the floor/wall sensors, which
    /// is the SPG's A/B floor probe and C/D wall probe simplified for flat
    /// ground plus 45 degree ramps.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(MetalSonicAnimator))]
    public class MetalSonicController : MonoBehaviour
    {
        [Header("Tuning")]
        [SerializeField] public float topSpeed = SonicPhysics.TopSpeed;
        [SerializeField] public float jumpForce = SonicPhysics.JumpForce;

        [Header("Sensors (world units)")]
        [SerializeField] public float floorProbeLength = 0.35f;
        [SerializeField] public float wallProbeLength = 0.3f;
        [SerializeField] public float bodyHalfHeight = 0.42f;
        [SerializeField] public float ballHalfHeight = 0.3f;
        [SerializeField] public float bodyHalfWidth = 0.18f;

        public IMetalSonicInput Input { get; set; }

        // --- movement state --------------------------------------------------
        public bool Grounded { get; private set; }
        public bool Rolling { get; private set; }
        public bool Hurt { get; private set; }
        public bool Dashing { get; private set; }
        public bool Hovering { get; private set; }
        // plain fields, not properties: the SPG math passes them by ref
        public float GroundSpeed;
        public float XSpeed;
        public float YSpeed;
        public float GroundAngle;   // radians, 0 = flat
        public int Facing = 1;
        public int ControlLock { get; private set; }
        public float DashTimer { get; private set; }
        public float HoverTimer { get; private set; }

        // --- resources (GDD 7.3 - 7.5) ---------------------------------------
        public float Tempus { get; private set; } = MetalSonicMechanics.TempusStart;
        public float TeleportCharge { get; private set; }
        public int Rings { get; private set; }
        public bool HasShield { get; private set; }
        public int Dimension { get; private set; }        // 0 = Mundo A, 1 = Mundo B
        public bool LastSwapWasBlocked { get; private set; }

        /// <summary>Set by the level when a dimension swap hits a wall.</summary>
        public bool NextSwapDestinationBlocked { get; set; }

        private MetalSonicAnimator _anim;
        private Rigidbody2D _body;
        private BoxCollider2D _col;
        private float _accum;

        private void Awake()
        {
            _anim = GetComponent<MetalSonicAnimator>();
            _body = GetComponent<Rigidbody2D>();
            _col = GetComponent<BoxCollider2D>();
            _body.gravityScale = 0f;                      // gravity applied manually
            _body.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (Input == null) Input = new KeyboardMetalSonicInput();
        }

        private void Update()
        {
            float step = 1f / SonicPhysics.PhysicsHz;
            _accum += Time.deltaTime;
            int guard = 0;
            while (_accum >= step && guard < 8)
            {
                _accum -= step;
                Tick(step);
                guard++;
            }
        }

        /// <summary>One 60 Hz physics frame. Public so tests can drive it.</summary>
        public void Tick(float dt)
        {
            // Tempus drains continuously (GDD 7.4); pause simply stops calling Tick.
            Tempus = MetalSonicMechanics.DrainTempus(Tempus, dt);
            TeleportCharge = MetalSonicMechanics.RegenTeleport(TeleportCharge, dt);

            if (ControlLock > 0) ControlLock--;

            if (Input.DimensionPressed && !Hurt) TrySwapDimension();
            if (Input.TeleportPressed && !Hurt && !Dashing) TryTeleport();
            if (Input.DashPressed && !Hurt && Grounded && !Dashing) StartDash();

            float h = ControlLock > 0 ? 0f : Input.Horizontal;

            if (Hurt) TickHurt(dt);
            else if (Dashing) TickDash(dt);
            else if (!Grounded) TickAir(dt, h, Input.JumpHeld);
            else TickGround(dt, h, Input.JumpHeld);

            if (Input.JumpPressed && Grounded && !Hurt && !Dashing) DoJump();

            ApplyMotion(dt);
            UpdateSensors();
            TickAnimation(dt);
        }

        // ------------------------------------------------------------------
        // ground
        // ------------------------------------------------------------------
        private void TickGround(float dt, float h, bool jumpHeld)
        {
            if (Dashing)
            {
                DashTimer -= dt;
                if (DashTimer <= 0f) EndDash();
            }
            else if (Mathf.Abs(h) > 0.01f)
            {
                Facing = h > 0 ? 1 : -1;
                GroundSpeed = SonicMovement.UpdateGroundSpeed(
                    GroundSpeed, h, Rolling, SonicPhysics.Acceleration, SonicPhysics.Deceleration,
                    SonicPhysics.Friction, topSpeed, SonicPhysics.RollFriction,
                    SonicPhysics.RollMinSpeed, SonicPhysics.RollUnrollSpeed, true);
            }
            else
            {
                GroundSpeed = SonicMovement.UpdateGroundSpeed(
                    GroundSpeed, 0f, Rolling, SonicPhysics.Acceleration, SonicPhysics.Deceleration,
                    SonicPhysics.Friction, topSpeed, SonicPhysics.RollFriction,
                    SonicPhysics.RollMinSpeed, SonicPhysics.RollUnrollSpeed, true);
            }

            // roll only if fast enough to have started rolling
            if (!Rolling && Mathf.Abs(GroundSpeed) >= SonicPhysics.RollMinSpeed && jumpHeld)
            {
                Rolling = true;
            }
            if (Rolling && Mathf.Abs(GroundSpeed) < SonicPhysics.RollUnrollSpeed && !jumpHeld)
            {
                Rolling = false;
            }

            // slope factor (pseudo gravity), SPG: Slope Physics
            GroundSpeed -= SonicMovement.SlopeFactorFor(GroundAngle, Rolling) * Mathf.Sin(GroundAngle);

            var xy = SonicMovement.GroundSpeedToXY(GroundSpeed, GroundAngle);
            XSpeed = xy.x;
            YSpeed = xy.y;
        }

        // ------------------------------------------------------------------
        // air
        // ------------------------------------------------------------------
        private void TickAir(float dt, float h, bool jumpHeld)
        {
            float xs;
            SonicMovement.UpdateAir(XSpeed, ref YSpeed, h, jumpHeld, out xs,
                SonicPhysics.AirAcceleration, topSpeed, SonicPhysics.Gravity,
                SonicPhysics.JumpReleaseCap);
            XSpeed = xs;

            // short hover: hold jump while rising/apex to extend the jump
            if (jumpHeld && !Hurt && YSpeed < SonicPhysics.HoverEngageSpeed)
            {
                if (HoverTimer <= 0f) HoverTimer = SonicPhysics.HoverMaxSeconds;
                HoverTimer -= dt;
                if (HoverTimer > 0f)
                {
                    Hovering = true;
                    YSpeed += SonicPhysics.Gravity - SonicPhysics.HoverGravity;
                }
                else Hovering = false;
            }
            else
            {
                Hovering = false;
                HoverTimer = 0f;
            }
        }

        // ------------------------------------------------------------------
        // dash (GDD 7.1: blocked by walls, defeats enemies)
        // ------------------------------------------------------------------
        private void StartDash()
        {
            Dashing = true;
            Rolling = true;
            DashTimer = SonicPhysics.DashDuration;
            GroundSpeed = Facing * SonicPhysics.DashSpeed;
        }

        private void EndDash()
        {
            Dashing = false;
            GroundSpeed = MetalSonicMechanics.DashExitSpeed(GroundSpeed);
        }

        private void TickDash(float dt)
        {
            // a wall in front cancels the dash (GDD 7.1: "nao atravessa paredes")
            Vector2 origin = transform.position;
            Vector2 probe = origin + Vector2.right * Facing * wallProbeLength;
            bool wallAhead = MetalSonicWorld.Tilemap != null
                && MetalSonicWorld.Tilemap.SolidAt(probe, out float _);

            if (wallAhead)
            {
                EndDash();
                ControlLock = 10;
                return;
            }

            DashTimer -= dt;
            if (DashTimer <= 0f) EndDash();

            GroundSpeed = Facing * SonicPhysics.DashSpeed;
            var xy = SonicMovement.GroundSpeedToXY(GroundSpeed, GroundAngle);
            XSpeed = xy.x;
            YSpeed = xy.y;
        }

        // ------------------------------------------------------------------
        // hurt
        // ------------------------------------------------------------------
        private void TickHurt(float dt)
        {
            YSpeed += SonicPhysics.HurtGravity;
            if (Grounded)
            {
                Hurt = false;
                XSpeed = 0f;
            }
        }

        /// <summary>
        /// Applies a hit. Shield absorbs exactly one hit and preserves rings;
        /// otherwise rings scatter (GDD 7.5).
        /// </summary>
        public void ApplyHit(float fromX)
        {
            if (HasShield)
            {
                HasShield = false;
                return;
            }

            int ringsAfter;
            MetalSonicMechanics.ResolveHit(Rings, false, out ringsAfter);
            Rings = ringsAfter;

            Hurt = true;
            Rolling = false;
            Dashing = false;
            YSpeed = SonicPhysics.HurtYForce;
            XSpeed = SonicPhysics.HurtXForce * (transform.position.x >= fromX ? 1f : -1f);
            GroundSpeed = 0f;
            Grounded = false;
        }

        // ------------------------------------------------------------------
        // jump
        // ------------------------------------------------------------------
        private void DoJump()
        {
            SonicMovement.ApplyJump(GroundAngle, ref XSpeed, ref YSpeed, jumpForce);
            Grounded = false;
            Rolling = true;
            HoverTimer = SonicPhysics.HoverMaxSeconds;
        }

        // ------------------------------------------------------------------
        // dimension swap (GDD 7.2)
        // ------------------------------------------------------------------
        private void TrySwapDimension()
        {
            float tempusAfter;
            MetalSonicMechanics.SwapResult result = MetalSonicMechanics.TrySwapDimension(
                NextSwapDestinationBlocked, Tempus, out tempusAfter);

            switch (result)
            {
                case MetalSonicMechanics.SwapResult.Success:
                    Dimension = 1 - Dimension;
                    Tempus = tempusAfter;
                    LastSwapWasBlocked = false;
                    break;
                case MetalSonicMechanics.SwapResult.BlockedByWall:
                case MetalSonicMechanics.SwapResult.NotEnoughTempus:
                    LastSwapWasBlocked = true;
                    break;
            }
        }

        // ------------------------------------------------------------------
        // teleport (GDD 7.3)
        // ------------------------------------------------------------------
        private void TryTeleport()
        {
            if (!MetalSonicMechanics.CanTeleport(TeleportCharge)) return;

            Vector2 origin = transform.position;
            Vector2 destination = origin + Vector2.right * Facing * MetalSonicMechanics.TeleportDistance;

            // destination must be free; invalid attempts do not consume energy
            if (MetalSonicWorld.Tilemap != null
                && MetalSonicWorld.Tilemap.SolidAt(destination + Vector2.down * bodyHalfHeight, out float _))
            {
                return;
            }

            transform.position = destination;
            TeleportCharge = MetalSonicMechanics.SpendTeleport(TeleportCharge);
        }

        // ------------------------------------------------------------------
        // resources
        // ------------------------------------------------------------------
        public void AddRings(int count)
        {
            Rings += count;
            TeleportCharge = MetalSonicMechanics.AddRingEnergy(TeleportCharge, count);
        }

        public void CollectShield() => HasShield = MetalSonicMechanics.CollectShield(HasShield);

        public void CollectTempusCapsule() =>
            Tempus = MetalSonicMechanics.ApplyTempusCapsule(Tempus);

        // ------------------------------------------------------------------
        // motion + terrain
        // ------------------------------------------------------------------
        private void ApplyMotion(float dt)
        {
            Vector2 pos = transform.position;
            pos.x += XSpeed * dt * SonicPhysics.PhysicsHz;
            pos.y += YSpeed * dt * SonicPhysics.PhysicsHz;
            transform.position = pos;
        }

        /// <summary>
        /// Floor sensor test. Returns the ground angle in radians
        /// (0 = flat, positive = rising to the right).
        /// </summary>
        public float SampleGround(Vector2 origin, out bool grounded)
        {
            grounded = false;
            if (MetalSonicWorld.Tilemap == null) return 0f;

            float probe = Rolling ? ballHalfHeight : bodyHalfHeight;
            Vector2 a = origin + Vector2.left * bodyHalfWidth;
            Vector2 b = origin + Vector2.right * bodyHalfWidth;

            bool hitA = MetalSonicWorld.Tilemap.SolidAt(a + Vector2.down * probe, out float ay);
            bool hitB = MetalSonicWorld.Tilemap.SolidAt(b + Vector2.down * probe, out float by);

            if (!hitA && !hitB)
            {
                // small snap-down keeps us glued to slopes
                bool sA = MetalSonicWorld.Tilemap.SolidAt(a + Vector2.down * (probe + 0.25f), out float syA);
                bool sB = MetalSonicWorld.Tilemap.SolidAt(b + Vector2.down * (probe + 0.25f), out float syB);
                if (!sA && !sB) return 0f;
                hitA = sA; hitB = sB; ay = syA; by = syB;
            }

            grounded = true;
            return Mathf.Atan2(by - ay, b.x - a.x);
        }

        private void UpdateSensors()
        {
            float angle = SampleGround(transform.position, out bool groundedNow);
            bool wasGrounded = Grounded;
            Grounded = groundedNow;
            GroundAngle = angle;

            if (Grounded && YSpeed <= 0.01f)
            {
                if (!wasGrounded)
                {
                    YSpeed = 0f;
                    GroundSpeed = XSpeed * Mathf.Cos(angle) - YSpeed * Mathf.Sin(angle);
                }
            }
            else if (!Grounded && wasGrounded)
            {
                XSpeed = GroundSpeed * Mathf.Cos(angle);
                YSpeed = -GroundSpeed * Mathf.Sin(angle);
            }
        }

        // ------------------------------------------------------------------
        // animation
        // ------------------------------------------------------------------
        private void TickAnimation(float dt)
        {
            _anim.SetFlip(Facing < 0);

            MetalSonicAnim anim;
            if (Hurt) anim = MetalSonicAnim.Hurt;
            else if (Dashing) anim = MetalSonicAnim.Dash;
            else if (!Grounded) anim = Rolling ? MetalSonicAnim.Ball : MetalSonicAnim.Roll;
            else if (Rolling) anim = MetalSonicAnim.Roll;
            else if (Mathf.Abs(GroundSpeed) >= topSpeed) anim = MetalSonicAnim.Dash;
            else if (Mathf.Abs(GroundSpeed) >= topSpeed * 0.55f) anim = MetalSonicAnim.Run;
            else if (Mathf.Abs(GroundSpeed) > 0.01f) anim = MetalSonicAnim.Walk;
            else anim = MetalSonicAnim.Idle;

            _anim.Play(anim);
            _anim.Step(dt);
        }
    }
}
