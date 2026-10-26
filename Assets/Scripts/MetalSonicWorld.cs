using UnityEngine;

namespace MetalSonic
{
    /// <summary>
    /// Bridge between the controller and whatever provides terrain.
    /// The level generator assigns a tilemap; headless tests assign a stub.
    /// </summary>
    public static class MetalSonicWorld
    {
        public static IMetalSonicTilemap Tilemap { get; set; }
    }

    /// <summary>
    /// World-space tile query. The test level fills this from the Palmtree
    /// Panic tileset; it is intentionally decoupled from Tilemap so the
    /// controller stays unit-testable.
    /// </summary>
    public interface IMetalSonicTilemap
    {
        /// <summary>True if the point is inside solid terrain.</summary>
        bool SolidAt(Vector2 worldPoint, out float surfaceY);
    }

    /// <summary>
    /// A flat floor plus 45 degree ramps, built from the Palmtree Panic
    /// tileset. Columns are expressed in tile units (16 px each) and converted
    /// to world units at build time.
    /// </summary>
    public class HeightfieldTilemap : IMetalSonicTilemap
    {
        private readonly float[] _floorY;   // surface height per column, world units
        private readonly float _originX;
        private readonly float _columnWidth;
        private readonly float _solidDepth;

        public float SurfaceY(int column)
        {
            if (column < 0 || column >= _floorY.Length) return float.NegativeInfinity;
            return _floorY[column];
        }

        public HeightfieldTilemap(float originX, float columnWidth, float[] floorY, float solidDepth)
        {
            _originX = originX;
            _columnWidth = columnWidth;
            _floorY = floorY;
            _solidDepth = solidDepth;
        }

        public bool SolidAt(Vector2 worldPoint, out float surfaceY)
        {
            surfaceY = 0f;
            int column = Mathf.FloorToInt((worldPoint.x - _originX) / _columnWidth);
            if (column < 0 || column >= _floorY.Length) return false;
            surfaceY = _floorY[column];
            // solid if the point is below the surface
            return worldPoint.y <= surfaceY + 0.001f;
        }
    }
}
