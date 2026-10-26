using UnityEngine;

namespace MetalSonic
{
    /// <summary>
    /// Physics constants and frame math.
    ///
    /// Movement values come from the Sonic Physics Guide
    /// (https://github.com/trentbrew/sonic-physics-guide, docs/10-forces.md,
    /// docs/09-slope-physics.md, docs/11-forces-subtopics/jumping.md).
    /// Ability values come from the GDD, section 7 -- those marked "a definir
    /// no prototipo" are prototype placeholders, flagged with PrototypeValue.
    /// </summary>
    public static class SonicPhysics
    {
        /// <summary>Physics runs at a fixed 60 Hz so per-frame values stay exact.</summary>
        public const int PhysicsHz = 60;

        // --- movement (SPG:Forces) -----------------------------------------
        public const float Acceleration = 12f / 256f;        // 0.046875 px/frame
        public const float Deceleration = 128f / 256f;       // 0.5 px/frame
        public const float Friction = 12f / 256f;            // 0.046875 px/frame
        public const float TopSpeed = 6f;                    // px/frame
        public const float AirAcceleration = 24f / 256f;     // 0.09375 px/frame

        // --- jumping (SPG:Jumping) ------------------------------------------
        public const float JumpForce = 6f + 128f / 256f;     // 6.5 px/frame
        public const float Gravity = 56f / 256f;             // 0.21875 px/frame
        public const float JumpReleaseCap = -4f;              // variable jump height

        // --- hover (GDD 7.1: "pulo com hover curto") ------------------------
        /// <summary>Prototype value: reduced gravity while the jump button is held.</summary>
        public const float HoverGravity = 8f / 256f;         // PrototypeValue
        /// <summary>Prototype value: seconds of hover available after the apex.</summary>
        public const float HoverMaxSeconds = 0.35f;          // PrototypeValue
        /// <summary>Upward speed at which hover starts engaging.</summary>
        public const float HoverEngageSpeed = -1.5f;         // PrototypeValue

        // --- rolling (SPG:Forces) -------------------------------------------
        public const float RollFriction = 6f / 256f;
        public const float RollDeceleration = 32f / 256f;
        public const float RollMinSpeed = 128f / 256f;
        public const float RollUnrollSpeed = 128f / 256f;

        // --- slopes (SPG:Slope Physics) -------------------------------------
        public const float SlopeFactor = 32f / 256f;
        public const float SlopeFactorRollingUp = 20f / 256f;
        public const float SlopeFactorRollingDown = 80f / 256f;

        // --- damage (SPG:Forces) --------------------------------------------
        public const float HurtXForce = 2f;
        public const float HurtYForce = -4f;
        public const float HurtGravity = 48f / 256f;

        // --- dash (GDD 7.1) --------------------------------------------------
        public const float DashSpeed = 9f;                   // PrototypeValue
        public const float DashDuration = 0.25f;             // PrototypeValue
        public const float DashWallProbe = 0.35f;            // PrototypeValue

        // --- animation timing (SPG:Animations) ------------------------------
        /// <summary>Idle subimages last 24 frames (SPG:Animations, Sonic 1).</summary>
        public const int IdleFrameDuration = 24;
        /// <summary>Walk/run subimages last 6 frames (SPG:Animations, Sonic 2+).</summary>
        public const int WalkFrameDuration = 6;
        /// <summary>The spindash animation advances a subimage every frame.</summary>
        public const int SpindashFrameDuration = 1;
        /// <summary>Roll subimages last 4 frames.</summary>
        public const int RollFrameDuration = 4;

        /// <summary>Requested playback rate for the animation clips.</summary>
        public const int AnimationFramesPerSecond = 24;

        // --- sprite sheet geometry ------------------------------------------
        public const int SheetColumns = 8;
        public const int SheetCell = 44;

        /// <summary>Which row of the sheet each animation lives in.</summary>
        public const int RowIdle = 0;
        public const int RowWalk = 1;
        public const int RowRun = 2;
        public const int RowDash = 3;
        public const int RowSpindash = 4;
        public const int RowRoll = 5;
        public const int RowBall = 6;
        public const int RowHurt = 7;
    }

    /// <summary>Frame-rate-independent animation clip definition.</summary>
    public enum MetalSonicAnim
    {
        Idle = SonicPhysics.RowIdle,
        Walk = SonicPhysics.RowWalk,
        Run = SonicPhysics.RowRun,
        Dash = SonicPhysics.RowDash,
        Spindash = SonicPhysics.RowSpindash,
        Roll = SonicPhysics.RowRoll,
        Ball = SonicPhysics.RowBall,
        Hurt = SonicPhysics.RowHurt,
    }
}
