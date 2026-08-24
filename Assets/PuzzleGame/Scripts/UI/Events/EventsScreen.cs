using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using PuzzleGame.Presentation.Content;

using PuzzleGame.Presentation.UI.Battle;
using PuzzleGame.Presentation.UI.Story;

namespace PuzzleGame.Presentation.UI.Events
{
    /// <summary>
    /// Rotating-content hub. Everything shown here comes from the core
    /// scheduler seam (IScheduleSource) — active event chapters, material
    /// dungeons, awakening stages, towers and boss rushes, plus an upcoming
    /// preview. No date logic lives in this screen.
    /// </summary>
    public class EventsScreen : UiScreen
    {
        ScrollView _list;

        protected override void Build(VisualElement root)
        {
            root.Add(Header("Events"));
            _list = new ScrollView(ScrollViewMode.Vertical);
            _list.style.flexGrow = 1f;
            UiKit.Pad(_list, 14f);
            root.Add(_list);
        }

        public override void OnEnter()
        {
            Rebuild();
        }

        void Rebuild()
        {
            _list.Clear();
            var schedule = PresentationServices.Get<IScheduleSource>();
            var active = schedule.GetActiveContent()
                .Where(c => c.Kind != ScheduledContentKind.Banner)
                .ToList();

            _list.Add(UiKit.Title("Active Now", 17f));
            if (active.Count == 0)
            {
                _list.Add(UiKit.Dim("Nothing is running right now. The goblins are on break.", 12f));
            }
            foreach (var content in active)
            {
                _list.Add(EventCard(content, schedule));
            }

            var upcoming = schedule.GetUpcomingContent(6)
                .Where(c => c.Kind != ScheduledContentKind.Banner)
                .ToList();
            if (upcoming.Count > 0)
            {
                var title = UiKit.Title("Coming Soon", 17f);
                title.style.marginTop = 14f;
                _list.Add(title);
                foreach (var content in upcoming)
                {
                    var row = UiKit.Row(8f);
                    row.style.marginBottom = 5f;
                    row.Add(KindBadge(content.Kind));
                    row.Add(UiKit.Text(content.DisplayName, 13f, true));
                    row.Add(UiKit.Dim("starts in " + UiKit.FormatTimeRemaining(content.StartUtc - schedule.NowUtc), 11f));
                    _list.Add(row);
                }
            }
        }

        VisualElement EventCard(ScheduledContentVm content, IScheduleSource schedule)
        {
            var card = UiKit.Panel(true);
            card.style.marginBottom = 10f;
            var head = UiKit.Row(8f);
            head.Add(KindBadge(content.Kind));
            head.Add(UiKit.Text(content.DisplayName, 16f, true));
            head.Add(UiKit.Spacer());
            head.Add(UiKit.Dim(content.Permanent
                ? "Always open"
                : "⏳ " + UiKit.FormatTimeRemaining(content.EndUtc - schedule.NowUtc), 11f));
            card.Add(head);
            if (!string.IsNullOrEmpty(content.Description))
            {
                card.Add(UiKit.Dim(content.Description, 12f));
            }

            var db = ContentDb.Instance;
            if (db.Events.TryGetValue(content.TargetId ?? "", out var eventDto) && eventDto.stages.Count > 0)
            {
                var stagesRow = UiKit.Row(6f);
                stagesRow.style.flexWrap = Wrap.Wrap;
                stagesRow.style.marginTop = 6f;
                foreach (var stage in eventDto.stages.Take(8))
                {
                    var btn = UiKit.Button(stage.stageNumber + ". " + stage.name, () => Launch(eventDto, stage));
                    stagesRow.Add(btn);
                }
                if (eventDto.stages.Count > 8)
                {
                    stagesRow.Add(UiKit.Dim("+" + (eventDto.stages.Count - 8) + " more", 11f));
                }
                card.Add(stagesRow);
            }
            return card;
        }

        void Launch(EventDto eventDto, StageDto stage)
        {
            var chapter = new ChapterDto { title = eventDto.name, theme = eventDto.theme };
            var factory = PresentationServices.Get<IBattleFactory>();
            System.Action<float> pump;
            var source = factory.Create(chapter, stage, out pump);
            Router.Push(new BattleScreen(source, pump, chapter, stage));
        }

        public static VisualElement KindBadge(ScheduledContentKind kind)
        {
            string text;
            Color color;
            switch (kind)
            {
                case ScheduledContentKind.EventChapter: text = "EVENT"; color = Theme.AccentWarm; break;
                case ScheduledContentKind.MaterialDungeon: text = "MATERIALS"; color = Theme.Accent; break;
                case ScheduledContentKind.AwakeningStage: text = "AWAKENING"; color = Theme.Rarity6; break;
                case ScheduledContentKind.ChallengeTower: text = "TOWER"; color = Theme.Rarity4; break;
                case ScheduledContentKind.BossRush: text = "BOSS RUSH"; color = Theme.Danger; break;
                default: text = "BANNER"; color = Theme.TextDim; break;
            }
            var badge = UiKit.Text(text, 10f, true, Theme.BgDeep);
            badge.style.backgroundColor = color;
            UiKit.Round(badge, 4f);
            badge.style.paddingLeft = 6f; badge.style.paddingRight = 6f;
            badge.style.paddingTop = 2f; badge.style.paddingBottom = 2f;
            return badge;
        }
    }
}
