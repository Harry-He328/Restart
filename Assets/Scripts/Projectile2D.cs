using UnityEngine;

namespace Restart
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class Projectile2D : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float lifetime = 6f;
        [SerializeField] private LayerMask hitLayers = (1 << 0) | (1 << 6) | (1 << 7) | (1 << 8) | (1 << 9);
        private Rigidbody2D body;
        private CircleCollider2D shape;
        private Vector2 velocity;
        private float age;
        private bool retired;
        private ProjectileEmitter2D owner;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<CircleCollider2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            shape.isTrigger = true;
        }
        public void Launch(Vector2 direction, float speed, ProjectileEmitter2D source = null)
        {
            velocity = direction.normalized * speed;
            owner = source;
        }

        private void FixedUpdate()
        {
            if (retired) return;
            age += Time.fixedDeltaTime;
            if (age >= lifetime) { Retire(); return; }
            var delta = velocity * Time.fixedDeltaTime;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = hitLayers, useTriggers = false };
            float radius = shape.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            Vector2 center = transform.TransformPoint(shape.offset);
            int count = Physics2D.CircleCast(center, radius, delta.normalized, filter, hits, delta.magnitude);
            int nearest = -1;
            float distance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].rigidbody == body || hits[i].distance >= distance) continue;
                nearest = i;
                distance = hits[i].distance;
            }
            if (nearest >= 0)
            {
                Hit(hits[nearest].collider);
                return;
            }
            body.MovePosition(body.position + delta);
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (retired || other.isTrigger || (hitLayers.value & (1 << other.gameObject.layer)) == 0) return;
            Hit(other);
        }
        private void Hit(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerMotor2D>();
            if (player != null) player.RequestDeath();
            var projection = other.GetComponentInParent<ProjectionActor>();
            if (projection != null && owner != null) owner.ReportProjectionBlock(projection);
            // Projections block bullets, but their recorded trajectory is never changed.
            Retire();
        }
        public void Retire()
        {
            if (retired) return;
            retired = true;
            if (shape != null) shape.enabled = false;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
