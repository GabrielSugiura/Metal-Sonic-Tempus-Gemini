using System.Collections.Generic;
using UnityEngine;

namespace MetalSonic
{
    /// <summary>
    /// Caches sliced frame sprites by "anim/frame" so the animator does not
    /// hit the asset database every step. Sprites are loaded lazily from
    /// Assets/Sprites on first use.
    /// </summary>
    public static class MetalSonicSpriteBank
    {
        private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

        private static readonly Dictionary<string, int> Expected = new Dictionary<string, int>
        {
            { "idle", 8 }, { "walk", 8 }, { "run", 8 }, { "dash", 8 },
            { "spindash", 8 }, { "roll", 6 }, { "ball", 7 }, { "hurt", 8 },
        };

        public static Sprite Get(string anim, int frame)
        {
            Sprite[] frames;
            if (!Cache.TryGetValue(anim, out frames))
            {
                int count;
                if (!Expected.TryGetValue(anim, out count)) count = 8;
                frames = new Sprite[count];
                for (int i = 0; i < count; i++)
                {
                    frames[i] = Resources.Load<Sprite>("Sprites/" + anim + "_" + i.ToString("00"));
                }
                Cache[anim] = frames;
            }
            if (frames == null || frames.Length == 0) return null;
            if (frame < 0 || frame >= frames.Length) frame = 0;
            return frames[frame];
        }

        public static void Warm()
        {
            foreach (string key in Expected.Keys) Get(key, 0);
        }
    }
}
