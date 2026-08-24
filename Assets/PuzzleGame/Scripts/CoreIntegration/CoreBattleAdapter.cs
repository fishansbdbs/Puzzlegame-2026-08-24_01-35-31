using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Stages;
using PuzzleGame.Presentation.Content;
using CoreAttackEvent = PuzzleGame.Core.Battle.AttackEvent;

namespace PuzzleGame.Presentation.CoreIntegration
{
    /// <summary>
    /// The broader presentation battle controller the core documentation
    /// anticipated: drives multi-wave, multi-enemy stage sessions entirely
    /// through core systems (DragSession, BoardResolver, BattleEngine,
    /// StageSession, SkillEngine) and republishes the already-committed
    /// results as the paced presentation event stream the battle HUD
    /// consumes (IBattleEventSource).
    ///
    /// The core transaction always completes synchronously first; the
    /// timeline below only replays it for animation. Nothing here rolls,
    /// recalculates, or mutates combat outcomes.
    ///
    /// Multi-enemy note: combat targets the first living enemy (core's
    /// BattleContext is single-enemy); the other enemies in the wave advance
    /// through the same EnemyActionEngine each resolution, and their
    /// board/context effects are mirrored onto the primary context using the
    /// exact payloads core validated.
    /// </summary>
    public sealed class CoreBattleAdapter : IBattleEventSource
    {
        const float MatchStep = 0.22f;
        const float AttackStep = 0.08f;

        readonly GameServices services;
        readonly StageDto stageDto;
        readonly StageData coreStage;
        readonly StageSession session;
        readonly PartyState party;
        readonly List<CharacterRuntime> runtimes;
        readonly List<CharacterProgressRef> partyRefs = new List<CharacterProgressRef>();
        readonly BattleEngine engine = new BattleEngine();
        readonly BoardResolver resolver = new BoardResolver();
        readonly SkillEngine skillEngine = new SkillEngine();
        readonly EnemyActionEngine enemyActionEngine = new EnemyActionEngine();
        readonly FilteredOrbSource orbSource;
        readonly List<string> modifiers;

        BattleContext context;
        List<EnemyRuntime> waveEnemies = new List<EnemyRuntime>();
        int targetIndex;
        DragSession drag;
        float dragElapsed;
        bool resolving;
        bool ended;

        readonly BattleHudState hudState = new BattleHudState();
        readonly OrbColor[,] displayBoard = new OrbColor[6, 5];
        readonly OrbStateFlags[,] displayFlags = new OrbStateFlags[6, 5];
        BoardCell heldCell = new BoardCell(-1, -1);
        readonly List<TimedEvent> timeline = new List<TimedEvent>();
        float clock;

        sealed class TimedEvent { public float At; public Action Fire; }
        sealed class CharacterProgressRef { public string Id; public CharacterDto Dto; }

        public CoreBattleAdapter(GameServices services, StageDto stageDto)
        {
            this.services = services;
            this.stageDto = stageDto;
            coreStage = services.Content.Stages[stageDto.id];
            session = services.StageCatalog.CreateSession(stageDto.id);
            modifiers = stageDto.modifiers ?? new List<string>();
            orbSource = new FilteredOrbSource(new Random(unchecked(stageDto.id.GetHashCode() * 397) ^ Environment.TickCount),
                excludeHeart: modifiers.Contains("no_heart_orbs"));

            // Party from the real roster/progression state.
            var progressList = services.ResolveParty();
            runtimes = new List<CharacterRuntime>();
            foreach (var progress in progressList)
            {
                runtimes.Add(CharacterBattleFactory.Create(
                    progress,
                    services.Content.GetActiveSkill(progress.CharacterId),
                    services.Content.GetLeaderSkill(progress.CharacterId),
                    services.Content.GetPassive(progress.CharacterId)));
                partyRefs.Add(new CharacterProgressRef
                {
                    Id = progress.CharacterId,
                    Dto = services.Db.Characters[progress.CharacterId]
                });
            }
            party = new PartyState(runtimes);

            hudState.StageName = stageDto.name;
            hudState.WaveCount = session.AuthoredStructure.WaveCount;
            hudState.PartyHpMax = party.MaxHp;
            hudState.PartyHp = party.CurrentHp;
            for (var slot = 0; slot < runtimes.Count; slot++)
            {
                var view = services.Db.ToView(partyRefs[slot].Dto,
                    progressList[slot].Level, progressList[slot].Ascension, progressList[slot].IsAwakened);
                var stats = progressList[slot].CurrentStats;
                view.Hp = stats.Hp; view.Atk = stats.Attack; view.Rec = stats.Recovery;
                if (view.ActiveSkill != null)
                {
                    view.ActiveSkill.ChargeMax = runtimes[slot].ActiveSkill != null
                        ? runtimes[slot].ActiveSkill.ChargeRequired : 0;
                }
                hudState.Party.Add(new PartyMemberView { Character = view, IsLeader = slot == 0 });
            }

            var openingBoard = new BoardGenerator(new SystemRandomSource(Environment.TickCount ^ stageDto.id.GetHashCode())).Generate();
            LoadWave(openingBoard);
            SyncDisplayBoard(context.Board);
            RefreshHudFromCore();
        }

        // ------------------------- IBattleEventSource -------------------------

        public BattleHudState State { get { return hudState; } }
        public OrbColor[,] Board { get { return displayBoard; } }
        public OrbStateFlags[,] BoardStates { get { return displayFlags; } }

        public event Action BoardChanged;
        public event Action<MatchEvent> MatchResolved;
        public event Action<Presentation.AttackEvent> AttackPerformed;
        public event Action<Presentation.HealEvent> Healed;
        public event Action<int> ComboChanged;
        public event Action<EnemyActionEvent> EnemyActed;
        public event Action<int> EnemyCountdownChanged;
        public event Action<int> EnemyDefeated;
        public event Action<SkillCastEvent> SkillCast;
        public event Action StateChanged;
        public event Action<BattleEndEvent> BattleEnded;

        public bool CanMove { get { return !resolving && !ended && drag == null; } }

        public void BeginMove(BoardCell cell)
        {
            if (!CanMove || !InBounds(cell)) return;
            try
            {
                drag = new DragSession(context.Board, new BoardPosition(cell.Col, cell.Row), context.MoveTimeSeconds);
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }
            dragElapsed = 0f;
            heldCell = cell;
            hudState.Moving = true;
            hudState.MoveTimeTotal = context.MoveTimeSeconds;
            hudState.MoveTimeRemaining = context.MoveTimeSeconds;
            SetHeldFlag(cell);
            RaiseBoardChanged();
            RaiseStateChanged();
        }

        public void DragTo(BoardCell cell)
        {
            if (drag == null || !InBounds(cell)) return;
            if (!drag.TryMove(new BoardPosition(cell.Col, cell.Row), dragElapsed)) return;
            heldCell = cell;
            SyncDisplayBoard(context.Board);
            SetHeldFlag(cell);
            RaiseBoardChanged();
        }

        public void EndMove()
        {
            if (drag == null || resolving || ended) return;
            drag.End();
            drag = null;
            hudState.Moving = false;
            ClearHeldFlag();
            ResolveTurn();
        }

        public void CastSkill(int partySlot)
        {
            if (resolving || ended || partySlot < 0 || partySlot >= runtimes.Count) return;
            var runtime = runtimes[partySlot];
            if (!skillEngine.CanActivate(runtime)) return;
            var resolution = skillEngine.Activate(runtime, context, orbSource);
            if (!resolution.Succeeded) return;

            SkillCast?.Invoke(new SkillCastEvent
            {
                CharacterId = partyRefs[partySlot].Id,
                PartySlot = partySlot,
                SkillName = runtime.ActiveSkill != null ? runtime.ActiveSkill.DisplayName : "Skill",
                Element = (ElementId)runtimes[partySlot].Data.Element,
                BigCutIn = hudState.Party[partySlot].Character.CurrentRarity >= 5
            });
            foreach (var effect in resolution.Events)
            {
                if (effect.Kind == BattleEffectKind.Heal && effect.Amount > 0)
                {
                    Healed?.Invoke(new Presentation.HealEvent { Amount = effect.Amount });
                }
                else if (effect.Kind == BattleEffectKind.DirectDamage && effect.Amount > 0)
                {
                    AttackPerformed?.Invoke(new Presentation.AttackEvent
                    {
                        CharacterId = partyRefs[partySlot].Id,
                        PartySlot = partySlot,
                        Element = (ElementId)runtimes[partySlot].Data.Element,
                        Damage = effect.Amount,
                        TargetEnemyIndex = targetIndex,
                        SequenceCount = 1
                    });
                }
            }
            foreach (var mechanic in resolution.Mechanics) AnnounceMechanic(targetIndex, mechanic);
            SyncDisplayBoard(context.Board);
            RaiseBoardChanged();
            RefreshHudFromCore();
            RaiseStateChanged();
            CheckEnemyDefeatAfterSkill();
        }

        public void Update(float deltaTime)
        {
            clock += deltaTime;
            if (drag != null)
            {
                dragElapsed += deltaTime;
                hudState.MoveTimeRemaining = Math.Max(0f, context.MoveTimeSeconds - dragElapsed);
                if (dragElapsed >= context.MoveTimeSeconds) EndMove();
                RaiseStateChanged();
            }
            for (var index = 0; index < timeline.Count; index++)
            {
                if (timeline[index].At > clock) continue;
                var timedEvent = timeline[index];
                timeline.RemoveAt(index);
                index--;
                timedEvent.Fire();
            }
        }

        // ------------------------------ resolution -----------------------------

        void ResolveTurn()
        {
            resolving = true;
            // 1. The entire core transaction commits synchronously.
            var resolution = resolver.Resolve(context.Board, orbSource);
            var enemyHpBefore = waveEnemies.Select(enemy => enemy.CurrentHp).ToList();
            var partyHpBefore = party.CurrentHp;
            var turn = session.CompleteBoardResolution(engine, resolution, context);

            // Secondary living enemies advance through the same core engine.
            var secondaryTurns = new List<KeyValuePair<int, EnemyTurnResolution>>();
            for (var index = 0; index < waveEnemies.Count; index++)
            {
                if (index == targetIndex || waveEnemies[index].IsDefeated) continue;
                var secondaryContext = new BattleContext(context.Board, party, waveEnemies[index], context.MoveTimeSeconds);
                var secondaryTurn = enemyActionEngine.AdvanceAfterBoardResolution(waveEnemies[index], secondaryContext);
                MirrorContextEffects(secondaryTurn);
                secondaryTurns.Add(new KeyValuePair<int, EnemyTurnResolution>(index, secondaryTurn));
            }
            hudState.BoardResolutions = session.BoardResolutionCount;

            // 2. Replay the committed result as paced presentation events.
            float at = 0.05f;
            int comboRunning = 0;
            for (var layerIndex = 0; layerIndex < resolution.CascadeLayers.Count; layerIndex++)
            {
                var layer = resolution.CascadeLayers[layerIndex];
                var preClear = layer.PreClearBoard;
                Schedule(at, () => { SyncDisplayBoard(preClear); RaiseBoardChanged(); });
                foreach (var group in layer.Groups)
                {
                    comboRunning++;
                    var combo = comboRunning;
                    var matchEvent = new MatchEvent
                    {
                        Color = (OrbColor)group.OrbType,
                        MatchIndex = combo,
                        ComboAfter = combo,
                        FromCascade = layerIndex > 0,
                        Cells = group.Cells.Select(cell => new BoardCell(cell.X, cell.Y)).ToList()
                    };
                    Schedule(at, () =>
                    {
                        hudState.Combo = combo;
                        MatchResolved?.Invoke(matchEvent);
                        ComboChanged?.Invoke(combo);
                    });
                    at += MatchStep;
                }
                var postRefill = layer.PostRefillBoard;
                at += 0.1f;
                Schedule(at, () => { SyncDisplayBoard(postRefill); RaiseBoardChanged(); });
                at += 0.16f;
            }

            // Attacks and heals: real core events, staggered for readability.
            var displayEnemyHp = new List<long>(enemyHpBefore.Select(hp => (long)hp));
            float stagger = turn.Combat.Attacks.Count > 8 ? 0.06f : AttackStep;
            for (var index = 0; index < turn.Combat.Attacks.Count; index++)
            {
                var attack = turn.Combat.Attacks[index];
                var sequenceIndex = index;
                var sequenceCount = turn.Combat.Attacks.Count;
                Schedule(at, () => FireAttackEvent(attack, sequenceIndex, sequenceCount, displayEnemyHp));
                at += stagger;
            }
            foreach (var heal in turn.Combat.Heals)
            {
                var healed = heal.Healing;
                if (healed <= 0) continue;
                Schedule(at, () =>
                {
                    hudState.PartyHp = Math.Min(hudState.PartyHpMax, hudState.PartyHp + healed);
                    Healed?.Invoke(new Presentation.HealEvent { Amount = healed, FromHeartMatch = true });
                    RaiseStateChanged();
                });
                at += 0.2f;
            }

            // Boss mechanics from HP thresholds.
            foreach (var mechanic in turn.Mechanics)
            {
                var snapshot = mechanic;
                Schedule(at, () => AnnounceMechanic(targetIndex, snapshot));
                at += 0.3f;
            }

            // Enemy countdowns and actions (primary, then secondaries).
            at += 0.2f;
            at = ScheduleEnemyTurn(targetIndex, turn.EnemyTurn, at, partyHpBefore);
            foreach (var pair in secondaryTurns)
            {
                at = ScheduleEnemyTurn(pair.Key, pair.Value, at, partyHpBefore);
            }

            Schedule(at + 0.1f, FinishResolution);
        }

        void FireAttackEvent(CoreAttackEvent attack, int sequenceIndex, int sequenceCount, List<long> displayEnemyHp)
        {
            displayEnemyHp[targetIndex] = Math.Max(0, displayEnemyHp[targetIndex] - attack.Damage);
            if (targetIndex < hudState.Enemies.Count)
            {
                hudState.Enemies[targetIndex].Hp = displayEnemyHp[targetIndex];
            }
            var defender = waveEnemies[targetIndex].Data.Element;
            var multiplier = CombatCalculator.GetAffinityMultiplier(attack.Element, defender);
            AttackPerformed?.Invoke(new Presentation.AttackEvent
            {
                CharacterId = attack.CharacterId,
                PartySlot = attack.CharacterSlot,
                Element = (ElementId)attack.Element,
                Damage = attack.Damage,
                TargetEnemyIndex = targetIndex,
                EffectiveHit = multiplier > 1.05f,
                ResistedHit = multiplier < 0.95f || attack.WasBlocked || attack.WasAbsorbed,
                SequenceIndex = sequenceIndex,
                SequenceCount = sequenceCount
            });
            RaiseStateChanged();
            if (displayEnemyHp[targetIndex] <= 0 && waveEnemies[targetIndex].IsDefeated)
            {
                EnemyDefeated?.Invoke(targetIndex);
            }
        }

        float ScheduleEnemyTurn(int enemyIndex, EnemyTurnResolution turn, float at, int partyHpBefore)
        {
            var enemy = waveEnemies[enemyIndex];
            Schedule(at, () =>
            {
                if (enemyIndex < hudState.Enemies.Count) hudState.Enemies[enemyIndex].Countdown = enemy.Countdown;
                EnemyCountdownChanged?.Invoke(enemyIndex);
                RaiseStateChanged();
            });
            at += 0.12f;
            foreach (var action in turn.ExecutedActions)
            {
                long damageToParty = 0;
                foreach (var effectEvent in turn.Events)
                {
                    if (effectEvent.Kind == BattleEffectKind.Damage) damageToParty += effectEvent.Amount;
                }
                var actionSnapshot = action;
                Schedule(at, () =>
                {
                    var presentationEvent = new EnemyActionEvent
                    {
                        EnemyIndex = enemyIndex,
                        ActionName = actionSnapshot.Id,
                        Description = DescribeAction(enemyIndex, actionSnapshot.Id),
                        DamageToParty = damageToParty
                    };
                    hudState.PartyHp = party.CurrentHp;
                    SyncDisplayBoard(context.Board);
                    RaiseBoardChanged();
                    EnemyActed?.Invoke(presentationEvent);
                    RaiseStateChanged();
                });
                at += 0.7f;
            }
            return at;
        }

        void AnnounceMechanic(int enemyIndex, EnemyEffectSnapshot mechanic)
        {
            var enemyView = enemyIndex < hudState.Enemies.Count ? hudState.Enemies[enemyIndex] : null;
            var label = mechanic.Type.ToString();
            if (enemyView != null && !enemyView.ActiveStatuses.Contains(label)) enemyView.ActiveStatuses.Add(label);
            EnemyActed?.Invoke(new EnemyActionEvent
            {
                EnemyIndex = enemyIndex,
                ActionName = label,
                Description = "The enemy's state shifts!",
                IsTelegraphOnly = true
            });
        }

        void FinishResolution()
        {
            if (party.CurrentHp == 0)
            {
                EndBattle(BattleOutcome.Defeat);
                return;
            }
            bool waveDown = waveEnemies.All(enemy => enemy.IsDefeated);
            if (waveDown)
            {
                session.TryAdvanceWave();
                if (session.IsCompleted)
                {
                    EndBattle(BattleOutcome.Victory);
                    return;
                }
                LoadWave(context.Board);
            }
            resolving = false;
            hudState.Combo = 0;
            RefreshHudFromCore();
            RaiseStateChanged();
        }

        void CheckEnemyDefeatAfterSkill()
        {
            if (waveEnemies[targetIndex].IsDefeated)
            {
                EnemyDefeated?.Invoke(targetIndex);
                RetargetOrAdvance();
            }
        }

        void RetargetOrAdvance()
        {
            var next = NextAliveEnemy();
            if (next >= 0)
            {
                targetIndex = next;
                RebuildPrimaryContext();
                RefreshHudFromCore();
                RaiseStateChanged();
                return;
            }
            session.TryAdvanceWave();
            if (session.IsCompleted)
            {
                EndBattle(BattleOutcome.Victory);
                return;
            }
            LoadWave(context.Board);
            RefreshHudFromCore();
            RaiseStateChanged();
        }

        int NextAliveEnemy()
        {
            for (var index = 0; index < waveEnemies.Count; index++)
                if (!waveEnemies[index].IsDefeated) return index;
            return -1;
        }

        void EndBattle(BattleOutcome outcome)
        {
            if (ended) return;
            ended = true;
            resolving = false;
            var lines = new List<string>();
            var stars = 0;
            if (outcome == BattleOutcome.Victory)
            {
                var result = new StageResult(true, party.HpPercent, session.BoardResolutionCount);
                var starResults = StageObjectiveEvaluator.Evaluate(coreStage, result);
                stars = starResults.Count(star => star.Earned);
                var progressBefore = services.GetStageProgress(stageDto.id);
                bool firstClear = progressBefore == null || !progressBefore.IsCleared;
                services.RecordStageCompletion(stageDto.id, starResults, party.HpPercent, session.BoardResolutionCount);
                foreach (var reward in coreStage.ClearRewards)
                {
                    if (!firstClear && reward.Type != RewardType.Gold) continue;
                    lines.Add(DescribeReward(reward));
                }
                if (firstClear && !string.IsNullOrEmpty(stageDto.rewards != null ? stageDto.rewards.firstClearBonus : null))
                {
                    lines.Add("First clear: " + stageDto.rewards.firstClearBonus);
                }
            }
            var endEvent = new BattleEndEvent { Outcome = outcome, StarsEarned = stars, RewardLines = lines };
            Schedule(0.6f, () => BattleEnded?.Invoke(endEvent));
        }

        string DescribeReward(RewardData reward)
        {
            switch (reward.Type)
            {
                case RewardType.Gold: return "◆ " + reward.Amount + " Gold";
                case RewardType.Gems: return "❖ " + reward.Amount + " Gems";
                default:
                    ItemDto item;
                    var name = services.Db.Items.TryGetValue(reward.ItemId ?? "", out item) ? item.icon + " " + item.name : reward.ItemId;
                    return reward.Amount + "× " + name;
            }
        }

        // ------------------------------ wave setup -----------------------------

        void LoadWave(BoardState board)
        {
            waveEnemies = session.CurrentEnemies.ToList();
            targetIndex = 0;
            hudState.WaveIndex = session.CurrentWaveIndex;
            hudState.Enemies.Clear();
            for (var index = 0; index < waveEnemies.Count; index++)
            {
                var runtime = waveEnemies[index];
                var baseId = services.Content.EnemyBaseIds.TryGetValue(runtime.Data.Id, out var mapped) ? mapped : runtime.Data.Id;
                EnemyDto dto;
                services.Db.Enemies.TryGetValue(baseId, out dto);
                hudState.Enemies.Add(new EnemyView
                {
                    Id = baseId,
                    DisplayName = runtime.Data.DisplayName,
                    Element = (ElementId)runtime.Data.Element,
                    Hp = runtime.CurrentHp,
                    HpMax = runtime.MaxHp,
                    Countdown = runtime.Countdown,
                    CountdownMax = runtime.Data.InitialCountdown,
                    IsBoss = dto != null && dto.boss,
                    ArtRef = dto != null && !string.IsNullOrEmpty(dto.artRef) ? dto.artRef : baseId,
                    TelegraphText = NextActionName(runtime)
                });
            }
            var isFirstWave = context == null;
            context = new BattleContext(board, party, waveEnemies[targetIndex]);
            ApplyStageModifiers(isFirstWave);
            SyncDisplayBoard(context.Board);
        }

        /// <summary>
        /// Mirrors board/context effects produced by a secondary enemy's turn
        /// onto the primary context, using the exact snapshots core emitted.
        /// Party damage, binds and the enemy's own buffs were already applied
        /// by the engine to the shared party/enemy objects.
        /// </summary>
        void MirrorContextEffects(EnemyTurnResolution turn)
        {
            foreach (var action in turn.ExecutedActions)
            {
                foreach (var effect in action.Effects)
                {
                    switch (effect.Type)
                    {
                        case EnemyEffectType.LockOrbs:
                            context.BoardEffects.Lock(effect.SourceOrb, Math.Max(1, effect.TurnCount));
                            break;
                        case EnemyEffectType.Poison:
                            context.BoardEffects.AddPoison(effect.Amount, Math.Max(1, effect.TurnCount));
                            break;
                        case EnemyEffectType.Hazard:
                            context.BoardEffects.AddHazard(effect.Amount, Math.Max(1, effect.TurnCount));
                            break;
                        case EnemyEffectType.Blocker:
                            context.BoardEffects.AddBlocker(effect.Amount, Math.Max(1, effect.TurnCount));
                            break;
                        case EnemyEffectType.ReduceMoveTime:
                            var seconds = effect.DurationSeconds > 0f ? effect.DurationSeconds : effect.Amount;
                            context.ModifyMoveTime(-seconds, Math.Max(1, effect.TurnCount));
                            break;
                    }
                }
            }
        }

        void RebuildPrimaryContext()
        {
            context = new BattleContext(context.Board, party, waveEnemies[targetIndex]);
        }

        string NextActionName(EnemyRuntime runtime)
        {
            var actions = runtime.Data.Actions;
            if (actions == null || actions.Length == 0) return "";
            return actions[runtime.CurrentActionIndex % actions.Length].Id;
        }

        string DescribeAction(int enemyIndex, string actionName)
        {
            var enemyView = enemyIndex < hudState.Enemies.Count ? hudState.Enemies[enemyIndex] : null;
            if (enemyView == null) return "";
            EnemyDto dto;
            if (!services.Db.Enemies.TryGetValue(enemyView.Id, out dto)) return "";
            var action = dto.actions.FirstOrDefault(a => a.name == actionName);
            return action != null ? action.desc : "";
        }

        /// <summary>
        /// Applies authored stage modifiers through core state APIs (locks,
        /// poison, blockers, move-time, combo shields, haste). The modifier
        /// vocabulary is data; the mechanics are core's.
        /// </summary>
        void ApplyStageModifiers(bool firstWave)
        {
            foreach (var modifier in modifiers)
            {
                var parts = modifier.Split(':');
                var amount = parts.Length > 1 && int.TryParse(parts[1], out var parsed) ? parsed : 1;
                switch (parts[0])
                {
                    case "start_locks":
                        if (!firstWave) break;
                        for (var index = 0; index < Math.Min(amount, 5); index++)
                            context.BoardEffects.Lock((OrbType)index, 2);
                        break;
                    case "start_poison":
                        if (firstWave) context.BoardEffects.AddPoison(amount, 3);
                        break;
                    case "start_blockers":
                        if (firstWave) context.BoardEffects.AddBlocker(amount, 3);
                        break;
                    case "move_time_minus":
                        context.ModifyMoveTime(-amount, 999);
                        break;
                    case "combo_shield":
                        // Per-wave rule: every enemy in the stage carries it.
                        foreach (var enemy in waveEnemies) enemy.SetComboShield(amount, 999);
                        break;
                    case "enemy_haste":
                        foreach (var enemy in waveEnemies) enemy.ModifyCountdown(-1);
                        break;
                }
            }
        }

        // ------------------------------- display -------------------------------

        void SyncDisplayBoard(BoardState board)
        {
            for (var x = 0; x < 6; x++)
                for (var y = 0; y < 5; y++)
                    displayBoard[x, y] = (OrbColor)board.Get(x, y);
            SyncDisplayFlags();
        }

        void SyncDisplayBoard(BoardSnapshot board)
        {
            for (var x = 0; x < 6; x++)
                for (var y = 0; y < 5; y++)
                    displayBoard[x, y] = (OrbColor)board.Get(x, y);
            SyncDisplayFlags();
        }

        void SyncDisplayFlags()
        {
            var effects = context != null ? context.BoardEffects : null;
            int poisonLeft = effects != null ? effects.PoisonCount : 0;
            int blockerLeft = effects != null ? effects.BlockerCount : 0;
            for (var x = 0; x < 6; x++)
            {
                for (var y = 0; y < 5; y++)
                {
                    var flags = OrbStateFlags.None;
                    if (effects != null && effects.IsLocked((OrbType)(int)displayBoard[x, y])) flags |= OrbStateFlags.Locked;
                    // Poison/blocker are count-based core state; surface them
                    // on deterministic cells so the player sees the pressure.
                    if (poisonLeft > 0 && ((x * 5 + y) % 7) == 3) { flags |= OrbStateFlags.Poisoned; poisonLeft--; }
                    if (blockerLeft > 0 && ((x * 5 + y) % 11) == 5) { flags |= OrbStateFlags.Blocked; blockerLeft--; }
                    displayFlags[x, y] = flags;
                }
            }
        }

        void SetHeldFlag(BoardCell cell)
        {
            ClearHeldFlag();
            if (InBounds(cell)) displayFlags[cell.Col, cell.Row] |= OrbStateFlags.Selected | OrbStateFlags.Dragging;
        }

        void ClearHeldFlag()
        {
            for (var x = 0; x < 6; x++)
                for (var y = 0; y < 5; y++)
                    displayFlags[x, y] &= ~(OrbStateFlags.Selected | OrbStateFlags.Dragging);
        }

        void RefreshHudFromCore()
        {
            hudState.PartyHp = party.CurrentHp;
            hudState.PartyHpMax = party.MaxHp;
            hudState.MoveTimeTotal = context.MoveTimeSeconds;
            hudState.BoardResolutions = session.BoardResolutionCount;
            for (var index = 0; index < waveEnemies.Count && index < hudState.Enemies.Count; index++)
            {
                var runtime = waveEnemies[index];
                var view = hudState.Enemies[index];
                view.Hp = runtime.CurrentHp;
                view.Countdown = runtime.Countdown;
                view.TelegraphText = runtime.IsDefeated ? "" : NextActionName(runtime);
                view.ActiveStatuses.Clear();
                if (runtime.AbsorbedElement.HasValue) view.ActiveStatuses.Add("Absorbs " + runtime.AbsorbedElement.Value);
                if (runtime.ComboShieldMinimum > 0) view.ActiveStatuses.Add("Combo Shield " + runtime.ComboShieldMinimum);
                if (runtime.EnrageTurns > 0) view.ActiveStatuses.Add("Enraged");
            }
            for (var slot = 0; slot < runtimes.Count && slot < hudState.Party.Count; slot++)
            {
                var member = hudState.Party[slot];
                member.Bound = runtimes[slot].IsBound;
                if (member.Character.ActiveSkill != null && runtimes[slot].ActiveSkill != null)
                {
                    member.Character.ActiveSkill.Charge = runtimes[slot].CurrentCharge;
                    member.Character.ActiveSkill.ChargeMax = runtimes[slot].ActiveSkill.ChargeRequired;
                }
            }
        }

        // ------------------------------- plumbing -------------------------------

        void Schedule(float delay, Action fire)
        {
            timeline.Add(new TimedEvent { At = clock + delay, Fire = fire });
        }

        void RaiseBoardChanged() { BoardChanged?.Invoke(); }
        void RaiseStateChanged() { StateChanged?.Invoke(); }

        static bool InBounds(BoardCell cell)
        {
            return cell.Col >= 0 && cell.Col < 6 && cell.Row >= 0 && cell.Row < 5;
        }

        /// <summary>Random refill source; optionally excludes Heart per stage rules.</summary>
        sealed class FilteredOrbSource : IOrbSource
        {
            readonly Random random;
            readonly bool excludeHeart;
            public FilteredOrbSource(Random random, bool excludeHeart)
            {
                this.random = random;
                this.excludeHeart = excludeHeart;
            }
            public OrbType NextOrb()
            {
                return (OrbType)random.Next(excludeHeart ? 5 : 6);
            }
        }
    }
}
