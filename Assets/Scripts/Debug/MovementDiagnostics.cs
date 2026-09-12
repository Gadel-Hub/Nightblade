using UnityEngine;
using UnityEngine.InputSystem;

namespace Nightblade
{
    // Optional scene tooling. The player prefab does not depend on this component.
    public sealed class MovementDiagnostics : MonoBehaviour
    {
        [SerializeField] private PlayerMovement player;
        [SerializeField] private BoxCollider2D playerCollider;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool visible;

        private void FixedUpdate()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                visible = !visible;
        }

        private void OnDisable() => visible = false;

        private void OnGUI()
        {
            if (!visible || player == null || !player.isActiveAndEnabled) return;
            Vector2 position = player.Position;
            Vector2 velocity = player.Velocity;
            GUI.Box(new Rect(8, 8, 290, 160), "");
            GUI.Label(new Rect(16, 12, 274, 150),
                $"MOVEMENT / F1 hides\n" +
                $"Position  {position.x:F3}, {position.y:F3}\n" +
                $"Velocity  {velocity.x:F3}, {velocity.y:F3}\n" +
                $"Ground {player.Grounded}  Left {player.LeftWall}  Right {player.RightWall}\n" +
                $"State  {player.State}\n" +
                $"Outward lock  {player.PushRemaining:F3}s\n" +
                $"Last wall  {player.LastJumpWall} (-1 left, +1 right)");
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enabled || !visible || playerCollider == null) return;
            Gizmos.color = Color.cyan;
            Bounds bounds = playerCollider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
#endif
    }
}
