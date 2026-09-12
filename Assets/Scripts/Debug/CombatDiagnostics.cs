using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nightblade
{
    // Optional scene tooling. Combat components do not depend on this overlay.
    public sealed class CombatDiagnostics : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerDamage damage;
        [SerializeField] private BoxCollider2D playerHurtbox;
        [SerializeField] private BoxCollider2D attackHitbox;
        [SerializeField] private CombatTarget[] targets;
        [SerializeField] private DamageSource[] damageSources;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool visible;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                visible = !visible;
        }

        private void OnDisable() => visible = false;

        private void OnGUI()
        {
            if (!visible || movement == null || combat == null || health == null || damage == null) return;

            StringBuilder text = new StringBuilder(320);
            text.AppendLine("COMBAT LAB / F1 hides");
            text.AppendLine($"Position  {movement.Position.x:F3}, {movement.Position.y:F3}");
            text.AppendLine($"Velocity  {movement.Velocity.x:F3}, {movement.Velocity.y:F3}");
            text.AppendLine($"Ground {movement.Grounded}  Left {movement.LeftWall}  Right {movement.RightWall}");
            text.AppendLine($"Movement {movement.State}  Control {movement.ControlEnabled}");
            text.AppendLine($"Attack {combat.Phase}  Box {combat.HitboxActive}  Hits {combat.HitCount}");
            text.AppendLine($"Health {health.CurrentHealth}/{health.MaxHealth}");
            text.AppendLine($"Damage {damage.State}  Hit {damage.IsInHitReaction}");
            text.AppendLine($"Invulnerable {damage.IsInvulnerable}  {damage.InvulnerabilityRemaining:F3}s");
            AppendTargets(text);

            GUI.Box(new Rect(8, 8, 304, 220), "");
            GUI.Label(new Rect(16, 12, 288, 208), text.ToString());
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enabled || !visible) return;

            if (playerHurtbox != null && damage != null && damage.State != PlayerDamage.DamageState.Dead)
            {
                Gizmos.color = Color.cyan;
                DrawBounds(playerHurtbox.bounds);
            }
            if (attackHitbox != null && combat != null && combat.HitboxActive)
            {
                Gizmos.color = Color.yellow;
                DrawBounds(attackHitbox.bounds);
            }
            if (targets != null)
            {
                Gizmos.color = new Color(1f, 0.55f, 0.2f);
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] == null || !targets[i].IsAlive) continue;
                    BoxCollider2D hurtbox = targets[i].GetComponent<BoxCollider2D>();
                    if (hurtbox != null) DrawBounds(hurtbox.bounds);
                }
            }
            if (damageSources != null)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < damageSources.Length; i++)
                    if (damageSources[i] != null) DrawBounds(damageSources[i].Bounds);
            }
        }

        private void AppendTargets(StringBuilder text)
        {
            if (targets == null) return;
            for (int i = 0; i < targets.Length; i++)
            {
                CombatTarget target = targets[i];
                if (target != null)
                    text.AppendLine($"Target {i + 1}  {target.CurrentHealth}/{target.MaxHealth}");
            }
        }

        private static void DrawBounds(Bounds bounds) => Gizmos.DrawWireCube(bounds.center, bounds.size);
#endif
    }
}
