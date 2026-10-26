using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MetalSonic.EditorTools
{
    /// <summary>
    /// Imports the Palmtree Panic tileset and Metal Sonic frames with the
    /// settings the level needs: 100 PPU, point filter, uncompressed.
    /// Also copies the sliced frames into Assets/Resources so the sprite bank
    /// can load them by path without a scene reference.
    /// </summary>
    public static class MetalSonicAssetSetup
    {
        public const float PixelsPerUnit = 100f;

        [MenuItem("Project/Metal Sonic/Setup Assets")]
        public static void Setup()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Sprites");

            // 1. copy sliced frames into Resources
            CopySpritesIntoResources();

            // 2. importer settings for every texture we care about
            foreach (string path in AllTextures())
            {
                bool isCharacter = path.IndexOf("Metal Sonic", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("Assets/Sprites", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("Resources/Sprites", System.StringComparison.OrdinalIgnoreCase) >= 0;
                Configure(path, isCharacter);
            }

            AssetDatabase.Refresh();
            SetActiveInputHandler();
            Debug.Log("[MetalSonic] asset setup complete");
        }

        /// <summary>
        /// The project ships with activeInputHandler: 1 (New Input System only),
        /// which silently kills legacy Input.GetKey / Input.GetAxisRaw -- the
        /// controls would read as permanently false. Force 2 (Both).
        /// </summary>
        private static void SetActiveInputHandler()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var prop = settings.FindProperty("activeInputHandler");
            if (prop == null)
            {
                Debug.LogWarning("[MetalSonic] activeInputHandler not found; leaving as-is");
                return;
            }
            if (prop.intValue == 2)
            {
                Debug.Log("[MetalSonic] activeInputHandler already 2 (Both)");
                return;
            }
            prop.intValue = 2;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[MetalSonic] activeInputHandler set to 2 (Both) so legacy Input works");
        }

        private static IEnumerable<string> AllTextures()
        {
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D",
                         new[] { "Assets", "Assets/Sprites", "Assets/Resources" }))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
            return paths;
        }

        private static void CopySpritesIntoResources()
        {
            const string source = "Assets/Sprites";
            const string dest = "Assets/Resources/Sprites";
            if (!Directory.Exists(source)) return;

            foreach (string file in Directory.GetFiles(source, "*.png"))
            {
                string name = Path.GetFileName(file);
                string target = dest + "/" + name;
                if (!File.Exists(target) || File.GetLastWriteTime(file) > File.GetLastWriteTime(target))
                {
                    File.Copy(file, target, true);
                }
                // copy meta too so the GUID stays stable
                string meta = file + ".meta";
                if (File.Exists(meta)) File.Copy(meta, target + ".meta", true);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void Configure(string path, bool isCharacter)
        {
            if (string.IsNullOrEmpty(path)) return;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = PixelsPerUnit;

            if (isCharacter)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
            }
            else
            {
                // the tileset uses purple as the sky/empty colour; treat it as
                // opaque so the level builder can mask it per-tile
                importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                importer.alphaIsTransparency = false;
            }

            importer.SaveAndReimport();
        }
    }
}
