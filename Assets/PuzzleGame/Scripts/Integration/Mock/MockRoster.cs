using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Presentation.Content;

namespace PuzzleGame.Presentation.Mock
{
    /// <summary>
    /// DEMO ONLY. In-memory owned roster with level/Ascension/Awakening
    /// state so progression screens can be exercised before core lands.
    /// Awakening follows the spec: level cap + farmable materials, never
    /// duplicates. Materials are mocked as a simple counter.
    /// </summary>
    public class MockRoster : IRosterSource
    {
        class OwnedState
        {
            public string Id;
            public int Level = 1;
            public int Ascension;
            public bool Awakened;
        }

        readonly Dictionary<string, OwnedState> _owned = new Dictionary<string, OwnedState>();
        readonly ContentDb _db;
        readonly MockEconomy _economy;

        public int AwakeningMaterials = 12;   // mock farmable material stock

        public event Action RosterChanged;

        public MockRoster(ContentDb db, MockEconomy economy)
        {
            _db = db;
            _economy = economy;
            // Starter roster: a spread of elements and rarities, one unit
            // already close to awakening so the ceremony is easy to demo.
            int i = 0;
            foreach (var dto in _db.Characters.Values.OrderByDescending(c => c.rarity))
            {
                if (i >= 12) break;
                var state = new OwnedState { Id = dto.id, Level = Math.Min(dto.maxLevel, 8 + i * 3) };
                if (i == 0) { state.Level = dto.maxLevel; state.Ascension = 2; }
                if (i == 1) { state.Level = dto.maxLevel; state.Awakened = true; state.Ascension = 5; }
                _owned[dto.id] = state;
                i++;
            }
        }

        public IReadOnlyList<CharacterView> GetOwned()
        {
            return _owned.Values
                .Select(ToView)
                .Where(v => v != null)
                .OrderByDescending(v => v.CurrentRarity)
                .ThenBy(v => v.Element)
                .ToList();
        }

        public CharacterView GetOwned(string characterId)
        {
            return _owned.TryGetValue(characterId ?? "", out var state) ? ToView(state) : null;
        }

        CharacterView ToView(OwnedState state)
        {
            return _db.Characters.TryGetValue(state.Id, out var dto)
                ? _db.ToView(dto, state.Level, state.Ascension, state.Awakened)
                : null;
        }

        public bool IsOwned(string characterId)
        {
            return _owned.ContainsKey(characterId ?? "");
        }

        /// <summary>Returns the duplicate outcome for a pull: (isNew, ascensionAfter, gained, overflow).</summary>
        public SummonCardResult ApplyPull(string characterId)
        {
            var result = new SummonCardResult { CharacterId = characterId };
            if (!_owned.TryGetValue(characterId, out var state))
            {
                _owned[characterId] = new OwnedState { Id = characterId };
                result.IsNew = true;
                result.AscensionAfter = 0;
            }
            else
            {
                result.IsNew = false;
                if (state.Ascension < 5)
                {
                    state.Ascension++;
                    result.AscensionGained = true;
                    result.AscensionAfter = state.Ascension;
                }
                else
                {
                    result.AscensionAfter = state.Ascension;
                    result.OverflowReward = "+50 Spark Essence";
                    _economy.Grant(CurrencyId.EventToken, 50);
                }
            }
            result.Character = GetOwned(characterId);
            RosterChanged?.Invoke();
            return result;
        }

        public bool TryLevelUp(string characterId, int levels)
        {
            if (!_owned.TryGetValue(characterId, out var state)) return false;
            if (!_db.Characters.TryGetValue(characterId, out var dto)) return false;
            int target = Math.Min(dto.maxLevel, state.Level + levels);
            int gained = target - state.Level;
            if (gained <= 0) return false;
            long cost = gained * 400L * dto.rarity;
            if (!_economy.TrySpend(CurrencyId.Gold, cost)) return false;
            state.Level = target;
            RosterChanged?.Invoke();
            return true;
        }

        public bool TryAwaken(string characterId)
        {
            if (!_owned.TryGetValue(characterId, out var state)) return false;
            if (!_db.Characters.TryGetValue(characterId, out var dto)) return false;
            if (state.Awakened || dto.awakened == null) return false;
            if (dto.rarity < 5 || state.Level < dto.maxLevel || AwakeningMaterials < 10) return false;
            AwakeningMaterials -= 10;
            state.Awakened = true;
            RosterChanged?.Invoke();
            return true;
        }

        public bool GetAwakenRequirements(string characterId, List<string> outLines)
        {
            outLines.Clear();
            if (!_owned.TryGetValue(characterId, out var state)) return false;
            if (!_db.Characters.TryGetValue(characterId, out var dto)) return false;
            if (dto.awakened == null || dto.rarity < 5)
            {
                outLines.Add("This character has no 6★ Awakened form yet.");
                return false;
            }
            if (state.Awakened)
            {
                outLines.Add("Already awakened.");
                return false;
            }
            bool levelOk = state.Level >= dto.maxLevel;
            bool matsOk = AwakeningMaterials >= 10;
            outLines.Add((levelOk ? "✓" : "✗") + " Reach level " + dto.maxLevel + " (now " + state.Level + ")");
            outLines.Add((matsOk ? "✓" : "✗") + " 10× Radiant Cores (have " + AwakeningMaterials + ")");
            outLines.Add("Duplicates are NOT required for Awakening.");
            return levelOk && matsOk;
        }
    }
}
