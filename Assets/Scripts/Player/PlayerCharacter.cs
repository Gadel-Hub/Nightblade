using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Nightblade
{
    public enum PlayerSkillSlot
    {
        NormalSkill1,
        NormalSkill2,
        DefensiveSkill,
        Ultimate
    }

    [Serializable]
    public sealed class PlayerSkillHook
    {
        [SerializeField] private UnityEvent activate = new UnityEvent();
        [SerializeField, Min(0f)] private float cooldownSeconds;

        public float CooldownSeconds => cooldownSeconds;
        public void Activate() => activate?.Invoke();
    }

    [Serializable]
    public sealed class PlayerCharacterProfile
    {
        [SerializeField] private string displayName;
        [SerializeField] private PlayerSkillHook normalSkill1 = new PlayerSkillHook();
        [SerializeField] private PlayerSkillHook normalSkill2 = new PlayerSkillHook();
        [SerializeField] private PlayerSkillHook defensiveSkill = new PlayerSkillHook();
        [SerializeField] private PlayerSkillHook ultimate = new PlayerSkillHook();

        public string DisplayName => displayName;

        public PlayerCharacterProfile() : this("Character") { }

        public PlayerSkillHook GetSkill(PlayerSkillSlot slot)
        {
            switch (slot)
            {
                case PlayerSkillSlot.NormalSkill1: return normalSkill1;
                case PlayerSkillSlot.NormalSkill2: return normalSkill2;
                case PlayerSkillSlot.DefensiveSkill: return defensiveSkill;
                case PlayerSkillSlot.Ultimate: return ultimate;
                default: throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
            }
        }

        public PlayerCharacterProfile(string displayName)
        {
            this.displayName = displayName;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerCombat), typeof(PlayerPresentation))]
    public sealed class PlayerCharacter : MonoBehaviour
    {
        private const int CharacterCount = 3;

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private PlayerCharacterProfile[] characters = CreateDefaultCharacters();
        [SerializeField] private UnityEvent runStarted = new UnityEvent();

        private readonly float[] cooldownRemaining = new float[4];
        private readonly InputAction[] skillActions = new InputAction[4];
        private readonly Action<InputAction.CallbackContext>[] skillCallbacks = new Action<InputAction.CallbackContext>[4];

        private InputActionAsset runtimeActions;
        private InputActionMap playerActions;
        private Rigidbody2D body;
        private PlayerMovement movement;
        private PlayerCombat combat;
        private PlayerPresentation presentation;
        private int selectedIndex = -1;
        private bool selectionConfirmed;
        private bool runInProgress;

        public int SelectedIndex => selectedIndex;
        public bool HasSelection => selectionConfirmed && selectedIndex >= 0;
        public bool RunInProgress => runInProgress;
        public IReadOnlyList<PlayerCharacterProfile> Characters => characters;

        private void Awake()
        {
            if (characters == null || characters.Length != CharacterCount)
            {
                Debug.LogError("PlayerCharacter needs exactly three character profiles.", this);
                enabled = false;
                return;
            }

            if (inputActions == null)
            {
                Debug.LogError("PlayerCharacter needs the shared player input actions.", this);
                enabled = false;
                return;
            }

            runtimeActions = Instantiate(inputActions);
            playerActions = runtimeActions.FindActionMap("Player", true);
            body = GetComponent<Rigidbody2D>();
            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            presentation = GetComponent<PlayerPresentation>();
            skillActions[0] = playerActions.FindAction("NormalSkill1", true);
            skillActions[1] = playerActions.FindAction("NormalSkill2", true);
            skillActions[2] = playerActions.FindAction("DefensiveSkill", true);
            skillActions[3] = playerActions.FindAction("Ultimate", true);

            for (int i = 0; i < skillCallbacks.Length; i++)
            {
                int slotIndex = i;
                skillCallbacks[i] = _ => TryUseSkill((PlayerSkillSlot)slotIndex);
            }

            body.simulated = false;
            movement.SetControlEnabled(false);
            combat.SetControlEnabled(false);
        }

        private void OnEnable()
        {
            for (int i = 0; i < skillActions.Length; i++)
                if (skillActions[i] != null) skillActions[i].performed += skillCallbacks[i];
            playerActions?.Enable();
        }

        private void OnDisable()
        {
            for (int i = 0; i < skillActions.Length; i++)
                if (skillActions[i] != null && skillCallbacks[i] != null)
                    skillActions[i].performed -= skillCallbacks[i];
            playerActions?.Disable();
        }

        private void OnDestroy()
        {
            if (runtimeActions != null) Destroy(runtimeActions);
        }

        private void Update()
        {
            for (int i = 0; i < cooldownRemaining.Length; i++)
                cooldownRemaining[i] = Mathf.Max(0f, cooldownRemaining[i] - Time.deltaTime);
        }

        public bool SelectCharacter(int index)
        {
            if (runInProgress || selectionConfirmed || index < 0 || index >= characters.Length)
                return false;
            if (!presentation.SelectCharacter(index)) return false;

            selectedIndex = index;

            Array.Clear(cooldownRemaining, 0, cooldownRemaining.Length);
            return true;
        }

        public bool ConfirmSelection()
        {
            if (runInProgress || selectedIndex < 0) return false;
            selectionConfirmed = true;
            return true;
        }

        public bool BeginRun()
        {
            if (!HasSelection || runInProgress) return false;

            runInProgress = true;
            body.simulated = true;
            movement.SetControlEnabled(true);
            combat.SetControlEnabled(true);
            runStarted.Invoke();
            return true;
        }

        public bool TryUseSkill(PlayerSkillSlot slot)
        {
            if (!runInProgress || selectedIndex < 0) return false;

            int index = (int)slot;
            if (index < 0 || index >= cooldownRemaining.Length || cooldownRemaining[index] > 0f)
                return false;

            PlayerSkillHook skill = characters[selectedIndex].GetSkill(slot);
            skill.Activate();
            cooldownRemaining[index] = skill.CooldownSeconds;
            return true;
        }

        public float GetCooldownRemaining(PlayerSkillSlot slot)
        {
            int index = (int)slot;
            if (index < 0 || index >= cooldownRemaining.Length)
                throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
            return cooldownRemaining[index];
        }

        private void OnGUI()
        {
            if (runInProgress) return;

            GUILayout.BeginArea(new Rect(8f, 8f, 230f, 190f), GUI.skin.box);
            GUILayout.Label("Choose Character");
            for (int i = 0; i < characters.Length; i++)
            {
                if (!selectionConfirmed && GUILayout.Button(characters[i].DisplayName))
                    SelectCharacter(i);
            }

            if (selectedIndex >= 0)
                GUILayout.Label("Selected: " + characters[selectedIndex].DisplayName);
            if (!selectionConfirmed && GUILayout.Button("Confirm Selection"))
                ConfirmSelection();
            if (HasSelection && GUILayout.Button("Begin Run"))
                BeginRun();
            GUILayout.EndArea();
        }

        private static PlayerCharacterProfile[] CreateDefaultCharacters()
        {
            return new[]
            {
                new PlayerCharacterProfile("Character 1"),
                new PlayerCharacterProfile("Character 2"),
                new PlayerCharacterProfile("Character 3")
            };
        }
    }
}
