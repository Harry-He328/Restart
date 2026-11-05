using UnityEngine;

namespace Restart
{
    [DefaultExecutionOrder(-150)]
    public sealed class MovingPlatform2D : AttemptMechanism
    {
        [SerializeField, Tooltip("实际移动并承载玩家的子物体，不要把整个根物体放在这里。")]
        private Rigidbody2D platformBody;
        [SerializeField, Tooltip("起点：与 Platform 同级，放在预制体根节点下。")]
        private Transform pointA;
        [SerializeField, Tooltip("终点：可以直接在场景视图拖动，支持水平、竖直或斜线移动。")]
        private Transform pointB;
        [SerializeField, Min(0.01f), Tooltip("移动速度，单位为每秒格数。")]
        private float speed = 2f;
        [SerializeField, Min(0), Tooltip("到达端点后停留的秒数，也用于本轮开始时的起点等待。")]
        private float endpointPause = 0.4f;
        private KinematicSurface2D surface;
        private bool movingToB = true;
        private float pauseRemaining;

        private void Awake()
        {
            if (platformBody == null) return;
            platformBody.bodyType = RigidbodyType2D.Kinematic;
            platformBody.gravityScale = 0;
            platformBody.freezeRotation = true;
            platformBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            platformBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            surface = platformBody.GetComponent<KinematicSurface2D>();
        }
        protected override void ResetAttempt()
        {
            movingToB = true;
            pauseRemaining = endpointPause;
            if (platformBody == null || pointA == null) return;
            platformBody.position = pointA.position;
            platformBody.transform.position = pointA.position;
            platformBody.MovePosition(pointA.position); // Replace any move queued earlier in the death step.
            platformBody.linearVelocity = Vector2.zero;
            if (surface != null) surface.Velocity = Vector2.zero;
        }
        private void FixedUpdate()
        {
            if (platformBody == null || pointA == null || pointB == null) return;
            if (surface != null) surface.Velocity = Vector2.zero;
            if (pauseRemaining > 0)
            {
                pauseRemaining -= Time.fixedDeltaTime;
                platformBody.MovePosition(platformBody.position);
                return;
            }
            Vector2 target = movingToB ? pointB.position : pointA.position;
            Vector2 next = Vector2.MoveTowards(platformBody.position, target, speed * Time.fixedDeltaTime);
            if (surface != null) surface.Velocity = (next - platformBody.position) / Time.fixedDeltaTime;
            platformBody.MovePosition(next);
            if ((next - target).sqrMagnitude < 0.000001f)
            {
                movingToB = !movingToB;
                pauseRemaining = endpointPause;
            }
        }
        private void OnDrawGizmosSelected()
        {
            if (pointA == null || pointB == null) return;
            Gizmos.color = new Color(1, 0.8f, 0.25f);
            Gizmos.DrawLine(pointA.position, pointB.position);
            Gizmos.DrawWireSphere(pointA.position, 0.16f);
            Gizmos.DrawWireSphere(pointB.position, 0.16f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(pointA.position + Vector3.up * 0.25f, "A");
            UnityEditor.Handles.Label(pointB.position + Vector3.up * 0.25f, "B");
#endif
        }
    }
}
