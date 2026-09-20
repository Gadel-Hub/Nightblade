using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCharacter), typeof(PlayerCombat), typeof(FireCharacterPresentation))]
    public sealed class FireCharacterGameplay : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private Texture2D projectileLeftTexture;
        [SerializeField] private Texture2D projectileRightTexture;
        [SerializeField, Min(0)] private int projectileFrameIndex = 3;
        [SerializeField, Min(1)] private int projectileFrameWidth = 64;
        [SerializeField, Min(0)] private int projectileCropWidth = 12;
        [SerializeField, Min(0)] private int projectileCropHeight = 10;
        [SerializeField, Min(0)] private int projectileLeftCropX;
        [SerializeField, Min(0)] private int projectileRightCropX = 52;
        [SerializeField, Min(0)] private int projectileCropY = 10;
        [SerializeField, Min(0.01f)] private float projectileSpeed = 8f;
        [SerializeField, Min(0.01f)] private float projectileLifetime = 2f;
        [SerializeField, Min(0.01f)] private float projectileRange = 8f;
        [SerializeField, Min(1)] private int projectileDamage = 3;
        [SerializeField] private LayerMask projectileTargetLayers;
        [SerializeField] private float projectileSpawnOffset = 0.75f;

        [Header("Shield")]
        [SerializeField, Min(0.01f)] private float shieldDuration = 1.25f;

        private PlayerCharacter character;
        private PlayerCombat combat;
        private FireCharacterPresentation presentation;
        private PlayerMovement movement;
        private Coroutine shieldRoutine;
        private readonly List<GameObject> activeProjectiles = new List<GameObject>();

        public bool IsShieldActive { get; private set; }

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            combat = GetComponent<PlayerCombat>();
            presentation = GetComponent<FireCharacterPresentation>();
            movement = GetComponent<PlayerMovement>();
        }

        public void TryMelee()
        {
            if (character.SelectedIndex != 0 || !character.RunInProgress) return;
            if (combat.TryStartAttack()) presentation.PlayMeleeAttack();
        }

        public void FireProjectile()
        {
            if (character.SelectedIndex != 0 || !character.RunInProgress) return;

            int facing = combat.Facing;
            Texture2D texture = facing < 0 ? projectileLeftTexture : projectileRightTexture;
            if (texture == null) return;

            GameObject projectile = new GameObject("FireProjectile");
            projectile.transform.position = transform.position + Vector3.right * (projectileSpawnOffset * facing);
            SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 2;
            renderer.sprite = CreateProjectileSprite(texture);
            renderer.flipX = false;

            FireProjectile projectileBehaviour = projectile.AddComponent<FireProjectile>();
            projectileBehaviour.Configure(Vector2.right * facing, projectileSpeed, projectileLifetime,
                projectileRange, projectileDamage, projectileTargetLayers);
            activeProjectiles.RemoveAll(item => item == null);
            activeProjectiles.Add(projectile);
            presentation.PlayNormalAttack();
        }

        public void ActivateShield()
        {
            if (character.SelectedIndex != 0 || !character.RunInProgress || IsShieldActive) return;
            if (shieldRoutine != null) StopCoroutine(shieldRoutine);
            shieldRoutine = StartCoroutine(ShieldRoutine());
        }

        private IEnumerator ShieldRoutine()
        {
            IsShieldActive = true;
            presentation.PlayShield();
            yield return new WaitForSeconds(shieldDuration);
            IsShieldActive = false;
            shieldRoutine = null;
            presentation.SetShieldPresentation(false);
        }

        private Sprite CreateProjectileSprite(Texture2D texture)
        {
            int index = Mathf.Clamp(projectileFrameIndex, 0, Mathf.Max(0, texture.width / projectileFrameWidth - 1));
            int cropX = combat.Facing < 0 ? projectileLeftCropX : projectileRightCropX;
            Rect rect = new Rect(index * projectileFrameWidth + cropX, projectileCropY,
                projectileCropWidth, projectileCropHeight);
            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 32f);
        }

        public void ResetActions()
        {
            if (shieldRoutine != null) StopCoroutine(shieldRoutine);
            shieldRoutine = null;
            IsShieldActive = false;
            if (presentation != null) presentation.SetShieldPresentation(false);
            for (int i = 0; i < activeProjectiles.Count; i++)
                if (activeProjectiles[i] != null) Destroy(activeProjectiles[i]);
            activeProjectiles.Clear();
            presentation?.ResetPresentation();
        }

        private void OnDisable() => ResetActions();
    }
}
