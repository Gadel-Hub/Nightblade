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

        public void Configure(Vector2 travelDirection, float travelSpeed, float maxLifetime, float maxRange,
            Vector2 gameplaySize, int damageAmount, LayerMask targetLayers)
        {
            direction = travelDirection.normalized;
            speed = Mathf.Max(0f, travelSpeed);
            lifetime = Mathf.Max(0.01f, maxLifetime);
            range = Mathf.Max(0.01f, maxRange);
            hitboxSize = new Vector2(Mathf.Max(0.01f, gameplaySize.x), Mathf.Max(0.01f, gameplaySize.y));
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
    }
}
