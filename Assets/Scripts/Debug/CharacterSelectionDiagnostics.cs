using UnityEngine;
using UnityEngine.InputSystem;

namespace Nightblade
{
    public sealed class CharacterSelectionDiagnostics : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter player;

        private PlayerPresentation presentation;

        private void Awake()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerCharacter>();
            if (player != null)
                presentation = player.GetComponent<PlayerPresentation>();
        }

        private void Update()
        {
            if (player == null || Keyboard.current == null) return;

            if (presentation != null && player.HasSelection)
            {
                if (Keyboard.current.jKey.wasPressedThisFrame) presentation.PlayNormalAttack();
                else if (Keyboard.current.kKey.wasPressedThisFrame) presentation.PlaySkill1();
                else if (Keyboard.current.lKey.wasPressedThisFrame) presentation.PlayDefensiveSkill();
                else if (Keyboard.current.uKey.wasPressedThisFrame) presentation.PlayUltimate();
            }

            if (!player.HasSelection)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) player.SelectCharacter(0);
                else if (Keyboard.current.digit2Key.wasPressedThisFrame) player.SelectCharacter(1);
                else if (Keyboard.current.digit3Key.wasPressedThisFrame) player.SelectCharacter(2);
            }

            if (!Keyboard.current.enterKey.wasPressedThisFrame && !Keyboard.current.spaceKey.wasPressedThisFrame)
                return;

            if (player.SelectedIndex < 0) return;
            if (!player.HasSelection) player.ConfirmSelection();
            else if (!player.RunInProgress)
                player.BeginRun();
        }

        private void OnGUI()
        {
            if (player == null) return;

            string selected = player.SelectedIndex >= 0
                ? player.Characters[player.SelectedIndex].DisplayName
                : "None";
            string status = player.RunInProgress ? "Running" : player.HasSelection ? "Ready" : "Choose a character";
            GUI.Box(new Rect(8f, 8f, 300f, 110f), "Character Lab");
            GUI.Label(new Rect(18f, 30f, 280f, 80f),
                "1/2/3: select    Enter/Space: confirm/start\n" +
                "J/K/L/U: attack/moving/shield/ultimate\n" +
                "Selected: " + selected + "\nStatus: " + status);
        }
    }
}
