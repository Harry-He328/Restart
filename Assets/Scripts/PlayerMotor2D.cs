using UnityEngine;
using UnityEngine.InputSystem;

namespace Restart
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7.5f;
        [SerializeField] private float groundAcceleration = 75f;
        [SerializeField] private float groundBraking = 95f;
        [SerializeField] private float airAcceleration = 45f;
        [Header("Jump")]
        [SerializeField] private float jumpSpeed = 13f;
        [SerializeField] private float gravity = 38f;
        [SerializeField] private float fallingGravityMultiplier = 1.45f;
        [SerializeField] private float releasedJumpGravityMultiplier = 2.3f;
        [SerializeField] private float maximumFallSpeed = 24f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;
        [SerializeField] private LayerMask groundLayers = 1 << 6;
        [Header("Dash")]
        [SerializeField] private float dashSpeed = 24f;
        [SerializeField] private float dashDuration = 0.18f;
        [SerializeField] private float dashCooldown = 0.15f;
        [Header("Climbing - hold E on marked walls")]
        [SerializeField] private LayerMask climbableLayers = 1 << 8;
        [SerializeField] private float climbSpeed = 4.5f;
        [SerializeField] private float wallJumpHorizontalSpeed = 10.5f;
        [SerializeField] private float wallJumpLockTime = 0.16f;
        [Header("Prototype restart")]
        [SerializeField] private float deathHeight = -8f;

        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private Vector2 spawnPosition;
        private float horizontalInput;
        private bool jumpHeld;
        private float lastGrounded = float.NegativeInfinity;
        private float lastJumpPressed = float.NegativeInfinity;
        private bool jumpConsumed;
        private bool restartRequested;
        private bool deathRequested;
        private Rigidbody2D groundSupport;
        private float verticalInput;
        private bool grabHeld;
        private bool dashRequested;
        private Vector2 dashInput;
        private Vector2 dashDirection;
        private int dashSteps;
        private float nextDashTime;
        private float wallJumpUntil;
        private float ledgeAssistUntil;
        private int wallDirection;
        private int facingDirection = 1;
        private SpriteRenderer appearance;
        private Color normalColor;
        public bool IsGrounded { get; private set; }
        public bool IsDashing { get; private set; }
        public bool IsClimbing { get; private set; }
        public bool DashAvailable { get; private set; } = true;
        public PlayerMotionState MotionState => IsDashing ? PlayerMotionState.Dashing : IsClimbing ? PlayerMotionState.Climbing : PlayerMotionState.Normal;
        public int RestartCount { get; private set; }
        public event System.Action Restarting;
        public event System.Action CheckpointActivated;
        public Checkpoint2D ActiveCheckpoint { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            spawnPosition = body.position;
            appearance = GetComponent<SpriteRenderer>();
            if (appearance != null) normalColor = appearance.color;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            float keyboardAxis = 0f;
            float keyboardVertical = 0f;
            if (keyboard != null)
            {
                keyboardAxis = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                restartRequested |= keyboard.escapeKey.wasPressedThisFrame;
                keyboardVertical = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            }
            float stick = gamepad != null ? gamepad.leftStick.x.ReadValue() : 0f;
            horizontalInput = Mathf.Abs(stick) > 0.15f ? stick : keyboardAxis;
            float verticalStick = gamepad != null ? gamepad.leftStick.y.ReadValue() : 0f;
            verticalInput = Mathf.Abs(verticalStick) > 0.15f ? verticalStick : keyboardVertical;
            if (Mathf.Abs(horizontalInput) > 0.15f) facingDirection = horizontalInput > 0 ? 1 : -1;
            grabHeld = (keyboard != null && keyboard.eKey.isPressed) || (gamepad != null && gamepad.leftShoulder.isPressed);
            bool dashPressed = (keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame))
                || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
            if (dashPressed)
            {
                dashRequested = true;
                dashInput = new Vector2(Mathf.Abs(horizontalInput) > 0.15f ? Mathf.Sign(horizontalInput) : 0,
                    Mathf.Abs(verticalInput) > 0.15f ? Mathf.Sign(verticalInput) : 0);
            }
            bool pressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
            pressed |= gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
            if (pressed) lastJumpPressed = Time.time;
            jumpHeld = keyboard != null && keyboard.spaceKey.isPressed;
            jumpHeld |= gamepad != null && gamepad.buttonSouth.isPressed;
        }

        private void FixedUpdate()
        {
            if (restartRequested || deathRequested || body.position.y < deathHeight)
            {
                Respawn();
                return;
            }

            var filter = new ContactFilter2D { useLayerMask = true, layerMask = groundLayers, useTriggers = false };
            int count = capsule.Cast(Vector2.down, filter, groundHits, 0.065f);
            IsGrounded = false;
            Vector2 supportVelocity = Vector2.zero;
            Rigidbody2D nextSupport = null;
            for (int i = 0; i < count; i++)
            {
                if (groundHits[i].normal.y <= 0.65f) continue;
                var support = groundHits[i].rigidbody;
                Vector2 candidateVelocity = Vector2.zero;
                if (support != null && support.bodyType == RigidbodyType2D.Kinematic)
                {
                    var surface = support.GetComponent<KinematicSurface2D>();
                    candidateVelocity = surface != null ? surface.Velocity : support.linearVelocity;
                }
                if (body.linearVelocity.y > candidateVelocity.y + 0.1f && (support == null || support != groundSupport)) continue;
                IsGrounded = true;
                nextSupport = support;
                supportVelocity = candidateVelocity;
                break;
            }
            groundSupport = nextSupport;

            if (IsGrounded)
            {
                lastGrounded = Time.time;
                jumpConsumed = false;
                if (!IsDashing && Time.time >= nextDashTime) DashAvailable = true;
            }
            if (IsDashing && dashSteps <= 0)
            {
                IsDashing = false;
                var exit = body.linearVelocity;
                exit.x = Mathf.Clamp(exit.x, -moveSpeed, moveSpeed);
                exit.y = Mathf.Clamp(exit.y, -6f, 6f);
                body.linearVelocity = exit;
            }
            if (dashRequested)
            {
                dashRequested = false;
                if (DashAvailable && !IsDashing && Time.time >= nextDashTime)
                {
                    dashDirection = dashInput.sqrMagnitude > 0 ? dashInput.normalized : new Vector2(facingDirection, 0);
                    dashSteps = Mathf.Max(1, Mathf.CeilToInt(dashDuration / Time.fixedDeltaTime));
                    IsDashing = true;
                    IsClimbing = false;
                    DashAvailable = false;
                    lastJumpPressed = lastGrounded = float.NegativeInfinity;
                    jumpConsumed = true;
                    nextDashTime = Time.time + dashDuration + dashCooldown;
                }
            }
            if (IsDashing)
            {
                var dashFilter = new ContactFilter2D { useLayerMask = true, layerMask = groundLayers, useTriggers = false };
                float speed = dashSpeed;
                int hits = capsule.Cast(dashDirection, dashFilter, groundHits, dashSpeed * Time.fixedDeltaTime + 0.015f);
                for (int i = 0; i < hits; i++)
                {
                    if (Vector2.Dot(groundHits[i].normal, dashDirection) > -0.05f) continue;
                    speed = Mathf.Min(speed, Mathf.Max(0, groundHits[i].distance - 0.01f) / Time.fixedDeltaTime);
                }
                body.linearVelocity = dashDirection * speed;
                dashSteps--;
                if (speed < dashSpeed - 0.1f) dashSteps = 0;
                IsGrounded = false;
                return;
            }

            int touchingWall = FindClimbableWall();
            bool wasClimbing = IsClimbing;
            IsClimbing = grabHeld && touchingWall != 0 && Time.time >= wallJumpUntil
                && (!IsGrounded || Mathf.Abs(verticalInput) > 0.1f);
            if (IsClimbing)
            {
                wallDirection = touchingWall;
                if (Time.time - lastJumpPressed <= jumpBufferTime)
                {
                    body.linearVelocity = new Vector2(-wallDirection * wallJumpHorizontalSpeed, jumpSpeed);
                    wallJumpUntil = Time.time + wallJumpLockTime;
                    lastJumpPressed = lastGrounded = float.NegativeInfinity;
                    jumpConsumed = true;
                    IsClimbing = IsGrounded = false;
                }
                else
                {
                    body.linearVelocity = new Vector2(wallDirection * 0.5f, verticalInput * climbSpeed);
                    IsGrounded = false;
                    lastGrounded = float.NegativeInfinity;
                }
                return;
            }
            if (wasClimbing && grabHeld && verticalInput > 0.1f && touchingWall == 0)
            {
                ledgeAssistUntil = Time.time + 0.14f;
                body.linearVelocity = new Vector2(wallDirection * moveSpeed, Mathf.Max(body.linearVelocity.y, 4f));
            }
            var velocity = body.linearVelocity;
            float acceleration = IsGrounded
                ? (Mathf.Abs(horizontalInput) > 0.01f ? groundAcceleration : groundBraking)
                : airAcceleration;
            if (Time.time >= wallJumpUntil)
                velocity.x = Time.time < ledgeAssistUntil ? wallDirection * moveSpeed
                    : Mathf.MoveTowards(velocity.x, horizontalInput * moveSpeed + supportVelocity.x, acceleration * Time.fixedDeltaTime);
            if (IsGrounded) velocity.y = supportVelocity.y;
            if (!jumpConsumed && Time.time - lastGrounded <= coyoteTime && Time.time - lastJumpPressed <= jumpBufferTime)
            {
                velocity.y = jumpSpeed + Mathf.Max(0, supportVelocity.y);
                lastJumpPressed = float.NegativeInfinity;
                jumpConsumed = true;
                IsGrounded = false;
            }
            float multiplier = velocity.y < 0f ? fallingGravityMultiplier
                : (!jumpHeld && velocity.y > 0f ? releasedJumpGravityMultiplier : 1f);
            velocity.y = Mathf.Max(velocity.y - gravity * multiplier * Time.fixedDeltaTime, -maximumFallSpeed);
            body.linearVelocity = velocity;
        }

        private int FindClimbableWall()
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = climbableLayers, useTriggers = false };
            for (int side = -1; side <= 1; side += 2)
            {
                int count = capsule.Cast(Vector2.right * side, filter, groundHits, 0.12f);
                for (int i = 0; i < count; i++)
                    if (groundHits[i].normal.x * side < -0.65f && groundHits[i].collider.GetComponentInParent<ClimbableWall>() != null)
                        return side;
            }
            return 0;
        }

        private void LateUpdate()
        {
            if (appearance != null) appearance.color = IsDashing ? new Color(1f, 0.75f, 0.3f)
                : IsClimbing ? new Color(0.65f, 1f, 0.8f) : normalColor;
        }

        public void RequestDeath() => deathRequested = true;

        public bool ActivateCheckpoint(Checkpoint2D checkpoint, Vector2 position)
        {
            if (checkpoint == null || checkpoint == ActiveCheckpoint || deathRequested || restartRequested) return false;
            if (ActiveCheckpoint != null) ActiveCheckpoint.SetActivated(false);
            ActiveCheckpoint = checkpoint;
            spawnPosition = position;
            checkpoint.SetActivated(true);
            // A checkpoint starts a new timing segment without teleporting the live player.
            CheckpointActivated?.Invoke();
            return true;
        }

        public void Respawn()
        {
            Restarting?.Invoke();
            body.position = spawnPosition;
            transform.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
            lastGrounded = lastJumpPressed = float.NegativeInfinity;
            jumpConsumed = false;
            restartRequested = false;
            deathRequested = false;
            groundSupport = null;
            IsGrounded = false;
            IsDashing = IsClimbing = false;
            DashAvailable = true;
            dashRequested = false;
            dashSteps = 0;
            nextDashTime = wallJumpUntil = ledgeAssistUntil = 0f;
            RestartCount++;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            horizontalInput = 0f;
            verticalInput = 0f;
            grabHeld = false;
            dashRequested = false;
            jumpHeld = false;
            lastJumpPressed = float.NegativeInfinity;
        }
    }
}
