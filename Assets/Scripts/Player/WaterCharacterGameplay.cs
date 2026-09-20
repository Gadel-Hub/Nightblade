using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCharacter), typeof(PlayerCombat), typeof(WaterCharacterPresentation))]
    public sealed class WaterCharacterGameplay : MonoBehaviour
    {
        [Header("Bubble projectile")]
        [SerializeField] private Texture2D bubbleLeftTexture;
        [SerializeField] private Texture2D bubbleRightTexture;
        [SerializeField, Min(0.01f)] private float bubbleSpeed = 8f;
        [SerializeField, Min(0.01f)] private float bubbleLifetime = 2f;
        [SerializeField, Min(0.01f)] private float bubbleRange = 8f;
        [SerializeField, Min(1)] private int bubbleDamage = 2;
        [SerializeField] private LayerMask bubbleTargetLayers;
        [SerializeField] private float bubbleSpawnOffset = 0.75f;

        [Header("Shield")]
        [SerializeField, Min(0.01f)] private float shieldDuration = 1.5f;

        [Header("Traveling wave")]
        [SerializeField] private Texture2D waveLeftTexture;
        [SerializeField] private Texture2D waveRightTexture;
        [SerializeField, Min(1)] private int waveFrameWidth = 80;
        [SerializeField, Min(1)] private int waveFrameHeight = 48;
        [SerializeField, Min(0.01f)] private float waveSpeed = 7f;
        [SerializeField, Min(0.01f)] private float waveLifetime = 2f;
        [SerializeField, Min(0.01f)] private float waveRange = 12f;
        [SerializeField] private Vector2 waveGameplaySize = new Vector2(1.5f, 1f);
        [SerializeField, Min(1)] private int waveDamage = 4;
        [SerializeField] private LayerMask waveTargetLayers;
        [SerializeField] private float waveSpawnOffset = 0.75f;

        private PlayerCharacter character;
        private PlayerCombat combat;
        private PlayerPresentation presentation;
        private WaterCharacterPresentation waterPresentation;
        private Coroutine shieldRoutine;
        private readonly List<GameObject> activeObjects = new List<GameObject>();

        public bool IsShieldActive { get; private set; }

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            combat = GetComponent<PlayerCombat>();
            presentation = GetComponent<PlayerPresentation>();
            waterPresentation = GetComponent<WaterCharacterPresentation>();
        }

        public void TryStrike()
        {
            if (character.SelectedIndex != 1 || !character.RunInProgress) return;
            if (combat.TryStartAttack()) waterPresentation.PlayPrimaryAttack();
        }

        public void FireBubble()
        {
            if (character.SelectedIndex != 1 || !character.RunInProgress) return;
            int facing = combat.Facing;
            Texture2D texture = facing < 0 ? bubbleLeftTexture : bubbleRightTexture;
            if (texture == null) return;

            GameObject bubble = new GameObject("WaterBubbleProjectile");
            bubble.transform.position = transform.position + Vector3.right * (bubbleSpawnOffset * facing);
            SpriteRenderer renderer = bubble.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 2;
            renderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 32f);

            FireProjectile projectile = bubble.AddComponent<FireProjectile>();
            projectile.Configure(Vector2.right * facing, bubbleSpeed, bubbleLifetime, bubbleRange,
                bubbleDamage, bubbleTargetLayers);
            Track(bubble);
            presentation.PlayNormalAttack();
        }

        public void ActivateShield()
        {
            if (character.SelectedIndex != 1 || !character.RunInProgress || IsShieldActive) return;
            if (shieldRoutine != null) StopCoroutine(shieldRoutine);
            shieldRoutine = StartCoroutine(ShieldRoutine());
        }

        public void ActivateUltimate()
        {
            if (character.SelectedIndex != 1 || !character.RunInProgress || waveLeftTexture == null || waveRightTexture == null)
                return;

            int facing = combat.Facing;
            Texture2D texture = facing < 0 ? waveLeftTexture : waveRightTexture;
            GameObject wave = new GameObject("WaterWaveUltimate");
            wave.transform.position = transform.position + Vector3.right * (waveSpawnOffset * facing);
            SpriteRenderer renderer = wave.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 2;
            renderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, waveFrameWidth, waveFrameHeight),
                new Vector2(0.5f, 0.5f), 32f);

            WaterWave waveBehaviour = wave.AddComponent<WaterWave>();
            waveBehaviour.Configure(Vector2.right * facing, waveSpeed, waveLifetime, waveRange,
                waveGameplaySize, waveDamage, waveTargetLayers, texture, renderer,
                waveFrameWidth, waveFrameHeight, 10f);
            Track(wave);
        }

        private IEnumerator ShieldRoutine()
        {
            IsShieldActive = true;
            waterPresentation.SetShieldPresentation(true);
            waterPresentation.PlayShield();
            yield return new WaitForSeconds(shieldDuration);
            IsShieldActive = false;
            shieldRoutine = null;
            waterPresentation.SetShieldPresentation(false);
        }

        public void ResetActions()
        {
            if (shieldRoutine != null) StopCoroutine(shieldRoutine);
            shieldRoutine = null;
            IsShieldActive = false;
            if (waterPresentation != null) waterPresentation.SetShieldPresentation(false);
            for (int i = 0; i < activeObjects.Count; i++)
                if (activeObjects[i] != null) Destroy(activeObjects[i]);
            activeObjects.Clear();
            waterPresentation?.ResetPresentation();
        }

        private void Track(GameObject instance)
        {
            activeObjects.RemoveAll(item => item == null);
            activeObjects.Add(instance);
        }

        private void OnDisable() => ResetActions();
    }
}
