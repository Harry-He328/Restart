using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Restart.Editor
{
    // Drives a temporary keyboard through the real input and physics loop in Play Mode.
    public static class RestartMovementVerification
    {
        private static bool running;

        [MenuItem("Restart/Verify Movement In Play Mode")]
        public static async void Verify()
        {
            if (!EditorApplication.isPlaying || running) return;
            running = true;
            Keyboard keyboard = null;
            var lines = new List<string>();
            bool background = Application.runInBackground;
            var behavior = InputSystem.settings.backgroundBehavior;
            var editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                Application.runInBackground = true;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard = InputSystem.AddDevice<Keyboard>("MovementVerification");
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerMotor2D>();
                var body = player.GetComponent<Rigidbody2D>();
                player.Respawn();
                Set(keyboard);
                await Wait(0.3f);
                Check(player.IsGrounded, "Player settles on Tilemap", lines);
                float start = body.position.x;
                Set(keyboard, Key.D);
                await Wait(0.4f);
                Check(body.position.x > start + 1.5f, "D moves right", lines);
                Set(keyboard);
                await Wait(0.2f);
                Check(Mathf.Abs(body.linearVelocity.x) < 0.05f, "Release brakes to a stop", lines);
                Set(keyboard, Key.A);
                await Wait(0.25f);
                Check(body.linearVelocity.x < -7f, "A moves left", lines);
                Set(keyboard);
                player.Respawn();
                await Wait(0.3f);
                float floor = body.position.y;
                Set(keyboard, Key.Space);
                float heldPeak = await Peak(body, 0.8f);
                Set(keyboard);
                await Wait(0.3f);
                Check(player.IsGrounded && heldPeak > floor + 1.8f, "Held jump rises and lands", lines);
                Set(keyboard, Key.Space);
                await Wait(0.06f);
                Set(keyboard);
                float tapPeak = await Peak(body, 0.7f);
                Check(tapPeak < heldPeak - 0.3f, "Tap jump is lower than held jump", lines);
                await Wait(0.3f);
                // Start at the lip with full running speed: the most favorable ordinary jump.
                body.position = new Vector2(-5.05f, floor);
                body.linearVelocity = new Vector2(7.5f, 0);
                Physics2D.SyncTransforms();
                await Wait(0.02f);
                int restarts = player.RestartCount;
                Set(keyboard, Key.D, Key.Space);
                await Wait(1.35f);
                Set(keyboard);
                Check(player.RestartCount > restarts && body.position.x < -5f,
                    "Maximum run-up jump cannot cross 9-unit gap; falling respawns", lines);
                await Wait(0.3f);
                Set(keyboard, Key.D);
                await Wait(0.25f);
                Set(keyboard, Key.Escape);
                await Wait(0.08f);
                Set(keyboard);
                Check(Mathf.Abs(body.position.x + 11f) < 0.1f, "Escape returns to spawn", lines);
                lines.Add($"Measured jump rise: hold={heldPeak - floor:F2}, tap={tapPeak - floor:F2}");
                player.Respawn();
            }
            catch (Exception exception) { lines.Add("FAIL: " + exception); }
            finally
            {
                if (keyboard != null) InputSystem.RemoveDevice(keyboard);
                Application.runInBackground = background;
                InputSystem.settings.backgroundBehavior = behavior;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
                running = false;
                Directory.CreateDirectory("Logs");
                File.WriteAllLines("Logs/movement-verification.txt", lines);
                Debug.Log("[Restart verification]\n" + string.Join("\n", lines));
            }
        }

        private static void Set(Keyboard keyboard, params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private static void Check(bool condition, string label, List<string> lines)
        {
            lines.Add((condition ? "PASS: " : "FAIL: ") + label);
            if (!condition) throw new Exception(label);
        }
        private static async Task Wait(float seconds)
        {
            float until = Time.time + seconds;
            double timeout = EditorApplication.timeSinceStartup + 12;
            while (Time.time < until)
            {
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > timeout) throw new Exception("Play Mode simulation stopped advancing.");
                await Task.Delay(10);
            }
        }
        private static async Task<float> Peak(Rigidbody2D body, float seconds)
        {
            float peak = body.position.y;
            for (int i = 0; i < 40; i++) { await Wait(seconds / 40); peak = Mathf.Max(peak, body.position.y); }
            return peak;
        }
    }
}
