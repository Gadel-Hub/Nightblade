using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [System.Serializable]
    public sealed class WaterSpriteSheet
    {
        [SerializeField] private Texture2D texture;
        [SerializeField, Min(1)] private int frameWidth = 64;
        [SerializeField, Min(1)] private int frameHeight = 64;
        [SerializeField, Min(0)] private int frameCount;
        [SerializeField, Min(0.01f)] private float framesPerSecond = 10f;

        public Texture2D Texture => texture;
        public int FrameWidth => frameWidth;
        public int FrameHeight => frameHeight;
        public int FrameCount => frameCount > 0 && texture != null ? frameCount : texture == null ? 0 : texture.width / frameWidth;
        public float FramesPerSecond => framesPerSecond;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerPresentation))]
    public sealed class WaterCharacterPresentation : MonoBehaviour
    {
        [SerializeField] private int waterCharacterIndex = 1;
        [SerializeField] private PlayerPresentation presentation;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Vector3 visualOffset;

        [Header("Movement")]
        [SerializeField] private WaterSpriteSheet movementLeft;
        [SerializeField] private WaterSpriteSheet movementRight;

        [Header("Attacks")]
        [SerializeField] private WaterSpriteSheet normalAttackLeft;
        [SerializeField] private WaterSpriteSheet normalAttackRight;
        [SerializeField] private WaterSpriteSheet movingAttackLeft;
        [SerializeField] private WaterSpriteSheet movingAttackRight;

        [Header("Shield and ultimate")]
        [SerializeField] private WaterSpriteSheet shield;
        [SerializeField] private WaterSpriteSheet ultimateLeft;
        [SerializeField] private WaterSpriteSheet ultimateRight;

        private readonly Dictionary<WaterSpriteSheet, Sprite[]> frames = new Dictionary<WaterSpriteSheet, Sprite[]>();
        private PlayerMovement movement;
        private PlayerCombat combat;
        private WaterSpriteSheet activeSheet;
        private PlayerPresentationState lastRequestedAction;
        private int lastActionRequestVersion;
        private float animationTime;
        private bool actionPlaying;
        private bool shieldPresentation;
        private bool facingLeft;

        private void Awake()
        {
            if (presentation == null) presentation = GetComponent<PlayerPresentation>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            if (spriteRenderer != null) spriteRenderer.transform.localPosition += visualOffset;
        }

        private void Update()
        {
            if (presentation == null || presentation.SelectedProfileIndex != waterCharacterIndex || spriteRenderer == null)
                return;

            UpdateFacing();
            if (combat != null && combat.IsAttacking && !actionPlaying)
                BeginAction(PlayerPresentationState.NormalAttack);
            if (presentation.ActionRequestVersion != lastActionRequestVersion ||
                presentation.LastRequestedAction != lastRequestedAction)
                BeginAction(presentation.LastRequestedAction);

            if (actionPlaying)
            {
                AdvanceAction();
                return;
            }

            ApplyMovement();
        }

        private void UpdateFacing()
        {
            if (movement == null) return;
            if (movement.Velocity.x < -0.01f) facingLeft = true;
            else if (movement.Velocity.x > 0.01f) facingLeft = false;
        }

        private void BeginAction(PlayerPresentationState state)
        {
            if (state == PlayerPresentationState.Idle || state == PlayerPresentationState.Walk) return;
            lastRequestedAction = state;
            lastActionRequestVersion = presentation.ActionRequestVersion;
            activeSheet = SelectActionSheet(state);
            animationTime = 0f;
            actionPlaying = activeSheet != null && activeSheet.FrameCount > 0;
            if (!actionPlaying) ApplyMovement();
        }

        private void AdvanceAction()
        {
            int frameCount = activeSheet.FrameCount;
            SetFrame(activeSheet, Mathf.Min(Mathf.FloorToInt(animationTime * activeSheet.FramesPerSecond), frameCount - 1));
            animationTime += Time.deltaTime;
            if (animationTime >= frameCount / activeSheet.FramesPerSecond)
            {
                actionPlaying = false;
                ApplyMovement();
            }
        }

        public void PlayPrimaryAttack() => BeginAction(PlayerPresentationState.NormalAttack);
        public void PlayShield() => BeginAction(PlayerPresentationState.DefensiveSkill);
        public void SetShieldPresentation(bool active)
        {
            shieldPresentation = active;
            if (!active) ApplyMovement();
        }

        private void ApplyMovement()
        {
            bool isMoving = movement != null && Mathf.Abs(movement.Velocity.x) > 0.01f;
            activeSheet = shieldPresentation ? shield : (facingLeft ? movementLeft : movementRight);
            if (activeSheet == null || activeSheet.FrameCount == 0) return;
            SetFrame(activeSheet, isMoving ? Mathf.FloorToInt(Time.time * activeSheet.FramesPerSecond) % activeSheet.FrameCount : 0);
        }

        private WaterSpriteSheet SelectActionSheet(PlayerPresentationState state)
        {
            bool isMoving = movement != null && Mathf.Abs(movement.Velocity.x) > 0.01f;
            switch (state)
            {
                case PlayerPresentationState.NormalAttack:
                    return isMoving
                        ? (facingLeft ? movingAttackLeft : movingAttackRight)
                        : (facingLeft ? normalAttackLeft : normalAttackRight);
                case PlayerPresentationState.Skill1:
                    return facingLeft ? movingAttackLeft : movingAttackRight;
                case PlayerPresentationState.DefensiveSkill:
                    return shield;
                case PlayerPresentationState.Ultimate:
                    return facingLeft ? ultimateLeft : ultimateRight;
                default:
                    return null;
            }
        }

        private void SetFrame(WaterSpriteSheet sheet, int index)
        {
            Sprite[] sheetFrames = GetFrames(sheet);
            if (sheetFrames == null || index < 0 || index >= sheetFrames.Length) return;
            spriteRenderer.flipX = sheet == shield && facingLeft;
            spriteRenderer.sprite = sheetFrames[index];
        }

        private Sprite[] GetFrames(WaterSpriteSheet sheet)
        {
            if (sheet == null || sheet.Texture == null) return null;
            if (frames.TryGetValue(sheet, out Sprite[] cached)) return cached;

            Sprite[] created = new Sprite[sheet.FrameCount];
            for (int i = 0; i < created.Length; i++)
            {
                Rect rect = new Rect(i * sheet.FrameWidth, 0f, sheet.FrameWidth, sheet.FrameHeight);
                created[i] = Sprite.Create(sheet.Texture, rect, new Vector2(0.5f, 0.5f), 32f);
                created[i].name = "Water_" + sheet.Texture.name + "_" + i;
            }
            frames.Add(sheet, created);
            return created;
        }
    }
}
