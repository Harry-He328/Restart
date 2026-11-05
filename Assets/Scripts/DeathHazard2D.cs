using UnityEngine;

namespace Restart
{
    public sealed class DeathHazard2D : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other) => Hit(other);
        private void OnTriggerStay2D(Collider2D other) => Hit(other);
        private static void Hit(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerMotor2D>();
            if (player != null) player.RequestDeath();
        }
    }
}
