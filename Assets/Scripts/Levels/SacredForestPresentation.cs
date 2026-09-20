using UnityEngine;

namespace Nightblade
{
    [DisallowMultipleComponent]
    public sealed class SacredForestPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer normalBackground;
        [SerializeField] private SpriteRenderer bossBackground;

        private void Awake()
        {
            ShowNormalForest();
        }

        public void ShowNormalForest()
        {
            SetBackgrounds(normalActive: true);
        }

        public void ShowBossForest()
        {
            SetBackgrounds(normalActive: false);
        }

        private void SetBackgrounds(bool normalActive)
        {
            if (normalBackground != null) normalBackground.gameObject.SetActive(normalActive);
            if (bossBackground != null) bossBackground.gameObject.SetActive(!normalActive);
        }
    }
}
