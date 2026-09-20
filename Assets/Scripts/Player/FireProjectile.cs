using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    public sealed class FireProjectile : MonoBehaviour
    {
        private LayerMask targetLayers;
        private Vector2 direction;
        private float speed;
        private float lifetime;
        private float range;
        private int damage;
        private float travelled;
        private bool hit;

        public void Configure(Vector2 travelDirection, float travelSpeed, float maxLifetime, float maxRange,
            int damageAmount, LayerMask targetLayers)
        {
            direction = travelDirection.normalized;
            speed = Mathf.Max(0f, travelSpeed);
            lifetime = Mathf.Max(0.01f, maxLifetime);
            range = Mathf.Max(0.01f, maxRange);
            damage = Mathf.Max(1, damageAmount);
            this.targetLayers = targetLayers;
        }

        private void Update()
        {
            if (hit || Time.deltaTime <= 0f) return;

            Vector2 start = transform.position;
            float activeDelta = Mathf.Min(Time.deltaTime, lifetime);
            float distance = Mathf.Min(speed * activeDelta, range - travelled);
            Vector2 end = start + direction * distance;

            CombatTarget[] targets = FindObjectsByType<CombatTarget>(FindObjectsSortMode.None);
            int samples = Mathf.Max(1, Mathf.CeilToInt(distance / 0.1f));
            for (int sample = 1; sample <= samples; sample++)
            {
                Vector2 samplePosition = Vector2.Lerp(start, end, sample / (float)samples);
                for (int i = 0; i < targets.Length; i++)
                {
                    CombatTarget target = targets[i];
                    if (target == null || (targetLayers.value & (1 << target.gameObject.layer)) == 0) continue;

                    Collider2D targetCollider = target.GetComponent<Collider2D>();
                    if (targetCollider == null ||
                        (targetCollider.ClosestPoint(samplePosition) - samplePosition).sqrMagnitude > 0.04f)
                        continue;
                    if (!target.TakeDamage(damage)) continue;

                    hit = true;
                    Destroy(gameObject);
                    return;
                }
            }

            transform.position = end;
            travelled += distance;
            lifetime -= Time.deltaTime;
            if (travelled >= range || lifetime <= 0f)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null) Destroy(renderer.sprite);
        }
    }
}
