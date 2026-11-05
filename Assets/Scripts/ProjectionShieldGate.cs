using UnityEngine;

namespace Restart
{
    // A tutorial exit: the assigned emitter's bullet must actually hit a playing projection.
    public sealed class ProjectionShieldGate : AttemptMechanism
    {
        [SerializeField] private ProjectileEmitter2D[] emitters;
        [SerializeField] private Collider2D barrier;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField, Min(1)] private int requiredBlocks = 1;
        private int blockedShots;
        public bool IsOpen { get; private set; }

        protected override void OnEnable()
        {
            if (!Application.isPlaying) return;
            base.OnEnable();
            if (emitters != null)
                foreach (var emitter in emitters)
                    if (emitter != null) emitter.BlockedByProjection += OnBlocked;
        }
        protected override void OnDisable()
        {
            if (emitters != null)
                foreach (var emitter in emitters)
                    if (emitter != null) emitter.BlockedByProjection -= OnBlocked;
            base.OnDisable();
        }
        protected override void ResetAttempt()
        {
            blockedShots = 0;
            IsOpen = false;
            Refresh();
        }
        private void OnBlocked(ProjectionActor projection)
        {
            if (IsOpen || projection == null || !projection.Collider.enabled) return;
            blockedShots++;
            if (blockedShots >= requiredBlocks) { IsOpen = true; Refresh(); }
        }
        private void Refresh()
        {
            if (barrier != null) barrier.enabled = !IsOpen;
            if (visual != null) visual.color = IsOpen ? new Color(0.25f, 1f, 0.7f, 0.12f) : new Color(1f, 0.7f, 0.24f, 0.8f);
        }
    }
}
