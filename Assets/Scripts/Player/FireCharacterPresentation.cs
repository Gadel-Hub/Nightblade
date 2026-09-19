using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [System.Serializable]
    public sealed class FireSpriteSheet
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
    public sealed class FireCharacterPresentation : MonoBehaviour
    {
        [SerializeField] private int fireCharacterIndex;
        [SerializeField] private PlayerPresentation presentation;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Movement")]
        [SerializeField] private FireSpriteSheet idleLeft;
        [SerializeField] private FireSpriteSheet idleRight;
        [SerializeField] private FireSpriteSheet movementRight;

        [Header("Attacks")]
        [SerializeField] private FireSpriteSheet normalAttackLeft;
        [SerializeField] private FireSpriteSheet normalAttackRight;
        [SerializeField] private FireSpriteSheet meleeAttackLeft;
        [SerializeField] private FireSpriteSheet meleeAttackRight;

        [Header("Shield")]
        [SerializeField] private FireSpriteSheet shieldIdleLeft;
        [SerializeField] private FireSpriteSheet shieldIdleRight;
        [SerializeField] private FireSpriteSheet shieldMovingLeft;
        [SerializeField] private FireSpriteSheet shieldMovingRight;

        private readonly Dictionary<FireSpriteSheet, Sprite[]> frames = new Dictionary<FireSpriteSheet, Sprite[]>();
        private PlayerMovement movement;
        private PlayerCombat combat;
        private PlayerPresentationState lastRequestedAction;
        private FireSpriteSheet activeSheet;
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
        }

        private void Update()
        {
            if (presentation == null || presentation.SelectedProfileIndex != fireCharacterIndex || spriteRenderer == null)
                return;

            if (combat != null && combat.IsAttacking && !actionPlaying)
                BeginAction(PlayerPresentationState.Skill1, true);

            if (movement != null)
            {
                if (movement.Velocity.x < -0.01f) facingLeft = true;
                else if (movement.Velocity.x > 0.01f) facingLeft = false;
            }

            if (presentation.LastRequestedAction != lastRequestedAction)
                BeginAction(presentation.LastRequestedAction);

            if (actionPlaying)
            {
                AdvanceAnimation();
                return;
            }

            ApplyLocomotion();
        }

        public void PlayNormalAttack() => BeginAction(PlayerPresentationState.NormalAttack, true);
        public void PlayMeleeAttack() => BeginAction(PlayerPresentationState.Skill1, true);
        public void PlayUltimate() => BeginAction(PlayerPresentationState.Ultimate, true);

        public void SetShieldPresentation(bool active)
        {
            shieldPresentation = active;
            if (!active) ApplyLocomotion();
        }

        private void BeginAction(PlayerPresentationState state, bool force = false)
        {
            if (!force && state == PlayerPresentationState.Idle) return;
            lastRequestedAction = state;
            activeSheet = SelectSheet(state);
            animationTime = 0f;
            actionPlaying = activeSheet != null && activeSheet.FrameCount > 0;
            if (!actionPlaying) ApplyLocomotion();
        }

        private void AdvanceAnimation()
        {
            int count = activeSheet.FrameCount;
            int frame = Mathf.Min(Mathf.FloorToInt(animationTime * activeSheet.FramesPerSecond), count - 1);
            SetFrame(activeSheet, frame);
            animationTime += Time.deltaTime;
            if (animationTime >= count / activeSheet.FramesPerSecond)
            {
                actionPlaying = false;
                ApplyLocomotion();
            }
        }

        private void ApplyLocomotion()
        {
            bool moving = movement != null && Mathf.Abs(movement.Velocity.x) > 0.01f;
            FireSpriteSheet sheet;
            if (shieldPresentation)
                sheet = moving ? SelectFacingSheet(shieldMovingLeft, shieldMovingRight) : SelectFacingSheet(shieldIdleLeft, shieldIdleRight);
            else if (moving)
                sheet = movementRight;
            else
                sheet = SelectFacingSheet(idleLeft, idleRight);

            if (sheet == null || sheet.FrameCount == 0) return;
            activeSheet = sheet;
            spriteRenderer.flipX = sheet == movementRight && facingLeft;
            SetFrame(sheet, Mathf.FloorToInt(Time.time * sheet.FramesPerSecond) % sheet.FrameCount);
        }

        private FireSpriteSheet SelectSheet(PlayerPresentationState state)
        {
            switch (state)
            {
                case PlayerPresentationState.NormalAttack: return SelectFacingSheet(normalAttackLeft, normalAttackRight);
                case PlayerPresentationState.Skill1: return SelectFacingSheet(meleeAttackLeft, meleeAttackRight);
                case PlayerPresentationState.DefensiveSkill: return SelectFacingSheet(shieldIdleLeft, shieldIdleRight);
                default: return null;
            }
        }

        private FireSpriteSheet SelectFacingSheet(FireSpriteSheet left, FireSpriteSheet right)
        {
            return facingLeft ? left : right;
        }

        private void SetFrame(FireSpriteSheet sheet, int index)
        {
            Sprite[] sheetFrames = GetFrames(sheet);
            if (sheetFrames != null && index >= 0 && index < sheetFrames.Length)
            {
                spriteRenderer.flipX = sheet == movementRight && facingLeft;
                spriteRenderer.sprite = sheetFrames[index];
            }
        }

        private Sprite[] GetFrames(FireSpriteSheet sheet)
        {
            if (sheet == null || sheet.Texture == null) return null;
            if (frames.TryGetValue(sheet, out Sprite[] cached)) return cached;

            int count = sheet.FrameCount;
            Sprite[] created = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                Rect rect = new Rect(i * sheet.FrameWidth, 0f, sheet.FrameWidth, sheet.FrameHeight);
                created[i] = Sprite.Create(sheet.Texture, rect, new Vector2(0.5f, 0.5f), 32f);
                created[i].name = "Fire_" + sheet.Texture.name + "_" + i;
            }
            frames.Add(sheet, created);
            return created;
        }
    }
}
