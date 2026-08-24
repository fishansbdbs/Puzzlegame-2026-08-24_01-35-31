using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PuzzleGame.Presentation.Mock;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Boots the presentation layer as a standalone demo. Installs demo
    /// mocks for any integration seam core systems haven't registered yet,
    /// then opens the main menu.
    ///
    /// Auto-starts only in the PresentationDemo/SampleScene scenes; once
    /// core (Codex) has its own boot flow, it should register real sources
    /// and call <see cref="Launch"/> (or push screens itself) instead.
    /// </summary>
    public class PresentationDemoBootstrap : MonoBehaviour
    {
        UiRouter _router;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            var scene = SceneManager.GetActiveScene().name;
            if (scene != "PresentationDemo" && scene != "SampleScene") return;
            if (Object.FindFirstObjectByType<PresentationDemoBootstrap>() != null) return;
            var go = new GameObject("PresentationDemo");
            go.AddComponent<PresentationDemoBootstrap>();
        }

        void Start()
        {
            Launch(gameObject);
        }

        /// <summary>Create the UI panel and open the main menu on the given host object.</summary>
        public void Launch(GameObject host)
        {
            if (_router != null) return;
            MockInstaller.InstallMissing();

            var doc = host.GetComponent<UIDocument>();
            if (doc == null) doc = host.AddComponent<UIDocument>();
            if (doc.panelSettings == null)
            {
                var settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.name = "PuzzlePanelSettings";
                var theme = Resources.Load<ThemeStyleSheet>("UI/PuzzleTheme");
                if (theme != null) settings.themeStyleSheet = theme;
                settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(1280, 720);
                settings.match = 0.5f;
                doc.panelSettings = settings;
            }

            var root = doc.rootVisualElement;
            root.style.flexGrow = 1f;
            _router = new UiRouter(root);
            _router.Home(new MainMenuScreen());
        }

        void Update()
        {
            _router?.Tick(Time.deltaTime);
        }
    }
}
