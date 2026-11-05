using UnityEngine;
using UnityEngine.Tilemaps;

namespace Restart
{
    // Standalone wall prefabs can mark the whole collider; a mixed Tilemap marks specific tiles.
    [DisallowMultipleComponent]
    public sealed class ClimbableWall : MonoBehaviour
    {
        [SerializeField, Tooltip("混合地形开启此项，只让指定地块可攀爬。独立墙体预制体可关闭。")]
        private bool restrictToTiles;
        [SerializeField] private TileBase[] climbableTiles;
        private Tilemap map;

        public bool CanClimbAt(Vector2 point, Vector2 outwardNormal)
        {
            if (!restrictToTiles) return true;
            if (map == null) map = GetComponent<Tilemap>();
            if (map == null || climbableTiles == null) return false;
            // Sample just inside the surface so tile boundaries resolve to the wall, not the empty cell.
            var tile = map.GetTile(map.WorldToCell(point - outwardNormal * 0.03f));
            if (tile == null) return false;
            foreach (var climbable in climbableTiles)
                if (tile == climbable) return true;
            return false;
        }
    }

    public enum PlayerMotionState { Normal, Dashing, Climbing }
}
