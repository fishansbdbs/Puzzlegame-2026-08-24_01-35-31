using System;
using NUnit.Framework;
using PuzzleGame.Core.Economy;

namespace PuzzleGame.Tests.EditMode.Economy
{
    public sealed class WalletTests
    {
        [Test]
        public void Wallet_tracks_each_named_currency_and_arbitrary_event_currency_independently()
        {
            var wallet = new Wallet();
            wallet.Add(WalletCurrencies.Gold, 10);
            wallet.Add(WalletCurrencies.Gems, 20);
            wallet.Add(WalletCurrencies.Tickets, 3);
            wallet.Add(WalletCurrencies.UniversalDuplicateResource, 7);
            wallet.Add("event:summer-2026", 9);

            Assert.That(wallet.GetBalance(WalletCurrencies.Gold), Is.EqualTo(10));
            Assert.That(wallet.GetBalance(WalletCurrencies.Gems), Is.EqualTo(20));
            Assert.That(wallet.GetBalance(WalletCurrencies.Tickets), Is.EqualTo(3));
            Assert.That(wallet.GetBalance(WalletCurrencies.UniversalDuplicateResource), Is.EqualTo(7));
            Assert.That(wallet.GetBalance("event:summer-2026"), Is.EqualTo(9));
        }

        [Test]
        public void Wallet_rejects_blank_currency_ids_and_negative_amounts()
        {
            var wallet = new Wallet();

            Assert.That(() => wallet.Add(" ", 1), Throws.TypeOf<ArgumentException>());
            Assert.That(() => wallet.GetBalance(null), Throws.TypeOf<ArgumentException>());
            Assert.That(() => wallet.Add(WalletCurrencies.Gold, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => wallet.TrySpend(WalletCurrencies.Gold, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Insufficient_spend_is_atomic()
        {
            var wallet = new Wallet();
            wallet.Add(WalletCurrencies.Gems, 150);

            Assert.That(wallet.TrySpend(WalletCurrencies.Gems, 151), Is.False);
            Assert.That(wallet.GetBalance(WalletCurrencies.Gems), Is.EqualTo(150));
        }

        [Test]
        public void Spend_changes_only_the_requested_currency()
        {
            var wallet = new Wallet();
            wallet.Add(WalletCurrencies.Gold, 50);
            wallet.Add(WalletCurrencies.Gems, 150);

            Assert.That(wallet.TrySpend(WalletCurrencies.Gems, 150), Is.True);
            Assert.That(wallet.GetBalance(WalletCurrencies.Gems), Is.EqualTo(0));
            Assert.That(wallet.GetBalance(WalletCurrencies.Gold), Is.EqualTo(50));
        }

        [Test]
        public void Overflowing_add_is_rejected_without_changing_the_balance()
        {
            var wallet = new Wallet();
            wallet.Add(WalletCurrencies.Gold, int.MaxValue);

            Assert.That(() => wallet.Add(WalletCurrencies.Gold, 1), Throws.TypeOf<OverflowException>());
            Assert.That(wallet.GetBalance(WalletCurrencies.Gold), Is.EqualTo(int.MaxValue));
        }
    }
}
