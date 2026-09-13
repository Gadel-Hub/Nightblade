using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nightblade
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombat : MonoBehaviour
    {
        public enum AttackPhase { Idle, Startup, Active, Recovery }

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Attack timing (seconds)")]
        [SerializeField, Min(0f)] private float startupDuration = 0.08f;
        [SerializeField, Min(0f)] private float activeDuration = 0.10f;
        [SerializeField, Min(0f)] private float recoveryDuration = 0.18f;

        [Header("Gameplay geometry")]
        [SerializeField] private BoxCollider2D attackHitbox;
        [SerializeField] private SpriteRenderer attackVisual;
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0.5625f, 0f);
        [SerializeField] private LayerMask targetLayers;
        [SerializeField, Min(1)] private int attackDamage = 1;

        private readonly HashSet<CombatTarget> hitTargets = new HashSet<CombatTarget>();
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private InputActionAsset runtimeActions;
        private InputActionMap playerActions;
        private InputAction moveAction;
        private InputAction attackAction;
        private ContactFilter2D targetFilter;
        private float phaseElapsed;
        private int facing = 1;
        private int swingFacing = 1;
        private bool controlEnabled = true;
        private bool attackRequested;

        public AttackPhase Phase { get; private set; } = AttackPhase.Idle;
        public bool IsAttacking => Phase != AttackPhase.Idle;
        public bool HitboxActive => attackHitbox != null && attackHitbox.enabled;
        public int Facing => facing;
        public int SwingFacing => swingFacing;
        public int HitCount => hitTargets.Count;
        public bool ControlEnabled => controlEnabled;

        private void Awake()
        {
            if (inputActions == null || attackHitbox == null || attackVisual == null || targetLayers.value == 0)
            {
                Debug.LogError("PlayerCombat needs input actions, an attack hitbox, an attack visual, and target layers.", this);
                enabled = false;
                return;
            }

            attackHitbox.enabled = false;
            attackVisual.enabled = false;
            targetFilter = new ContactFilter2D { useTriggers = true };
            targetFilter.SetLayerMask(targetLayers);
            runtimeActions = Instantiate(inputActions);
            playerActions = runtimeActions.FindActionMap("Player", true);
            moveAction = playerActions.FindAction("Move", true);
            attackAction = playerActions.FindAction("Attack", true);
        }

        private void OnEnable()
        {
            if (playerActions == null || attackAction == null) return;
            attackAction.performed += OnAttackPerformed;
            playerActions.Enable();
        }

        private void OnDisable()
        {
            if (attackAction != null) attackAction.performed -= OnAttackPerformed;
            playerActions?.Disable();
            InterruptAttack();
        }

        private void OnDestroy()
        {
            if (runtimeActions != null) Destroy(runtimeActions);
        }

        private void Update()
        {
            AdvanceAttack(Time.deltaTime);
            if (Phase == AttackPhase.Active) DamageOverlappingTargets();
        }

        private void FixedUpdate()
        {
            if (!controlEnabled) return;
            float direction = moveAction.ReadValue<float>();
            if (direction != 0f) facing = direction < 0f ? -1 : 1;
            if (!attackRequested) return;

            attackRequested = false;
            TryStartAttack();
        }

        private void OnAttackPerformed(InputAction.CallbackContext _)
        {
            if (controlEnabled) attackRequested = true;
        }

        public bool TryStartAttack()
        {
            if (Phase != AttackPhase.Idle) return false;

            Phase = AttackPhase.Startup;
            phaseElapsed = 0f;
            swingFacing = facing;
            hitTargets.Clear();
            PlaceHitbox();
            return true;
        }

        public void InterruptAttack()
        {
            Phase = AttackPhase.Idle;
            phaseElapsed = 0f;
            attackRequested = false;
            hitTargets.Clear();
            if (attackHitbox != null) attackHitbox.enabled = false;
            if (attackVisual != null) attackVisual.enabled = false;
        }

        public void ResetCombat()
        {
            InterruptAttack();
            controlEnabled = true;
            facing = 1;
            swingFacing = 1;
            PlaceHitbox();
        }

        public void SetControlEnabled(bool value)
        {
            controlEnabled = value;
            if (!value) InterruptAttack();
        }

        private void AdvanceAttack(float deltaTime)
        {
            if (Phase == AttackPhase.Idle) return;

            phaseElapsed += deltaTime;
            float activeEnd = startupDuration + activeDuration;
            if (phaseElapsed >= activeEnd + recoveryDuration)
                SetPhase(AttackPhase.Idle);
            else if (phaseElapsed >= activeEnd)
                SetPhase(AttackPhase.Recovery);
            else if (phaseElapsed >= startupDuration)
                SetPhase(AttackPhase.Active);
        }

        private void SetPhase(AttackPhase nextPhase)
        {
            Phase = nextPhase;
            bool isActive = nextPhase == AttackPhase.Active;
            attackHitbox.enabled = isActive;
            attackVisual.enabled = isActive;
        }

        private void PlaceHitbox()
        {
            if (attackHitbox == null) return;
            attackHitbox.transform.localPosition = new Vector3(
                hitboxOffset.x * swingFacing,
                hitboxOffset.y,
                0f);
        }

        private void DamageOverlappingTargets()
        {
            Bounds bounds = attackHitbox.bounds;
            overlaps.Clear();
            Physics2D.OverlapBox(bounds.center, bounds.size, 0f, targetFilter, overlaps);
            for (int i = 0; i < overlaps.Count; i++)
            {
                CombatTarget target = overlaps[i].GetComponentInParent<CombatTarget>();
                if (target == null || hitTargets.Contains(target)) continue;
                if (target.TakeDamage(attackDamage)) hitTargets.Add(target);
            }
        }
    }
}
