using UnityEngine;

namespace Restart
{
    // Death may reset mechanisms; a short Escape only resets player and projections.
    public abstract class AttemptMechanism : MonoBehaviour
    {
        private PlayerMotor2D player;
        protected virtual void OnEnable()
        {
            if (!Application.isPlaying) return;
            player = FindFirstObjectByType<PlayerMotor2D>();
            if (player != null)
            {
                player.MechanismsResetting += ResetAttempt;
            }
            ResetAttempt();
        }
        protected virtual void OnDisable()
        {
            if (player != null)
            {
                player.MechanismsResetting -= ResetAttempt;
            }
        }
        protected abstract void ResetAttempt();
    }
}
