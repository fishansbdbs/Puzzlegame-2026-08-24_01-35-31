using System;
using NUnit.Framework;
using PuzzleGame.Core.Gacha;

namespace PuzzleGame.Tests.EditMode.Gacha
{
    public sealed class PackSummonFlowTests
    {
        [Test]
        public void Flow_follows_every_ordered_transition_one_card_at_a_time()
        {
            var flow = PackSummonFlow.Start(FlowFixtures.BatchOfOne());
            var hooks = "";
            flow.Transitioned += e => hooks += e.State + ",";

            flow.PresentPack(); flow.StartPackRip(); flow.OpenPack();
            flow.PrepareNextCard(); flow.RevealReadyCard();
            flow.MarkAllCardsRevealed(); flow.CompleteResults();

            Assert.That(flow.State, Is.EqualTo(PackSummonState.ResultsComplete));
            Assert.That(hooks, Is.EqualTo("PackPresented,PackRipStarted,PackOpened,CardReady,CardRevealed,AllCardsRevealed,ResultsComplete,"));
        }

        [Test]
        public void Reveal_all_emits_each_remaining_card_and_finishes_results()
        {
            var flow = PackSummonFlow.Start(FlowFixtures.BatchOfTen());
            var ready = 0; var revealed = 0;
            flow.CardReady += e => ready++;
            flow.CardRevealed += e => revealed++;
            flow.PresentPack(); flow.StartPackRip(); flow.OpenPack();

            flow.RevealAll();

            Assert.That(ready, Is.EqualTo(10));
            Assert.That(revealed, Is.EqualTo(10));
            Assert.That(flow.State, Is.EqualTo(PackSummonState.ResultsComplete));
        }

        [Test]
        public void Invalid_duplicate_and_out_of_order_transitions_throw_without_mutation()
        {
            var flow = PackSummonFlow.Start(FlowFixtures.BatchOfOne());
            Assert.That(() => flow.OpenPack(), Throws.TypeOf<InvalidOperationException>());
            Assert.That(flow.State, Is.EqualTo(PackSummonState.PurchaseValidated));
            flow.PresentPack();
            Assert.That(() => flow.PresentPack(), Throws.TypeOf<InvalidOperationException>());
            Assert.That(flow.State, Is.EqualTo(PackSummonState.PackPresented));
        }

        [Test]
        public void Reentrant_subscriber_cannot_advance_or_corrupt_the_flow()
        {
            var flow = PackSummonFlow.Start(FlowFixtures.BatchOfOne());
            var attempted = false;
            flow.PackPresented += e =>
            {
                attempted = true;
                Assert.That(() => flow.StartPackRip(), Throws.TypeOf<InvalidOperationException>());
            };

            flow.PresentPack();

            Assert.That(attempted, Is.True);
            Assert.That(flow.State, Is.EqualTo(PackSummonState.PackPresented));
        }

        [Test]
        public void Create_then_begin_emits_purchase_validated_exactly_once_before_any_other_hook()
        {
            var flow = PackSummonFlow.Create(FlowFixtures.BatchOfOne());
            var hooks = "";
            flow.PurchaseValidated += e => hooks += "purchase:" + e.CardIndex + ",";
            flow.Transitioned += e => hooks += e.State + ",";

            flow.Begin();
            Assert.That(flow.State, Is.EqualTo(PackSummonState.PurchaseValidated));
            Assert.That(hooks, Is.EqualTo("PurchaseValidated,purchase:-1,"));
            Assert.That(() => flow.Begin(), Throws.TypeOf<InvalidOperationException>());
            flow.PresentPack();
            Assert.That(hooks, Does.EndWith("PackPresented,"));
        }

        [Test]
        public void Partial_reveal_all_keeps_ready_card_index_and_emits_remaining_typed_indexes_once()
        {
            var flow = PackSummonFlow.Create(FlowFixtures.BatchOfTen());
            var ready = ""; var revealed = "";
            flow.CardReady += e => ready += e.CardIndex + ",";
            flow.CardRevealed += e => revealed += e.CardIndex + ",";
            flow.Begin(); flow.PresentPack(); flow.StartPackRip(); flow.OpenPack();
            flow.PrepareNextCard();
            flow.RevealAll();

            Assert.That(ready, Is.EqualTo("0,1,2,3,4,5,6,7,8,9,"));
            Assert.That(revealed, Is.EqualTo("0,1,2,3,4,5,6,7,8,9,"));
            Assert.That(flow.State, Is.EqualTo(PackSummonState.ResultsComplete));
        }

        [Test]
        public void Typed_hooks_emit_complete_order_with_exact_card_indexes()
        {
            var flow = PackSummonFlow.Create(FlowFixtures.BatchOfOne());
            var hooks = "";
            flow.PurchaseValidated += e => hooks += "PurchaseValidated:" + e.CardIndex + ",";
            flow.PackPresented += e => hooks += "PackPresented:" + e.CardIndex + ",";
            flow.PackRipStarted += e => hooks += "PackRipStarted:" + e.CardIndex + ",";
            flow.PackOpened += e => hooks += "PackOpened:" + e.CardIndex + ",";
            flow.CardReady += e => hooks += "CardReady:" + e.CardIndex + ",";
            flow.CardRevealed += e => hooks += "CardRevealed:" + e.CardIndex + ",";
            flow.AllCardsRevealed += e => hooks += "AllCardsRevealed:" + e.CardIndex + ",";
            flow.ResultsComplete += e => hooks += "ResultsComplete:" + e.CardIndex + ",";

            flow.Begin(); flow.PresentPack(); flow.StartPackRip(); flow.OpenPack(); flow.PrepareNextCard(); flow.RevealReadyCard(); flow.MarkAllCardsRevealed(); flow.CompleteResults();

            Assert.That(hooks, Is.EqualTo("PurchaseValidated:-1,PackPresented:-1,PackRipStarted:-1,PackOpened:-1,CardReady:0,CardRevealed:0,AllCardsRevealed:-1,ResultsComplete:-1,"));
        }
    }

    internal static class FlowFixtures
    {
        internal static SummonBatch BatchOfOne() { return Batch(1); }
        internal static SummonBatch BatchOfTen() { return Batch(10); }
        internal static SummonBatch Batch(int count)
        {
            if (count != 1 && count != 10) throw new ArgumentOutOfRangeException("count", "Only valid summon batch sizes can be constructed.");
            var values = new int[count];
            var service = SummonFixtures.CreateService(PuzzleGame.Core.Contracts.BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) }, new ScriptedRandom(values));
            service.Wallet.Add(PuzzleGame.Core.Economy.WalletCurrencies.Gems, count == 1 ? 150 : 1500);
            return service.PurchaseAndRoll(count);
        }
    }
}
