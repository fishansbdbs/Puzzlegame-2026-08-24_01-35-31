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
            var flow = PackSummonFlow.Start(FlowFixtures.Batch(1));
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
            var flow = PackSummonFlow.Start(FlowFixtures.Batch(2));
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
            var flow = PackSummonFlow.Start(FlowFixtures.Batch(1));
            Assert.That(() => flow.OpenPack(), Throws.TypeOf<InvalidOperationException>());
            Assert.That(flow.State, Is.EqualTo(PackSummonState.PurchaseValidated));
            flow.PresentPack();
            Assert.That(() => flow.PresentPack(), Throws.TypeOf<InvalidOperationException>());
            Assert.That(flow.State, Is.EqualTo(PackSummonState.PackPresented));
        }

        [Test]
        public void Reentrant_subscriber_cannot_advance_or_corrupt_the_flow()
        {
            var flow = PackSummonFlow.Start(FlowFixtures.Batch(1));
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
    }

    internal static class FlowFixtures
    {
        internal static SummonBatch Batch(int count)
        {
            var values = new int[count == 1 ? 1 : 10];
            var service = SummonFixtures.CreateService(PuzzleGame.Core.Contracts.BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) }, new ScriptedRandom(values));
            service.Wallet.Add(PuzzleGame.Core.Economy.WalletCurrencies.Gems, count == 1 ? 150 : 1500);
            return service.PurchaseAndRoll(count == 1 ? 1 : 10);
        }
    }
}
