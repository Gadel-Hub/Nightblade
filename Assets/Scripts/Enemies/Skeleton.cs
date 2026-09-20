using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatTarget), typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class Skeleton : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField, Min(1)] private int health = 4;
        [SerializeField, Min(0f)] private float moveSpeed = 2f;
        [SerializeField, Min(0.01f)] private float preferredRange = 4f;
        [SerializeField, Min(0.01f)] private float attackCooldown = 1.25f;
        [SerializeField, Min(1)] private int arrowDamage = 2;
        [SerializeField, Min(0.01f)] private float arrowSpeed = 8f;
        [SerializeField, Min(0.01f)] private float arrowLifetime = 2f;
        [SerializeField, Min(0.01f)] private float arrowRange = 10f;
        [SerializeField] private LayerMask playerLayers;

        [Header("Presentation")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Texture2D walkLeft;
        [SerializeField] private Texture2D walkRight;
        [SerializeField] private Texture2D attackLeft;
        [SerializeField] private Texture2D attackRight;
        [SerializeField] private Texture2D movingAttackLeft;
        [SerializeField] private Texture2D movingAttackRight;
        [SerializeField] private Texture2D arrowLeft;
        [SerializeField] private Texture2D arrowRight;
        [SerializeField, Min(1)] private int frameWidth = 48;
        [SerializeField, Min(1)] private int frameHeight = 64;
        [SerializeField, Min(0.01f)] private float framesPerSecond = 10f;
        [SerializeField, Min(0.01f)] private float attackDuration = 0.9f;
        [SerializeField, Min(0f)] private float arrowReleaseDelay = 0.3f;
        [SerializeField, Min(0.01f)] private float arrowReleaseOffset = 0.65f;

        private readonly Dictionary<Texture2D, Sprite[]> frameCache = new Dictionary<Texture2D, Sprite[]>();
        private CombatTarget target;
        private Rigidbody2D body;
        private PlayerDamage player;
        private Coroutine attackRoutine;
        private float nextAttackTime;
        private bool facingLeft;
        private bool dying;

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
                ApplyFrame(walkLeft, 0);
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

            float absoluteDistance = Mathf.Abs(distance);
            if (absoluteDistance > preferredRange + 0.25f)
            {
                Move(direction, movingAttackLeft, movingAttackRight);
            }
            else if (absoluteDistance < preferredRange - 0.5f)
            {
                Move(-direction, movingAttackLeft, movingAttackRight);
            }
            else
            {
                StopMoving();
                ApplyFrame(facingLeft ? walkLeft : walkRight, 0);
                if (Time.time >= nextAttackTime)
                    attackRoutine = StartCoroutine(AttackRoutine());
            }
        }

        private void Move(int direction, Texture2D left, Texture2D right)
        {
            body.linearVelocity = new Vector2(direction * moveSpeed, body.linearVelocity.y);
            Texture2D texture = facingLeft ? left : right;
            int frameCount = texture == null ? 1 : Mathf.Max(1, texture.width / frameWidth);
            ApplyFrame(texture, Mathf.FloorToInt(Time.time * framesPerSecond) % frameCount);
        }

        private IEnumerator AttackRoutine()
        {
            nextAttackTime = Time.time + attackCooldown;
            float elapsed = 0f;
            bool released = false;
            while (elapsed < attackDuration)
            {
                Texture2D texture = facingLeft ? attackLeft : attackRight;
                int frame = Mathf.FloorToInt(elapsed * framesPerSecond);
                ApplyFrame(texture, frame);
                if (!released && elapsed >= arrowReleaseDelay)
                {
                    FireArrow();
                    released = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            attackRoutine = null;
        }

        private void FireArrow()
        {
            int direction = facingLeft ? -1 : 1;
            Texture2D texture = facingLeft ? arrowLeft : arrowRight;
            GameObject arrowObject = new GameObject("SkeletonArrow");
            arrowObject.layer = gameObject.layer;
            arrowObject.transform.position = transform.position + Vector3.right * direction * arrowReleaseOffset;
            SpriteRenderer renderer = arrowObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateArrowSprite(texture);
            renderer.sortingOrder = 2;
            SkeletonArrow arrow = arrowObject.AddComponent<SkeletonArrow>();
            arrow.Configure(Vector2.right * direction, arrowSpeed, arrowLifetime, arrowRange, arrowDamage, playerLayers);
        }

        private Sprite CreateArrowSprite(Texture2D texture)
        {
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 32f);
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
