using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nightblade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        public enum MovementState { Idle, Run, Jump, Fall, WallSlide, WallJump }

        [Header("Input and terrain")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private LayerMask terrainLayers;

        // Reference pixels / 32 PPU; Unity's positive Y points upward.
        [Header("Movement (world units and seconds)")]
        [SerializeField, Min(0f)] private float runSpeed = 4.375f;
        [SerializeField, Min(0f)] private float gravity = 21.875f;
        [SerializeField, Min(0f)] private float jumpVelocity = 8.75f;
        [SerializeField, Min(0f)] private float wallSlideSpeed = 1.71875f;
        [SerializeField, Min(0f)] private float wallJumpHorizontalVelocity = 5.3125f;
        [SerializeField, Min(0f)] private float wallJumpVerticalVelocity = 8.4375f;
        [SerializeField, Min(0f)] private float wallJumpPushDuration = 0.12f;

        // The reference uses axis-aligned terrain. Ignore diagonal corner normals.
        private const float ContactNormalThreshold = 0.9f;
        private readonly List<ContactPoint2D> contacts = new List<ContactPoint2D>(8);
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
                if (Grounded) return Velocity.x == 0f ? MovementState.Idle : MovementState.Run;
                if (pushRemaining > 0f) return MovementState.WallJump;
                if (sliding) return MovementState.WallSlide;
                return Velocity.y > 0f ? MovementState.Jump : MovementState.Fall;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            terrainFilter = new ContactFilter2D { useTriggers = false };
            terrainFilter.SetLayerMask(terrainLayers);

            if (inputActions == null || terrainLayers.value == 0)
            {
                Debug.LogError("PlayerMovement needs Movement actions and a terrain mask.", this);
                enabled = false;
                return;
            }

            // Each prefab instance owns its enabled actions, not the shared asset.
            runtimeActions = Instantiate(inputActions);
            movementMap = runtimeActions.FindActionMap("Player", true);
            moveAction = movementMap.FindAction("Move", true);
            jumpAction = movementMap.FindAction("Jump", true);
        }

        private void OnEnable() => movementMap?.Enable();
        private void OnDisable() => movementMap?.Disable();
        private void OnDestroy()
        {
            if (runtimeActions != null) Destroy(runtimeActions);
        }

        private void FixedUpdate()
        {
            RefreshContacts();
            if (!body.simulated) return;

            float step = Time.fixedDeltaTime;
            Vector2 velocity = body.linearVelocity;
            if (!controlEnabled)
            {
                // Phaser gravity continues while hit reaction owns horizontal
                // velocity, so only gravity is applied while controls are locked.
                velocity.y -= gravity * step;
                body.linearVelocity = velocity;
                return;
            }

            float input = moveAction.ReadValue<float>();
            int direction = input > 0f ? 1 : input < 0f ? -1 : 0;
            // Input is processed in fixed updates. The press belongs to this step
            // only: an ineligible press is discarded, never saved until landing.
            bool jumpPressed = jumpAction.WasPressedThisFrame();
            int wall = LeftWall ? -1 : RightWall ? 1 : 0;

            pushRemaining = Mathf.Max(0f, pushRemaining - step);
            if (Grounded)
            {
                lastJumpWall = 0;
                pushRemaining = 0f;
            }
            else if (wall != 0 && wall == pushDirection)
            {
                // Opposite-wall contact releases control early in a narrow shaft.
                pushRemaining = 0f;
            }

            sliding = !Grounded && wall != 0 && direction == wall
                && velocity.y <= 0f && pushRemaining == 0f;

            if (jumpPressed)
            {
                if (Grounded)
                {
                    velocity.y = jumpVelocity;
                    Grounded = false;
                }
                else if (wall != 0 && wall != lastJumpWall)
                {
                    lastJumpWall = wall;
                    pushDirection = -wall;
                    pushRemaining = wallJumpPushDuration;
                    sliding = false;
                    velocity.y = wallJumpVerticalVelocity;
                }
            }

            velocity.x = pushRemaining > 0f
                ? pushDirection * wallJumpHorizontalVelocity
                : direction * runSpeed;

            // Rigidbody gravityScale is zero. Apply gravity before the slide cap
            // so the physics step cannot add gravity beyond that cap.
            velocity.y -= gravity * step;
            if (sliding) velocity.y = Mathf.Max(velocity.y, -wallSlideSpeed);
            body.linearVelocity = velocity;
        }

        public void SetControlEnabled(bool value)
        {
            controlEnabled = value;
            if (value) return;

            // Damage cancels transient wall motion without resetting the last
            // wall, so a hit cannot grant another jump from that same wall.
            pushRemaining = 0f;
            pushDirection = 0;
            sliding = false;
        }

        private void RefreshContacts()
        {
            Grounded = false;
            LeftWall = false;
            RightWall = false;
            int count = body.GetContacts(terrainFilter, contacts);
            for (int i = 0; i < count; i++)
            {
                Vector2 normal = contacts[i].normal;
                // Contacts come from the last solved physics step. An upward
                // launch must not reuse a lingering floor contact for another jump.
                if (normal.y >= ContactNormalThreshold && body.linearVelocity.y <= 0f)
                    Grounded = true;
                if (normal.x >= ContactNormalThreshold) LeftWall = true;
                if (normal.x <= -ContactNormalThreshold) RightWall = true;
            }
        }
    }
}
