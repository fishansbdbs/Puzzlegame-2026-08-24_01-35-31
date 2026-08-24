using System;
using System.Collections.Generic;

namespace PuzzleGame.Core.Economy
{
    public static class WalletCurrencies
    {
        public const string Gold = "gold";
        public const string Gems = "gems";
        public const string Tickets = "tickets";
        public const string UniversalDuplicateResource = "universal-duplicate-resource";
    }

    public sealed class Wallet
    {
        private readonly Dictionary<string, int> balances = new Dictionary<string, int>(StringComparer.Ordinal);

        public int GetBalance(string currencyId)
        {
            ValidateCurrencyId(currencyId);
            int balance;
            return balances.TryGetValue(currencyId, out balance) ? balance : 0;
        }

        public int Add(string currencyId, int amount)
        {
            ValidateCurrencyId(currencyId);
            ValidateAmount(amount);

            var current = GetBalance(currencyId);
            int next;
            try
            {
                next = checked(current + amount);
            }
            catch (OverflowException)
            {
                throw new OverflowException("Wallet balance cannot exceed Int32.MaxValue.");
            }

            balances[currencyId] = next;
            return next;
        }

        public bool CanAfford(string currencyId, int amount)
        {
            ValidateCurrencyId(currencyId);
            ValidateAmount(amount);
            return GetBalance(currencyId) >= amount;
        }

        public bool TrySpend(string currencyId, int amount)
        {
            ValidateCurrencyId(currencyId);
            ValidateAmount(amount);
            var current = GetBalance(currencyId);
            if (current < amount) return false;
            balances[currencyId] = current - amount;
            return true;
        }

        private static void ValidateCurrencyId(string currencyId)
        {
            if (string.IsNullOrWhiteSpace(currencyId))
                throw new ArgumentException("Currency ID is required.", "currencyId");
        }

        private static void ValidateAmount(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException("amount", "Amount cannot be negative.");
        }
    }
}
