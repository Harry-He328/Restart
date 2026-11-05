using UnityEngine;

namespace Restart
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(200)]
    public sealed class MultiLevelCamera2D : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D player;
        [SerializeField] private Vector2 offset = new Vector2(2f, 2.2f);
        [SerializeField] private Rect worldBounds = new Rect(-41, -5, 82, 29);
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
        private void RequestSnap() => snap = true;
        private void LateUpdate()
        {
            if (player == null) return;
            view.orthographicSize = viewHeight;
            Vector3 target = player.transform.position + (Vector3)offset;
            float halfWidth = viewHeight * view.aspect;
            target.x = Mathf.Clamp(target.x, worldBounds.xMin + halfWidth, worldBounds.xMax - halfWidth);
            target.y = Mathf.Clamp(target.y, worldBounds.yMin + viewHeight, worldBounds.yMax - viewHeight);
            target.z = transform.position.z;
            transform.position = snap ? target : Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
            if (snap) { velocity = Vector3.zero; snap = false; }
        }
    }
}
