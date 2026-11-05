using UnityEngine;
using UnityEngine.Tilemaps;

namespace Restart
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(200)]
    public sealed class MultiLevelCamera2D : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D player;
        [SerializeField] private Vector2 offset = new Vector2(2f, 2.2f);
        [SerializeField] private Rect worldBounds = new Rect(-41, -5, 82, 29);
        [SerializeField, Tooltip("根据当前关卡的 Tilemap 自动计算相机范围，扩建地图后无需手动改边界。")]
        private bool autoFitLevel = true;
        [SerializeField] private Vector2 boundsPadding = new Vector2(2, 3);
        [SerializeField] private float viewHeight = 6f;
        [SerializeField] private float smoothTime = 0.15f;
        private Camera view;
        private Vector3 velocity;
        private bool snap = true;
        private void Awake()
        {
            view = GetComponent<Camera>();
            if (player != null) player.Restarting += RequestSnap;
        }
        private void OnDestroy() { if (player != null) player.Restarting -= RequestSnap; }
        private void Start()
        {
            if (autoFitLevel) RefreshBounds();
        }
        private void RefreshBounds()
        {
            bool found = false;
            Bounds bounds = default;
            foreach (var map in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (map.gameObject.scene != gameObject.scene || map.GetUsedTilesCount() == 0) continue;
                var local = map.localBounds;
                foreach (var point in new[] { local.min, local.max, new Vector3(local.min.x, local.max.y), new Vector3(local.max.x, local.min.y) })
                {
                    var world = map.transform.TransformPoint(point);
                    if (!found) { bounds = new Bounds(world, Vector3.zero); found = true; }
                    else bounds.Encapsulate(world);
                }
            }
            if (!found) return;
            foreach (var goal in FindObjectsByType<LevelGoal>(FindObjectsSortMode.None))
                if (goal.gameObject.scene == gameObject.scene) bounds.Encapsulate(goal.transform.position);
            worldBounds = Rect.MinMaxRect(bounds.min.x - boundsPadding.x, bounds.min.y - boundsPadding.y,
                bounds.max.x + boundsPadding.x, bounds.max.y + boundsPadding.y);
        }
        private void RequestSnap() => snap = true;
        private void LateUpdate()
        {
            if (player == null) return;
            view.orthographicSize = viewHeight;
            Vector3 target = player.transform.position + (Vector3)offset;
            float halfWidth = viewHeight * view.aspect;
            target.x = worldBounds.width <= halfWidth * 2 ? worldBounds.center.x
                : Mathf.Clamp(target.x, worldBounds.xMin + halfWidth, worldBounds.xMax - halfWidth);
            target.y = worldBounds.height <= viewHeight * 2 ? worldBounds.center.y
                : Mathf.Clamp(target.y, worldBounds.yMin + viewHeight, worldBounds.yMax - viewHeight);
            target.z = transform.position.z;
            transform.position = snap ? target : Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
            if (snap) { velocity = Vector3.zero; snap = false; }
        }
    }
}
