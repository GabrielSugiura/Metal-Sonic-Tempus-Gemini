using UnityEngine;

namespace MetalSonic
{
    /// <summary>
    /// Frame-stepping animation player for the Metal Sonic sheet.
    ///
    /// Rows in the sheet (verified by slicing the source PNG):
    ///   0 idle 8 | 1 walk 8 | 2 run 8 | 3 dash 8
    ///   4 spindash 8 | 5 roll 6 | 6 ball 7 | 7 hurt 8
    ///
    /// Timings follow SPG:Animations -- a subimage lasts duration+1 frames
    /// because the counter resets on the frame it reaches -1. Action clips play
    /// at <see cref="MetalSonicAnimator.framesPerSecond"/>; the idle clip keeps
    /// the authentic SPG 24-frame subimage length.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class MetalSonicAnimator : MonoBehaviour
    {
        [SerializeField] public int framesPerSecond = SonicPhysics.AnimationFramesPerSecond;
        [SerializeField] public bool flipX;

        public MetalSonicAnim Current { get; private set; } = MetalSonicAnim.Idle;
        public int Frame { get; private set; }
        public int FrameCount { get; private set; } = 8;
        public float FrameDuration { get; private set; } = 1f / 24f;

        private SpriteRenderer _renderer;
        private float _timer;
        private bool _dirty = true;

        // frames per row, measured from the source sheet
        private static readonly int[] RowFrameCount = { 8, 8, 8, 8, 8, 6, 7, 8 };

        // SPG subimage durations per row (game frames at 60 Hz)
        private static readonly int[] RowDuration =
        {
            SonicPhysics.IdleFrameDuration,      // idle
            SonicPhysics.WalkFrameDuration,      // walk
            SonicPhysics.WalkFrameDuration,      // run
            SonicPhysics.WalkFrameDuration,      // dash
            SonicPhysics.SpindashFrameDuration,  // spindash
            SonicPhysics.RollFrameDuration,      // roll
            SonicPhysics.RollFrameDuration,      // ball
            SonicPhysics.IdleFrameDuration,      // hurt
        };

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Play(MetalSonicAnim anim)
        {
            if (Current == anim && !_dirty) return;
            Current = anim;
            Frame = 0;
            _timer = 0f;
            int row = (int)anim;
            FrameCount = RowFrameCount[row];
            // SPG timing: duration+1 game frames, converted to seconds
            FrameDuration = (RowDuration[row] + 1) / (float)SonicPhysics.PhysicsHz;
            _dirty = true;
            Apply();
        }

        /// <summary>
        /// Advances the animation. Takes seconds, so playback speed does not
        /// depend on render frame rate.
        /// </summary>
        public void Step(float dtSeconds)
        {
            if (FrameCount <= 1) { Apply(); return; }
            _timer += dtSeconds;
            while (_timer >= FrameDuration)
            {
                _timer -= FrameDuration;
                Frame = (Frame + 1) % FrameCount;
                _dirty = true;
            }
            Apply();
        }

        /// <summary>Sets the frame without advancing the timer.</summary>
        public void SetFrame(int index)
        {
            Frame = Mathf.Clamp(index, 0, Mathf.Max(0, FrameCount - 1));
            _timer = 0f;
            _dirty = true;
            Apply();
        }

        public void SetFlip(bool flip) => flipX = flip;

        public void Apply()
        {
            if (!_dirty || _renderer == null) return;
            Sprite sprite = MetalSonicSpriteBank.Get(Current.ToString().ToLowerInvariant(), Frame);
            if (sprite != null) _renderer.sprite = sprite;
            _renderer.flipX = flipX;
            _dirty = false;
        }
    }
}
