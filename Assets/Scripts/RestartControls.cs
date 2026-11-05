using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Restart
{
    public sealed class RestartControls : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D player;
        [SerializeField, Min(0.2f)] private float fullResetHoldSeconds = 1.2f;
        private bool holding;
        private bool reloading;
        private float pressedAt;
        private void Awake() { if (player == null) player = FindFirstObjectByType<PlayerMotor2D>(); }
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || reloading) return;
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                holding = true;
                pressedAt = Time.unscaledTime;
            }
            if (!holding) return;
            if (keyboard.escapeKey.isPressed && Time.unscaledTime - pressedAt >= fullResetHoldSeconds)
            {
                reloading = true;
                holding = false;
                Time.timeScale = 1;
                SceneManager.LoadScene(gameObject.scene.path);
            }
            else if (keyboard.escapeKey.wasReleasedThisFrame)
            {
                holding = false;
                if (player != null && player.enabled) player.RequestRestart();
            }
        }
        private void OnApplicationFocus(bool focused) { if (!focused) holding = false; }
    }
}
