using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Restart.Editor
{
    public static class RestartTutorialLevelBuilder
    {
        private const string Art = "Assets/Art/Prototype/";
        private const string Mechanisms = "Assets/Prefabs/Mechanisms/";
        private const string Backup = "Assets/Scenes/Backups/RestartPrototype_BeforeTutorial.unity";

        [MenuItem("Restart/Build Layered Tutorial")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (GameObject.Find("Layered Tutorial") != null)
                throw new InvalidOperationException("The layered tutorial already exists. Edit its Tilemaps directly to preserve layout changes.");
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMotor2D>();
            if (player == null) throw new InvalidOperationException("Open RestartPrototype first.");
            var scene = player.gameObject.scene;
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Assets/Scenes/Backups");
            if (!File.Exists(Backup) && !AssetDatabase.CopyAsset(scene.path, Backup))
                throw new InvalidOperationException("Could not back up the current scene.");

            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var gateLayer = tags.FindProperty("layers").GetArrayElementAtIndex(12);
            if (!string.IsNullOrEmpty(gateLayer.stringValue) && gateLayer.stringValue != "ProjectionGate")
                throw new InvalidOperationException("Layer 12 is occupied.");
            gateLayer.stringValue = "ProjectionGate";
            tags.ApplyModifiedProperties();
            var motor = new SerializedObject(player);
            motor.FindProperty("groundLayers").intValue |= 1 << 12;
            motor.ApplyModifiedPropertiesWithoutUndo();

            // Keep every existing collider-bearing level root and all its prefab overrides intact.
            var previousRoots = scene.GetRootGameObjects();
            var formal = new GameObject("Formal Level (Existing)");
            foreach (var root in previousRoots)
            {
                if (root == player.gameObject || root.GetComponentInChildren<Canvas>() != null) continue;
                if (root.GetComponentInChildren<Collider2D>(true) == null) continue;
                root.transform.SetParent(formal.transform, true);
            }
            formal.transform.position = new Vector3(22, 10, 0);

            var rootObject = new GameObject("Layered Tutorial");
            var gridObject = new GameObject("Tutorial Grid", typeof(Grid));
            gridObject.transform.SetParent(rootObject.transform, false);
            var top = AssetDatabase.LoadAssetAtPath<Tile>(Art + "PlatformTop.asset");
            var fill = AssetDatabase.LoadAssetAtPath<Tile>(Art + "PlatformFill.asset");
            var climb = AssetDatabase.LoadAssetAtPath<Tile>(Art + "ClimbableTile.asset");
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Art + "NoFriction.physicsMaterial2D");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "PrototypeTile.png");

            var floor = Map(gridObject.transform, "Lower Floor - Dash Then Jump", 6, Vector3.zero, material);
            Fill(floor, -39, -31, -3, 0, top, fill);
            Fill(floor, -27, -7, -3, 0, top, fill);
            Fill(floor, -40, -39, -4, 5, fill, fill);
            // Two blocks on the same lower floor introduce jumping without changing storeys.
            Fill(floor, -21, -19, 0, 1, top, fill);
            Fill(floor, -16, -14, 0, 2, top, fill);
            var tunnel = Map(gridObject.transform, "Low Tunnel Ceiling - 1.75m Clearance", 6, new Vector3(0, 0.75f), material);
            Fill(tunnel, -40, -24, 1, 4, fill, fill);
            var shaft = Map(gridObject.transform, "Climbable Shaft - Lower To Upper", 8, Vector3.zero, material);
            shaft.gameObject.AddComponent<ClimbableWall>();
            Fill(shaft, -7, -6, 0, 10, climb, climb);

            var upper = Map(gridObject.transform, "Upper Floor - Projection Gallery", 6, Vector3.zero, material);
            Fill(upper, -6, 7, 8, 10, top, fill);
            // The shaft stays open beside the climbing wall; no overhang blocks the ascent.
            Fill(upper, -9, -8, 8, 19, fill, fill);
            var roof = Map(gridObject.transform, "Upper Low Roof - 2.65m Clearance", 6, new Vector3(0, 0.65f), material);
            Fill(roof, -9, 6, 12, 18, fill, fill);

            var pit = new GameObject("Dash Pit - Death Volume", typeof(BoxCollider2D), typeof(DeathHazard2D));
            pit.layer = 10;
            pit.transform.SetParent(rootObject.transform, false);
            pit.transform.position = new Vector3(-29, -4.5f);
            pit.GetComponent<BoxCollider2D>().isTrigger = true;
            pit.GetComponent<BoxCollider2D>().size = new Vector2(4, 2);

            var cover = new GameObject("Gallery Entry Cover", typeof(SpriteRenderer), typeof(BoxCollider2D));
            cover.layer = 6;
            cover.transform.SetParent(rootObject.transform, false);
            cover.transform.position = new Vector3(-4.65f, 10.5f);
            Visual(cover, sprite, new Vector2(0.5f, 1), new Color(0.32f, 0.46f, 0.55f));
            cover.GetComponent<BoxCollider2D>().size = new Vector2(0.5f, 1);

            var lowEmitter = Emitter(rootObject.transform, "Gallery Emitter - Low Lane", new Vector3(5.3f, 11.1f), 1.5f);
            var highEmitter = Emitter(rootObject.transform, "Gallery Emitter - Jump Lane", new Vector3(5.3f, 12.1f), 1.95f);
            var gate = new GameObject("Projection Shield Exit", typeof(SpriteRenderer), typeof(BoxCollider2D));
            gate.layer = 12; // Bullets pass this force field; the player cannot until a projection shields a shot.
            gate.transform.SetParent(rootObject.transform, false);
            gate.transform.position = new Vector3(3.5f, 11.325f);
            Visual(gate, sprite, new Vector2(0.28f, 2.65f), new Color(1f, 0.7f, 0.24f, 0.8f));
            var barrier = gate.GetComponent<BoxCollider2D>();
            barrier.size = new Vector2(0.28f, 2.65f);
            var gateScript = gate.AddComponent<ProjectionShieldGate>();
            var gateSettings = new SerializedObject(gateScript);
            var emitters = gateSettings.FindProperty("emitters");
            emitters.arraySize = 2;
            emitters.GetArrayElementAtIndex(0).objectReferenceValue = lowEmitter;
            emitters.GetArrayElementAtIndex(1).objectReferenceValue = highEmitter;
            gateSettings.FindProperty("barrier").objectReferenceValue = barrier;
            gateSettings.FindProperty("visual").objectReferenceValue = gate.GetComponent<SpriteRenderer>();
            gateSettings.ApplyModifiedPropertiesWithoutUndo();

            // Short in-world key hints replace full-screen explanations.
            Hint(rootObject.transform, "Dash Key", "SHIFT  >", new Vector3(-33.5f, 2.8f), new Color(1, 0.8f, 0.3f));
            Hint(rootObject.transform, "Jump Key", "SPACE  ^", new Vector3(-22.4f, 3.3f), Color.white);
            Hint(rootObject.transform, "Climb Key", "E + W  ^", new Vector3(-9.3f, 4.5f), new Color(0.77f, 0.63f, 1));
            Hint(rootObject.transform, "Record Key", "R  >  ESC", new Vector3(-6.1f, 12f), new Color(0.35f, 1, 0.82f));
            Hint(rootObject.transform, "Shield Exit Hint", "SHIELD  >", new Vector3(2.5f, 13.5f), new Color(1, 0.8f, 0.3f));
            Hint(rootObject.transform, "Upper Route", "PROJECTION", new Vector3(-2f, 15.5f), new Color(0.4f, 0.65f, 0.75f));

            var start = new GameObject("Start - Lower Tunnel");
            start.transform.SetParent(rootObject.transform, false);
            start.transform.position = new Vector3(-35.5f, 0.85f);
            player.transform.position = start.transform.position;
            var playerBody = player.GetComponent<Rigidbody2D>();
            playerBody.position = start.transform.position;
            var camera = Camera.main;
            camera.orthographicSize = 6;
            camera.transform.position = new Vector3(-30.3f, 3.05f, -10);
            var follow = camera.GetComponent<MultiLevelCamera2D>();
            if (follow == null) follow = camera.gameObject.AddComponent<MultiLevelCamera2D>();
            var cameraSettings = new SerializedObject(follow);
            cameraSettings.FindProperty("player").objectReferenceValue = player;
            cameraSettings.ApplyModifiedPropertiesWithoutUndo();
            Physics2D.SyncTransforms();
            RestartCheckpointSetup.Setup();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rootObject;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.pivot = new Vector3(0, 6);
                SceneView.lastActiveSceneView.size = 42;
            }
            Debug.Log("[Restart] Layered tutorial built. Lower: dash / jump / climb. Upper: projection shield gallery, then preserved formal level. Backup: " + Backup);
        }

        private static Tilemap Map(Transform parent, string name, int layer, Vector3 offset, PhysicsMaterial2D material)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.layer = layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.sharedMaterial = material;
            return go.GetComponent<Tilemap>();
        }
        private static void Fill(Tilemap map, int left, int right, int bottom, int top, TileBase cap, TileBase body)
        {
            for (int x = left; x < right; x++)
                for (int y = bottom; y < top; y++) map.SetTile(new Vector3Int(x, y), y == top - 1 ? cap : body);
        }
        private static void Visual(GameObject go, Sprite sprite, Vector2 size, Color color)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = 3;
        }
        private static ProjectileEmitter2D Emitter(Transform parent, string name, Vector3 position, float delay)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Mechanisms + "ProjectileEmitter.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0, 0, 180);
            // Mounted in the background: its body art does not obstruct the gallery exit.
            go.GetComponent<BoxCollider2D>().enabled = false;
            var emitter = go.GetComponent<ProjectileEmitter2D>();
            var settings = new SerializedObject(emitter);
            settings.FindProperty("initialDelay").floatValue = delay;
            settings.FindProperty("fireInterval").floatValue = 0.9f;
            settings.FindProperty("bulletsPerBurst").intValue = 1;
            settings.FindProperty("bulletSpeed").floatValue = 8;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponent<BoxCollider2D>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(emitter);
            return emitter;
        }
        private static void Hint(Transform parent, string name, string content, Vector3 position, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.localScale = Vector3.one * 0.012f;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 15;
            var go = new GameObject("Key", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(root.transform, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(330, 50);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = content;
            text.color = color;
            text.raycastTarget = false;
        }
    }
}
