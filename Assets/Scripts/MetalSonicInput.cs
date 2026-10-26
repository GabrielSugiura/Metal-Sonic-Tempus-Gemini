using UnityEngine;

namespace MetalSonic
{
    /// <summary>
    /// Input mapping straight from the GDD control table:
    ///   W  look up          A  back (left)       D  forward (right)
    ///   X  switch dimension SPACE jump (hold = short hover)
    ///   V  teleport         SHIFT dash (investida)
    ///
    /// Split behind an interface so the whole state machine can be driven
    /// with synthetic values from a headless test.
    /// </summary>
    public interface IMetalSonicInput
    {
        float Horizontal { get; }      // -1..1, left..right
        bool UpHeld { get; }
        bool JumpPressed { get; }      // pressed this tick
        bool JumpHeld { get; }
        bool DimensionPressed { get; } // X
        bool TeleportPressed { get; }  // V
        bool DashPressed { get; }      // SHIFT
    }

    /// <summary>Keyboard reader using the GDD key map.</summary>
    public class KeyboardMetalSonicInput : IMetalSonicInput
    {
        public float Horizontal
        {
            get
            {
                float h = Input.GetAxisRaw("Horizontal");
                return Mathf.Abs(h) < 0.001f ? 0f : h;
            }
        }

        public bool UpHeld => Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        public bool JumpPressed => Input.GetButtonDown("Jump");
        public bool JumpHeld => Input.GetButton("Jump");
        public bool DimensionPressed => Input.GetKeyDown(KeyCode.X);
        public bool TeleportPressed => Input.GetKeyDown(KeyCode.V);
        public bool DashPressed => Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
    }

    /// <summary>Scriptable input used by headless tests.</summary>
    public class ScriptedMetalSonicInput : IMetalSonicInput
    {
        public float Horizontal { get; set; }
        public bool UpHeld { get; set; }
        public bool JumpPressed { get; set; }
        public bool JumpHeld { get; set; }
        public bool DimensionPressed { get; set; }
        public bool TeleportPressed { get; set; }
        public bool DashPressed { get; set; }
    }
}
