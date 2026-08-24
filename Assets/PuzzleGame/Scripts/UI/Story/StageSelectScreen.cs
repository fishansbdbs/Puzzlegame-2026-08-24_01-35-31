using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using PuzzleGame.Presentation.Content;
using PuzzleGame.Presentation.Mock;
using PuzzleGame.Presentation.UI.Battle;

namespace PuzzleGame.Presentation.UI.Story
{
    /// <summary>
    /// Story chapter/stage select. Chapters on the left, the chapter's 25
    /// stages on the right with kind markers (story/miniboss/boss), star
    /// goals and rewards preview. Stages launch the battle screen.
    /// </summary>
    public class StageSelectScreen : UiScreen
    {
        int _chapterNumber = 1;
        VisualElement _chapterList;
        ScrollView _stageList;
        Label _chapterTitle;
        Label _chapterBlurb;

        protected override void Build(VisualElement root)
        {
            root.Add(Header("Story"));
            var main = UiKit.Row(0f);
            main.style.flexGrow = 1f;
            root.Add(main);

            var left = new ScrollView(ScrollViewMode.Vertical);
            left.style.width = 230f;
            left.style.flexShrink = 0f;
            left.style.backgroundColor = Theme.BgDeep.WithAlpha(0.5f);
            _chapterList = UiKit.Column(4f);
            UiKit.Pad(_chapterList, 10f);
            left.Add(_chapterList);
            main.Add(left);

            var right = UiKit.Column(6f);
            right.style.flexGrow = 1f;
            UiKit.Pad(right, 12f);
            _chapterTitle = UiKit.Title("", 20f);
            right.Add(_chapterTitle);
            _chapterBlurb = UiKit.Dim("", 12f);
            right.Add(_chapterBlurb);
            _stageList = new ScrollView(ScrollViewMode.Vertical);
            _stageList.style.flexGrow = 1f;
            _stageList.contentContainer.style.flexDirection = FlexDirection.Row;
            _stageList.contentContainer.style.flexWrap = Wrap.Wrap;
            right.Add(_stageList);
            main.Add(right);
        }

        public override void OnEnter()
        {
            RebuildChapters();
            RebuildStages();
        }

        void RebuildChapters()
        {
            _chapterList.Clear();
            var db = ContentDb.Instance;
            foreach (var number in db.Chapters.Keys.OrderBy(n => n))
            {
                var chapter = db.Chapters[number];
                var btn = UiKit.Button(number + ". " + chapter.title, () =>
                {
                    _chapterNumber = number;
                    RebuildStages();
                }, number == _chapterNumber);
                btn.style.unityTextAlign = TextAnchor.MiddleLeft;
                _chapterList.Add(btn);
            }
        }

        void RebuildStages()
        {
            _stageList.Clear();
            var db = ContentDb.Instance;
            if (!db.Chapters.TryGetValue(_chapterNumber, out var chapter))
            {
                _chapterTitle.text = "No chapters authored yet";
                return;
            }
            _chapterTitle.text = "Chapter " + chapter.chapterNumber + ": " + chapter.title;
            _chapterBlurb.text = chapter.blurb;
            RebuildChapters();

            foreach (var stage in chapter.stages.OrderBy(s => s.stageNumber))
            {
                _stageList.Add(StageCard(chapter, stage));
            }
        }

        VisualElement StageCard(ChapterDto chapter, StageDto stage)
        {
            var card = UiKit.Panel(stage.kind == "boss");
            card.style.width = 172f;
            card.style.marginRight = 10f;
            card.style.marginBottom = 10f;
            if (stage.kind == "boss") UiKit.Border(card, Theme.Danger, 2f);
            else if (stage.kind == "miniboss") UiKit.Border(card, Theme.AccentWarm, 1.5f);

            var head = UiKit.Row(6f);
            head.Add(UiKit.Text(stage.stageNumber.ToString(), 17f, true,
                stage.kind == "boss" ? Theme.Danger : Theme.Accent));
            string marker = stage.kind == "boss" ? "☠ BOSS"
                : stage.kind == "miniboss" ? "⚔ MINIBOSS"
                : stage.kind == "story" ? "✦ STORY" : "";
            if (marker != "")
            {
                head.Add(UiKit.Text(marker, 10f, true,
                    stage.kind == "boss" ? Theme.Danger : Theme.AccentWarm));
            }
            card.Add(head);
            var name = UiKit.Text(stage.name, 12f, true);
            card.Add(name);
            card.Add(UiKit.Dim(stage.waves.Count + " wave" + (stage.waves.Count == 1 ? "" : "s") +
                               " • ★ under " + stage.resolutionsStar + " moves", 10f));
            if (stage.rewards != null && (stage.rewards.gold > 0 || stage.rewards.gems > 0))
            {
                card.Add(UiKit.Dim("◆" + stage.rewards.gold + (stage.rewards.gems > 0 ? "  ❖" + stage.rewards.gems : ""), 10f));
            }

            card.RegisterCallback<PointerDownEvent>(_ => Launch(chapter, stage));
            return card;
        }

        void Launch(ChapterDto chapter, StageDto stage)
        {
            var party = BuildParty();
            if (party.Count == 0) return;
            var sim = new MockBattleSimulator(ContentDb.Instance, chapter, stage, party);
            Router.Push(new BattleScreen(sim, sim.Update, chapter, stage));
        }

        public static List<CharacterView> BuildParty()
        {
            var roster = PresentationServices.Get<IRosterSource>();
            return roster.GetOwned().Take(5).ToList();
        }
    }
}
