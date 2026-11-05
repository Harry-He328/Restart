using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Restart
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class LevelGoal : MonoBehaviour
    {
        public bool IsCompleted { get; private set; }
        private float previousTimeScale;
        private bool ownsPause;

        private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsCompleted) return;
            var player = other.GetComponentInParent<PlayerMotor2D>();
            if (player == null) return;
            IsCompleted = true;
            var session = FindFirstObjectByType<ProjectionSession>();
            float elapsed = session != null ? session.AttemptTime : Time.timeSinceLevelLoad;
            if (session != null) session.CompleteAttempt();
            player.enabled = false;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            var effect = FindFirstObjectByType<RecordingScreenEffect>();
            if (effect != null) effect.enabled = false;
            previousTimeScale = Time.timeScale;
            ownsPause = true;
            Time.timeScale = 0;
            ShowResult(elapsed);
        }
        private void Update()
        {
            if (!IsCompleted || Keyboard.current == null || !Keyboard.current.enterKey.wasPressedThisFrame) return;
            Time.timeScale = previousTimeScale;
            ownsPause = false;
            SceneManager.LoadScene(gameObject.scene.path);
        }
        private void OnDestroy()
        {
            if (ownsPause) Time.timeScale = previousTimeScale;
        }
        private void ShowResult(float elapsed)
        {
            var canvasObject = new GameObject("Level Complete", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 0.85f);
            var label = new GameObject("Result", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(panel.transform, false);
            label.GetComponent<RectTransform>().sizeDelta = new Vector2(1100, 400);
            var text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.65f, 1, 0.85f);
            text.text = $"LEVEL COMPLETE\n\nATTEMPT TIME  {(int)(elapsed / 60):00}:{elapsed % 60:00.00}\n\nENTER  -  RESTART LEVEL";
        }
    }
}
