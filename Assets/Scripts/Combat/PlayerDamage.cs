using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerHealth), typeof(PlayerMovement))]
    public sealed class PlayerDamage : MonoBehaviour
    {
        public enum DamageState { Normal, Hit, Dead }

        [Header("Hit response (world units and seconds)")]
        [SerializeField, Min(0f)] private float knockbackHorizontal = 4.6875f;
        [SerializeField, Min(0f)] private float knockbackVertical = 5f;
        [SerializeField, Min(0f)] private float hitReactionDuration = 0.18f;
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.45f;

        [Header("Death")]
        [SerializeField, Min(0f)] private float respawnDelay = 0.8f;
        [SerializeField] private GameObject visualRoot;

        private Rigidbody2D body;
        private PlayerHealth health;
        private PlayerMovement movement;
        private PlayerCombat combat;
        private PlayerCharacter character;
        private float stateRemaining;

        public DamageState State { get; private set; } = DamageState.Normal;
        public float InvulnerabilityRemaining { get; private set; }
        public float StateRemaining => stateRemaining;
        public bool IsInvulnerable => InvulnerabilityRemaining > 0f;
        public bool ReadyToRespawn => State == DamageState.Dead && stateRemaining == 0f;
        public bool IsInHitReaction => State == DamageState.Hit;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<PlayerHealth>();
            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            character = GetComponent<PlayerCharacter>();
            if (combat == null || visualRoot == null)
            {
                Debug.LogError("PlayerDamage needs player combat and a separate visual root.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            InvulnerabilityRemaining = Mathf.Max(0f, InvulnerabilityRemaining - Time.deltaTime);
            stateRemaining = Mathf.Max(0f, stateRemaining - Time.deltaTime);
            if (State != DamageState.Hit || stateRemaining > 0f) return;

            State = DamageState.Normal;
            bool runActive = character == null || character.RunInProgress;
            movement.SetControlEnabled(runActive);
            combat.SetControlEnabled(runActive);
        }

        public bool ReceiveDamage(int amount, float sourceX)
        {
            if (GetComponent<FireCharacterGameplay>()?.IsShieldActive == true ||
                GetComponent<WaterCharacterGameplay>()?.IsShieldActive == true ||
                GetComponent<AirCharacterGameplay>()?.IsShieldActive == true) return false;
            if (State != DamageState.Normal || IsInvulnerable || amount <= 0) return false;
            if (!health.TakeDamage(amount)) return false;

            movement.SetControlEnabled(false);
            combat.SetControlEnabled(false);
            if (health.IsDepleted)
            {
                EnterDeadState();
                return true;
            }

            State = DamageState.Hit;
            stateRemaining = hitReactionDuration;
            InvulnerabilityRemaining = invulnerabilityDuration;
            int away = Mathf.Approximately(body.position.x, sourceX)
                ? -combat.Facing
                : body.position.x > sourceX ? 1 : -1;
            body.linearVelocity = new Vector2(away * knockbackHorizontal, knockbackVertical);
            return true;
        }

        public void RespawnAt(Vector2 worldPosition)
        {
            health.RestoreFull();
            State = DamageState.Normal;
            stateRemaining = 0f;
            InvulnerabilityRemaining = 0f;
            visualRoot.SetActive(true);
            movement.ResetMovement(worldPosition);
            combat.ResetCombat();
        }

        private void EnterDeadState()
        {
            State = DamageState.Dead;
            stateRemaining = respawnDelay;
            InvulnerabilityRemaining = 0f;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            visualRoot.SetActive(false);
        }
    }
}
