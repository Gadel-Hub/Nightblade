using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCharacter))]
    public sealed class FireUltimate : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(0f)] private float durationSeconds;
        [SerializeField, Min(1)] private int damage = 5;

        [Header("Coverage")]
        [SerializeField] private float coverageStart;
        [SerializeField] private float coverageEnd;
        [SerializeField, Min(0.01f)] private float flameSpacing = 2f;
        [SerializeField] private float flameY;

        [Header("Flame visual")]
        [SerializeField] private Texture2D flameSheet;
        [SerializeField] private Transform flameContainer;
        [SerializeField, Min(0.01f)] private float flameFramesPerSecond = 12f;

        private readonly List<GameObject> activeFlames = new List<GameObject>();
        private readonly HashSet<CombatTarget> damagedTargets = new HashSet<CombatTarget>();
        private PlayerMovement movement;
        private FireCharacterPresentation presentation;
        private PlayerCharacter character;
        private PlayerDamage playerDamage;
        private Rigidbody2D body;
        private Coroutine runningUltimate;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            presentation = GetComponent<FireCharacterPresentation>();
            body = GetComponent<Rigidbody2D>();
            character = GetComponent<PlayerCharacter>();
            playerDamage = GetComponent<PlayerDamage>();
        }

        private void FixedUpdate()
        {
            if (runningUltimate != null && body != null)
                body.linearVelocity = Vector2.zero;
        }

        public void Activate()
        {
            if (runningUltimate != null || flameSheet == null || coverageEnd <= coverageStart || durationSeconds <= 0f)
                return;
            runningUltimate = StartCoroutine(RunUltimate());
        }

        public void FinishUltimate()
        {
            if (runningUltimate != null)
                StopCoroutine(runningUltimate);
            runningUltimate = null;
            ClearFlames();
            if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            if (movement != null)
                movement.SetControlEnabled(character != null && character.RunInProgress &&
                    playerDamage != null && playerDamage.State == PlayerDamage.DamageState.Normal);
        }

        private IEnumerator RunUltimate()
        {
            movement.SetControlEnabled(false);
            if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            if (presentation != null) presentation.PlayUltimate();
            damagedTargets.Clear();
            SpawnFlames();
            float elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                DamageTargetsInCoverage();
                elapsed += Time.deltaTime;
                yield return null;
            }
            FinishUltimate();
        }

        private void SpawnFlames()
        {
            ClearFlames();
            if (flameContainer == null) flameContainer = transform;
            float inset = Mathf.Min(flameSpacing * 0.5f, (coverageEnd - coverageStart) * 0.5f);
            float firstCenter = coverageStart + inset;
            float lastCenter = coverageEnd - inset;
            int intervals = Mathf.Max(0, Mathf.RoundToInt((lastCenter - firstCenter) / flameSpacing));
            for (int i = 0; i <= intervals; i++)
            {
                GameObject flame = new GameObject("FireUltimateFlame");
                flame.transform.SetParent(flameContainer, false);
                flame.transform.position = new Vector3(firstCenter + i * flameSpacing, flameY, 0f);
                SpriteRenderer renderer = flame.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 2;
                FireFlameAnimation animation = flame.AddComponent<FireFlameAnimation>();
                animation.Configure(flameSheet, renderer, flameFramesPerSecond);
                activeFlames.Add(flame);
            }
        }

        private void ClearFlames()
        {
            for (int i = activeFlames.Count - 1; i >= 0; i--)
                if (activeFlames[i] != null) Destroy(activeFlames[i]);
            activeFlames.Clear();
            damagedTargets.Clear();
        }

        private void DamageTargetsInCoverage()
        {
            CombatTarget[] targets = FindObjectsByType<CombatTarget>(FindObjectsSortMode.None);
            for (int i = 0; i < targets.Length; i++)
            {
                CombatTarget target = targets[i];
                if (target == null || damagedTargets.Contains(target)) continue;
                float x = target.transform.position.x;
                if (x < coverageStart || x > coverageEnd) continue;
                if (target.TakeDamage(damage)) damagedTargets.Add(target);
            }
        }

        private void OnDisable() => FinishUltimate();
    }
}
