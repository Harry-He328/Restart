using UnityEngine;

namespace Restart
{
    public sealed class ProjectionActor : MonoBehaviour
    {
        public ProjectionRecording Recording { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsVisible { get; private set; }
        public CapsuleCollider2D Collider { get; private set; }
        public Rigidbody2D Body { get; private set; }
        private SpriteRenderer[] visuals;
        private Color[] originalColors;
        private PlayerMotor2D player;
        private CapsuleCollider2D playerCollider;
        public bool AwaitingRestart { get; private set; }
        private readonly Color tint = new Color(0.26f, 0.91f, 0.85f, 0.7f);

        public void Initialize(ProjectionRecording recording, PlayerMotor2D player, int id, bool awaitingRestart = false)
        {
            Recording = recording;
            this.player = player;
            playerCollider = player.GetComponent<CapsuleCollider2D>();
            gameObject.layer = 7;
            Body = gameObject.AddComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Kinematic;
            Body.gravityScale = 0;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.freezeRotation = true;
            var source = player.GetComponent<CapsuleCollider2D>();
            Collider = gameObject.AddComponent<CapsuleCollider2D>();
            Collider.size = source.size;
            Collider.offset = source.offset;
            Collider.direction = source.direction;
            Collider.sharedMaterial = source.sharedMaterial;
            Collider.enabled = false;
            foreach (var original in player.GetComponentsInChildren<SpriteRenderer>())
            {
                var visual = new GameObject(original.gameObject.name + " Projection", typeof(SpriteRenderer));
                visual.transform.SetParent(transform, false);
                visual.transform.localPosition = player.transform.InverseTransformPoint(original.transform.position);
                visual.transform.localRotation = Quaternion.Inverse(player.transform.rotation) * original.transform.rotation;
                visual.transform.localScale = original.transform == player.transform ? Vector3.one : original.transform.localScale;
                var renderer = visual.GetComponent<SpriteRenderer>();
                renderer.sprite = original.sprite;
                renderer.sharedMaterial = original.sharedMaterial;
                renderer.drawMode = original.drawMode;
                renderer.size = original.size;
                renderer.flipX = original.flipX;
                renderer.flipY = original.flipY;
                renderer.sortingLayerID = original.sortingLayerID;
                renderer.sortingOrder = original.sortingOrder - 2;
                renderer.color = original.transform == player.transform ? Color.white : original.color;
            }
            visuals = GetComponentsInChildren<SpriteRenderer>();
            originalColors = new Color[visuals.Length];
            for (int i = 0; i < visuals.Length; i++) originalColors[i] = visuals[i].color;
            ResetPlayback();
            if (awaitingRestart)
            {
                AwaitingRestart = true;
                Collider.enabled = false;
                UpdatePresentation(0);
            }
        }

        public void ResetPlayback()
        {
            AwaitingRestart = false;
            Collider.enabled = Recording.DurationTicks > 0;
            Body.linearVelocity = Vector2.zero;
            Body.position = Recording.Frames[0].Position;
            Body.rotation = Recording.Frames[0].Rotation;
            transform.position = Body.position;
            ApplyPlayerCollision();
            UpdatePresentation(0);
        }

        // Called before physics: move to the next recorded boundary so kinematic contacts push the player.
        public void Step(int tick)
        {
            if (AwaitingRestart) return;
            bool playing = tick >= 0 && tick < Recording.DurationTicks;
            Collider.enabled = playing;
            if (!playing) { Body.linearVelocity = Vector2.zero; return; }
            ApplyPlayerCollision();
            if (tick == 0)
            {
                Body.position = Recording.Frames[0].Position;
                Body.rotation = Recording.Frames[0].Rotation;
            }
            var target = Recording.Frames[tick + 1];
            Body.MovePosition(target.Position);
            Body.MoveRotation(target.Rotation);
        }

        public void UpdatePresentation(int tick)
        {
            IsVisible = AwaitingRestart || (tick >= 0 && tick < Recording.DurationTicks);
            IsPlaying = IsVisible && !AwaitingRestart;
            for (int i = 0; i < visuals.Length; i++)
            {
                var renderer = visuals[i];
                renderer.enabled = IsVisible;
                var color = originalColors[i] * (AwaitingRestart ? Color.white : tint);
                color.a = AwaitingRestart ? 0.3f : 0.7f;
                renderer.color = color;
            }
            if (!IsVisible || AwaitingRestart) Collider.enabled = false;
        }

        private void ApplyPlayerCollision()
        {
            if (player != null && playerCollider != null && Collider.enabled)
                Physics2D.IgnoreCollision(playerCollider, Collider, player.IgnoresProjectionCollision);
        }
    }
}
