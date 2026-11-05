using UnityEngine;

namespace Restart
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Checkpoint2D : MonoBehaviour
    {
        [SerializeField, Tooltip("复活时玩家中心的位置，放在安全地面上方约 0.85 格。")]
        private Transform respawnPoint;
        [SerializeField] private SpriteRenderer indicator;
        [SerializeField] private Color inactiveColor = new Color(0.38f, 0.58f, 0.64f);
        [SerializeField] private Color activeColor = new Color(0.3f, 1f, 0.65f);
        public bool IsActivated { get; private set; }

        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
            SetActivated(false);
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerMotor2D>();
            if (player == null) return;
            Vector2 point = respawnPoint != null ? respawnPoint.position : transform.position + Vector3.up * 0.85f;
            player.ActivateCheckpoint(this, point);
        }
        public void SetActivated(bool activated)
        {
            IsActivated = activated;
            if (indicator != null) indicator.color = activated ? activeColor : inactiveColor;
        }
        private void OnDrawGizmosSelected()
        {
            Vector3 point = respawnPoint != null ? respawnPoint.position : transform.position + Vector3.up * 0.85f;
            Gizmos.color = new Color(0.3f, 1f, 0.65f);
            Gizmos.DrawWireCube(point, new Vector3(0.7f, 1.45f, 0));
        }
    }
}
