using System.Collections.Generic;
using UnityEngine;

namespace Restart
{
    public readonly struct ProjectionFrame
    {
        public readonly Vector2 Position;
        public readonly float Rotation;
        public readonly PlayerMotionState MotionState;
        public ProjectionFrame(Rigidbody2D body)
        {
            Position = body.position;
            Rotation = body.rotation;
            var motor = body.GetComponent<PlayerMotor2D>();
            MotionState = motor != null ? motor.MotionState : PlayerMotionState.Normal;
        }
    }

    // Samples are actual physics poses, never re-simulated input. The final sample is the endpoint.
    public sealed class ProjectionRecording
    {
        private readonly List<ProjectionFrame> frames = new List<ProjectionFrame>();
        public int StartTick { get; }
        public int EndTick => StartTick + frames.Count - 1;
        public float Step { get; }
        public IReadOnlyList<ProjectionFrame> Frames => frames;
        public ProjectionRecording(int startTick, float step) { StartTick = startTick; Step = step; }

        public void Capture(int tick, Rigidbody2D body)
        {
            int index = tick - StartTick;
            var frame = new ProjectionFrame(body);
            if (index == frames.Count) frames.Add(frame);
            else if (index == frames.Count - 1) frames[index] = frame;
            else throw new System.InvalidOperationException("Recording requires consecutive physics ticks.");
        }
    }
}
