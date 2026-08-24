using UnityEngine;
using UnityEngine.UIElements;
using PuzzleGame.Presentation.UI.Events;
using PuzzleGame.Presentation.UI.Party;
using PuzzleGame.Presentation.UI.Story;
using PuzzleGame.Presentation.UI.Summon;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Home hub. Title comes from GameInfo so the final name is a one-line
    /// swap. Includes the effects/reduced-motion options panel.
    /// </summary>
    public class MainMenuScreen : UiScreen
    {
        protected override void Build(VisualElement root)
        {
            root.Add(Header(GameInfo.Title, backButton: false));

            var main = UiKit.Row(30f);
            main.style.flexGrow = 1f;
            main.style.alignItems = Align.Center;
            main.style.justifyContent = Justify.Center;
            root.Add(main);

            var buttons = UiKit.Column(12f);
            buttons.style.minWidth = 260f;
            main.Add(buttons);

            buttons.Add(MenuButton("📖  Story", "Chapters of extremely serious adventure",
                () => Router.Push(new StageSelectScreen())));
            buttons.Add(MenuButton("❖  Summon", "Rip packs. Meet legends.",
                () => Router.Push(new SummonHubScreen())));
            buttons.Add(MenuButton("👥  Characters", "Roster, upgrades, Ascension & Awakening",
                () => Router.Push(new PartyScreen())));
            buttons.Add(MenuButton("🗓  Events", "What's rotating right now",
                () => Router.Push(new EventsScreen())));

            // Options: effect intensity & reduced motion.
            var options = UiKit.Panel();
            options.style.minWidth = 240f;
            options.Add(UiKit.Text("Effects", 14f, true));
            var reduced = new Toggle("Reduced motion") { value = MotionSettings.ReducedMotion };
            reduced.RegisterValueChangedCallback(evt => MotionSettings.ReducedMotion = evt.newValue);
            options.Add(reduced);
            var intensity = new Slider("Intensity", 0f, 1f) { value = MotionSettings.Intensity };
            intensity.RegisterValueChangedCallback(evt => MotionSettings.Intensity = evt.newValue);
            options.Add(intensity);
            options.Add(UiKit.Dim("Screen shake, hit-stop and long reveals scale with these.", 10f));
            main.Add(options);

            var version = UiKit.Dim(GameInfo.WorkingTitle + " — presentation preview", 10f);
            version.style.position = Position.Absolute;
            version.style.bottom = 8f;
            version.style.right = 12f;
            root.Add(version);
        }

        VisualElement MenuButton(string title, string subtitle, System.Action onClick)
        {
            var panel = UiKit.Panel(true);
            panel.Add(UiKit.Text(title, 19f, true));
            panel.Add(UiKit.Dim(subtitle, 11f));
            panel.RegisterCallback<PointerDownEvent>(_ => onClick());
            panel.RegisterCallback<PointerEnterEvent>(_ => panel.style.backgroundColor = Theme.PanelRaised);
            panel.RegisterCallback<PointerLeaveEvent>(_ => panel.style.backgroundColor = Theme.PanelRaised);
            return panel;
        }
    }
}
