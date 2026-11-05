using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Restart.Editor
{
    public static class RestartTraversalSetup
    {
        public const string PrefabPath = "Assets/Prefabs/ClimbableWall.prefab";

        [MenuItem("Restart/Setup Dash And Climbing")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            var player = Object.FindFirstObjectByType<PlayerMotor2D>();
            if (player == null) throw new System.InvalidOperationException("Open the prototype scene first.");
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layer = tags.FindProperty("layers").GetArrayElementAtIndex(8);
            if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != "Climbable")
                throw new System.InvalidOperationException("Layer 8 is occupied.");
            layer.stringValue = "Climbable";
            tags.ApplyModifiedProperties();
            var settings = new SerializedObject(player);
            settings.FindProperty("groundLayers").intValue |= 1 << 8;
            settings.FindProperty("climbableLayers").intValue = 1 << 8;
            settings.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory("Assets/Prefabs");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                const string tilePath = "Assets/Art/Prototype/ClimbableTile.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/PrototypeTile.png");
                    tile.color = new Color(0.58f, 0.43f, 0.86f);
                    tile.colliderType = Tile.ColliderType.Grid;
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                var root = new GameObject("ClimbableWall", typeof(Grid));
                root.layer = 8;
                try
                {
                    var wall = new GameObject("Climbable Tilemap", typeof(Tilemap), typeof(TilemapRenderer), typeof(ClimbableWall));
                    wall.layer = 8;
                    wall.transform.SetParent(root.transform, false);
                    var map = wall.GetComponent<Tilemap>();
                    for (int y = 0; y < 6; y++) map.SetTile(new Vector3Int(0, y), tile);
                    wall.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                    wall.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
                    var composite = wall.AddComponent<CompositeCollider2D>();
                    composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                    composite.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Art/Prototype/NoFriction.physicsMaterial2D");
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally { Object.DestroyImmediate(root); }
            }
            Place(prefab, "Climbable Wall Left", new Vector3(-15, 0));
            Place(prefab, "Climbable Wall Right", new Vector3(11, 0));
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            EditorSceneManager.SaveScene(player.gameObject.scene);
            Debug.Log("[Restart] Shift dashes; hold E + W/S to climb purple Tilemap walls; Space wall-jumps. Prefab: " + PrefabPath);
        }

        private static void Place(GameObject prefab, string name, Vector3 position)
        {
            if (GameObject.Find(name) != null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.position = position;
        }
    }
}
