using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    public enum PlayerPresentationState
    {
        Idle,
        Walk,
        NormalAttack,
        Skill1,
        Skill2,
        DefensiveSkill,
        Ultimate,
        Hurt,
        Death
    }

    [Serializable]
    public sealed class PlayerPresentationProfile
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private RuntimeAnimatorController animatorController;

        public Sprite Sprite => sprite;
        public GameObject VisualRoot => visualRoot;
        public RuntimeAnimatorController AnimatorController => animatorController;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerDamage))]
    public sealed class PlayerPresentation : MonoBehaviour
    {
        private const int CharacterCount = 3;

        [SerializeField] private GameObject sharedVisualRoot;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerPresentationProfile[] characters = CreateDefaultCharacters();

        [Header("Animator parameters")]
        [SerializeField] private string locomotionParameter = "Locomotion";
        [SerializeField] private string normalAttackTrigger = "NormalAttack";
        [SerializeField] private string skill1Trigger = "Skill1";
        [SerializeField] private string skill2Trigger = "Skill2";
        [SerializeField] private string defensiveSkillTrigger = "DefensiveSkill";
        [SerializeField] private string ultimateTrigger = "Ultimate";
        [SerializeField] private string hurtTrigger = "Hurt";
        [SerializeField] private string deathTrigger = "Death";

        private readonly Dictionary<SpriteRenderer, Sprite> defaultSprites = new Dictionary<SpriteRenderer, Sprite>();
        private readonly Dictionary<Animator, RuntimeAnimatorController> defaultControllers = new Dictionary<Animator, RuntimeAnimatorController>();
        private PlayerMovement movement;
        private PlayerDamage damage;
        private SpriteRenderer sharedSpriteRenderer;
        private Animator sharedAnimator;
        private GameObject activeCharacterRoot;
        private PlayerDamage.DamageState previousDamageState;
        private bool facingLeft;

        public int SelectedProfileIndex { get; private set; } = -1;
        public PlayerPresentationState CurrentState { get; private set; } = PlayerPresentationState.Idle;
        public PlayerPresentationState LastRequestedAction { get; private set; } = PlayerPresentationState.Idle;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            damage = GetComponent<PlayerDamage>();

            if (sharedVisualRoot == null)
                sharedVisualRoot = damage.transform.Find("Visual")?.gameObject;
            if (spriteRenderer == null && sharedVisualRoot != null)
                spriteRenderer = sharedVisualRoot.GetComponentInChildren<SpriteRenderer>(true);
            if (animator == null && sharedVisualRoot != null)
                animator = sharedVisualRoot.GetComponentInChildren<Animator>(true);
            sharedSpriteRenderer = spriteRenderer;
            sharedAnimator = animator;

            if (characters == null || characters.Length != CharacterCount)
            {
                Debug.LogError("PlayerPresentation needs exactly three character profiles.", this);
                enabled = false;
                return;
            }

            CacheDefaults(spriteRenderer, animator);
            previousDamageState = damage.State;
        }

        private void Update()
        {
            UpdateFacing();
            UpdateDamagePresentation();
            CurrentState = GetLocomotionState();

            if (CurrentState == PlayerPresentationState.Hurt || CurrentState == PlayerPresentationState.Death)
                return;

            SetLocomotionParameter(CurrentState == PlayerPresentationState.Walk ? 1 : 0);
        }

        public bool SelectCharacter(int index)
        {
            if (characters == null || index < 0 || index >= characters.Length) return false;

            PlayerPresentationProfile profile = characters[index];
            if (profile == null) return false;
            GameObject targetRoot = profile.VisualRoot != null ? profile.VisualRoot : sharedVisualRoot;
            if (profile.VisualRoot != null && sharedVisualRoot != null &&
                profile.VisualRoot != sharedVisualRoot &&
                !profile.VisualRoot.transform.IsChildOf(sharedVisualRoot.transform))
            {
                Debug.LogError("Character visual roots must be inside the shared player visual root.", profile.VisualRoot);
                return false;
            }

            SetCharacterRoots(index);
            if (activeCharacterRoot != null && activeCharacterRoot != sharedVisualRoot && activeCharacterRoot != targetRoot)
                activeCharacterRoot.SetActive(false);
            if (targetRoot != null) targetRoot.SetActive(true);
            activeCharacterRoot = targetRoot;

            SpriteRenderer selectedRenderer = targetRoot != null
                ? targetRoot.GetComponentInChildren<SpriteRenderer>(true)
                : sharedSpriteRenderer;
            Animator selectedAnimator = targetRoot != null
                ? targetRoot.GetComponentInChildren<Animator>(true)
                : sharedAnimator;

            if (selectedRenderer == null) selectedRenderer = sharedSpriteRenderer;
            if (selectedAnimator == null) selectedAnimator = sharedAnimator;
            CacheDefaults(selectedRenderer, selectedAnimator);

            if (selectedRenderer != null)
            {
                spriteRenderer = selectedRenderer;
                spriteRenderer.sprite = profile.Sprite != null ? profile.Sprite : defaultSprites[spriteRenderer];
                spriteRenderer.flipX = facingLeft;
            }

            if (selectedAnimator != null)
            {
                animator = selectedAnimator;
                animator.runtimeAnimatorController = profile.AnimatorController != null
                    ? profile.AnimatorController
                    : defaultControllers[animator];
            }

            SelectedProfileIndex = index;
            return true;
        }

        public void PlayNormalAttack() => RequestAction(PlayerPresentationState.NormalAttack, normalAttackTrigger);
        public void PlaySkill1() => RequestAction(PlayerPresentationState.Skill1, skill1Trigger);
        public void PlaySkill2() => RequestAction(PlayerPresentationState.Skill2, skill2Trigger);
        public void PlayDefensiveSkill() => RequestAction(PlayerPresentationState.DefensiveSkill, defensiveSkillTrigger);
        public void PlayUltimate() => RequestAction(PlayerPresentationState.Ultimate, ultimateTrigger);
        public void PlayHurt() => RequestAction(PlayerPresentationState.Hurt, hurtTrigger);
        public void PlayDeath() => RequestAction(PlayerPresentationState.Death, deathTrigger);

        private void UpdateFacing()
        {
            float horizontalVelocity = movement.Velocity.x;
            if (horizontalVelocity < -0.01f) facingLeft = true;
            else if (horizontalVelocity > 0.01f) facingLeft = false;

            if (spriteRenderer != null) spriteRenderer.flipX = facingLeft;
        }

        private void UpdateDamagePresentation()
        {
            if (damage.State == previousDamageState) return;

            if (damage.State == PlayerDamage.DamageState.Hit)
                PlayHurt();
            else if (damage.State == PlayerDamage.DamageState.Dead)
                PlayDeath();

            previousDamageState = damage.State;
        }

        private PlayerPresentationState GetLocomotionState()
        {
            if (damage.State == PlayerDamage.DamageState.Dead) return PlayerPresentationState.Death;
            if (damage.State == PlayerDamage.DamageState.Hit) return PlayerPresentationState.Hurt;

            return movement.State == PlayerMovement.MovementState.Run || Mathf.Abs(movement.Velocity.x) > 0.01f
                ? PlayerPresentationState.Walk
                : PlayerPresentationState.Idle;
        }

        private void SetLocomotionParameter(int value)
        {
            if (animator == null || animator.runtimeAnimatorController == null ||
                !HasParameter(animator, locomotionParameter, AnimatorControllerParameterType.Int)) return;

            if (animator.GetInteger(locomotionParameter) != value)
                animator.SetInteger(locomotionParameter, value);
        }

        private void RequestAction(PlayerPresentationState state, string trigger)
        {
            LastRequestedAction = state;
            if (animator == null || animator.runtimeAnimatorController == null ||
                !HasParameter(animator, trigger, AnimatorControllerParameterType.Trigger)) return;

            animator.SetTrigger(trigger);
        }

        private static bool HasParameter(Animator target, string parameterName, AnimatorControllerParameterType type)
        {
            if (string.IsNullOrEmpty(parameterName)) return false;
            AnimatorControllerParameter[] parameters = target.parameters;
            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i].name == parameterName && parameters[i].type == type) return true;

            return false;
        }

        private void SetCharacterRoots(int selectedIndex)
        {
            GameObject selectedRoot = characters[selectedIndex]?.VisualRoot;
            for (int i = 0; i < characters.Length; i++)
            {
                GameObject root = characters[i]?.VisualRoot;
                if (root != null && root != sharedVisualRoot)
                    root.SetActive(root == selectedRoot);
            }
        }

        private void CacheDefaults(SpriteRenderer renderer, Animator targetAnimator)
        {
            if (renderer != null && !defaultSprites.ContainsKey(renderer))
                defaultSprites.Add(renderer, renderer.sprite);
            if (targetAnimator != null && !defaultControllers.ContainsKey(targetAnimator))
                defaultControllers.Add(targetAnimator, targetAnimator.runtimeAnimatorController);
        }

        private static PlayerPresentationProfile[] CreateDefaultCharacters()
        {
            return new[]
            {
                new PlayerPresentationProfile(),
                new PlayerPresentationProfile(),
                new PlayerPresentationProfile()
            };
        }
    }
}
