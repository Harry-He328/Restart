using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Restart.Editor
{
    public static class RestartMechanismSetup
    {
        private const string Folder = "Assets/Prefabs/Mechanisms/";
        private const string Art = "Assets/Art/Prototype/";

        [MenuItem("Restart/Setup Mechanisms")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMotor2D>();
            if (player == null) throw new InvalidOperationException("Open the prototype scene first.");
            Directory.CreateDirectory(Folder);
            SetLayer(9, "MovingPlatform");
            SetLayer(10, "Hazard");
            SetLayer(11, "Projectile");
            var motor = new SerializedObject(player);
            motor.FindProperty("groundLayers").intValue |= 1 << 9;
            motor.ApplyModifiedPropertiesWithoutUndo();

            var tile = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "PrototypeTile.png");
            var spikeSprite = CreateSprite("Spikes", 96, 32, 32, Vector2.zero, (x, y) =>
            {
                float center = x % 32 - 15.5f;
                return y <= 28f * (1f - Mathf.Abs(center) / 15.5f) ? Color.white : Color.clear;
            });
            var bulletSprite = CreateSprite("Projectile", 32, 32, 128, Vector2.one * 0.5f, (x, y) =>
                new Vector2(x - 15.5f, y - 15.5f).sqrMagnitude <= 15f * 15f ? Color.white : Color.clear);
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Art + "NoFriction.physicsMaterial2D");

            var bulletPrefab = CreatePrefab("Projectile", () =>
            {
                var root = new GameObject("Projectile", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D));
                root.layer = 11;
                var visual = root.GetComponent<SpriteRenderer>();
                visual.sprite = bulletSprite;
                visual.color = new Color(1f, 0.35f, 0.2f);
                visual.sortingOrder = 8;
                var body = root.GetComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                var shape = root.GetComponent<CircleCollider2D>();
                shape.radius = 0.12f;
                shape.isTrigger = true;
                var script = root.AddComponent<Projectile2D>();
                var settings = new SerializedObject(script);
                settings.FindProperty("hitLayers").intValue = (1 << 0) | (1 << 6) | (1 << 7) | (1 << 8) | (1 << 9);
                settings.ApplyModifiedPropertiesWithoutUndo();
                return root;
            });

            var emitterPrefab = CreatePrefab("ProjectileEmitter", () =>
            {
                var root = new GameObject("ProjectileEmitter", typeof(SpriteRenderer), typeof(BoxCollider2D));
                root.layer = 6;
                SetVisual(root, tile, new Vector2(0.8f, 0.8f), new Color(0.64f, 0.26f, 0.16f));
                root.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.8f);
                var barrel = new GameObject("Barrel", typeof(SpriteRenderer));
                barrel.transform.SetParent(root.transform, false);
                barrel.transform.localPosition = new Vector3(0.4f, 0);
                SetVisual(barrel, tile, new Vector2(0.6f, 0.3f), new Color(1, 0.53f, 0.25f));
                var muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(root.transform, false);
                muzzle.localPosition = new Vector3(0.85f, 0);
                var script = root.AddComponent<ProjectileEmitter2D>();
                var settings = new SerializedObject(script);
                settings.FindProperty("projectilePrefab").objectReferenceValue = bulletPrefab.GetComponent<Projectile2D>();
                settings.FindProperty("muzzle").objectReferenceValue = muzzle;
                settings.ApplyModifiedPropertiesWithoutUndo();
                return root;
            });

            var spikesPrefab = CreatePrefab("Spikes", () =>
            {
                var root = new GameObject("Spikes", typeof(SpriteRenderer), typeof(PolygonCollider2D), typeof(DeathHazard2D));
                root.layer = 10;
                root.GetComponent<SpriteRenderer>().sprite = spikeSprite;
                root.GetComponent<SpriteRenderer>().color = new Color(1f, 0.38f, 0.44f);
                root.GetComponent<SpriteRenderer>().sortingOrder = 3;
                ConfigureSpikeCollider(root);
                return root;
            });

            var platformPrefab = CreatePrefab("MovingPlatform", () =>
            {
                var root = new GameObject("MovingPlatform");
                root.layer = 9;
                var pointA = new GameObject("Point A").transform;
                pointA.SetParent(root.transform, false);
                pointA.localPosition = new Vector3(-3.5f, 0);
                var pointB = new GameObject("Point B").transform;
                pointB.SetParent(root.transform, false);
                pointB.localPosition = new Vector3(2.5f, 0);
                var platform = new GameObject("Platform", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(KinematicSurface2D));
                platform.layer = 9;
                platform.transform.SetParent(root.transform, false);
                platform.transform.localPosition = pointA.localPosition;
                SetVisual(platform, tile, new Vector2(2.4f, 0.4f), new Color(1f, 0.79f, 0.28f));
                var body = platform.GetComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                var shape = platform.GetComponent<BoxCollider2D>();
                shape.size = new Vector2(2.4f, 0.4f);
                shape.sharedMaterial = material;
                var script = root.AddComponent<MovingPlatform2D>();
                var settings = new SerializedObject(script);
                settings.FindProperty("platformBody").objectReferenceValue = body;
                settings.FindProperty("pointA").objectReferenceValue = pointA;
                settings.FindProperty("pointB").objectReferenceValue = pointB;
                settings.ApplyModifiedPropertiesWithoutUndo();
                return root;
            });

            var group = GameObject.Find("Mechanisms") ?? new GameObject("Mechanisms");
            Place(emitterPrefab, "Bullet Emitter", new Vector3(9, 1.1f), 180, group.transform);
            Place(spikesPrefab, "Ground Spikes", new Vector3(-8.3f, 0.01f), 0, group.transform);
            Place(platformPrefab, "Gap Moving Platform", new Vector3(0, -1), 0, group.transform);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            EditorSceneManager.SaveScene(player.gameObject.scene);
            Debug.Log("[Restart] Mechanism prefabs created and placed. Edit Point A / Point B under Gap Moving Platform to change its route.");
        }

        private static GameObject CreatePrefab(string name, Func<GameObject> create)
        {
            string path = Folder + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = create();
            try { return PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void ConfigureSpikeCollider(GameObject root)
        {
            var renderer = root.GetComponent<SpriteRenderer>();
            var bounds = renderer.sprite.bounds;
            var shape = root.GetComponent<PolygonCollider2D>();
            shape.offset = Vector2.zero;
            shape.isTrigger = true;
            shape.pathCount = 3;
            float width = bounds.size.x / 3f;
            float bottom = bounds.min.y + 0.02f;
            float tip = bounds.max.y - 0.03f;
            for (int i = 0; i < 3; i++)
            {
                float left = bounds.min.x + i * width;
                var points = new[] { new Vector2(left + width * 0.04f, bottom),
                    new Vector2(left + width * 0.5f, tip), new Vector2(left + width * 0.96f, bottom) };
                for (int j = 0; j < points.Length; j++)
                {
                    if (renderer.flipX) points[j].x = -points[j].x;
                    if (renderer.flipY) points[j].y = -points[j].y;
                }
                shape.SetPath(i, points);
            }
        }

        [MenuItem("Restart/Repair Spike Colliders")]
        public static void RepairSpikeColliders()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            string path = Folder + "Spikes.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ConfigureSpikeCollider(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Spikes.png");
            foreach (var hazard in UnityEngine.Object.FindObjectsByType<DeathHazard2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var renderer = hazard.GetComponent<SpriteRenderer>();
                if (renderer == null || renderer.sprite != sprite) continue;
                var shape = hazard.GetComponent<PolygonCollider2D>();
                if (shape == null) continue;
                Undo.RecordObject(shape, "Align spike collider to sprite");
                ConfigureSpikeCollider(hazard.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(shape);
                EditorSceneManager.MarkSceneDirty(hazard.gameObject.scene);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Restart] Spike colliders now follow the actual sprite bounds and pivot.");
        }
        private static void SetVisual(GameObject target, Sprite sprite, Vector2 size, Color color)
        {
            var renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = 2;
        }
        private static void SetLayer(int index, string name)
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layer = tags.FindProperty("layers").GetArrayElementAtIndex(index);
            if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != name)
                throw new InvalidOperationException($"Layer {index} is already occupied by {layer.stringValue}.");
            layer.stringValue = name;
            tags.ApplyModifiedProperties();
        }
        private static Sprite CreateSprite(string name, int width, int height, float ppu, Vector2 pivot, Func<int, int, Color> pixel)
        {
            string path = Art + name + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++) pixels[y * width + x] = pixel(x, y);
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static void Place(GameObject prefab, string name, Vector3 position, float angle, Transform parent)
        {
            if (GameObject.Find(name) != null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
