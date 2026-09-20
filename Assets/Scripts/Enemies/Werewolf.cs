using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatTarget), typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class Werewolf : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField, Min(1)] private int health = 9;
        [SerializeField, Min(0f)] private float moveSpeed = 3.2f;
        [SerializeField, Min(0.01f)] private float meleeRange = 0.9f;
        [SerializeField, Min(0.01f)] private float attackCooldown = 1.05f;
        [SerializeField, Min(1)] private int meleeDamage = 2;
        [SerializeField] private Vector2 meleeHitboxSize = new Vector2(1f, 0.9f);
        [SerializeField, Min(0f)] private float meleeForwardOffset = 0.55f;
        [SerializeField, Min(0f)] private float attackHitDelay = 0.2f;
        [SerializeField] private LayerMask playerLayers;

        [Header("Presentation")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Texture2D idleLeft;
        [SerializeField] private Texture2D idleRight;
        [SerializeField] private Texture2D runningLeft;
        [SerializeField] private Texture2D runningRight;
        [SerializeField] private Texture2D attackLeft;
        [SerializeField] private Texture2D attackRight;
        [SerializeField, Min(1)] private int frameWidth = 64;
        [SerializeField, Min(1)] private int frameHeight = 64;
        [SerializeField, Min(0.01f)] private float framesPerSecond = 10f;
        [SerializeField, Min(0.01f)] private float attackDuration = 0.6f;

        private readonly Dictionary<Texture2D, Sprite[]> frameCache = new Dictionary<Texture2D, Sprite[]>();
        private CombatTarget target;
        private Rigidbody2D body;
        private PlayerDamage player;
        private Coroutine attackRoutine;
        private float nextAttackTime;
        private bool facingLeft;
        private bool dying;
        private float initialAttackDelay;
        private bool initialAttackDelayPending;

        public void SetInitialAttackDelay(float delay)
        {
            initialAttackDelay = Mathf.Max(0f, delay);
            initialAttackDelayPending = initialAttackDelay > 0f;
        }

        private void Awake()
        {
            target = GetComponent<CombatTarget>();
            body = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            target.SetMaxHealth(health);
            player = FindFirstObjectByType<PlayerDamage>();
        }

        private void Update()
        {
            if (dying) return;
            if (!target.IsAlive)
            {
                Die();
                return;
            }

            if (player == null) player = FindFirstObjectByType<PlayerDamage>();
            if (player == null || player.State == PlayerDamage.DamageState.Dead)
            {
                StopMoving();
                ApplyFrame(facingLeft ? idleLeft : idleRight, 0);
                return;
            }

            float distance = player.transform.position.x - transform.position.x;
            int direction = distance < 0f ? -1 : 1;
            facingLeft = direction < 0;
            if (attackRoutine != null)
            {
                StopMoving();
                return;
            }

            if (Mathf.Abs(distance) > meleeRange)
            {
                body.linearVelocity = new Vector2(direction * moveSpeed, body.linearVelocity.y);
                Texture2D running = facingLeft ? runningLeft : runningRight;
                int frameCount = running == null ? 1 : Mathf.Max(1, running.width / frameWidth);
                ApplyFrame(running, Mathf.FloorToInt(Time.time * framesPerSecond) % frameCount);
            }
            else
            {
                StopMoving();
                ApplyFrame(facingLeft ? idleLeft : idleRight, 0);
                if (initialAttackDelayPending)
                {
                    nextAttackTime = Time.time + initialAttackDelay;
                    initialAttackDelayPending = false;
                }
                if (Time.time >= nextAttackTime)
                    attackRoutine = StartCoroutine(AttackRoutine());
            }
        }

        private IEnumerator AttackRoutine()
        {
            nextAttackTime = Time.time + attackCooldown;
            float elapsed = 0f;
            bool damageApplied = false;
            while (elapsed < attackDuration)
            {
                ApplyFrame(facingLeft ? attackLeft : attackRight,
                    Mathf.FloorToInt(elapsed * framesPerSecond));
                if (!damageApplied && elapsed >= attackHitDelay)
                {
                    ApplyMeleeDamage();
                    damageApplied = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            attackRoutine = null;
        }

        private void ApplyMeleeDamage()
        {
            Vector2 center = (Vector2)transform.position +
                Vector2.right * (facingLeft ? -1f : 1f) * meleeForwardOffset;
            List<Collider2D> overlaps = new List<Collider2D>(4);
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(playerLayers);
            Physics2D.OverlapBox(center, meleeHitboxSize, 0f, filter, overlaps);
            for (int i = 0; i < overlaps.Count; i++)
            {
                PlayerDamage victim = overlaps[i].GetComponentInParent<PlayerDamage>();
                if (victim != null)
                {
                    victim.ReceiveDamage(meleeDamage, transform.position.x);
                    break;
                }
            }
        }

        private void Die()
        {
            dying = true;
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = null;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            Destroy(gameObject);
        }

        private void StopMoving() => body.linearVelocity = new Vector2(0f, body.linearVelocity.y);

        private void ApplyFrame(Texture2D texture, int index)
        {
            if (spriteRenderer == null || texture == null) return;
            Sprite[] sprites = GetFrames(texture);
            if (sprites.Length == 0) return;
            spriteRenderer.sprite = sprites[Mathf.Clamp(index, 0, sprites.Length - 1)];
            spriteRenderer.flipX = false;
        }

        private Sprite[] GetFrames(Texture2D texture)
        {
            if (frameCache.TryGetValue(texture, out Sprite[] sprites)) return sprites;
            int count = Mathf.Max(1, texture.width / frameWidth);
            sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
                sprites[i] = Sprite.Create(texture, new Rect(i * frameWidth, 0f, frameWidth, frameHeight),
                    new Vector2(0.5f, 0.5f), 32f);
            frameCache.Add(texture, sprites);
            return sprites;
        }
    }
}
