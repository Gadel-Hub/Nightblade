using Nightblade;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CombatTarget))]
public sealed class EnemyHealthBar : MonoBehaviour
{
    [SerializeField, Min(0f)] private float verticalOffset = 1f;
    [SerializeField, Range(20f, 28f)] private float widthPixels = 26f;
    [SerializeField, Range(3f, 4f)] private float heightPixels = 4f;

    private CombatTarget target;
    private GameObject barObject;
    private Image fill;
    private float fullFillWidth;

    private void Awake()
    {
        target = GetComponent<CombatTarget>();
        CreateBar();
    }

    private void Start() => UpdateBar();

    private void LateUpdate() => UpdateBar();

    private void CreateBar()
    {
        barObject = new GameObject("Enemy Health Bar", typeof(RectTransform), typeof(Canvas));
        barObject.transform.SetParent(transform, false);
        barObject.transform.localPosition = Vector3.up * verticalOffset;
        barObject.transform.localScale = Vector3.one / 32f;

        RectTransform canvasRect = (RectTransform)barObject.transform;
        canvasRect.sizeDelta = new Vector2(widthPixels, heightPixels + 1f);
        Canvas canvas = barObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5;
        canvas.pixelPerfect = true;

        Image backing = CreateImage("Backing", canvasRect, new Color(0.025f, 0.04f, 0.03f, 0.85f),
            new Vector2(widthPixels, heightPixels + 1f));
        Image track = CreateImage("Track", backing.rectTransform, new Color(0.12f, 0.17f, 0.13f, 1f),
            new Vector2(widthPixels - 2f, heightPixels - 1f));

        fill = CreateImage("Fill", track.rectTransform, new Color(0.25f, 0.78f, 0.34f, 1f),
            new Vector2(widthPixels - 2f, heightPixels - 1f));
        fullFillWidth = widthPixels - 2f;
        fill.type = Image.Type.Simple;
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.up;
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(fullFillWidth, 0f);
    }

    private void UpdateBar()
    {
        if (target == null)
        {
            if (barObject != null) barObject.SetActive(false);
            return;
        }

        if (fill != null)
        {
            float healthFraction = target.MaxHealth > 0
                ? Mathf.Clamp01(target.CurrentHealth / (float)target.MaxHealth)
                : 0f;
            Vector2 fillSize = fill.rectTransform.sizeDelta;
            fillSize.x = fullFillWidth * healthFraction;
            fill.rectTransform.sizeDelta = fillSize;
        }

        if (!target.IsAlive && barObject != null)
            barObject.SetActive(false);
    }

    private static Image CreateImage(string objectName, Transform parent, Color color, Vector2 size)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)imageObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
