using UnityEngine;

namespace Restart
{
    // Add this marker to a solid Tilemap (or a parent of its collider), on the Climbable layer.
    [DisallowMultipleComponent]
    public sealed class ClimbableWall : MonoBehaviour { }

    public enum PlayerMotionState { Normal, Dashing, Climbing }
}
