using System;
using System.Collections.Generic;

namespace PuzzleGame.Presentation.Mock
{
    /// <summary>
    /// DEMO ONLY. Stands in for Codex's economy/save systems so presentation
    /// screens can run. Not authoritative; holds state in memory only.
    /// </summary>
    public class MockEconomy : IEconomySource
    {
        readonly Dictionary<CurrencyId, long> _balances = new Dictionary<CurrencyId, long>
        {
            { CurrencyId.Gold, 128500 },
            { CurrencyId.Gems, 12000 },
            { CurrencyId.Ticket, 3 },
            { CurrencyId.EventToken, 240 }
        };

        public event Action BalancesChanged;

        public long GetBalance(CurrencyId currency)
        {
            return _balances.TryGetValue(currency, out var v) ? v : 0;
        }

        public bool TrySpend(CurrencyId currency, long amount)
        {
            if (GetBalance(currency) < amount) return false;
            _balances[currency] = GetBalance(currency) - amount;
            BalancesChanged?.Invoke();
            return true;
        }

        public void Grant(CurrencyId currency, long amount)
        {
            _balances[currency] = GetBalance(currency) + amount;
            BalancesChanged?.Invoke();
        }
    }
}
