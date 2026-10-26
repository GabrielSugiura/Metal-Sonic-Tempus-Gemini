using UnityEngine;

namespace MetalSonic
{
    /// <summary>
    /// Pure, allocation-free implementation of the SPG movement rules.
    ///
    /// Everything here takes explicit numbers and returns explicit numbers, so
    /// the headless test suite can assert exact per-frame values (top speed,
    /// the deceleration sign-flip quirk, variable jump height, slope factor)
    /// without a scene, a Rigidbody, or the Input System.
    ///
    /// Values are in pixels per frame; 256 subpixels = 1 pixel.
    /// </summary>
    public static class SonicMovement
    {
        /// <summary>
        /// Ground speed update for one frame. Mirrors SPG:Forces "Running".
        /// </summary>
        /// <param name="groundSpeed">Current ground speed, px/frame.</param>
        /// <param name="input">Horizontal input: -1 left, 0 none, 1 right.</param>
        /// <param name="rolling">Rolling ignores acceleration entirely.</param>
        public static float UpdateGroundSpeed(
            float groundSpeed,
            float input,
            bool rolling,
            float acceleration = SonicPhysics.Acceleration,
            float deceleration = SonicPhysics.Deceleration,
            float friction = SonicPhysics.Friction,
            float topSpeed = SonicPhysics.TopSpeed,
            float rollFriction = SonicPhysics.RollFriction,
            float rollMinSpeed = SonicPhysics.RollMinSpeed,
            float rollUnrollSpeed = SonicPhysics.RollUnrollSpeed,
            bool downHeld = false)
        {
            if (rolling)
            {
                groundSpeed -= Mathf.Min(Mathf.Abs(groundSpeed), rollFriction) * Mathf.Sign(groundSpeed);
                if (Mathf.Abs(groundSpeed) < rollMinSpeed) groundSpeed = 0f;
                return groundSpeed;
            }

            if (Mathf.Abs(input) > 0.01f)
            {
                if (groundSpeed * input < 0f)
                {
                    groundSpeed += deceleration * input;
                    if (Mathf.Abs(groundSpeed) < deceleration) groundSpeed = -0.5f * -input;
                }
                else if (Mathf.Abs(groundSpeed) < topSpeed)
                {
                    // SPG: acceleration is NOT applied once above top speed
                    // (Sonic 2 onwards); speed is preserved and only decays.
                    groundSpeed += acceleration * input;
                    if (Mathf.Abs(groundSpeed) > topSpeed) groundSpeed = topSpeed * input;
                }
            }
            else
            {
                float drop = Mathf.Min(Mathf.Abs(groundSpeed), friction);
                groundSpeed -= drop * Mathf.Sign(groundSpeed);
                if (Mathf.Abs(groundSpeed) < friction) groundSpeed = 0f;
            }
            return groundSpeed;
        }

        /// <summary>
        /// Air speed update for one frame. Mirrors SPG:Forces "Midair":
        /// air acceleration limited by top speed, then gravity, then the
        /// variable jump height cap.
        /// </summary>
        public static void UpdateAir(
            float xSpeed,
            ref float ySpeed,
            float input,
            bool jumpHeld,
            out float newXSpeed,
            float airAcceleration = SonicPhysics.AirAcceleration,
            float topSpeed = SonicPhysics.TopSpeed,
            float gravity = SonicPhysics.Gravity,
            float jumpCap = SonicPhysics.JumpReleaseCap)
        {
            float xs = xSpeed;
            if (Mathf.Abs(input) > 0.01f && Mathf.Abs(xs) < topSpeed)
            {
                xs += airAcceleration * input;
                if (Mathf.Abs(xs) > topSpeed) xs = topSpeed * input;
            }

            ySpeed += gravity;
            if (!jumpHeld && ySpeed < jumpCap) ySpeed = jumpCap;

            newXSpeed = xs;
        }

        /// <summary>
        /// Jump impulse, SPG: "Jumping". The impulse is applied along the
        /// ground angle so momentum is preserved.
        /// </summary>
        public static void ApplyJump(float groundAngle, ref float xSpeed, ref float ySpeed, float jumpForce)
        {
            xSpeed -= jumpForce * Mathf.Sin(groundAngle);
            ySpeed -= jumpForce * Mathf.Cos(groundAngle);
        }

        /// <summary>Slope factor (pseudo gravity), SPG: "Slowing Down Uphill".</summary>
        public static float SlopeFactorFor(float groundAngle, bool rolling)
        {
            if (!rolling) return SonicPhysics.SlopeFactor;
            float s = Mathf.Sin(groundAngle);
            // sign of ground speed is implied by the caller's sign convention;
            // here we only need uphill vs downhill relative to travel.
            return s >= 0f ? SonicPhysics.SlopeFactorRollingDown : SonicPhysics.SlopeFactorRollingUp;
        }

        /// <summary>
        /// Spin dash release speed, SPG: "Spindash": 8 + floor(rev)/2, capped at 12.
        /// </summary>
        public static float SpindashReleaseSpeed(int rev)
        {
            return Mathf.Min(12f, 8f + Mathf.Floor(rev) / 2f);
        }

        /// <summary>Spin rev drag, applied while charging: rev -= (rev div 8)/256.</summary>
        public static int SpinRevDrag(int rev)
        {
            return rev - (rev / 8) / 256;
        }

        /// <summary>Converts a ground angle to X/Y speed components (SPG: Slope Physics).</summary>
        public static Vector2 GroundSpeedToXY(float groundSpeed, float groundAngle)
        {
            return new Vector2(
                groundSpeed * Mathf.Cos(groundAngle),
                -groundSpeed * Mathf.Sin(groundAngle));
        }
    }
}
