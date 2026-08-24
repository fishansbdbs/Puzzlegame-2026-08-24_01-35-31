using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PuzzleGame.Presentation.Content;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Presentation.CoreIntegration
{
    /// <summary>
    /// The real game entry point. Composes GameServices (core systems +
    /// translated content + save), registers the core-backed adapters for
    /// every presentation seam, and opens the main menu. Runs in the "Game"
    /// scene; the demo/mock bootstrap stays scoped to "PresentationDemo" and
    /// Codex's vertical slice keeps "SampleScene".
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        UiRouter router;

        public UiRouter Router { get { return router; } }
        public GameServices Services { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (SceneManager.GetActiveScene().name != "Game") return;
            if (UnityEngine.Object.FindFirstObjectByType<GameBootstrap>() != null) return;
            var host = new GameObject("Game");
            host.AddComponent<GameBootstrap>();
        }

        void Start()
        {
            Launch(GameServices.Instance);
        }

        /// <summary>Compose against the given services (tests inject their own).</summary>
        public void Launch(GameServices services)
        {
            if (router != null) return;
            Services = services;
            RegisterAdapters(services);

            var doc = gameObject.GetComponent<UIDocument>();
            if (doc == null) doc = gameObject.AddComponent<UIDocument>();
            if (doc.panelSettings == null)
            {
                var settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.name = "GamePanelSettings";
                var theme = Resources.Load<ThemeStyleSheet>("UI/PuzzleTheme");
                if (theme != null) settings.themeStyleSheet = theme;
                settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(1280, 720);
                settings.match = 0.5f;
                doc.panelSettings = settings;
            }
            var root = doc.rootVisualElement;
            root.style.flexGrow = 1f;
            router = new UiRouter(root);
            router.Home(new MainMenuScreen());
        }

        /// <summary>Registers every presentation seam against core systems.</summary>
        public static void RegisterAdapters(GameServices services)
        {
            var roster = new CoreRosterAdapter(services);
            PresentationServices.Register<IEconomySource>(new CoreEconomyAdapter(services));
            PresentationServices.Register<IRosterSource>(roster);
            PresentationServices.Register<IScheduleSource>(new CoreScheduleAdapter(services));
            PresentationServices.Register<ISummonSource>(new CoreSummonAdapter(services, roster));
            PresentationServices.Register<IContentLibrary>(new CoreContentAdapter(services));
            PresentationServices.Register<IBattleFactory>(new CoreBattleFactory(services));
        }

        void Update()
        {
            if (router != null) router.Tick(Time.deltaTime);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && Services != null) Services.SaveNow();
        }

        void OnApplicationQuit()
        {
            if (Services != null) Services.SaveNow();
        }
    }

    /// <summary>Core-backed battle factory: every launched stage is a real core session.</summary>
    public sealed class CoreBattleFactory : IBattleFactory
    {
        readonly GameServices services;
        public CoreBattleFactory(GameServices services) { this.services = services; }

        public IBattleEventSource Create(ChapterDto chapter, StageDto stage, out Action<float> pump)
        {
            var adapter = new CoreBattleAdapter(services, stage);
            pump = adapter.Update;
            return adapter;
        }
    }
}
