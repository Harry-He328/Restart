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
        private readonly Color tint = new Color(0.26f, 0.91f, 0.85f, 0.7f);

        public void Initialize(ProjectionRecording recording, PlayerMotor2D player, int id)
        {
            Recording = recording;
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
                renderer.color = (original.transform == player.transform ? Color.white : original.color) * tint;
            }
            visuals = GetComponentsInChildren<SpriteRenderer>();
            ResetPlayback();
        }

        public void ResetPlayback()
        {
            Collider.enabled = Recording.DurationTicks > 0;
            Body.linearVelocity = Vector2.zero;
            Body.position = Recording.Frames[0].Position;
            Body.rotation = Recording.Frames[0].Rotation;
            transform.position = Body.position;
            UpdatePresentation(0);
        }

        // Called before physics: move to the next recorded boundary so kinematic contacts push the player.
        public void Step(int tick)
        {
            bool playing = tick >= 0 && tick < Recording.DurationTicks;
            Collider.enabled = playing;
            if (!playing) { Body.linearVelocity = Vector2.zero; return; }
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
            IsVisible = tick >= 0 && tick < Recording.DurationTicks;
            IsPlaying = IsVisible;
            foreach (var renderer in visuals)
            {
                renderer.enabled = IsVisible;
                var color = renderer.color;
                color.a = 0.7f;
                renderer.color = color;
            }
            if (!IsVisible) Collider.enabled = false;
        }
    }
}
