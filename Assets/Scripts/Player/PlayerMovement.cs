using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nightblade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        public enum MovementState
        {
            Idle,Run,Jump,Fall,WallSlide,WallJump
        }

        [Header("Input and terrain")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private LayerMask terrainLayers;

        [Header("Movement (world units and seconds)")]
        [SerializeField, Min(0f)] private float runSpeed = 4.375f;
        [SerializeField, Min(0f)] private float gravity = 21.875f;
        [SerializeField, Min(0f)] private float jumpVelocity = 8.75f;

        [Header("Jump Feel")]
        [Tooltip("Jump input'u yere değmeden kısa süre önce alınırsa saklanır.")]
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        [Tooltip("Platformdan ayrıldıktan sonra kısa süre daha jump yapılmasına izin verir.")]
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;

        [Tooltip("Jump tuşu bırakıldığında yukarı doğru hızı ne kadar kesilecek.")]
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;

        [Tooltip("Düşerken gravity'nin çarpanı.")]
        [SerializeField, Min(1f)] private float fallGravityMultiplier = 1.5f;

        [Tooltip("Jump'ın tepe noktasında gravity'nin çarpanı.")]
        [SerializeField, Range(0f, 1f)] private float apexGravityMultiplier = 0.5f;

        [Tooltip("Apex hang time'ın etki edeceği dikey hız aralığı.")]
        [SerializeField, Min(0f)] private float apexThreshold = 1.5f;

        [Header("Wall Movement")]
        [SerializeField, Min(0f)] private float wallSlideSpeed = 1.71875f;
        [SerializeField, Min(0f)] private float wallJumpHorizontalVelocity = 5.3125f;
        [SerializeField, Min(0f)] private float wallJumpVerticalVelocity = 8.4375f;
        [SerializeField, Min(0f)] private float wallJumpPushDuration = 0.12f;

        private const float ContactNormalThreshold = 0.9f;

        private readonly List<ContactPoint2D> contacts =
            new List<ContactPoint2D>(8);

        private Rigidbody2D body;
        private ContactFilter2D terrainFilter;

        private InputActionAsset runtimeActions;
        private InputActionMap movementMap;
        private InputAction moveAction;
        private InputAction jumpAction;

        private int lastJumpWall;
        private int pushDirection;

        private float pushRemaining;

        private bool sliding;
        private bool controlEnabled = true;

        // Jump buffering
        private float jumpBufferCounter;

        // Coyote time
        private float coyoteCounter;

        // Variable jump
        private bool jumpStarted;

        public bool Grounded { get; private set; }
        public bool LeftWall { get; private set; }
        public bool RightWall { get; private set; }

        public Vector2 Position => body.position;
        public Vector2 Velocity => body.linearVelocity;

        public float PushRemaining => pushRemaining;
        public int LastJumpWall => lastJumpWall;

        public bool ControlEnabled => controlEnabled;

        public MovementState State
        {
            get
            {
                if (Grounded)
                    return Velocity.x == 0f
                        ? MovementState.Idle
                        : MovementState.Run;

                if (pushRemaining > 0f)
                    return MovementState.WallJump;

                if (sliding)
                    return MovementState.WallSlide;

                return Velocity.y > 0f
                    ? MovementState.Jump
                    : MovementState.Fall;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();

            terrainFilter = new ContactFilter2D
            {
                useTriggers = false
            };

            terrainFilter.SetLayerMask(terrainLayers);

            if (inputActions == null || terrainLayers.value == 0)
            {
                Debug.LogError(
                    "PlayerMovement needs Movement actions and a terrain mask.",
                    this);

                enabled = false;
                return;
            }

            runtimeActions = Instantiate(inputActions);

            movementMap =
                runtimeActions.FindActionMap("Player", true);

            moveAction =
                movementMap.FindAction("Move", true);

            jumpAction =
                movementMap.FindAction("Jump", true);
        }

        private void OnEnable()
        {
            movementMap?.Enable();
        }

        private void OnDisable()
        {
            movementMap?.Disable();
        }

        private void OnDestroy()
        {
            if (runtimeActions != null)
                Destroy(runtimeActions);
        }

        private void FixedUpdate()
        {
            RefreshContacts();

            if (!body.simulated)
                return;

            float step = Time.fixedDeltaTime;

            Vector2 velocity = body.linearVelocity;

            // --------------------------------------------------
            // INPUT
            // --------------------------------------------------

            float input = moveAction.ReadValue<float>();

            int direction =
                input > 0f ? 1 :
                input < 0f ? -1 :
                0;

            bool jumpPressed =
                jumpAction.WasPressedThisFrame();

            bool jumpReleased =
                jumpAction.WasReleasedThisFrame();

            // --------------------------------------------------
            // JUMP BUFFER
            // --------------------------------------------------

            if (jumpPressed)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= step;
            }

            // --------------------------------------------------
            // COYOTE TIME
            // --------------------------------------------------

            if (Grounded)
            {
                coyoteCounter = coyoteTime;
            }
            else
            {
                coyoteCounter -= step;
            }

            // --------------------------------------------------
            // CONTROL DISABLED
            // --------------------------------------------------

            if (!controlEnabled)
            {
                velocity.y -= gravity * step;
                body.linearVelocity = velocity;
                return;
            }

            // --------------------------------------------------
            // WALL STATE
            // --------------------------------------------------

            int wall =
                LeftWall ? -1 :
                RightWall ? 1 :
                0;

            pushRemaining =
                Mathf.Max(0f, pushRemaining - step);

            if (Grounded)
            {
                lastJumpWall = 0;
                pushRemaining = 0f;
            }
            else if (wall != 0 && wall == pushDirection)
            {
                pushRemaining = 0f;
            }

            sliding =
                !Grounded &&
                wall != 0 &&
                direction == wall &&
                velocity.y <= 0f &&
                pushRemaining == 0f;

            // --------------------------------------------------
            // NORMAL JUMP
            // --------------------------------------------------

            if (jumpBufferCounter > 0f &&
                coyoteCounter > 0f)
            {
                velocity.y = jumpVelocity;

                Grounded = false;

                jumpBufferCounter = 0f;
                coyoteCounter = 0f;

                jumpStarted = true;

                sliding = false;
            }

            // --------------------------------------------------
            // WALL JUMP
            // --------------------------------------------------

            else if (jumpPressed &&
                     !Grounded &&
                     wall != 0 &&
                     wall != lastJumpWall)
            {
                lastJumpWall = wall;

                pushDirection = -wall;

                pushRemaining = wallJumpPushDuration;

                sliding = false;

                velocity.y = wallJumpVerticalVelocity;

                jumpStarted = true;

                // Wall jump, normal jump buffer'ı tüketir.
                jumpBufferCounter = 0f;
            }

            // --------------------------------------------------
            // VARIABLE JUMP HEIGHT
            // --------------------------------------------------

            if (jumpReleased &&
                jumpStarted &&
                velocity.y > 0f)
            {
                velocity.y *= jumpCutMultiplier;

                jumpStarted = false;
            }

            // --------------------------------------------------
            // HORIZONTAL MOVEMENT
            // --------------------------------------------------

            velocity.x =
                pushRemaining > 0f
                    ? pushDirection * wallJumpHorizontalVelocity
                    : direction * runSpeed;

            // --------------------------------------------------
            // BETTER GRAVITY
            // --------------------------------------------------

            float currentGravity = gravity;

            // Düşüşte daha güçlü gravity
            if (velocity.y < 0f)
            {
                currentGravity *= fallGravityMultiplier;
            }

            // --------------------------------------------------
            // APEX HANG TIME
            // --------------------------------------------------

            if (Mathf.Abs(velocity.y) < apexThreshold)
            {
                currentGravity *= apexGravityMultiplier;
            }

            velocity.y -= currentGravity * step;

            // --------------------------------------------------
            // WALL SLIDE
            // --------------------------------------------------

            if (sliding)
            {
                velocity.y =
                    Mathf.Max(
                        velocity.y,
                        -wallSlideSpeed);
            }

            body.linearVelocity = velocity;
        }

        public void SetControlEnabled(bool value)
        {
            controlEnabled = value;

            if (value)
                return;

            pushRemaining = 0f;
            pushDirection = 0;
            sliding = false;

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            jumpStarted = false;
        }

        public void ResetMovement(Vector2 worldPosition)
        {
            body.simulated = true;

            body.position = worldPosition;
            body.linearVelocity = Vector2.zero;

            Grounded = false;
            LeftWall = false;
            RightWall = false;

            lastJumpWall = 0;

            pushDirection = 0;
            pushRemaining = 0f;

            sliding = false;

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            jumpStarted = false;

            controlEnabled = true;
        }

        private void RefreshContacts()
        {
            Grounded = false;
            LeftWall = false;
            RightWall = false;

            int count =
                body.GetContacts(
                    terrainFilter,
                    contacts);

            for (int i = 0; i < count; i++)
            {
                Vector2 normal =
                    contacts[i].normal;

                // Ground
                if (normal.y >= ContactNormalThreshold &&
                    body.linearVelocity.y <= 0f)
                {
                    Grounded = true;
                }

                // Left wall
                if (normal.x >= ContactNormalThreshold)
                {
                    LeftWall = true;
                }

                // Right wall
                if (normal.x <= -ContactNormalThreshold)
                {
                    RightWall = true;
                }
            }
        }
    }
}


