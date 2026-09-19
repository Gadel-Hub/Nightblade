using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [System.Serializable]
    public sealed class AirSpriteSheet
    {
        [SerializeField] private Texture2D texture;
        [SerializeField, Min(1)] private int frameWidth = 144;
        [SerializeField, Min(1)] private int frameHeight = 176;
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
    public sealed class AirCharacterPresentation : MonoBehaviour
    {
        [SerializeField] private int airCharacterIndex = 2;
        [SerializeField] private PlayerPresentation presentation;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Vector3 visualOffset;

        [Header("Locomotion")]
        [SerializeField] private AirSpriteSheet idleLeft;
        [SerializeField] private AirSpriteSheet idleRight;
        [SerializeField] private AirSpriteSheet movementLeft;
        [SerializeField] private AirSpriteSheet movementRight;

        [Header("Authored actions")]
        [SerializeField] private AirSpriteSheet normalAttackLeft;
        [SerializeField] private AirSpriteSheet normalAttackRight;
        [SerializeField] private AirSpriteSheet shieldLeft;
        [SerializeField] private AirSpriteSheet shieldRight;
        [SerializeField] private AirSpriteSheet ultimateLeft;
        [SerializeField] private AirSpriteSheet ultimateRight;

        private readonly Dictionary<AirSpriteSheet, Sprite[]> frames = new Dictionary<AirSpriteSheet, Sprite[]>();
        private PlayerMovement movement;
        private PlayerCombat combat;
        private AirSpriteSheet activeSheet;
        private PlayerPresentationState lastRequestedAction;
        private int lastActionRequestVersion;
        private float animationTime;
        private bool actionPlaying;
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
            if (presentation == null || presentation.SelectedProfileIndex != airCharacterIndex || spriteRenderer == null)
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

        private void ApplyMovement()
        {
            bool isMoving = movement != null && Mathf.Abs(movement.Velocity.x) > 0.01f;
            activeSheet = isMoving
                ? (facingLeft ? movementLeft : movementRight)
                : (facingLeft ? idleLeft : idleRight);
            if (activeSheet == null || activeSheet.FrameCount == 0) return;
            SetFrame(activeSheet, isMoving
                ? Mathf.FloorToInt(Time.time * activeSheet.FramesPerSecond) % activeSheet.FrameCount
                : 0);
        }

        private AirSpriteSheet SelectActionSheet(PlayerPresentationState state)
        {
            switch (state)
            {
                case PlayerPresentationState.NormalAttack:
                    return facingLeft ? normalAttackLeft : normalAttackRight;
                case PlayerPresentationState.DefensiveSkill:
                    return facingLeft ? shieldLeft : shieldRight;
                case PlayerPresentationState.Ultimate:
                    return facingLeft ? ultimateLeft : ultimateRight;
                default:
                    return null;
            }
        }

        private void SetFrame(AirSpriteSheet sheet, int index)
        {
            Sprite[] sheetFrames = GetFrames(sheet);
            if (sheetFrames == null || index < 0 || index >= sheetFrames.Length) return;
            spriteRenderer.flipX = false;
            spriteRenderer.sprite = sheetFrames[index];
        }

        private Sprite[] GetFrames(AirSpriteSheet sheet)
        {
            if (sheet == null || sheet.Texture == null) return null;
            if (frames.TryGetValue(sheet, out Sprite[] cached)) return cached;

            Sprite[] created = new Sprite[sheet.FrameCount];
            for (int i = 0; i < created.Length; i++)
            {
                Rect rect = new Rect(i * sheet.FrameWidth, 0f, sheet.FrameWidth, sheet.FrameHeight);
                created[i] = Sprite.Create(sheet.Texture, rect, new Vector2(0.5f, 0.5f), 32f);
                created[i].name = "Air_" + sheet.Texture.name + "_" + i;
            }
            frames.Add(sheet, created);
            return created;
        }
    }
}
