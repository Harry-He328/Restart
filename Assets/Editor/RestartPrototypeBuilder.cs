using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Restart.Editor
{
    public static class RestartPrototypeBuilder
    {
        private const string ScenePath = "Assets/Scenes/RestartPrototype.unity";
        private const string ArtPath = "Assets/Art/Prototype";

        [MenuItem("Restart/Build Movement Prototype")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(ArtPath);
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var groundLayer = tags.FindProperty("layers").GetArrayElementAtIndex(6);
            if (string.IsNullOrEmpty(groundLayer.stringValue))
            {
                groundLayer.stringValue = "Ground";
                tags.ApplyModifiedProperties();
            }
            var sprite = CreateSprite();
            var top = CreateTile("PlatformTop", sprite, new Color(0.24f, 0.75f, 0.7f));
            var fill = CreateTile("PlatformFill", sprite, new Color(0.14f, 0.26f, 0.34f));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10.5f;
            camera.backgroundColor = new Color(0.035f, 0.06f, 0.105f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0, 2, -10);

            var grid = new GameObject("Level Grid", typeof(Grid));
            var terrain = new GameObject("Platforms - 9 tile gap", typeof(Tilemap), typeof(TilemapRenderer));
            terrain.layer = 6;
            terrain.transform.SetParent(grid.transform);
            var map = terrain.GetComponent<Tilemap>();
            for (int x = -16; x < 16; x++)
            {
                if (x >= -5 && x < 4) continue;
                for (int y = -5; y < 0; y++) map.SetTile(new Vector3Int(x, y), y == -1 ? top : fill);
            }
            var groundBody = terrain.AddComponent<Rigidbody2D>();
            groundBody.bodyType = RigidbodyType2D.Static;
            var tileCollider = terrain.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = terrain.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ArtPath + "/NoFriction.physicsMaterial2D");
            if (material == null)
            {
                material = new PhysicsMaterial2D("NoFriction") { friction = 0, bounciness = 0 };
                AssetDatabase.CreateAsset(material, ArtPath + "/NoFriction.physicsMaterial2D");
            }
            composite.sharedMaterial = material;

            var player = new GameObject("Player", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CapsuleCollider2D));
            player.tag = "Player";
            player.transform.position = new Vector3(-11, 0.85f, 0);
            var renderer = player.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(0.7f, 1.45f);
            renderer.color = new Color(0.95f, 0.97f, 1f);
            renderer.sortingOrder = 5;
            var capsule = player.GetComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.7f, 1.45f);
            capsule.sharedMaterial = material;
            var motor = player.AddComponent<PlayerMotor2D>();
            var motorSettings = new SerializedObject(motor);
            motorSettings.FindProperty("groundLayers").intValue = 1 << terrain.layer;
            motorSettings.ApplyModifiedPropertiesWithoutUndo();
            for (int i = 0; i < 2; i++)
            {
                var eye = new GameObject("Eye", typeof(SpriteRenderer));
                eye.transform.SetParent(player.transform);
                eye.transform.localPosition = new Vector3(i == 0 ? -0.14f : 0.14f, 0.23f, -0.01f);
                eye.transform.localScale = new Vector3(0.09f, 0.13f, 1);
                eye.GetComponent<SpriteRenderer>().sprite = sprite;
                eye.GetComponent<SpriteRenderer>().color = camera.backgroundColor;
                eye.GetComponent<SpriteRenderer>().sortingOrder = 6;
            }

            var ui = new GameObject("Prototype HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = ui.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            var scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            SetupProjectionSystem();
            RestartTraversalSetup.Setup();
            RestartMechanismSetup.Setup();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log("[Restart] Built RestartPrototype: two Tilemap platforms, gap [-5, 4] = 9 units, player speed 7.5, jump speed 13.");
        }

        private static Sprite CreateSprite()
        {
            string path = ArtPath + "/PrototypeTile.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(32, 32);
                var pixels = new Color[32 * 32];
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++) pixels[y * 32 + x] = x == 0 || y == 0 ? new Color(0.8f, 0.8f, 0.8f) : Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        [MenuItem("Restart/Capture Prototype")]
        public static void Capture()
        {
            var camera = Camera.main;
            var target = RenderTexture.GetTemporary(1600, 900, 24);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                Canvas.ForceUpdateCanvases();
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                texture.Apply();
                Directory.CreateDirectory("Captures");
                File.WriteAllBytes("Captures/RestartPrototype.png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previous;
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(texture);
            }
        }

        private static Tile CreateTile(string name, Sprite sprite, Color color)
        {
            string path = ArtPath + "/" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        [MenuItem("Restart/Setup Projection System")]
        public static void SetupProjectionSystem()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            var player = Object.FindFirstObjectByType<PlayerMotor2D>();
            if (player == null) throw new System.InvalidOperationException("Open the prototype scene first.");
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layer = tags.FindProperty("layers").GetArrayElementAtIndex(7);
            if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != "Projection")
                throw new System.InvalidOperationException("Layer 7 is already occupied.");
            layer.stringValue = "Projection";
            tags.ApplyModifiedProperties();
            var motor = new SerializedObject(player);
            motor.FindProperty("groundLayers").intValue |= 1 << 7;
            motor.ApplyModifiedPropertiesWithoutUndo();
            var oldHud = GameObject.Find("Prototype HUD");
            if (oldHud != null) Object.DestroyImmediate(oldHud);
            var ui = new GameObject("Prototype HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = ui.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1;
            var scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            var timer = new GameObject("Attempt Time", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            timer.transform.SetParent(ui.transform, false);
            var rect = timer.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-32, -24);
            rect.sizeDelta = new Vector2(320, 60);
            var text = timer.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "00:00.00";
            text.fontSize = 34;
            text.alignment = TextAnchor.UpperRight;
            text.color = new Color(0.8f, 0.94f, 0.96f);
            text.raycastTarget = false;
            var root = GameObject.Find("Projections") ?? new GameObject("Projections");
            var session = Object.FindFirstObjectByType<ProjectionSession>();
            if (session == null) session = new GameObject("Attempt Session").AddComponent<ProjectionSession>();
            var settings = new SerializedObject(session);
            settings.FindProperty("player").objectReferenceValue = player;
            settings.FindProperty("attemptText").objectReferenceValue = text;
            settings.FindProperty("projectionRoot").objectReferenceValue = root.transform;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            EditorSceneManager.SaveScene(player.gameObject.scene,
                string.IsNullOrEmpty(player.gameObject.scene.path) ? ScenePath : player.gameObject.scene.path);
            Debug.Log("[Restart] Projection system installed. R records; Escape restarts.");
        }
    }
}
