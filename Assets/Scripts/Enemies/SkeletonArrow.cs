using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    public sealed class SkeletonArrow : MonoBehaviour
    {
        private readonly List<Collider2D> overlaps = new List<Collider2D>(4);
        private ContactFilter2D targetFilter;
        private Vector2 direction;
        private float speed;
        private float lifetime;
        private float range;
        private int damage;
        private float travelled;

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
            Physics2D.OverlapCircle(transform.position, 0.15f, targetFilter, overlaps);
            for (int i = 0; i < overlaps.Count; i++)
            {
                PlayerDamage target = overlaps[i].GetComponentInParent<PlayerDamage>();
                if (target == null) continue;
                target.ReceiveDamage(damage, transform.position.x);
                Destroy(gameObject);
                return;
            }
        }
    }
}
