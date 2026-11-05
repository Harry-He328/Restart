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
    public static class RestartProjectionVerification
    {
        private static bool running;
        [MenuItem("Restart/Verify Projections In Play Mode")]
        public static async void Verify()
        {
            if (!EditorApplication.isPlaying || running) return;
            running = true;
            var results = new List<string>();
            Keyboard keyboard = null;
            bool background = Application.runInBackground;
            var behavior = InputSystem.settings.backgroundBehavior;
            var editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                Application.runInBackground = true;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard = InputSystem.AddDevice<Keyboard>("ProjectionVerification");
                var session = UnityEngine.Object.FindFirstObjectByType<ProjectionSession>();
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerMotor2D>();
                var body = player.GetComponent<Rigidbody2D>();
                Check(session.Recordings.Count == 0, "Fresh Play Mode starts with no history", results);
                player.Respawn();
                await WaitTicks(session, 18);
                int restartCount = player.RestartCount;
                await Press(keyboard, Key.R, session);
                Check(session.IsRecording && player.RestartCount == restartCount, "R begins recording without restarting", results);
                Set(keyboard, Key.D);
                await WaitTicks(session, session.AttemptTick + 14);
                Set(keyboard, Key.D, Key.Space);
                await WaitTicks(session, session.AttemptTick + 8);
                Set(keyboard, Key.D);
                await WaitTicks(session, session.AttemptTick + 12);
                Set(keyboard);
                await Press(keyboard, Key.R, session);
                Check(!session.IsRecording && session.Recordings.Count == 1 && session.Projections.Count == 0,
                    "Second R saves a clip; it appears only on the next attempt", results);
                var first = session.Recordings[0];
                Check(first.StartTick >= 18 && first.Frames.Count > 25, "Clip stores delayed start and full physics trajectory", results);
                await Restart(keyboard, player);
                Check(session.AttemptTick < 6 && session.Projections.Count == 1, "Escape resets time and creates the historical projection", results);
                var ghost = session.Projections[0];
                Check(ghost.IsVisible && ghost.IsPlaying && ghost.Collider.enabled,
                    "Projection immediately plays with collision", results);
                body.position = new Vector2(-14, 0.74f);
                await WaitTicks(session, first.StartTick + 10);
                int frame = session.AttemptTick - first.StartTick;
                float error = Vector2.Distance(ghost.Body.position, first.Frames[frame].Position);
                Check(ghost.IsPlaying && ghost.Collider.enabled && error < 0.025f,
                    $"Playback enables collision and matches the recorded tick (error {error:F4})", results);
                int pausedTick = session.AttemptTick;
                EditorApplication.isPaused = true;
                await Task.Delay(160);
                Check(session.AttemptTick == pausedTick, "Pause freezes attempt time and playback", results);
                EditorApplication.isPaused = false;
                await WaitTicks(session, first.EndTick + 3);
                Check(!ghost.IsVisible && !ghost.Collider.enabled,
                    "Finished projection disappears with collision removed", results);

                await Restart(keyboard, player);
                Check(session.Recordings.Count == 1 && ghost.IsVisible && !ghost.Collider.enabled,
                    "Next attempt restores the previous projection", results);
                body.position = new Vector2(8, 0.74f);
                body.linearVelocity = Vector2.zero;
                await WaitTicks(session, 10);
                await Press(keyboard, Key.R, session);
                await WaitTicks(session, session.AttemptTick + 90);
                await Restart(keyboard, player);
                Check(!session.IsRecording && session.Recordings.Count == 2 && session.Projections.Count == 2,
                    "Escape during recording saves a second clip and retains the first", results);
                var second = session.Recordings[1];
                Check(second.Frames[second.Frames.Count - 1].Position.x > 7,
                    "Restart captures the final pose before teleporting the player", results);
                var support = session.Projections[1];
                body.position = new Vector2(8, 3.2f);
                body.linearVelocity = Vector2.zero;
                await WaitTicks(session, second.StartTick + 45);
                Check(player.IsGrounded && body.position.y > 2 && support.Collider.enabled,
                    "Player can land and stand on a playing projection", results);
                Set(keyboard, Key.Space);
                await WaitTicks(session, session.AttemptTick + 3);
                Check(body.linearVelocity.y > 0 && !player.IsGrounded, "Player can jump from a projection", results);
                Set(keyboard);
                await WaitTicks(session, second.EndTick + 3);
                Check(!support.Collider.enabled, "Support collision ends at its recorded endpoint", results);

                await Restart(keyboard, player);
                body.position = new Vector2(-13, 0.74f);
                body.linearVelocity = Vector2.zero;
                await WaitTicks(session, 16);
                await Press(keyboard, Key.R, session);
                await WaitTicks(session, session.AttemptTick + 12);
                int beforeDeath = player.RestartCount;
                body.position = new Vector2(-13, -9);
                double timeout = EditorApplication.timeSinceStartup + 8;
                while (player.RestartCount == beforeDeath && EditorApplication.timeSinceStartup < timeout) await Task.Delay(10);
                await Task.Delay(40);
                Check(player.RestartCount > beforeDeath && !session.IsRecording && session.Recordings.Count == 3 && session.Projections.Count == 3,
                    "Death ends recording, restarts and retains all three clips", results);
                var third = session.Recordings[2];
                Check(third.Frames[third.Frames.Count - 1].Position.y < -8,
                    "Death recording includes the final death pose", results);
                await Restart(keyboard, player);
                Check(session.Recordings.Count == 3 && session.Projections[2].IsVisible,
                    "Restart without recording keeps every historical clip", results);
                EditorApplication.isPaused = true;
                RestartPrototypeBuilder.Capture();
                File.Copy("Captures/RestartPrototype.png", "Captures/ProjectionMechanic.png", true);
            }
            catch (Exception exception) { results.Add("FAIL: " + exception); }
            finally
            {
                EditorApplication.isPaused = false;
                if (keyboard != null) InputSystem.RemoveDevice(keyboard);
                Application.runInBackground = background;
                InputSystem.settings.backgroundBehavior = behavior;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
                running = false;
                File.WriteAllLines("Logs/projection-verification.txt", results);
                Debug.Log("[Projection verification]\n" + string.Join("\n", results));
            }
        }

        private static void Set(Keyboard keyboard, params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private static async Task Press(Keyboard keyboard, Key key, ProjectionSession session)
        {
            Set(keyboard, key);
            await WaitTicks(session, session.AttemptTick + 3);
            Set(keyboard);
            await WaitTicks(session, session.AttemptTick + 2);
        }
        private static async Task Restart(Keyboard keyboard, PlayerMotor2D player)
        {
            int before = player.RestartCount;
            Set(keyboard, Key.Escape);
            double timeout = EditorApplication.timeSinceStartup + 8;
            while (player.RestartCount == before)
            {
                if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Escape restart timed out.");
                await Task.Delay(10);
            }
            Set(keyboard);
            await Task.Delay(30);
        }
        private static async Task WaitTicks(ProjectionSession session, int target)
        {
            double timeout = EditorApplication.timeSinceStartup + 12;
            while (session.AttemptTick < target)
            {
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > timeout) throw new Exception("Simulation stopped advancing.");
                await Task.Delay(5);
            }
        }
        private static void Check(bool condition, string name, List<string> results)
        {
            results.Add((condition ? "PASS: " : "FAIL: ") + name);
            if (!condition) throw new Exception(name);
        }
    }
}
