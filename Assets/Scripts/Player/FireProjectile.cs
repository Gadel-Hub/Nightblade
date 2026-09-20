using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    public sealed class FireProjectile : MonoBehaviour
    {
        private readonly List<Collider2D> overlaps = new List<Collider2D>(4);
        private ContactFilter2D targetFilter;
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
            targetFilter = new ContactFilter2D { useTriggers = true };
            targetFilter.SetLayerMask(targetLayers);
        }

        private void Update()
        {
            if (hit) return;

            float distance = speed * Time.deltaTime;
            transform.position += (Vector3)(direction * distance);
            travelled += distance;
            lifetime -= Time.deltaTime;
            if (travelled >= range || lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            overlaps.Clear();
            Physics2D.OverlapCircle(transform.position, 0.2f, targetFilter, overlaps);
            for (int i = 0; i < overlaps.Count; i++)
            {
                CombatTarget target = overlaps[i].GetComponentInParent<CombatTarget>();
                if (target == null || !target.TakeDamage(damage)) continue;
                hit = true;
                Destroy(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null) Destroy(renderer.sprite);
        }
    }
}
