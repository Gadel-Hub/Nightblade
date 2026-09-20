using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    public sealed class WaterWave : MonoBehaviour
    {
        private readonly HashSet<CombatTarget> damagedTargets = new HashSet<CombatTarget>();
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private ContactFilter2D targetFilter;
        private Vector2 direction;
        private Vector2 hitboxSize;
        private float speed;
        private float lifetime;
        private float range;
        private int damage;
        private float travelled;
        private Texture2D animationSheet;
        private SpriteRenderer spriteRenderer;
        private Sprite[] frames;
        private float framesPerSecond;
        private float animationElapsed;

        public void Configure(Vector2 travelDirection, float travelSpeed, float maxLifetime, float maxRange,
            Vector2 gameplaySize, int damageAmount, LayerMask targetLayers, Texture2D sheet,
            SpriteRenderer renderer, int frameWidth, int frameHeight, float animationFramesPerSecond)
        {
            direction = travelDirection.normalized;
            speed = Mathf.Max(0f, travelSpeed);
            lifetime = Mathf.Max(0.01f, maxLifetime);
            range = Mathf.Max(0.01f, maxRange);
            hitboxSize = new Vector2(Mathf.Max(0.01f, gameplaySize.x), Mathf.Max(0.01f, gameplaySize.y));
            damage = Mathf.Max(1, damageAmount);
            targetFilter = new ContactFilter2D { useTriggers = true };
            targetFilter.SetLayerMask(targetLayers);
            animationSheet = sheet;
            spriteRenderer = renderer;
            framesPerSecond = Mathf.Max(0.01f, animationFramesPerSecond);
            int frameCount = animationSheet == null ? 0 : animationSheet.width / frameWidth;
            frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
                frames[i] = Sprite.Create(animationSheet, new Rect(i * frameWidth, 0f, frameWidth, frameHeight),
                    new Vector2(0.5f, 0.5f), 32f);
        }

        private void Update()
        {
            float distance = speed * Time.deltaTime;
            transform.position += (Vector3)(direction * distance);
            travelled += distance;
            lifetime -= Time.deltaTime;
            animationElapsed += Time.deltaTime;
            if (spriteRenderer != null && frames != null && frames.Length > 0)
                spriteRenderer.sprite = frames[Mathf.FloorToInt(animationElapsed * framesPerSecond) % frames.Length];

            overlaps.Clear();
            Physics2D.OverlapBox(transform.position, hitboxSize, 0f, targetFilter, overlaps);
            for (int i = 0; i < overlaps.Count; i++)
            {
                CombatTarget target = overlaps[i].GetComponentInParent<CombatTarget>();
                if (target != null && !damagedTargets.Contains(target) && target.TakeDamage(damage))
                    damagedTargets.Add(target);
            }

            if (travelled >= range || lifetime <= 0f)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (frames == null) return;
            for (int i = 0; i < frames.Length; i++)
                if (frames[i] != null) Destroy(frames[i]);
        }
    }
}
