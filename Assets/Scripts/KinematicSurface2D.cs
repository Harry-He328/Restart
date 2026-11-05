using UnityEngine;

namespace Restart
{
    // Published before the motor runs, rather than waiting for the physics solver's velocity.
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class KinematicSurface2D : MonoBehaviour
    {
        public Vector2 Velocity { get; internal set; }
    }
}
