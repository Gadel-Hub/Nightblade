using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    public sealed class FireFlameAnimation : MonoBehaviour
    {
        [SerializeField] private Texture2D spriteSheet;
        [SerializeField, Min(1)] private int frameWidth = 64;
        [SerializeField, Min(1)] private int frameHeight = 22;
        [SerializeField, Min(1)] private int frameCount = 8;
        [SerializeField, Min(0.01f)] private float framesPerSecond = 12f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Sprite[] frames;
        private float elapsed;

        public void Configure(Texture2D sheet, SpriteRenderer targetRenderer, float fps)
        {
            spriteSheet = sheet;
            spriteRenderer = targetRenderer;
            framesPerSecond = fps;
            if (spriteSheet != null) frameCount = Mathf.Max(1, spriteSheet.width / frameWidth);
            BuildFrames();
        }

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            BuildFrames();
        }

        private void BuildFrames()
        {
            if (spriteSheet == null || spriteRenderer == null) return;
            frames = new Sprite[frameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                Rect rect = new Rect(i * frameWidth, 0f, frameWidth, frameHeight);
                frames[i] = Sprite.Create(spriteSheet, rect, new Vector2(0.5f, 0f), 32f);
            }
            spriteRenderer.sprite = frames[0];
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0) return;
            elapsed += Time.deltaTime;
            spriteRenderer.sprite = frames[Mathf.FloorToInt(elapsed * framesPerSecond) % frames.Length];
        }

        private void OnDestroy()
        {
            if (frames == null) return;
            for (int i = 0; i < frames.Length; i++)
                if (frames[i] != null) Destroy(frames[i]);
        }
    }
}
