using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PuzzleGame.Presentation.Content;
using Random = System.Random;

namespace PuzzleGame.Presentation.Mock
{
    /// <summary>
    /// DEMO ONLY battle driver. Emulates enough of the core loop (drag-swap
    /// movement, match/cascade resolution, per-match attack events, enemy
    /// countdowns and scripted actions) that the battle HUD and VFX can be
    /// built and reviewed. Codex's board/combat systems are authoritative;
    /// this class must never be shipped as gameplay.
    /// Call <see cref="Update"/> every frame from the demo driver.
    /// </summary>
    public class MockBattleSimulator : IBattleEventSource
    {
        public const int Cols = 6;
        public const int Rows = 5;

        readonly Random _rng = new Random();
        readonly ContentDb _db;
        readonly ChapterDto _chapter;
        readonly StageDto _stage;
        int _waveIndex = -1;

        readonly OrbColor[,] _board = new OrbColor[Cols, Rows];
        readonly OrbStateFlags[,] _states = new OrbStateFlags[Cols, Rows];
        readonly BattleHudState _state = new BattleHudState();

        class TimedEvent
        {
            public float At;
            public Action Fire;
        }

        readonly List<TimedEvent> _timeline = new List<TimedEvent>();
        float _clock;
        float _busyUntil;
        bool _resolving;
        BoardCell _dragCell;
        bool _dragging;
        bool _ended;

        public BattleHudState State => _state;
        public OrbColor[,] Board => _board;
        public OrbStateFlags[,] BoardStates => _states;

        public event Action BoardChanged;
        public event Action<MatchEvent> MatchResolved;
        public event Action<AttackEvent> AttackPerformed;
        public event Action<HealEvent> Healed;
        public event Action<int> ComboChanged;
        public event Action<EnemyActionEvent> EnemyActed;
        public event Action<int> EnemyCountdownChanged;
        public event Action<int> EnemyDefeated;
        public event Action<SkillCastEvent> SkillCast;
        public event Action StateChanged;
        public event Action<BattleEndEvent> BattleEnded;

        public MockBattleSimulator(ContentDb db, ChapterDto chapter, StageDto stage, List<CharacterView> party)
        {
            _db = db;
            _chapter = chapter;
            _stage = stage;
            _state.StageName = stage != null ? stage.name : "Demo Skirmish";
            _state.WaveCount = stage != null ? Math.Max(1, stage.waves.Count) : 1;
            foreach (var c in party.Take(5))
            {
                _state.Party.Add(new PartyMemberView { Character = c, IsLeader = _state.Party.Count == 0 });
            }
            _state.PartyHpMax = _state.Party.Sum(p => (long)p.Character.Hp);
            _state.PartyHp = _state.PartyHpMax;
            FillBoardNoMatches();
            NextWave();
        }

        // ------------------------------ Board -------------------------------

        void FillBoardNoMatches()
        {
            for (int c = 0; c < Cols; c++)
            {
                for (int r = 0; r < Rows; r++)
                {
                    _board[c, r] = RandomOrbAvoidingMatch(c, r);
                    _states[c, r] = OrbStateFlags.None;
                }
            }
        }

        OrbColor RandomOrbAvoidingMatch(int c, int r)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                var orb = (OrbColor)_rng.Next(6);
                bool horiz = c >= 2 && _board[c - 1, r] == orb && _board[c - 2, r] == orb;
                bool vert = r >= 2 && _board[c, r - 1] == orb && _board[c, r - 2] == orb;
                if (!horiz && !vert) return orb;
            }
            return (OrbColor)_rng.Next(6);
        }

        public bool CanMove => !_resolving && !_ended && _clock >= _busyUntil;

        public void BeginMove(BoardCell cell)
        {
            if (!CanMove || !InBounds(cell)) return;
            if (_states[cell.Col, cell.Row].HasFlag(OrbStateFlags.Blocked)) return;
            _dragging = true;
            _dragCell = cell;
            _state.Moving = true;
            _state.MoveTimeRemaining = _state.MoveTimeTotal;
            _states[cell.Col, cell.Row] |= OrbStateFlags.Selected | OrbStateFlags.Dragging;
            BoardChanged?.Invoke();
            StateChanged?.Invoke();
        }

        public void DragTo(BoardCell cell)
        {
            if (!_dragging || !InBounds(cell)) return;
            if (cell.Col == _dragCell.Col && cell.Row == _dragCell.Row) return;
            // Only adjacent steps; the view feeds us cell-by-cell.
            if (Math.Abs(cell.Col - _dragCell.Col) + Math.Abs(cell.Row - _dragCell.Row) != 1) return;
            if (_states[cell.Col, cell.Row].HasFlag(OrbStateFlags.Blocked)) return;
            var tmp = _board[cell.Col, cell.Row];
            _board[cell.Col, cell.Row] = _board[_dragCell.Col, _dragCell.Row];
            _board[_dragCell.Col, _dragCell.Row] = tmp;
            _states[_dragCell.Col, _dragCell.Row] &= ~(OrbStateFlags.Selected | OrbStateFlags.Dragging);
            _states[cell.Col, cell.Row] |= OrbStateFlags.Selected | OrbStateFlags.Dragging;
            _dragCell = cell;
            BoardChanged?.Invoke();
        }

        public void EndMove()
        {
            if (!_dragging) return;
            _dragging = false;
            _state.Moving = false;
            _states[_dragCell.Col, _dragCell.Row] &= ~(OrbStateFlags.Selected | OrbStateFlags.Dragging);
            StateChanged?.Invoke();
            ResolveBoard();
        }

        static bool InBounds(BoardCell cell)
        {
            return cell.Col >= 0 && cell.Col < Cols && cell.Row >= 0 && cell.Row < Rows;
        }

        // --------------------------- Resolution ------------------------------

        void ResolveBoard()
        {
            _resolving = true;
            _state.BoardResolutions++;
            int combo = 0;
            float t = 0.15f;
            bool anyMatch = true;
            var comboColors = new List<OrbColor>();
            int cascadeRound = 0;

            while (anyMatch)
            {
                var groups = FindMatchGroups();
                anyMatch = groups.Count > 0;
                if (!anyMatch) break;
                foreach (var group in groups)
                {
                    combo++;
                    int comboNow = combo;
                    var ev = new MatchEvent
                    {
                        Color = group.Color,
                        Cells = group.Cells.ToList(),
                        MatchIndex = comboNow,
                        ComboAfter = comboNow,
                        FromCascade = cascadeRound > 0
                    };
                    comboColors.Add(group.Color);
                    // NOTE: no BoardChanged here — the model board has already
                    // advanced past the displayed state during synchronous
                    // cascade computation; the view animates ev.Cells directly
                    // and re-reads the board only on snapshot application.
                    Schedule(t, () =>
                    {
                        _state.Combo = comboNow;
                        MatchResolved?.Invoke(ev);
                        ComboChanged?.Invoke(comboNow);
                    });
                    t += 0.22f;
                    // Remove immediately in the model; the view animates from events.
                    foreach (var cell in group.Cells) _board[cell.Col, cell.Row] = (OrbColor)(-1);
                }
                t += 0.1f;
                Collapse();
                var snapshot = SnapshotBoard();
                Schedule(t, () =>
                {
                    ApplySnapshot(snapshot);
                    ClearFlagEverywhere(OrbStateFlags.Matched);
                    BoardChanged?.Invoke();
                });
                t += 0.18f;
                cascadeRound++;
            }

            int finalCombo = combo;
            // Attacks: one attack event per eligible party member per match.
            float attackStart = t + 0.05f;
            t = ScheduleAttacks(comboColors, finalCombo, attackStart);
            Schedule(t + 0.1f, () => FinishResolution(finalCombo));
        }

        class MatchGroup
        {
            public OrbColor Color;
            public HashSet<BoardCell> Cells = new HashSet<BoardCell>();
        }

        List<MatchGroup> FindMatchGroups()
        {
            var matched = new bool[Cols, Rows];
            // Horizontal runs.
            for (int r = 0; r < Rows; r++)
            {
                int runStart = 0;
                for (int c = 1; c <= Cols; c++)
                {
                    bool same = c < Cols && _board[c, r] == _board[runStart, r] && (int)_board[c, r] >= 0
                                && !IsInert(c, r) && !IsInert(runStart, r);
                    if (!same)
                    {
                        if (c - runStart >= 3 && (int)_board[runStart, r] >= 0 && !IsInert(runStart, r))
                        {
                            for (int i = runStart; i < c; i++) matched[i, r] = true;
                        }
                        runStart = c;
                    }
                }
            }
            // Vertical runs.
            for (int c = 0; c < Cols; c++)
            {
                int runStart = 0;
                for (int r = 1; r <= Rows; r++)
                {
                    bool same = r < Rows && _board[c, r] == _board[c, runStart] && (int)_board[c, r] >= 0
                                && !IsInert(c, r) && !IsInert(c, runStart);
                    if (!same)
                    {
                        if (r - runStart >= 3 && (int)_board[c, runStart] >= 0 && !IsInert(c, runStart))
                        {
                            for (int i = runStart; i < r; i++) matched[c, i] = true;
                        }
                        runStart = r;
                    }
                }
            }
            // Flood-fill matched cells into same-color groups.
            var groups = new List<MatchGroup>();
            var visited = new bool[Cols, Rows];
            for (int c = 0; c < Cols; c++)
            {
                for (int r = 0; r < Rows; r++)
                {
                    if (!matched[c, r] || visited[c, r]) continue;
                    var group = new MatchGroup { Color = _board[c, r] };
                    var stack = new Stack<BoardCell>();
                    stack.Push(new BoardCell(c, r));
                    visited[c, r] = true;
                    while (stack.Count > 0)
                    {
                        var cell = stack.Pop();
                        group.Cells.Add(cell);
                        foreach (var n in Neighbors(cell))
                        {
                            if (!visited[n.Col, n.Row] && matched[n.Col, n.Row]
                                && _board[n.Col, n.Row] == group.Color)
                            {
                                visited[n.Col, n.Row] = true;
                                stack.Push(n);
                            }
                        }
                    }
                    groups.Add(group);
                }
            }
            return groups;
        }

        bool IsInert(int c, int r)
        {
            var s = _states[c, r];
            return s.HasFlag(OrbStateFlags.Locked) || s.HasFlag(OrbStateFlags.Blocked);
        }

        static IEnumerable<BoardCell> Neighbors(BoardCell cell)
        {
            if (cell.Col > 0) yield return new BoardCell(cell.Col - 1, cell.Row);
            if (cell.Col < Cols - 1) yield return new BoardCell(cell.Col + 1, cell.Row);
            if (cell.Row > 0) yield return new BoardCell(cell.Col, cell.Row - 1);
            if (cell.Row < Rows - 1) yield return new BoardCell(cell.Col, cell.Row + 1);
        }

        void Collapse()
        {
            for (int c = 0; c < Cols; c++)
            {
                int write = 0;
                for (int r = 0; r < Rows; r++)
                {
                    if ((int)_board[c, r] >= 0)
                    {
                        _board[c, write] = _board[c, r];
                        _states[c, write] = _states[c, r] & ~(OrbStateFlags.Matched | OrbStateFlags.Selected | OrbStateFlags.Dragging);
                        write++;
                    }
                }
                for (int r = write; r < Rows; r++)
                {
                    _board[c, r] = (OrbColor)_rng.Next(6);
                    _states[c, r] = OrbStateFlags.None;
                }
            }
        }

        OrbColor[,] SnapshotBoard()
        {
            return (OrbColor[,])_board.Clone();
        }

        void ApplySnapshot(OrbColor[,] snapshot)
        {
            for (int c = 0; c < Cols; c++)
                for (int r = 0; r < Rows; r++)
                    _board[c, r] = snapshot[c, r];
        }

        void ClearFlagEverywhere(OrbStateFlags flag)
        {
            for (int c = 0; c < Cols; c++)
                for (int r = 0; r < Rows; r++)
                    _states[c, r] &= ~flag;
        }

        float ScheduleAttacks(List<OrbColor> comboColors, int totalCombo, float t)
        {
            float comboMult = 1f + Math.Max(0, totalCombo - 1) * 0.25f;
            var sequence = new List<AttackEvent>();
            long healTotal = 0;
            foreach (var color in comboColors)
            {
                if (color == OrbColor.Heart)
                {
                    healTotal += (long)(_state.Party.Sum(p => (long)p.Character.Rec) * comboMult * 0.8f);
                    continue;
                }
                for (int slot = 0; slot < _state.Party.Count; slot++)
                {
                    var member = _state.Party[slot];
                    if (member.Bound || member.Character.Element.ToOrb() != color) continue;
                    var target = AliveEnemyIndex();
                    if (target < 0) continue;
                    var enemy = _state.Enemies[target];
                    float elemMult = ElementMultiplier(member.Character.Element, enemy.Element);
                    bool crit = _rng.NextDouble() < 0.12;
                    long dmg = (long)(member.Character.Atk * comboMult * elemMult * (crit ? 1.8f : 1f) * (0.92f + _rng.NextDouble() * 0.16f));
                    sequence.Add(new AttackEvent
                    {
                        CharacterId = member.Character.Id,
                        PartySlot = slot,
                        Element = member.Character.Element,
                        Damage = dmg,
                        TargetEnemyIndex = target,
                        Critical = crit,
                        EffectiveHit = elemMult > 1.05f,
                        ResistedHit = elemMult < 0.95f
                    });
                }
            }
            // Repeated attacks must feel fast: tight stagger, tighter as count grows.
            float stagger = sequence.Count > 8 ? 0.06f : 0.09f;
            for (int i = 0; i < sequence.Count; i++)
            {
                var ev = sequence[i];
                ev.SequenceIndex = i;
                ev.SequenceCount = sequence.Count;
                Schedule(t, () => FireAttack(ev));
                t += stagger;
            }
            if (healTotal > 0)
            {
                long heal = healTotal;
                Schedule(t, () =>
                {
                    _state.PartyHp = Math.Min(_state.PartyHpMax, _state.PartyHp + heal);
                    Healed?.Invoke(new HealEvent { Amount = heal, FromHeartMatch = true });
                    StateChanged?.Invoke();
                });
                t += 0.2f;
            }
            // Skill charge from own-element matches.
            foreach (var member in _state.Party)
            {
                var skill = member.Character.ActiveSkill;
                if (skill == null) continue;
                int gain = comboColors.Count(cc => cc == member.Character.Element.ToOrb()) * 3 + totalCombo;
                skill.Charge = Math.Min(skill.ChargeMax, skill.Charge + gain);
            }
            return t;
        }

        void FireAttack(AttackEvent ev)
        {
            if (ev.TargetEnemyIndex >= _state.Enemies.Count) return;
            var enemy = _state.Enemies[ev.TargetEnemyIndex];
            if (enemy.Hp <= 0)
            {
                ev.TargetEnemyIndex = AliveEnemyIndex();
                if (ev.TargetEnemyIndex < 0) return;
                enemy = _state.Enemies[ev.TargetEnemyIndex];
            }
            enemy.Hp = Math.Max(0, enemy.Hp - ev.Damage);
            AttackPerformed?.Invoke(ev);
            StateChanged?.Invoke();
            if (enemy.Hp <= 0)
            {
                EnemyDefeated?.Invoke(ev.TargetEnemyIndex);
            }
        }

        int AliveEnemyIndex()
        {
            for (int i = 0; i < _state.Enemies.Count; i++)
            {
                if (_state.Enemies[i].Hp > 0) return i;
            }
            return -1;
        }

        static float ElementMultiplier(ElementId attacker, ElementId defender)
        {
            // Fire > Nature > Water > Fire; Light <-> Dark bonus.
            if (attacker == ElementId.Fire && defender == ElementId.Nature) return 1.5f;
            if (attacker == ElementId.Nature && defender == ElementId.Water) return 1.5f;
            if (attacker == ElementId.Water && defender == ElementId.Fire) return 1.5f;
            if (attacker == ElementId.Fire && defender == ElementId.Water) return 0.75f;
            if (attacker == ElementId.Water && defender == ElementId.Nature) return 0.75f;
            if (attacker == ElementId.Nature && defender == ElementId.Fire) return 0.75f;
            if (attacker == ElementId.Light && defender == ElementId.Dark) return 1.5f;
            if (attacker == ElementId.Dark && defender == ElementId.Light) return 1.5f;
            return 1f;
        }

        void FinishResolution(int combo)
        {
            _state.Combo = 0;
            if (AliveEnemyIndex() < 0)
            {
                if (_waveIndex + 1 < _state.WaveCount)
                {
                    NextWave();
                    _resolving = false;
                    StateChanged?.Invoke();
                    return;
                }
                EndBattle(BattleOutcome.Victory);
                return;
            }
            // Countdown ticks once per completed resolution.
            float t = 0.2f;
            for (int i = 0; i < _state.Enemies.Count; i++)
            {
                var enemy = _state.Enemies[i];
                if (enemy.Hp <= 0) continue;
                enemy.Countdown--;
                int idx = i;
                Schedule(t, () => EnemyCountdownChanged?.Invoke(idx));
                t += 0.12f;
                if (enemy.Countdown <= 0)
                {
                    var action = NextEnemyAction(idx);
                    Schedule(t, () => PerformEnemyAction(idx, action));
                    t += 0.7f;
                }
            }
            Schedule(t, () =>
            {
                _resolving = false;
                StateChanged?.Invoke();
                if (_state.PartyHp <= 0) EndBattle(BattleOutcome.Defeat);
            });
        }

        readonly Dictionary<int, int> _actionCursor = new Dictionary<int, int>();

        EnemyActionDto NextEnemyAction(int enemyIndex)
        {
            var enemy = _state.Enemies[enemyIndex];
            var dto = _db.Enemies.TryGetValue(enemy.Id, out var e) ? e : null;
            if (dto == null || dto.actions.Count == 0)
            {
                return new EnemyActionDto { name = "Attack", desc = "A basic attack.", type = "damage", amount = 800 };
            }
            int cursor = _actionCursor.TryGetValue(enemyIndex, out var c) ? c : 0;
            _actionCursor[enemyIndex] = (cursor + 1) % dto.actions.Count;
            return dto.actions[cursor % dto.actions.Count];
        }

        void PerformEnemyAction(int enemyIndex, EnemyActionDto action)
        {
            var enemy = _state.Enemies[enemyIndex];
            if (enemy.Hp <= 0) return;
            var ev = new EnemyActionEvent
            {
                EnemyIndex = enemyIndex,
                ActionName = action.name,
                Description = action.desc
            };
            var dto = _db.Enemies.TryGetValue(enemy.Id, out var e) ? e : null;
            long atk = dto != null ? dto.atk : 500;
            switch ((action.type ?? "damage").ToLowerInvariant())
            {
                case "bigdamage":
                    ev.DamageToParty = (long)(atk * 2.2f);
                    break;
                case "convert":
                    ConvertRandomOrbs(ev, action.count > 0 ? action.count : 3, ParseOrb(action.color) ?? OrbColor.Dark, OrbStateFlags.None);
                    break;
                case "lock":
                    ConvertRandomOrbs(ev, action.count > 0 ? action.count : 3, null, OrbStateFlags.Locked);
                    break;
                case "poison":
                    ConvertRandomOrbs(ev, action.count > 0 ? action.count : 2, null, OrbStateFlags.Poisoned);
                    break;
                case "block":
                    ConvertRandomOrbs(ev, action.count > 0 ? action.count : 1, null, OrbStateFlags.Blocked);
                    break;
                case "bind":
                    int slot = _rng.Next(_state.Party.Count);
                    _state.Party[slot].Bound = true;
                    ev.Description = _state.Party[slot].Character.DisplayName + " is bound!";
                    break;
                case "timerdown":
                    _state.MoveTimeTotal = Mathf.Max(4f, _state.MoveTimeTotal - (action.amount > 0 ? action.amount : 2));
                    break;
                case "heal":
                    enemy.Hp = Math.Min(enemy.HpMax, enemy.Hp + (action.amount > 0 ? action.amount : (long)(enemy.HpMax * 0.15f)));
                    break;
                case "absorb":
                case "comboshield":
                case "enrage":
                case "taunt":
                    if (!enemy.ActiveStatuses.Contains(action.name)) enemy.ActiveStatuses.Add(action.name);
                    break;
                default:
                    ev.DamageToParty = atk;
                    break;
            }
            if (ev.DamageToParty > 0)
            {
                _state.PartyHp = Math.Max(0, _state.PartyHp - ev.DamageToParty);
            }
            enemy.Countdown = enemy.CountdownMax;
            enemy.TelegraphText = PeekNextActionName(enemyIndex);
            EnemyActed?.Invoke(ev);
            StateChanged?.Invoke();
        }

        string PeekNextActionName(int enemyIndex)
        {
            var enemy = _state.Enemies[enemyIndex];
            var dto = _db.Enemies.TryGetValue(enemy.Id, out var e) ? e : null;
            if (dto == null || dto.actions.Count == 0) return "";
            int cursor = _actionCursor.TryGetValue(enemyIndex, out var c) ? c : 0;
            return dto.actions[cursor % dto.actions.Count].name;
        }

        static OrbColor? ParseOrb(string value)
        {
            switch ((value ?? "").ToLowerInvariant())
            {
                case "fire": return OrbColor.Fire;
                case "water": return OrbColor.Water;
                case "nature": return OrbColor.Nature;
                case "light": return OrbColor.Light;
                case "dark": return OrbColor.Dark;
                case "heart": return OrbColor.Heart;
                default: return null;
            }
        }

        void ConvertRandomOrbs(EnemyActionEvent ev, int count, OrbColor? convertTo, OrbStateFlags addFlag)
        {
            var candidates = new List<BoardCell>();
            for (int c = 0; c < Cols; c++)
                for (int r = 0; r < Rows; r++)
                    if (_states[c, r] == OrbStateFlags.None) candidates.Add(new BoardCell(c, r));
            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                int pick = _rng.Next(candidates.Count);
                var cell = candidates[pick];
                candidates.RemoveAt(pick);
                if (convertTo.HasValue) _board[cell.Col, cell.Row] = convertTo.Value;
                if (addFlag != OrbStateFlags.None) _states[cell.Col, cell.Row] |= addFlag;
                ev.AffectedCells.Add(cell);
            }
            ev.ConvertTo = convertTo;
            BoardChanged?.Invoke();
        }

        // ------------------------------ Skills -------------------------------

        public void CastSkill(int partySlot)
        {
            if (_resolving || _ended || partySlot < 0 || partySlot >= _state.Party.Count) return;
            var member = _state.Party[partySlot];
            var skill = member.Character.ActiveSkill;
            if (skill == null || !skill.Ready || member.Bound) return;
            skill.Charge = 0;
            var ev = new SkillCastEvent
            {
                CharacterId = member.Character.Id,
                PartySlot = partySlot,
                SkillName = skill.Name,
                Element = member.Character.Element,
                BigCutIn = member.Character.CurrentRarity >= 5
            };
            SkillCast?.Invoke(ev);
            string desc = (skill.Description ?? "").ToLowerInvariant();
            if (desc.Contains("heal") || desc.Contains("recover"))
            {
                long heal = (long)(member.Character.Rec * 6f);
                _state.PartyHp = Math.Min(_state.PartyHpMax, _state.PartyHp + heal);
                Healed?.Invoke(new HealEvent { Amount = heal });
            }
            else if (desc.Contains("delay"))
            {
                foreach (var enemy in _state.Enemies.Where(en => en.Hp > 0))
                {
                    enemy.Countdown += 2;
                }
            }
            else if (desc.Contains("convert") || desc.Contains("change"))
            {
                var evc = new EnemyActionEvent();
                ConvertRandomOrbs(evc, 6, member.Character.Element.ToOrb(), OrbStateFlags.None);
            }
            else if (desc.Contains("unlock") || desc.Contains("cleanse"))
            {
                ClearFlagEverywhere(OrbStateFlags.Locked);
                ClearFlagEverywhere(OrbStateFlags.Poisoned);
                BoardChanged?.Invoke();
            }
            else
            {
                int target = AliveEnemyIndex();
                if (target >= 0)
                {
                    FireAttack(new AttackEvent
                    {
                        CharacterId = member.Character.Id,
                        PartySlot = partySlot,
                        Element = member.Character.Element,
                        Damage = (long)(member.Character.Atk * 5f),
                        TargetEnemyIndex = target,
                        SequenceCount = 1
                    });
                }
            }
            StateChanged?.Invoke();
        }

        // ------------------------------ Waves --------------------------------

        void NextWave()
        {
            _waveIndex++;
            _state.WaveIndex = _waveIndex;
            _state.Enemies.Clear();
            _actionCursor.Clear();
            if (_stage == null || _stage.waves.Count == 0)
            {
                _state.Enemies.Add(new EnemyView
                {
                    Id = "demo_dummy",
                    DisplayName = "Training Dummy",
                    Element = ElementId.Nature,
                    Hp = 60000,
                    HpMax = 60000,
                    Countdown = 3,
                    CountdownMax = 3
                });
                return;
            }
            var wave = _stage.waves[Mathf.Clamp(_waveIndex, 0, _stage.waves.Count - 1)];
            foreach (var we in wave.enemies)
            {
                if (!_db.Enemies.TryGetValue(we.enemyId, out var dto)) continue;
                long hp = (long)(dto.hp * we.hpMult);
                var enemy = new EnemyView
                {
                    Id = dto.id,
                    DisplayName = dto.name,
                    Element = ContentDb.ParseElement(dto.element),
                    Hp = hp,
                    HpMax = hp,
                    Countdown = we.countdownOverride > 0 ? we.countdownOverride : dto.countdown,
                    CountdownMax = we.countdownOverride > 0 ? we.countdownOverride : dto.countdown,
                    IsBoss = dto.boss,
                    ArtRef = string.IsNullOrEmpty(dto.artRef) ? dto.id : dto.artRef
                };
                if (dto.actions.Count > 0) enemy.TelegraphText = dto.actions[0].name;
                _state.Enemies.Add(enemy);
            }
        }

        void EndBattle(BattleOutcome outcome)
        {
            if (_ended) return;
            _ended = true;
            int stars = 0;
            var lines = new List<string>();
            if (outcome == BattleOutcome.Victory)
            {
                stars = 1;
                float hpFrac = _state.PartyHpMax > 0 ? _state.PartyHp / (float)_state.PartyHpMax : 0f;
                if (_stage == null || hpFrac >= _stage.hpThresholdStar) stars++;
                if (_stage == null || _state.BoardResolutions <= _stage.resolutionsStar) stars++;
                if (_stage != null)
                {
                    if (_stage.rewards.gold > 0) lines.Add("◆ " + _stage.rewards.gold + " Gold");
                    if (_stage.rewards.gems > 0) lines.Add("❖ " + _stage.rewards.gems + " Gems");
                    foreach (var item in _stage.rewards.items)
                    {
                        // Player-facing item names/icons come from the registry.
                        lines.Add(_db.Items.TryGetValue(item.id, out var def)
                            ? def.icon + " " + item.count + "× " + def.name
                            : item.count + "× " + item.id);
                    }
                    if (!string.IsNullOrEmpty(_stage.rewards.firstClearBonus))
                    {
                        lines.Add("First clear: " + _stage.rewards.firstClearBonus);
                    }
                }
            }
            Schedule(0.6f, () => BattleEnded?.Invoke(new BattleEndEvent
            {
                Outcome = outcome,
                StarsEarned = stars,
                RewardLines = lines
            }));
        }

        // ------------------------------ Pump ---------------------------------

        void Schedule(float delay, Action fire)
        {
            _timeline.Add(new TimedEvent { At = _clock + delay, Fire = fire });
            _busyUntil = Math.Max(_busyUntil, _clock + delay);
        }

        public void Update(float deltaTime)
        {
            _clock += deltaTime;
            if (_dragging)
            {
                _state.MoveTimeRemaining -= deltaTime;
                if (_state.MoveTimeRemaining <= 0f)
                {
                    _state.MoveTimeRemaining = 0f;
                    EndMove();
                }
                StateChanged?.Invoke();
            }
            for (int i = 0; i < _timeline.Count; i++)
            {
                if (_timeline[i].At <= _clock)
                {
                    var ev = _timeline[i];
                    _timeline.RemoveAt(i);
                    i--;
                    ev.Fire();
                }
            }
        }
    }
}
