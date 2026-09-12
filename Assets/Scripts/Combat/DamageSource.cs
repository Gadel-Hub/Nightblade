using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class DamageSource : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 1;

        public Bounds Bounds => GetComponent<BoxCollider2D>().bounds;

        private void Awake()
        {
            if (GetComponent<BoxCollider2D>().isTrigger) return;
            Debug.LogError("DamageSource requires a trigger collider.", this);
            enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // The player component is on the explicit body/hurtbox collider.
            // Child attack colliders must never receive environmental damage.
            if (other.TryGetComponent(out PlayerDamage player))
                player.ReceiveDamage(damage, transform.position.x);
        }
    }
}
