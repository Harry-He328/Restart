using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Restart.Editor
{
    public static class RestartTraversalVerification
    {
        private static bool running;
        [MenuItem("Restart/Verify Dash And Climbing In Play Mode")]
        public static async void Verify()
        {
            if (!EditorApplication.isPlaying || running) return;
            running = true;
            Keyboard keyboard = null;
            var results = new List<string>();
            bool background = Application.runInBackground;
            var behavior = InputSystem.settings.backgroundBehavior;
            var editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                Application.runInBackground = true;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard = InputSystem.AddDevice<Keyboard>("TraversalVerification");
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerMotor2D>();
                var body = player.GetComponent<Rigidbody2D>();
                var session = UnityEngine.Object.FindFirstObjectByType<ProjectionSession>();
                player.Respawn();
                await Wait(0.3f);
                Teleport(body, new Vector2(-10, 3));
                body.linearVelocity = Vector2.zero;
                Set(keyboard, Key.D, Key.LeftShift);
                await Wait(0.06f);
                Check(player.IsDashing && !player.DashAvailable && Mathf.Abs(body.linearVelocity.x - 24f) < 0.1f,
                    "Shift starts a fast horizontal air dash and consumes the charge", results);
                Set(keyboard);
                await Wait(0.2f);
                Check(!player.IsDashing && Mathf.Abs(body.linearVelocity.x) <= 7.6f, "Dash ends without retaining dash speed", results);
                Set(keyboard, Key.LeftShift);
                await Wait(0.04f);
                Check(!player.IsDashing && !player.DashAvailable, "Cannot dash twice before landing", results);
                Set(keyboard);
                await Wait(0.8f);
                Check(player.IsGrounded && player.DashAvailable, "Landing restores the dash charge", results);

                player.Respawn();
                Teleport(body, new Vector2(-11, 3));
                body.linearVelocity = Vector2.zero;
                Set(keyboard, Key.D, Key.W, Key.LeftShift);
                await Wait(0.06f);
                Check(player.IsDashing && body.linearVelocity.x > 16f && body.linearVelocity.y > 16f
                    && Mathf.Abs(body.linearVelocity.magnitude - 24f) < 0.15f,
                    "Diagonal dash has equal total speed, with no diagonal speed boost", results);
                Set(keyboard);
                await Wait(0.25f);
                player.Respawn();
                Teleport(body, new Vector2(10.55f, 2));
                Set(keyboard, Key.D, Key.LeftShift);
                await Wait(0.12f);
                Check(body.position.x < 10.68f && !player.IsDashing, "Dash stops at a solid wall without tunnelling through it", results);
                Set(keyboard);

                player.Respawn();
                Teleport(body, new Vector2(10.64f, 2));
                body.linearVelocity = Vector2.zero;
                Set(keyboard, Key.E, Key.W);
                await Wait(0.3f);
                Check(player.IsClimbing && body.position.y > 3, "Hold E and W to climb the marked Tilemap wall", results);
                Set(keyboard, Key.E);
                await Wait(0.06f);
                float hangingY = body.position.y;
                await Wait(0.14f);
                Check(player.IsClimbing && Mathf.Abs(body.position.y - hangingY) < 0.05f, "Holding E with no vertical input hangs on the wall", results);
                Set(keyboard, Key.E, Key.S);
                await Wait(0.16f);
                Check(player.IsClimbing && body.position.y < hangingY - 0.4f, "S climbs down", results);
                Set(keyboard);
                await Wait(0.08f);
                Check(!player.IsClimbing && body.linearVelocity.y < 0, "Releasing E releases the wall and restores gravity", results);
                Set(keyboard, Key.E, Key.Space);
                await Wait(0.06f);
                Check(!player.IsClimbing && body.linearVelocity.x < -8 && body.linearVelocity.y > 5,
                    "Space wall-jumps away, with a short lock against immediate re-grabbing", results);
                Set(keyboard);
                player.Respawn();
                Teleport(body, new Vector2(3.64f, -2));
                Set(keyboard, Key.E, Key.W);
                await Wait(0.12f);
                Check(!player.IsClimbing, "Ordinary Tilemap platform sides cannot be climbed", results);
                Set(keyboard);

                player.Respawn();
                Teleport(body, new Vector2(10.64f, 4.8f));
                Set(keyboard, Key.E, Key.W);
                await Wait(0.9f);
                Check(body.position.y > 6.6f && body.position.x > 11.1f, "Climbing over the top assists the player onto the ledge", results);
                Set(keyboard);

                // Record actual climbing and a dash, then compare a dash pose after restarting.
                player.Respawn();
                Teleport(body, new Vector2(10.64f, 2));
                await Wait(0.1f);
                Set(keyboard, Key.E, Key.W, Key.R);
                await Wait(0.06f);
                Set(keyboard, Key.E, Key.W);
                await Wait(0.2f);
                Set(keyboard, Key.A, Key.LeftShift);
                await Wait(0.1f);
                Set(keyboard, Key.A);
                await Wait(0.25f);
                Set(keyboard, Key.R);
                await Wait(0.06f);
                Set(keyboard);
                await Wait(0.04f);
                var record = session.Recordings.Last();
                Check(record.Frames.Any(f => f.MotionState == PlayerMotionState.Climbing)
                    && record.Frames.Any(f => f.MotionState == PlayerMotionState.Dashing),
                    "Recording captures climbing and dashing motion states", results);
                player.Respawn();
                Teleport(body, new Vector2(-11, 0.75f));
                int dashFrame = Enumerable.Range(0, record.Frames.Count).First(i => record.Frames[i].MotionState == PlayerMotionState.Dashing) + 2;
                await WaitUntilTick(session, record.StartTick + dashFrame);
                var ghost = session.Projections.Last();
                float error = Vector2.Distance(ghost.Body.position, record.Frames[session.AttemptTick - record.StartTick].Position);
                Check(ghost.IsPlaying && error < 0.025f, $"Projection replays the dash trajectory at the same tick (error {error:F4})", results);

                player.Respawn();
                Teleport(body, new Vector2(10.64f, 2));
                Set(keyboard, Key.E, Key.W);
                await Wait(0.2f);
                EditorApplication.isPaused = true;
                RestartPrototypeBuilder.Capture();
                File.Copy("Captures/RestartPrototype.png", "Captures/DashAndClimbing.png", true);
                EditorApplication.isPaused = false;
                Set(keyboard);
                player.Respawn();
                Check(player.DashAvailable && !player.IsDashing && !player.IsClimbing, "Restart resets traversal state and dash availability", results);
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
                File.WriteAllLines("Logs/traversal-verification.txt", results);
                Debug.Log("[Traversal verification]\n" + string.Join("\n", results));
            }
        }

        private static void Set(Keyboard keyboard, params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private static void Teleport(Rigidbody2D body, Vector2 position)
        {
            body.position = position;
            body.transform.position = position;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }
        private static async Task Wait(float seconds)
        {
            float until = Time.time + seconds;
            double timeout = EditorApplication.timeSinceStartup + 10;
            while (Time.time < until)
            {
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > timeout) throw new Exception("Simulation stopped advancing.");
                await Task.Delay(5);
            }
        }
        private static async Task WaitUntilTick(ProjectionSession session, int tick)
        {
            double timeout = EditorApplication.timeSinceStartup + 10;
            while (session.AttemptTick < tick)
            {
                if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Replay timed out.");
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

