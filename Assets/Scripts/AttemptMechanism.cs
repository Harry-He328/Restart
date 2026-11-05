using UnityEngine;

namespace Restart
{
    // Initialize with the scene. Player death and short Escape never reset mechanisms.
    public abstract class AttemptMechanism : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            if (!Application.isPlaying) return;
            ResetAttempt();
        }
        protected virtual void OnDisable()
        {
        }
        protected abstract void ResetAttempt();
    }
}
