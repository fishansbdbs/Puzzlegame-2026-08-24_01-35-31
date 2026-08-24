using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using PuzzleGame.Presentation;
using PuzzleGame.Presentation.Content;
using PuzzleGame.Presentation.Mock;
using PuzzleGame.Presentation.UI;
using PuzzleGame.Presentation.UI.Battle;
using PuzzleGame.Presentation.UI.Events;
using PuzzleGame.Presentation.UI.Party;
using PuzzleGame.Presentation.UI.Story;
using PuzzleGame.Presentation.UI.Summon;

namespace PuzzleGame.Tests.PlayMode
{
    /// <summary>
    /// Boots the presentation demo for real (UIDocument, panel, animations)
    /// and walks every screen: menu, roster, character detail, summon hub,
    /// banner detail, a full pack-opening reveal, events hub, stage select
    /// and a live battle with the simulator pumping. Catches runtime UI
    /// construction and event-wiring failures the headless tests cannot.
    /// </summary>
    public class UiWalkthroughTests
    {
        GameObject _host;
        PresentationDemoBootstrap _bootstrap;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            PresentationServices.Clear();
            _host = new GameObject("UiWalkthroughHost");
            _bootstrap = _host.AddComponent<PresentationDemoBootstrap>();
            // Start() runs Launch on the first frame.
            yield return null;
            yield return null;
            Assert.IsNotNull(_bootstrap.Router, "router created");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null) Object.Destroy(_host);
            PresentationServices.Clear();
            yield return null;
        }

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenuBuilds()
        {
            var doc = _host.GetComponent<UIDocument>();
            Assert.IsNotNull(doc, "UIDocument exists");
            yield return Frames(3);
            Assert.Greater(doc.rootVisualElement.Q<Label>().text.Length, 0, "menu shows text");
            Assert.IsTrue(doc.rootVisualElement.Query<Label>().ToList()
                .Any(l => l.text.Contains(GameInfo.Title)), "title shown from GameInfo");
        }

        [UnityTest]
        public IEnumerator RosterAndCharacterDetailBuild()
        {
            var router = _bootstrap.Router;
            router.Push(new PartyScreen());
            yield return Frames(3);
            var roster = PresentationServices.Get<IRosterSource>();
            var first = roster.GetOwned()[0];
            router.Push(new CharacterDetailScreen(first.Id));
            yield return Frames(3);
            router.Pop();
            router.Pop();
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator SummonFlowRunsThroughPackOpening()
        {
            var router = _bootstrap.Router;
            router.Push(new SummonHubScreen());
            yield return Frames(3);
            router.Push(new BannerDetailScreen("standard_permanent"));
            yield return Frames(3);

            var summons = PresentationServices.Get<ISummonSource>();
            var session = summons.RequestSummon("standard_permanent", true);
            Assert.IsNotNull(session, "10-pull session generated");
            var pack = new PackOpeningScreen(session);
            router.Push(pack);
            yield return Frames(5);

            pack.AutoOpenForAutomation();
            // Wait for the tear + card deal animations.
            yield return new WaitForSeconds(1.2f);
            pack.AutoRevealForAutomation();
            // 10 cards, possibly with dramatic 5★ build-ups.
            yield return new WaitForSeconds(6f);
            router.Pop();
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator EventsHubBuilds()
        {
            var router = _bootstrap.Router;
            router.Push(new EventsScreen());
            yield return Frames(3);
            router.Pop();
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator BattleRunsWithLiveHud()
        {
            var router = _bootstrap.Router;
            router.Push(new StageSelectScreen());
            yield return Frames(3);

            var db = ContentDb.Instance;
            var chapter = db.Chapters[1];
            var stage = chapter.stages.First(s => s.stageNumber == 2); // no dialogue gate
            var party = StageSelectScreen.BuildParty();
            var sim = new MockBattleSimulator(db, chapter, stage, party);
            router.Push(new BattleScreen(sim, sim.Update, chapter, stage));
            yield return Frames(5);

            // Drive a few moves through the real HUD tick loop.
            var rng = new System.Random(42);
            float timeout = Time.time + 60f;
            bool ended = false;
            sim.BattleEnded += _ => ended = true;
            while (!ended && Time.time < timeout)
            {
                // Accelerate the simulator clock beyond realtime so the
                // walkthrough finishes quickly; the HUD still renders live.
                sim.Update(0.25f);
                if (sim.CanMove)
                {
                    var start = new BoardCell(rng.Next(6), rng.Next(5));
                    sim.BeginMove(start);
                    var cell = start;
                    for (int i = 0; i < 10; i++)
                    {
                        var next = cell;
                        if (rng.Next(2) == 0) next.Col = Mathf.Clamp(next.Col + (rng.Next(2) == 0 ? 1 : -1), 0, 5);
                        else next.Row = Mathf.Clamp(next.Row + (rng.Next(2) == 0 ? 1 : -1), 0, 4);
                        sim.DragTo(next);
                        cell = next;
                    }
                    sim.EndMove();
                }
                yield return null;
            }
            Assert.IsTrue(ended, "battle ends within the play-mode timeout");
            yield return Frames(5);
        }
    }
}
