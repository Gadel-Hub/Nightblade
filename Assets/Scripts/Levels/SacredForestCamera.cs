using UnityEngine;
using UnityEngine.U2D;

namespace Nightblade
{
    [DisallowMultipleComponent]
    public sealed class SacredForestCamera : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer backgroundBoundsSource;
        [SerializeField] private BoxCollider2D leftBoundary;
        [SerializeField] private BoxCollider2D rightBoundary;
        [SerializeField] private float verticalPosition;

        private Transform target;
        private Camera cameraComponent;
        private PixelPerfectCamera pixelPerfectCamera;
        private float worldMinX;
        private float worldMaxX;

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
            PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
            if (player != null) target = player.transform;
        }

        private void Start()
        {
            if (backgroundBoundsSource == null)
            {
                Debug.LogError("SacredForestCamera needs the production background SpriteRenderer.", this);
                enabled = false;
                return;
            }

            Bounds bounds = backgroundBoundsSource.bounds;
            worldMinX = bounds.min.x;
            worldMaxX = bounds.max.x;

            SetBoundaryPosition(leftBoundary, worldMinX);
            SetBoundaryPosition(rightBoundary, worldMaxX);
            Physics2D.SyncTransforms();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float halfWidth = pixelPerfectCamera != null
                ? pixelPerfectCamera.refResolutionX * 0.5f / pixelPerfectCamera.assetsPPU
                : cameraComponent.orthographicSize * cameraComponent.aspect;
            float minX = worldMinX + halfWidth;
            float maxX = worldMaxX - halfWidth;
            float x = minX <= maxX
                ? Mathf.Clamp(target.position.x, minX, maxX)
                : (worldMinX + worldMaxX) * 0.5f;
            transform.position = new Vector3(x, verticalPosition, transform.position.z);
        }

        private static void SetBoundaryPosition(BoxCollider2D boundary, float x)
        {
            if (boundary == null) return;

            Vector3 position = boundary.transform.position;
            position.x = x;
            boundary.transform.position = position;
        }
    }
}
