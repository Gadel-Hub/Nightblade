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
        private PlayerMovement movement;
        private FireCharacterPresentation presentation;
        private Rigidbody2D body;
        private Coroutine runningUltimate;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            presentation = GetComponent<FireCharacterPresentation>();
            body = GetComponent<Rigidbody2D>();
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
            if (movement != null) movement.SetControlEnabled(true);
        }

        private IEnumerator RunUltimate()
        {
            movement.SetControlEnabled(false);
            if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            if (presentation != null) presentation.PlayUltimate();
            SpawnFlames();
            yield return new WaitForSeconds(durationSeconds);
            FinishUltimate();
        }

        private void SpawnFlames()
        {
            ClearFlames();
            if (flameContainer == null) flameContainer = transform;
            int count = Mathf.Max(1, Mathf.CeilToInt((coverageEnd - coverageStart) / flameSpacing));
            for (int i = 0; i <= count; i++)
            {
                GameObject flame = new GameObject("FireUltimateFlame");
                flame.transform.SetParent(flameContainer, false);
                flame.transform.position = new Vector3(coverageStart + i * flameSpacing, flameY, 0f);
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
        }

        private void OnDisable() => FinishUltimate();
    }
}
