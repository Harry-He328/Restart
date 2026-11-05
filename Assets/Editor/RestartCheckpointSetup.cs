using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Restart.Editor
{
    public static class RestartCheckpointSetup
    {
        private const string CheckpointPath = "Assets/Prefabs/Mechanisms/Checkpoint.prefab";
        private const string EmitterPath = "Assets/Prefabs/Mechanisms/ProjectileEmitter.prefab";

        [MenuItem("Restart/Setup Checkpoints And Slower Emitters")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMotor2D>();
            if (player == null) throw new InvalidOperationException("Open the level scene first.");
            Directory.CreateDirectory("Assets/Prefabs/Mechanisms");
            var emitterPrefab = PrefabUtility.LoadPrefabContents(EmitterPath);
            try
            {
                SetEmitter(emitterPrefab.GetComponent<ProjectileEmitter2D>(), false);
                PrefabUtility.SaveAsPrefabAsset(emitterPrefab, EmitterPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(emitterPrefab); }
            foreach (var emitter in UnityEngine.Object.FindObjectsByType<ProjectileEmitter2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SetEmitter(emitter, emitter.name.StartsWith("Gallery Emitter", StringComparison.Ordinal));
                PrefabUtility.RecordPrefabInstancePropertyModifications(emitter);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPath);
            if (prefab == null)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Prototype/PrototypeTile.png");
                var root = new GameObject("Checkpoint", typeof(BoxCollider2D));
                try
                {
                    var trigger = root.GetComponent<BoxCollider2D>();
                    trigger.isTrigger = true;
                    trigger.size = new Vector2(1.1f, 1.7f);
                    trigger.offset = new Vector2(0, 0.85f);
                    Visual(root.transform, "Base", sprite, new Vector2(0.7f, 0.12f), new Vector2(0, 0.06f), new Color(0.25f, 0.42f, 0.48f));
                    Visual(root.transform, "Pole", sprite, new Vector2(0.07f, 0.85f), new Vector2(-0.16f, 0.48f), new Color(0.7f, 0.85f, 0.9f));
                    var flag = Visual(root.transform, "Flag", sprite, new Vector2(0.45f, 0.28f), new Vector2(0.1f, 0.78f), new Color(0.38f, 0.58f, 0.64f));
                    var spawn = new GameObject("Respawn Point").transform;
                    spawn.SetParent(root.transform, false);
                    spawn.localPosition = new Vector3(0, 0.85f);
                    var checkpoint = root.AddComponent<Checkpoint2D>();
                    var settings = new SerializedObject(checkpoint);
                    settings.FindProperty("respawnPoint").objectReferenceValue = spawn;
                    settings.FindProperty("indicator").objectReferenceValue = flag;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, CheckpointPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var group = GameObject.Find("Checkpoints") ?? new GameObject("Checkpoints");
            if (GameObject.Find("Layered Tutorial") != null)
            {
                Place(prefab, group.transform, "Checkpoint 1 - After Dash", new Vector3(-25.3f, 0));
                Place(prefab, group.transform, "Checkpoint 2 - Projection Entry", new Vector3(-6.2f, 10));
                Place(prefab, group.transform, "Checkpoint 3 - Formal Level Entry", new Vector3(6.4f, 10));
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            EditorSceneManager.SaveScene(player.gameObject.scene);
            Debug.Log("[Restart] Checkpoints placed. Gallery interval 0.9s; formal burst interval 3.5s. Checkpoint activation keeps projection history and starts a fresh timing segment.");
        }

        private static void SetEmitter(ProjectileEmitter2D emitter, bool gallery)
        {
            var settings = new SerializedObject(emitter);
            settings.FindProperty("fireInterval").floatValue = gallery ? 0.9f : 3.5f;
            settings.FindProperty("burstSpacing").floatValue = 0.32f;
            if (gallery)
                settings.FindProperty("initialDelay").floatValue = emitter.name.Contains("Jump Lane") ? 1.95f : 1.5f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(emitter);
        }
        private static SpriteRenderer Visual(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position, Color color)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = 4;
            return renderer;
        }
        private static void Place(GameObject prefab, Transform parent, string name, Vector3 position)
        {
            if (GameObject.Find(name) != null) return;
            var checkpoint = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            checkpoint.name = name;
            checkpoint.transform.SetParent(parent, false);
            checkpoint.transform.position = position;
        }
    }
}
