using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatTarget), typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class Guardian : MonoBehaviour
    {
        private enum BossState { Intro, Move, Attack, Recover, Dead }

        [Header("Gameplay")]
        [SerializeField, Min(1)] private int health = 36;
        [SerializeField, Min(0f)] private float moveSpeed = 2.2f;
        [SerializeField, Min(0.01f)] private float normalAttackRange = 1.2f;
        [SerializeField, Min(0.01f)] private float alternateAttackRange = 1.8f;
        [SerializeField, Min(0.01f)] private float normalAttackCooldown = 0.9f;
        [SerializeField, Min(0.01f)] private float alternateAttackCooldown = 1.6f;
        [SerializeField, Min(1)] private int normalAttackDamage = 3;
        [SerializeField, Min(1)] private int alternateAttackDamage = 4;
        [SerializeField] private Vector2 normalHitboxSize = new Vector2(1.1f, 1f);
        [SerializeField] private Vector2 alternateHitboxSize = new Vector2(1.8f, 1.5f);
        [SerializeField, Min(0f)] private float normalHitboxOffset = 0.65f;
        [SerializeField, Min(0f)] private float alternateHitboxOffset = 0.7f;
        [SerializeField, Min(0f)] private float attackHitDelay = 0.3f;
        [SerializeField, Min(0.01f)] private float normalAttackDuration = 1.2f;
        [SerializeField, Min(0.01f)] private float alternateAttackDuration = 1.2f;
        [SerializeField, Min(0f)] private float recoverDuration = 0.2f;
        [SerializeField] private LayerMask playerLayers;

        [Header("Presentation")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Texture2D idleLeft;
        [SerializeField] private Texture2D idleRight;
        [SerializeField] private Texture2D walkingLeft;
        [SerializeField] private Texture2D walkingRight;
        [SerializeField] private Texture2D attackLeft;
        [SerializeField] private Texture2D attackRight;
        [SerializeField] private Texture2D alternateLeft;
        [SerializeField] private Texture2D alternateRight;
        [SerializeField] private Texture2D spawnAnimation;
        [SerializeField, Min(1)] private int frameWidth = 254;
        [SerializeField, Min(1)] private int frameHeight = 192;
        [SerializeField, Min(0.01f)] private float framesPerSecond = 10f;

        private readonly Dictionary<Texture2D, Sprite[]> frameCache = new Dictionary<Texture2D, Sprite[]>();
        private CombatTarget target;
        private Rigidbody2D body;
        private PlayerDamage player;
        private BossState state;
        private Coroutine introRoutine;
        private Coroutine attackRoutine;
        private float nextNormalAttackTime;
        private float nextAlternateAttackTime;
        private float recoverUntil;
        private bool facingLeft;
        private bool dying;

        private void Awake()
        {
            target = GetComponent<CombatTarget>();
            body = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            target.SetMaxHealth(health);
        }

        private void OnEnable()
        {
            if (target == null || body == null) return;
            target.ResetTarget();
            body.linearVelocity = Vector2.zero;
            state = BossState.Intro;
            nextNormalAttackTime = 0f;
            nextAlternateAttackTime = 0f;
            recoverUntil = 0f;
            facingLeft = false;
            dying = false;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            ApplyFrame(spawnAnimation, 0);
            introRoutine = StartCoroutine(IntroRoutine());
        }

        private void OnDisable()
        {
            if (introRoutine != null) StopCoroutine(introRoutine);
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            introRoutine = null;
            attackRoutine = null;
            if (body != null) body.linearVelocity = Vector2.zero;
            state = BossState.Intro;
        }

        private void Update()
        {
            if (dying) return;
            if (!target.IsAlive)
            {
                Die();
                return;
            }

            if (state == BossState.Intro || state == BossState.Attack) return;
            if (state == BossState.Recover)
            {
                StopMoving();
                if (Time.time >= recoverUntil) state = BossState.Move;
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
            float absoluteDistance = Mathf.Abs(distance);

            if (absoluteDistance <= alternateAttackRange && Time.time >= nextAlternateAttackTime)
            {
                attackRoutine = StartCoroutine(AttackRoutine(true));
                return;
            }

            if (absoluteDistance <= normalAttackRange && Time.time >= nextNormalAttackTime)
            {
                attackRoutine = StartCoroutine(AttackRoutine(false));
                return;
            }

            state = BossState.Move;
            body.linearVelocity = new Vector2(direction * moveSpeed, body.linearVelocity.y);
            Texture2D walking = facingLeft ? walkingLeft : walkingRight;
            int frameCount = Mathf.Max(1, GetFrameCount(walking));
            ApplyFrame(walking, Mathf.FloorToInt(Time.time * framesPerSecond) % frameCount);
        }

        private IEnumerator IntroRoutine()
        {
            float duration = GetFrameCount(spawnAnimation) / framesPerSecond;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                StopMoving();
                ApplyFrame(spawnAnimation, Mathf.FloorToInt(elapsed * framesPerSecond));
                elapsed += Time.deltaTime;
                yield return null;
            }
            introRoutine = null;
            state = BossState.Move;
            ApplyFrame(facingLeft ? idleLeft : idleRight, 0);
        }

        private IEnumerator AttackRoutine(bool alternate)
        {
            state = BossState.Attack;
            StopMoving();
            if (alternate) nextAlternateAttackTime = Time.time + alternateAttackCooldown;
            else nextNormalAttackTime = Time.time + normalAttackCooldown;

            Texture2D texture = alternate
                ? (facingLeft ? alternateLeft : alternateRight)
                : (facingLeft ? attackLeft : attackRight);
            float duration = alternate ? alternateAttackDuration : normalAttackDuration;
            float elapsed = 0f;
            bool damaged = false;
            while (elapsed < duration)
            {
                ApplyFrame(texture, Mathf.FloorToInt(elapsed * framesPerSecond));
                if (!damaged && elapsed >= attackHitDelay)
                {
                    ApplyAttackDamage(alternate);
                    damaged = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            attackRoutine = null;
            recoverUntil = Time.time + recoverDuration;
            state = BossState.Recover;
        }

        private void ApplyAttackDamage(bool alternate)
        {
            Vector2 center = (Vector2)transform.position +
                Vector2.right * (facingLeft ? -1f : 1f) *
                (alternate ? alternateHitboxOffset : normalHitboxOffset);
            Vector2 size = alternate ? alternateHitboxSize : normalHitboxSize;
            int damage = alternate ? alternateAttackDamage : normalAttackDamage;
            List<Collider2D> overlaps = new List<Collider2D>(4);
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(playerLayers);
            Physics2D.OverlapBox(center, size, 0f, filter, overlaps);
            for (int i = 0; i < overlaps.Count; i++)
            {
                PlayerDamage victim = overlaps[i].GetComponentInParent<PlayerDamage>();
                if (victim != null)
                {
                    victim.ReceiveDamage(damage, transform.position.x);
                    break;
                }
            }
        }

        private void Die()
        {
            dying = true;
            state = BossState.Dead;
            if (introRoutine != null) StopCoroutine(introRoutine);
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            introRoutine = null;
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
            int count = GetFrameCount(texture);
            sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
                sprites[i] = Sprite.Create(texture, new Rect(i * frameWidth, 0f, frameWidth, frameHeight),
                    new Vector2(0.5f, 0.5f), 32f);
            frameCache.Add(texture, sprites);
            return sprites;
        }

        private int GetFrameCount(Texture2D texture) => texture == null ? 0 : Mathf.Max(1, texture.width / frameWidth);
    }
}
