using UnityEngine;

namespace Nightblade
{
    // Development-scene flow only. Production checkpoints remain a later system.
    public sealed class CombatLabRespawn : MonoBehaviour
    {
        [SerializeField] private PlayerDamage player;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private CombatTarget[] resetTargets;

        private void Awake()
        {
            if (player != null && spawnPoint != null) return;
            Debug.LogError("CombatLabRespawn needs a player and spawn point.", this);
            enabled = false;
        }

        private void Update()
        {
            if (!player.ReadyToRespawn) return;

            player.RespawnAt(spawnPoint.position);
            if (resetTargets == null) return;
            for (int i = 0; i < resetTargets.Length; i++)
                if (resetTargets[i] != null) resetTargets[i].ResetTarget();
        }
    }
}
