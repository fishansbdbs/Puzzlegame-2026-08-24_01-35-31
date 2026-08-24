using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using PuzzleGame.Core.Scheduling;
using PuzzleGame.Presentation;
using PuzzleGame.Presentation.CoreIntegration;
using PuzzleGame.Presentation.UI;
using PuzzleGame.Presentation.UI.Battle;
using PuzzleGame.Presentation.UI.Story;
using PuzzleGame.Presentation.UI.Summon;

namespace PuzzleGame.Tests.PresentationPlayMode
{
    /// <summary>
    /// Boots the REAL game composition (GameBootstrap + GameServices over
    /// in-memory save storage + core adapters) and walks the actual loop:
    /// main menu → summon hub → banner → real 10-pull → pack opening with
    /// core PackSummonFlow → stage select → real core battle to an end
    /// state → persisted progress. No mocks anywhere in this path.
    /// </summary>
    public class CoreGameUiTests
    {
        sealed class FixedClock : IClock
        {
            public DateTimeOffset Now { get { return new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero); } }
        }

        GameObject host;
        GameBootstrap bootstrap;
        GameServices services;
        InMemorySaveStorage storage;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            PresentationServices.Clear();
            GameServices.ResetForTests();
            storage = new InMemorySaveStorage();
            services = GameServices.Create(storage, new FixedClock(), 20260824);
            GameServices.InstallForTests(services);
            host = new GameObject("CoreGameUiHost");
            bootstrap = host.AddComponent<GameBootstrap>();
            yield return null;
            yield return null;
            Assert.IsNotNull(bootstrap.Router, "real game router created");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (host != null) UnityEngine.Object.Destroy(host);
            PresentationServices.Clear();
            GameServices.ResetForTests();
            yield return null;
        }

        static IEnumerator Frames(int count)
        {
            for (var index = 0; index < count; index++) yield return null;
        }

        [UnityTest]
        public IEnumerator RealSummonFlowThroughPackOpening()
        {
            var router = bootstrap.Router;
            router.Push(new SummonHubScreen());
            yield return Frames(3);
            router.Push(new BannerDetailScreen("standard_permanent"));
            yield return Frames(3);

            long gemsBefore = services.Wallet.GetBalance(Core.Economy.WalletCurrencies.Gems);
            var summons = PresentationServices.Get<ISummonSource>();
            var session = summons.RequestSummon("standard_permanent", true);
            Assert.IsNotNull(session, "real ten-pull rolled");
            Assert.AreEqual(gemsBefore - 1500, services.Wallet.GetBalance(Core.Economy.WalletCurrencies.Gems),
                "real gems spent through core wallet");
            var flow = session.CorePackFlow as Core.Gacha.PackSummonFlow;
            Assert.IsNotNull(flow, "core pack flow present");

            var pack = new PackOpeningScreen(session);
            router.Push(pack);
            yield return Frames(5);
            Assert.AreEqual(Core.Gacha.PackSummonState.PackPresented, flow.State, "pack presented through core flow");

            pack.AutoOpenForAutomation();
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(Core.Gacha.PackSummonState.PackOpened, flow.State, "pack opened through core flow");
            pack.AutoRevealForAutomation();
            yield return new WaitForSeconds(7f);
            Assert.AreEqual(Core.Gacha.PackSummonState.AllCardsRevealed, flow.State, "all cards revealed through core flow");

            foreach (var card in session.Cards)
            {
                Assert.IsTrue(services.IsOwned(card.CharacterId), "every revealed card is truly owned");
                if (!card.IsNew && card.AscensionGained)
                {
                    Assert.AreEqual(card.AscensionAfter, services.GetProgress(card.CharacterId).Ascension,
                        "revealed duplicate matches real core Ascension");
                }
            }
            router.Pop();
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator RealBattleDrivesHudToVictoryAndPersists()
        {
            var router = bootstrap.Router;
            router.Push(new StageSelectScreen());
            yield return Frames(3);

            var db = services.Db;
            var chapter = db.Chapters[1];
            var stage = chapter.stages.First(s => s.stageNumber == 1);
            var factory = PresentationServices.Get<IBattleFactory>();
            Action<float> pump;
            var source = factory.Create(chapter, stage, out pump);
            Assert.IsInstanceOf<CoreBattleAdapter>(source, "battle factory produced the core adapter, not a mock");
            router.Push(new BattleScreen(source, pump, chapter, stage));
            yield return Frames(5);

            int attacks = 0;
            bool endedFlag = false;
            BattleOutcome outcome = BattleOutcome.Retreat;
            source.AttackPerformed += _ => attacks++;
            source.BattleEnded += e => { endedFlag = true; outcome = e.Outcome; };

            var rng = new System.Random(7);
            float timeout = Time.time + 90f;
            var adapter = (CoreBattleAdapter)source;
            while (!endedFlag && Time.time < timeout)
            {
                // Accelerate the presentation replay beyond realtime.
                adapter.Update(0.25f);
                if (adapter.CanMove)
                {
                    var cell = new BoardCell(rng.Next(6), rng.Next(5));
                    adapter.BeginMove(cell);
                    for (var move = 0; move < 10; move++)
                    {
                        var next = cell;
                        if (rng.Next(2) == 0) next.Col = Mathf.Clamp(next.Col + (rng.Next(2) == 0 ? 1 : -1), 0, 5);
                        else next.Row = Mathf.Clamp(next.Row + (rng.Next(2) == 0 ? 1 : -1), 0, 4);
                        adapter.DragTo(next);
                        cell = next;
                    }
                    adapter.EndMove();
                }
                yield return null;
            }
            Assert.IsTrue(endedFlag, "core battle reached an end state through the HUD loop");
            Assert.AreEqual(BattleOutcome.Victory, outcome, "battle won");
            Assert.Greater(attacks, 0, "real attack events reached the HUD");
            yield return Frames(5);

            var progress = services.GetStageProgress(stage.id);
            Assert.IsNotNull(progress, "stage progress persisted");
            Assert.IsTrue(progress.IsCleared, "clear persisted through core save path");
            Assert.IsTrue(storage.Content != null && storage.Content.Contains(stage.id),
                "serialized save contains the cleared stage");
        }

        [UnityTest]
        public IEnumerator StageSelectShowsRealUnlockGating()
        {
            var library = PresentationServices.Get<IContentLibrary>();
            var stages = library.GetChapterStages(1).OrderBy(s => s.StageNumber).ToList();
            Assert.IsTrue(stages[0].Unlocked, "stage 1 unlocked from the start");
            // Later stages stay locked until their predecessor is cleared.
            var firstUncleared = stages.FirstOrDefault(s => !s.Cleared);
            if (firstUncleared != null && firstUncleared.StageNumber > 1)
            {
                var next = stages.FirstOrDefault(s => s.StageNumber == firstUncleared.StageNumber + 1);
                if (next != null) Assert.IsFalse(next.Unlocked, "stage after first uncleared stays locked");
            }
            yield return null;
        }
    }
}
