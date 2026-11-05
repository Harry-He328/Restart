using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Restart.Editor
{
    public static class LevelEditingMenu
    {
        [MenuItem("Restart/编辑地形", priority = 0)]
        public static void OpenPalette()
        {
            var selection = Selection.activeGameObject;
            var selectedMap = selection != null ? selection.GetComponent<Tilemap>() ?? selection.GetComponentInParent<Tilemap>() ?? selection.GetComponentInChildren<Tilemap>() : null;
            var map = selectedMap != null ? selectedMap
                : Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None).FirstOrDefault(m => m.gameObject.layer == 6);
            if (map == null) { Debug.LogWarning("当前场景没有可绘制的地形。"); return; }
            Selection.activeGameObject = map.gameObject;
            GridPaintingState.scenePaintTarget = map.gameObject;
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            var palette = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TilePalettes/关卡调色板.prefab");
            if (palette != null) GridPaintingState.palette = palette;
        }

    }
}
