using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nightblade
{
    [DisallowMultipleComponent]
    public sealed class SacredForestPresentation : MonoBehaviour
    {
        private const string ScenePath = "Assets/Scenes/SacredForest.unity";

        [SerializeField] private SpriteRenderer normalBackground;
        [SerializeField] private SpriteRenderer bossBackground;
        [SerializeField] private GameObject productionUIRoot;
        [SerializeField] private SacredForestCamera productionCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneInitialization()
        {
            SceneManager.sceneLoaded -= InitializeLoadedScene;
            SceneManager.sceneLoaded += InitializeLoadedScene;
        }

        private static void InitializeLoadedScene(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != ScenePath) return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SacredForestPresentation presentation =
                    root.GetComponentInChildren<SacredForestPresentation>(true);
                if (presentation == null) continue;

                presentation.InitializeProductionState();
                return;
            }
        }

        private void Awake()
        {
            ShowNormalForest();
        }

        private void InitializeProductionState()
        {
            gameObject.SetActive(true);
            ShowNormalForest();

            if (productionCamera != null)
            {
                productionCamera.gameObject.SetActive(true);
                productionCamera.InitializeForSceneStartup();
            }

            if (productionUIRoot == null) return;

            productionUIRoot.SetActive(true);
            productionUIRoot.GetComponent<ProductionRunUI>()?.InitializeForSceneStartup();
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
