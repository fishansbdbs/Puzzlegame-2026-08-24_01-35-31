using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PuzzleGame.Core.Economy;
using PuzzleGame.Core.Gacha;
using PuzzleGame.Core.Scheduling;
using PuzzleGame.Presentation.CoreIntegration;

namespace PuzzleGame.Presentation.EditorTools
{
    /// <summary>
    /// Headless cross-system integration checks: content translation onto
    /// core contracts, real summons (standard + Gather-In guarantees +
    /// duplicates), a real battle through CoreBattleAdapter to victory with
    /// persisted stars/rewards, real Awakening, scheduler-driven banners,
    /// and a full save round-trip. Run:
    ///   Unity -batchmode -quit -executeMethod
    ///     PuzzleGame.Presentation.EditorTools.CoreIntegrationSmokeTest.Run
    /// </summary>
    public static class CoreIntegrationSmokeTest
    {
        sealed class FixedClock : IClock
        {
            readonly DateTimeOffset now;
            public FixedClock(DateTimeOffset now) { this.now = now; }
            public DateTimeOffset Now { get { return now; } }
        }

        static void Check(bool condition, string what)
        {
            if (!condition) throw new Exception("Integration check failed: " + what);
            Debug.Log("[CoreIntegration] ok: " + what);
        }

        public static void Run()
        {
            var clock = new FixedClock(new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero));
            var storage = new InMemorySaveStorage();
            var services = GameServices.Create(storage, clock, 424242);

            TestTranslation(services);
            TestSchedule(services);
            TestSummons(services);
            TestGatherIn(services);
            TestBattle(services);
            TestAwakening(services);
            TestSaveRoundTrip(services, storage, clock);
            Debug.Log("[CoreIntegration] ALL OK");
        }

        static void TestTranslation(GameServices services)
        {
            var content = services.Content;
            Check(content.Characters.Count == services.Db.Characters.Count, "all characters translated (" + content.Characters.Count + ")");
            Check(content.Stages.Count >= 600, "all stages translated (" + content.Stages.Count + ")");
            Check(content.Banners.Count == services.Db.Banners.Count, "all banners translated (" + content.Banners.Count + ")");
            Check(content.Schedules.Count == services.Db.Schedule.Count, "all schedules translated (" + content.Schedules.Count + ")");
            Check(content.Enemies.Count >= services.Db.Enemies.Count, "enemy variants materialized (" + content.Enemies.Count + ")");
            var fiveStar = services.Db.Characters.Values.First(c => c.rarity == 5 && c.HasAwakenedForm);
            var translated = content.Characters[fiveStar.id];
            Check(translated.Awakening.RequiredLevel == fiveStar.maxLevel, "5★ awakening requires max level");
            Check(translated.AwakenedVisuals.PortraitKey == fiveStar.awakened.artRef, "awakened visuals keep authored art keys");
        }

        static void TestSchedule(GameServices services)
        {
            var active = services.GetActiveContent();
            Check(active.Count > 0, "scheduler returns active content (" + active.Count + ")");
            Check(active.Any(c => c.ContentId == "standard_permanent"), "standard banner active");
            Check(active.Any(c => c.ContentId == "featured_gym_legends"), "featured banner active for injected date");
            Check(active.Any(c => c.ContentId == "gatherin_glimmer_r1"), "gather-in rotation 1 active for injected date");
            Check(active.Any(c => c.ContentId == "ev_bog_tax_season"), "bog event active for injected date");
        }

        static void TestSummons(GameServices services)
        {
            var roster = new CoreRosterAdapter(services);
            var adapter = new CoreSummonAdapter(services, roster);
            var banners = adapter.GetBanners();
            Check(banners.Count >= 4, "summon hub lists scheduled banners (" + banners.Count + ")");

            long gemsBefore = services.Wallet.GetBalance(WalletCurrencies.Gems);
            var session = adapter.RequestSummon("standard_permanent", false);
            Check(session != null && session.Cards.Count == 1, "single summon yields one card");
            Check(services.Wallet.GetBalance(WalletCurrencies.Gems) == gemsBefore - 150, "single pull spent 150 real gems");
            Check(session.CorePackFlow is PackSummonFlow, "core pack flow attached to session");
            var card = session.Cards[0];
            Check(card.Character != null && card.Character.Id == card.CharacterId, "card resolves presentation view for real result");
            Check(services.IsOwned(card.CharacterId), "summoned character is owned in the real collection");

            // Duplicates: keep pulling until one repeats, then verify real
            // Ascension moved and the presentation card reported it.
            var found = false;
            for (var attempt = 0; attempt < 30 && !found; attempt++)
            {
                var multi = adapter.RequestSummon("standard_permanent", true);
                Check(multi != null && multi.Cards.Count == 10, "ten-pull yields ten cards");
                foreach (var pulled in multi.Cards)
                {
                    if (pulled.IsNew) continue;
                    var progress = services.GetProgress(pulled.CharacterId);
                    if (pulled.AscensionGained)
                    {
                        Check(progress.Ascension == pulled.AscensionAfter, "presentation Ascension matches core progress");
                    }
                    else
                    {
                        Check(progress.Ascension == 5 && !string.IsNullOrEmpty(pulled.OverflowReward), "over-cap duplicate reports overflow");
                    }
                    found = true;
                    break;
                }
            }
            Check(found, "duplicate summon observed within bounded pulls");
        }

        static void TestGatherIn(GameServices services)
        {
            services.Wallet.Add(WalletCurrencies.Gems, 20000);
            var roster = new CoreRosterAdapter(services);
            var adapter = new CoreSummonAdapter(services, roster);
            var banner = adapter.GetBanner("gatherin_glimmer_r1");
            Check(banner != null && banner.Steps.Count == 8, "gather-in rotation 1 exposes 8 steps");
            SummonSession last = null;
            for (var step = 0; step < 8; step++)
            {
                last = adapter.RequestStepSummon("gatherin_glimmer_r1");
                Check(last != null, "gather-in step " + (step + 1) + " summons");
                Check(last.Cards.Count == banner.Steps[step].PullCount, "step " + (step + 1) + " yields authored pull count");
            }
            Check(last.Cards.Any(c => c.Character.BaseRarity == 5), "rotation 1 final step guarantees a real 5★");
            Check(adapter.RequestStepSummon("gatherin_glimmer_r1") == null, "steps are one-time within the rotation");

            for (var step = 0; step < 7; step++) adapter.RequestStepSummon("gatherin_glimmer_r2");
            var final = adapter.RequestStepSummon("gatherin_glimmer_r2");
            Check(final != null && final.Cards.Any(c => c.CharacterId == "water_bathtub"),
                "rotation 2 final step guarantees the featured 5★");
        }

        static void TestBattle(GameServices services)
        {
            var chapter = services.Db.Chapters[1];
            var stage = chapter.stages.First(s => s.stageNumber == 1);
            var adapter = new CoreBattleAdapter(services, stage);
            int matches = 0, attacks = 0, heals = 0, boardChanges = 0;
            BattleEndEvent end = null;
            adapter.MatchResolved += _ => matches++;
            adapter.AttackPerformed += _ => attacks++;
            adapter.Healed += _ => heals++;
            adapter.BoardChanged += () => boardChanges++;
            adapter.BattleEnded += e => end = e;

            long goldBefore = services.Wallet.GetBalance(WalletCurrencies.Gold);
            var rng = new System.Random(99);
            var safety = 0;
            while (end == null && safety++ < 300)
            {
                if (adapter.CanMove)
                {
                    var cell = new BoardCell(rng.Next(6), rng.Next(5));
                    adapter.BeginMove(cell);
                    for (var move = 0; move < 8; move++)
                    {
                        var next = cell;
                        if (rng.Next(2) == 0) next.Col = Mathf.Clamp(next.Col + (rng.Next(2) == 0 ? 1 : -1), 0, 5);
                        else next.Row = Mathf.Clamp(next.Row + (rng.Next(2) == 0 ? 1 : -1), 0, 4);
                        adapter.DragTo(next);
                        cell = next;
                    }
                    adapter.EndMove();
                }
                for (var tick = 0; tick < 60; tick++) adapter.Update(0.1f);
            }
            Check(end != null, "core battle reaches an end state (iterations: " + safety + ")");
            Check(end.Outcome == BattleOutcome.Victory, "stage 1 winnable with the real starter party");
            Check(matches > 0 && attacks > 0, "match/attack events observed (" + matches + "/" + attacks + ")");
            Check(boardChanges > 0, "board change events observed");
            Check(end.StarsEarned >= 1, "stars earned (" + end.StarsEarned + ")");

            var progress = services.GetStageProgress(stage.id);
            Check(progress != null && progress.IsCleared, "stage completion persisted");
            Check(progress.Stars[0], "clear star persisted");
            Check(services.Wallet.GetBalance(WalletCurrencies.Gold) > goldBefore, "stage rewards granted through the real wallet");
        }

        static void TestAwakening(GameServices services)
        {
            var fiveStarId = services.OwnedCharacterIds
                .FirstOrDefault(id => services.Db.Characters[id].rarity == 5 && services.Db.Characters[id].HasAwakenedForm
                                      && !services.GetProgress(id).IsAwakened);
            Check(fiveStarId != null, "an unawakened owned 5★ exists after summons");
            var progress = services.GetProgress(fiveStarId);
            var requirements = progress.AuthoredData.Awakening;

            services.Wallet.Add(WalletCurrencies.Gold, 5000000);
            while (progress.Level < requirements.RequiredLevel)
            {
                Check(services.TrySpendGoldForLevels(fiveStarId, 10), "level-up spends gold and applies real experience");
            }
            foreach (var material in requirements.Materials) services.GrantMaterial(material.MaterialId, material.Amount);

            var roster = new CoreRosterAdapter(services);
            var lines = new List<string>();
            Check(roster.GetAwakenRequirements(fiveStarId, lines), "awakening requirements met after farming");
            long goldBefore = services.Wallet.GetBalance(WalletCurrencies.Gold);
            Check(roster.TryAwaken(fiveStarId), "real Awakening succeeds");
            Check(services.Wallet.GetBalance(WalletCurrencies.Gold) == goldBefore - requirements.GoldCost, "awakening spent real gold");
            Check(progress.IsAwakened && progress.EffectiveRarity == 6, "core progress is 6★ awakened");
            var dto = services.Db.Characters[fiveStarId];
            Check(progress.CurrentVisuals.PortraitKey == dto.awakened.artRef, "core visuals switched to awakened art keys");
            var view = roster.GetOwned(fiveStarId);
            Check(view.Awakened && view.CurrentRarity == 6, "presentation view reflects awakened state");
        }

        static void TestSaveRoundTrip(GameServices services, InMemorySaveStorage storage, IClock clock)
        {
            services.SaveNow();
            var expectedGems = services.Wallet.GetBalance(WalletCurrencies.Gems);
            var expectedGold = services.Wallet.GetBalance(WalletCurrencies.Gold);
            var expectedOwned = services.OwnedCharacterIds.Count;
            var awakenedId = services.OwnedCharacterIds.First(id => services.GetProgress(id).IsAwakened);
            var ascended = services.OwnedCharacterIds
                .Select(id => services.GetProgress(id))
                .First(p => p.Ascension > 0);

            var reloaded = GameServices.Create(storage, clock, 5555);
            Check(reloaded.Wallet.GetBalance(WalletCurrencies.Gems) == expectedGems, "gems survive save round-trip");
            Check(reloaded.Wallet.GetBalance(WalletCurrencies.Gold) == expectedGold, "gold survives save round-trip");
            Check(reloaded.OwnedCharacterIds.Count == expectedOwned, "collection survives save round-trip (" + expectedOwned + ")");
            Check(reloaded.GetProgress(awakenedId).IsAwakened, "awakened state survives save round-trip");
            Check(reloaded.GetProgress(ascended.CharacterId).Ascension == ascended.Ascension, "ascension survives save round-trip");
            var stageProgress = reloaded.GetStageProgress("ch01_st01");
            Check(stageProgress != null && stageProgress.IsCleared, "stage clears survive save round-trip");
            var gatherState = reloaded.GetSummonService("gatherin_glimmer_r1").State;
            Check(gatherState.IsComplete, "gather-in step progress survives save round-trip");
        }
    }
}
