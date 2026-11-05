using UnityEngine;

namespace Restart
{
    // Mechanisms share the player's restart boundary, including death and Escape.
    public abstract class AttemptMechanism : MonoBehaviour
    {
        private PlayerMotor2D player;
        protected virtual void OnEnable()
        {
            if (!Application.isPlaying) return;
            player = FindFirstObjectByType<PlayerMotor2D>();
            if (player != null)
            {
                player.Restarting += ResetAttempt;
            }
            ResetAttempt();
        }
        protected virtual void OnDisable()
        {
            if (player != null)
            {
                player.Restarting -= ResetAttempt;
            }
        }
        protected abstract void ResetAttempt();
    }
}
