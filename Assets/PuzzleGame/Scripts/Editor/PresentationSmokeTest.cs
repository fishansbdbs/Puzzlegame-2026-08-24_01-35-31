using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PuzzleGame.Presentation.Content;
using PuzzleGame.Presentation.Mock;

namespace PuzzleGame.Presentation.EditorTools
{
    /// <summary>
    /// Headless smoke test of the demo integration layer. Drives the mock
    /// summon flow (singles, multis, full Gather-In ladder, duplicates) and
    /// a complete battle through the simulator without any UI. Run:
    ///   Unity -batchmode -quit -executeMethod
    ///     PuzzleGame.Presentation.EditorTools.PresentationSmokeTest.Run
    /// </summary>
    public static class PresentationSmokeTest
    {
        static void Check(bool condition, string what)
        {
            if (!condition) throw new Exception("Smoke test failed: " + what);
            Debug.Log("[SmokeTest] ok: " + what);
        }

        public static void Run()
        {
            PresentationServices.Clear();
            var db = ContentDb.Load();
            var economy = new MockEconomy();
            var schedule = new MockSchedule(db);
            var roster = new MockRoster(db, economy);
            var summons = new MockSummons(db, economy, roster, schedule);

            TestSummons(db, economy, roster, summons);
            TestBattle(db, roster);
            Debug.Log("[SmokeTest] ALL OK");
        }

        static void TestSummons(ContentDb db, MockEconomy economy, MockRoster roster, MockSummons summons)
        {
            var banners = summons.GetBanners();
            Check(banners.Count >= 4, "active banners listed (" + banners.Count + ")");
            Check(banners.Any(b => b.Kind == BannerKind.Standard), "standard banner present");
            Check(banners.Any(b => b.Kind == BannerKind.GatherIn), "gather-in banner active");

            long gemsBefore = economy.GetBalance(CurrencyId.Gems);
            var single = summons.RequestSummon("standard_permanent", false);
            Check(single != null && single.Cards.Count == 1, "single summon yields 1 card");
            Check(economy.GetBalance(CurrencyId.Gems) == gemsBefore - 150, "single costs 150 gems");
            Check(single.Cards[0].Character != null, "card resolves to a character");

            var multi = summons.RequestSummon("standard_permanent", true);
            Check(multi != null && multi.Cards.Count == 10, "10-pull yields 10 cards");
            Check(multi.Cards.Any(c => c.Character.BaseRarity >= 4), "10-pull contains a 4-star or better");

            // Walk the entire Gather-In rotation 1 ladder.
            var gatherId = "gatherin_glimmer_r1";
            var gather = summons.GetBanner(gatherId);
            Check(gather != null && gather.Steps.Count == 8, "gather-in has 8 steps");
            SummonSession last = null;
            for (int i = 0; i < gather.Steps.Count; i++)
            {
                last = summons.RequestStepSummon(gatherId);
                Check(last != null, "gather-in step " + (i + 1) + " summons");
                Check(last.Cards.Count == gather.Steps[i].PullCount,
                    "step " + (i + 1) + " yields " + gather.Steps[i].PullCount + " cards");
            }
            Check(last.Cards.Any(c => c.Character.BaseRarity == 5), "final gather-in step guarantees a 5-star");
            Check(summons.RequestStepSummon(gatherId) == null, "steps are one-time within the rotation");

            // Rotation 2 final step guarantees the featured unit.
            var r2 = "gatherin_glimmer_r2";
            for (int i = 0; i < 7; i++) summons.RequestStepSummon(r2);
            var final2 = summons.RequestStepSummon(r2);
            Check(final2 != null && final2.Cards.Any(c => c.CharacterId == "water_bathtub"),
                "rotation 2 final step guarantees the featured 5-star");

            // Duplicate handling: pulling the same unit advances Ascension then overflows.
            var target = roster.GetOwned().First(c => c.Ascension < c.AscensionCap).Id;
            int before = roster.GetOwned(target).Ascension;
            var dup = roster.ApplyPull(target);
            Check(!dup.IsNew && dup.AscensionGained && dup.AscensionAfter == before + 1,
                "duplicate advances Ascension");
            for (int i = 0; i < 6; i++) dup = roster.ApplyPull(target);
            Check(!dup.AscensionGained && !string.IsNullOrEmpty(dup.OverflowReward),
                "over-cap duplicate converts to overflow reward");
        }

        static void TestBattle(ContentDb db, MockRoster roster)
        {
            var chapter = db.Chapters[1];
            var stage = chapter.stages.First(s => s.stageNumber == 1);
            var party = roster.GetOwned().Take(5).ToList();
            Check(party.Count == 5, "party of five assembled");

            var sim = new MockBattleSimulator(db, chapter, stage, party);
            int attacks = 0, matches = 0, combos = 0;
            bool ended = false;
            BattleEndEvent endEvent = null;
            sim.AttackPerformed += _ => attacks++;
            sim.MatchResolved += _ => matches++;
            sim.ComboChanged += c => combos = Math.Max(combos, c);
            sim.BattleEnded += e => { ended = true; endEvent = e; };

            var rng = new System.Random(1234);
            int safety = 0;
            while (!ended && safety++ < 400)
            {
                if (sim.CanMove)
                {
                    var start = new BoardCell(rng.Next(6), rng.Next(5));
                    sim.BeginMove(start);
                    var cell = start;
                    int steps = 3 + rng.Next(8);
                    for (int i = 0; i < steps; i++)
                    {
                        var next = cell;
                        if (rng.Next(2) == 0) next.Col = Mathf.Clamp(next.Col + (rng.Next(2) == 0 ? 1 : -1), 0, 5);
                        else next.Row = Mathf.Clamp(next.Row + (rng.Next(2) == 0 ? 1 : -1), 0, 4);
                        sim.DragTo(next);
                        cell = next;
                    }
                    sim.EndMove();
                }
                // Pump simulated time forward.
                for (int i = 0; i < 40; i++) sim.Update(0.1f);
            }
            Check(ended, "battle reaches an end state (after " + safety + " iterations)");
            Check(endEvent.Outcome == BattleOutcome.Victory, "stage 1 is winnable by random play with starter roster");
            Check(matches > 0, "matches resolved (" + matches + ")");
            Check(attacks > 0, "attack events fired (" + attacks + ")");
            Check(endEvent.StarsEarned >= 1, "stars awarded (" + endEvent.StarsEarned + ")");

            // Boss stage smoke: enemy actions fire (locks, conversions...).
            var bossStage = chapter.stages.First(s => s.stageNumber == 25);
            var bossSim = new MockBattleSimulator(db, chapter, bossStage, party);
            int enemyActions = 0;
            bool bossEnded = false;
            bossSim.EnemyActed += _ => enemyActions++;
            bossSim.BattleEnded += _ => bossEnded = true;
            safety = 0;
            while (!bossEnded && enemyActions < 3 && safety++ < 200)
            {
                if (bossSim.CanMove)
                {
                    bossSim.BeginMove(new BoardCell(rng.Next(6), rng.Next(5)));
                    bossSim.DragTo(new BoardCell(rng.Next(6), rng.Next(5)));
                    bossSim.EndMove();
                }
                for (int i = 0; i < 40; i++) bossSim.Update(0.1f);
            }
            Check(enemyActions >= 1 || bossEnded, "boss performs scripted actions");
        }
    }
}
