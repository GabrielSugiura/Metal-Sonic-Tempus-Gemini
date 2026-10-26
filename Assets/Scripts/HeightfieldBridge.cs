using UnityEngine;

namespace MetalSonic
{
    /// <summary>
    /// Runtime bridge exposing a <see cref="HeightfieldTilemap"/> to the
    /// controller. Lives in the runtime assembly (not Editor) so the scene's
    /// reference survives a player build.
    /// </summary>
    public class HeightfieldBridge : MonoBehaviour
    {
        [SerializeField] public float originX;
        [SerializeField] public float columnWidth = 0.16f;
        [SerializeField] public float solidDepth = 40f;
        [SerializeField] public float[] heights = new float[0];

        private void Awake() => Apply();

        public void Configure(float ox, float cw, float[] h, float depth)
        {
            originX = ox;
            columnWidth = cw;
            heights = h;
            solidDepth = depth;
            Apply();
        }

        private void Apply()
        {
            if (heights == null || heights.Length == 0) return;
            MetalSonicWorld.Tilemap = new HeightfieldTilemap(originX, columnWidth, heights, solidDepth);
        }
    }
}
