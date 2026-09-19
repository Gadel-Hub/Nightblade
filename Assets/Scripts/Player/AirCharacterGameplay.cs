using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCharacter), typeof(PlayerCombat), typeof(PlayerMovement))]
    [RequireComponent(typeof(AirCharacterPresentation))]
    public sealed class AirCharacterGameplay : MonoBehaviour
    {
        [Header("Gust")]
        [SerializeField, Min(0.01f)] private float gustDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float gustForwardOffset = 0.75f;
        [SerializeField] private Vector2 gustSize = new Vector2(1.25f, 0.9f);
        [SerializeField, Min(1)] private int gustDamage = 1;
        [SerializeField] private LayerMask gustTargetLayers;

        [Header("Dash")]
        [SerializeField, Min(0.01f)] private float dashDuration = 0.15f;
        [SerializeField, Min(0f)] private float dashSpeed = 10f;

        [Header("Shield")]
        [SerializeField, Min(0.01f)] private float shieldDuration = 1.25f;

        [Header("Tornado")]
        [SerializeField] private Texture2D tornadoLeftTexture;
        [SerializeField] private Texture2D tornadoRightTexture;
        [SerializeField, Min(1)] private int tornadoFrameWidth = 144;
        [SerializeField, Min(1)] private int tornadoFrameHeight = 176;
        [SerializeField, Min(0f)] private float tornadoSpeed = 7f;
        [SerializeField, Min(0.01f)] private float tornadoLifetime = 2f;
        [SerializeField, Min(0.01f)] private float tornadoRange = 12f;
        [SerializeField] private Vector2 tornadoGameplaySize = new Vector2(1.5f, 2f);
        [SerializeField, Min(1)] private int tornadoDamage = 2;
        [SerializeField] private LayerMask tornadoTargetLayers;
        [SerializeField] private Vector3 tornadoVisualOffset;
        [SerializeField, Min(0.01f)] private float tornadoVisualScale = 1f;

        private PlayerCharacter character;
        private PlayerCombat combat;
        private PlayerMovement movement;
        private AirCharacterPresentation presentation;
        private Rigidbody2D body;
        private Coroutine gustRoutine;
        private Coroutine dashRoutine;
        private Coroutine shieldRoutine;
        private GameObject activeTornado;

        public bool IsShieldActive { get; private set; }

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            combat = GetComponent<PlayerCombat>();
            movement = GetComponent<PlayerMovement>();
            presentation = GetComponent<AirCharacterPresentation>();
            body = GetComponent<Rigidbody2D>();
        }

        public void TryGust()
        {
            if (!CanAct() || gustRoutine != null) return;
            gustRoutine = StartCoroutine(GustRoutine());
        }

        public void TryDash()
        {
            if (!CanAct() || dashRoutine != null) return;
            dashRoutine = StartCoroutine(DashRoutine());
        }

        public void ActivateShield()
        {
            if (!CanAct() || shieldRoutine != null) return;
            shieldRoutine = StartCoroutine(ShieldRoutine());
        }

        public void ActivateUltimate()
        {
            if (!CanAct() || activeTornado != null) return;
            int facing = combat.Facing;
            Texture2D texture = facing < 0 ? tornadoLeftTexture : tornadoRightTexture;
            activeTornado = new GameObject("AirTornado");
            activeTornado.transform.position = transform.position + Vector3.right * facing * gustForwardOffset;
            activeTornado.transform.localScale = Vector3.one * tornadoVisualScale;
            SpriteRenderer renderer = activeTornado.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateVisual(texture);
            renderer.sortingOrder = 2;
            activeTornado.transform.position += tornadoVisualOffset;
            AirTornado tornado = activeTornado.AddComponent<AirTornado>();
            tornado.Configure(Vector2.right * facing, tornadoSpeed, tornadoLifetime, tornadoRange,
                tornadoGameplaySize, tornadoDamage, tornadoTargetLayers);
            StartCoroutine(ClearTornadoWhenFinished(activeTornado, tornadoLifetime));
            presentation.PlayTornado();
        }

        private IEnumerator GustRoutine()
        {
            presentation.PlayGust();
            HashSet<CombatTarget> damaged = new HashSet<CombatTarget>();
            List<Collider2D> overlaps = new List<Collider2D>(8);
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(gustTargetLayers);
            float remaining = gustDuration;
            while (remaining > 0f)
            {
                overlaps.Clear();
                Vector2 center = body.position + Vector2.right * combat.Facing * gustForwardOffset;
                Physics2D.OverlapBox(center, gustSize, 0f, filter, overlaps);
                for (int i = 0; i < overlaps.Count; i++)
                {
                    CombatTarget target = overlaps[i].GetComponentInParent<CombatTarget>();
                    if (target != null && damaged.Add(target)) target.TakeDamage(gustDamage);
                }
                remaining -= Time.deltaTime;
                yield return null;
            }
            gustRoutine = null;
        }

        private IEnumerator DashRoutine()
        {
            movement.SetControlEnabled(false);
            int facing = combat.Facing;
            float remaining = dashDuration;
            while (remaining > 0f)
            {
                body.linearVelocity = new Vector2(facing * dashSpeed, body.linearVelocity.y);
                remaining -= Time.deltaTime;
                yield return new WaitForFixedUpdate();
            }
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            movement.SetControlEnabled(true);
            dashRoutine = null;
        }

        private IEnumerator ShieldRoutine()
        {
            IsShieldActive = true;
            presentation.SetShieldPresentation(true);
            presentation.PlayShield();
            yield return new WaitForSeconds(shieldDuration);
            IsShieldActive = false;
            presentation.SetShieldPresentation(false);
            shieldRoutine = null;
        }

        private IEnumerator ClearTornadoWhenFinished(GameObject tornado, float lifetime)
        {
            yield return new WaitForSeconds(lifetime + 0.05f);
            if (tornado == activeTornado) activeTornado = null;
            if (tornado != null) Destroy(tornado);
        }

        private Sprite CreateVisual(Texture2D texture)
        {
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, tornadoFrameWidth, tornadoFrameHeight),
                new Vector2(0.5f, 0.5f), 32f);
        }

        private bool CanAct() => character != null && character.RunInProgress && character.SelectedIndex == 2;

        private void OnDisable()
        {
            if (dashRoutine != null)
            {
                StopCoroutine(dashRoutine);
                dashRoutine = null;
                movement.SetControlEnabled(true);
            }
            if (shieldRoutine != null) StopCoroutine(shieldRoutine);
            IsShieldActive = false;
            presentation?.SetShieldPresentation(false);
            if (activeTornado != null) Destroy(activeTornado);
            activeTornado = null;
        }
    }
}
