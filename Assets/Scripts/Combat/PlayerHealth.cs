using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 3;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDepleted => CurrentHealth == 0;

        private void Awake() => RestoreFull();

        public bool TakeDamage(int amount)
        {
            if (amount <= 0 || IsDepleted) return false;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            return true;
        }

        public void RestoreFull() => CurrentHealth = maxHealth;
    }
}
