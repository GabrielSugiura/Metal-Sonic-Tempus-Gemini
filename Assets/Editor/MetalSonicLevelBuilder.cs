using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MetalSonic.EditorTools
{
    /// <summary>
    /// Generates the Palmtree Panic test level from code: flat floor, two 45
    /// degree ramps, a step, plus the Metal Sonic player. This is the project's
    /// canonical level generator -- re-run it after any layout change rather
    /// than hand-editing the scene.
    /// </summary>
    public static class MetalSonicLevelBuilder
    {
        [MenuItem("Project/Metal Sonic/Build Test Level")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const float tileWorld = 16f / MetalSonicAssetSetup.PixelsPerUnit; // 0.16 units/tile

            // floor 8 tiles below origin, 4-tile ramp up, plateau, ramp down,
            // then a 2-tile step up (GDD: Palmtree Panic, Mundo A).
            var floorY = new List<float>();
            for (int i = 0; i < 40; i++) floorY.Add(-8f);
            for (int i = 1; i <= 4; i++) floorY.Add(-8f + i);
            for (int i = 0; i < 20; i++) floorY.Add(-4f);
            for (int i = 1; i <= 4; i++) floorY.Add(-4f - i);
            for (int i = 0; i < 20; i++) floorY.Add(-8f);
            for (int i = 0; i < 2; i++) floorY.Add(-6f);
            for (int i = 0; i < 40; i++) floorY.Add(-6f);

            float[] heights = floorY.ToArray();

            // --- world bridge -------------------------------------------------
            var bridgeGo = new GameObject("MetalSonicWorldBridge");
            var bridge = bridgeGo.AddComponent<HeightfieldBridge>();
            bridge.Configure(0f, tileWorld, heights, 40f);

            // --- terrain visuals ----------------------------------------------
            BuildTerrainVisuals(heights, tileWorld);

            // --- player -------------------------------------------------------
            var player = new GameObject("MetalSonic");
            player.transform.position = new Vector3(0.5f, heights[0] + 1.0f, 0f);
            player.tag = "Player";

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = player.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.3f, 0.6f);
            col.isTrigger = false;

            var animator = player.AddComponent<MetalSonicAnimator>();
            animator.framesPerSecond = SonicPhysics.AnimationFramesPerSecond;

            var controller = player.AddComponent<MetalSonicController>();
            controller.topSpeed = SonicPhysics.TopSpeed;
            controller.jumpForce = SonicPhysics.JumpForce;

            // --- camera -------------------------------------------------------
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.55f, 0.44f, 0.8f, 1f);
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/TestLevel.unity");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/TestLevel.unity", true),
            };

            Debug.Log(string.Format(
                "[MetalSonic] level built: {0} columns, {1:0.00} units wide",
                heights.Length, heights.Length * tileWorld));
        }

        /// <summary>One stretched quad per run of equal floor height.</summary>
        private static void BuildTerrainVisuals(float[] heights, float tileWorld)
        {
            var ground = new GameObject("Ground");
            const float solidDepth = 40f;

            float runStart = 0f;
            float runHeight = heights[0];

            for (int i = 1; i <= heights.Length; i++)
            {
                bool runEnds = i == heights.Length || !Mathf.Approximately(heights[i], runHeight);
                if (!runEnds) continue;

                float x0 = runStart * tileWorld;
                float width = (i - runStart) * tileWorld;
                float top = runHeight;

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = string.Format("Ground_{0}_{1}", runStart, i);
                Object.DestroyImmediate(quad.GetComponent<SpriteRenderer>());
                quad.GetComponent<Renderer>().sortingOrder = 0;
                quad.transform.SetParent(ground.transform);
                quad.transform.localScale = new Vector3(width, solidDepth, 1f);
                quad.transform.position = new Vector3(
                    x0 + width * 0.5f, top - solidDepth * 0.5f, 0f);

                runStart = i;
                if (i < heights.Length) runHeight = heights[i];
            }
        }
    }
}
