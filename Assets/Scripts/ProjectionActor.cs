using UnityEngine;
using UnityEngine.UI;

namespace Restart
{
    public sealed class ProjectionActor : MonoBehaviour
    {
        public ProjectionRecording Recording { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsVisible { get; private set; }
        public CapsuleCollider2D Collider { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public Text Countdown { get; private set; }
        private SpriteRenderer[] visuals;
        private readonly Color tint = new Color(0.26f, 0.91f, 0.85f, 0.7f);
        private int number;

        public void Initialize(ProjectionRecording recording, PlayerMotor2D player, int id)
        {
            Recording = recording;
            number = id;
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
            var label = new GameObject("Countdown", typeof(RectTransform), typeof(Canvas));
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0, source.size.y * 0.5f + 0.45f, 0);
            label.transform.localScale = Vector3.one * 0.012f;
            var canvas = label.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            var textObject = new GameObject("Time", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(label.transform, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(230, 45);
            Countdown = textObject.GetComponent<Text>();
            Countdown.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Countdown.fontSize = 25;
            Countdown.alignment = TextAnchor.MiddleCenter;
            Countdown.raycastTarget = false;
            Countdown.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            ResetPlayback();
        }

        public void ResetPlayback()
        {
            Collider.enabled = false;
            Body.linearVelocity = Vector2.zero;
            Body.position = Recording.Frames[0].Position;
            Body.rotation = Recording.Frames[0].Rotation;
            transform.position = Body.position;
            UpdatePresentation(0);
        }

        // Called before physics: move to the next recorded boundary so kinematic contacts push the player.
        public void Step(int tick)
        {
            bool playing = tick >= Recording.StartTick && tick < Recording.EndTick;
            Collider.enabled = playing;
            if (!playing) { Body.linearVelocity = Vector2.zero; return; }
            if (tick == Recording.StartTick)
            {
                Body.position = Recording.Frames[0].Position;
                Body.rotation = Recording.Frames[0].Rotation;
            }
            var target = Recording.Frames[tick - Recording.StartTick + 1];
            Body.MovePosition(target.Position);
            Body.MoveRotation(target.Rotation);
        }

        public void UpdatePresentation(int tick)
        {
            IsVisible = tick < Recording.EndTick;
            IsPlaying = IsVisible && tick >= Recording.StartTick;
            foreach (var renderer in visuals)
            {
                renderer.enabled = IsVisible;
                var color = renderer.color;
                color.a = IsPlaying ? 0.7f : 0.24f;
                renderer.color = color;
            }
            Countdown.gameObject.SetActive(IsVisible);
            if (!IsVisible) { Collider.enabled = false; return; }
            float remaining = (IsPlaying ? Recording.EndTick - tick : Recording.StartTick - tick) * Recording.Step;
            Countdown.text = $"#{number}  {remaining:0.00}s";
            Countdown.color = IsPlaying ? new Color(0.4f, 1, 0.85f) : new Color(0.65f, 0.8f, 0.86f);
        }
    }
}
