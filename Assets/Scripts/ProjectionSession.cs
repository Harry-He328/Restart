using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Restart
{
    [DefaultExecutionOrder(100)]
    public sealed class ProjectionSession : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D player;
        [SerializeField] private Transform projectionRoot;
        private readonly List<ProjectionRecording> recordings = new List<ProjectionRecording>();
        private readonly List<ProjectionActor> projections = new List<ProjectionActor>();
        private ProjectionRecording current;
        private ProjectionActor currentProjection;
        private Rigidbody2D playerBody;
        private bool toggleRequested;
        private bool restartedThisStep;
        private float step;
        public int AttemptTick { get; private set; }
        public float AttemptTime => AttemptTick * step;
        public bool IsRecording => current != null;
        public IReadOnlyList<ProjectionRecording> Recordings => recordings;
        public IReadOnlyList<ProjectionActor> Projections => projections;

        private void Awake()
        {
            step = Time.fixedDeltaTime;
            playerBody = player.GetComponent<Rigidbody2D>();
            player.Restarting += OnRestart;
        }
        private void OnDestroy()
        {
            if (player == null) return;
            player.Restarting -= OnRestart;
        }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                toggleRequested = true;
        }
        private void FixedUpdate()
        {
            if (restartedThisStep) { restartedThisStep = false; return; }
            if (toggleRequested)
            {
                toggleRequested = false;
                if (IsRecording) EndRecording();
                else
                {
                    current = new ProjectionRecording(AttemptTick, step);
                    current.Capture(AttemptTick, playerBody);
                    player.SetRecordingRespawnPosition(current.Frames[0].Position);
                    currentProjection = new GameObject($"Projection {projections.Count + 1}").AddComponent<ProjectionActor>();
                    currentProjection.transform.SetParent(projectionRoot, false);
                    currentProjection.Initialize(current, player, projections.Count + 1, true);
                    projections.Add(currentProjection);
                }
            }
            if (IsRecording) current.Capture(AttemptTick, playerBody);
            foreach (var projection in projections) projection.Step(AttemptTick);
            AttemptTick++;
        }
        private void EndRecording()
        {
            if (current == null) return;
            current.Capture(AttemptTick, playerBody);
            if (current.Frames.Count >= 2) recordings.Add(current);
            else if (currentProjection != null)
            {
                projections.Remove(currentProjection);
                Destroy(currentProjection.gameObject);
            }
            current = null;
            currentProjection = null;
        }

        public void CompleteAttempt()
        {
            EndRecording();
            toggleRequested = false;
            LateUpdate();
            enabled = false;
        }
        private void OnRestart()
        {
            EndRecording(); // Capture the old pose before the motor teleports to spawn.
            toggleRequested = false;
            AttemptTick = 0;
            restartedThisStep = true;
            foreach (var projection in projections) projection.ResetPlayback();
        }
        private void LateUpdate()
        {
            foreach (var projection in projections) projection.UpdatePresentation(AttemptTick);
        }
    }
}
