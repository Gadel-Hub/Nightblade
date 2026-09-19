using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CombatTarget : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField] private Renderer placeholderVisual;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;

        private void Awake() => ResetTarget();

        public bool TakeDamage(int amount)
        {
            if (!IsAlive || amount <= 0) return false;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (!IsAlive && placeholderVisual != null)
                placeholderVisual.enabled = false;
            return true;
        }

        public void SetMaxHealth(int value)
        {
            maxHealth = Mathf.Max(1, value);
            ResetTarget();
        }

        public void ResetTarget()
        {
            CurrentHealth = maxHealth;
            if (placeholderVisual != null) placeholderVisual.enabled = true;
        }
    }
}
