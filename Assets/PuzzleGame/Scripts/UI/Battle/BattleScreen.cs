using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using PuzzleGame.Presentation.Content;
using PuzzleGame.Presentation.UI.Story;
using PuzzleGame.Presentation.VFX;

namespace PuzzleGame.Presentation.UI.Battle
{
    /// <summary>
    /// The full battle HUD. Landscape layout: battlefield with enemies on
    /// the left, board + party on the right. Shows everything the spec
    /// requires: enemy/boss + HP + countdown + statuses, 6x5 board, move
    /// timer, combo count, party HP, five portraits with skill charge and
    /// element info. All juice flows through UiFx and honors reduced motion.
    /// </summary>
    public class BattleScreen : UiScreen
    {
        readonly IBattleEventSource _source;
        readonly Action<float> _pump;      // demo driver tick; null when core drives itself
        readonly ChapterDto _chapter;
        readonly StageDto _stage;

        VisualElement _fxLayer;
        VisualElement _battlefield;
        VisualElement _enemyRow;
        VisualElement _partyRow;
        VisualElement _partyHpBar;
        Label _partyHpLabel;
        Label _comboLabel;
        Label _waveLabel;
        Label _resolutionsLabel;
        VisualElement _timerBar;
        Label _timerLabel;
        OrbBoardView _board;
        readonly List<EnemyWidget> _enemyWidgets = new List<EnemyWidget>();
        readonly List<PartyWidget> _partyWidgets = new List<PartyWidget>();
        bool _ended;

        class EnemyWidget
        {
            public VisualElement Root;
            public VisualElement Portrait;
            public VisualElement HpBar;
            public Label HpLabel;
            public Label CountdownLabel;
            public Label StatusLabel;
            public Label TelegraphLabel;
        }

        class PartyWidget
        {
            public VisualElement Root;
            public VisualElement Portrait;
            public VisualElement ChargeBar;
            public Label ChargeLabel;
        }

        public BattleScreen(IBattleEventSource source, Action<float> pump, ChapterDto chapter, StageDto stage)
        {
            _source = source;
            _pump = pump;
            _chapter = chapter;
            _stage = stage;
        }

        protected override void Build(VisualElement root)
        {
            var state = _source.State;

            // ------------------------------ Top bar ---------------------------
            var top = UiKit.Row(10f);
            top.style.paddingLeft = 12f;
            top.style.paddingRight = 12f;
            top.style.paddingTop = 6f;
            top.style.paddingBottom = 6f;
            top.style.backgroundColor = Theme.BgDeep.WithAlpha(0.9f);
            top.style.flexShrink = 0f;
            top.Add(UiKit.Button("‹ Retreat", () => Router.Pop()));
            top.Add(UiKit.Title(state.StageName, 16f));
            _waveLabel = UiKit.Dim("", 13f);
            top.Add(_waveLabel);
            top.Add(UiKit.Spacer());
            _resolutionsLabel = UiKit.Dim("", 12f);
            top.Add(_resolutionsLabel);
            root.Add(top);

            // ------------------------------ Main row --------------------------
            var main = UiKit.Row(0f);
            main.style.flexGrow = 1f;
            main.style.alignItems = Align.Stretch;
            root.Add(main);

            // Battlefield (left).
            _battlefield = new VisualElement();
            _battlefield.style.flexGrow = 1f;
            _battlefield.style.backgroundImage = new StyleBackground(
                PlaceholderArt.Background(_chapter != null ? _chapter.theme : "demo"));
            _battlefield.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
            _battlefield.style.justifyContent = Justify.Center;
            main.Add(_battlefield);

            _enemyRow = UiKit.Row(18f);
            _enemyRow.style.justifyContent = Justify.Center;
            _enemyRow.style.alignItems = Align.Center;
            _battlefield.Add(_enemyRow);
            BuildEnemies();

            // Combo readout pinned bottom-left of battlefield.
            _comboLabel = UiKit.Text("", 22f, true, Theme.Accent);
            _comboLabel.style.position = Position.Absolute;
            _comboLabel.style.left = 14f;
            _comboLabel.style.bottom = 12f;
            _comboLabel.style.unityTextOutlineWidth = 1.5f;
            _comboLabel.style.unityTextOutlineColor = Theme.BgDeep;
            _battlefield.Add(_comboLabel);

            // Right column: party, timer, board.
            var right = UiKit.Column(6f);
            right.style.width = Length.Percent(44);
            right.style.minWidth = 360f;
            right.style.maxWidth = 560f;
            right.style.paddingLeft = 8f;
            right.style.paddingRight = 8f;
            right.style.paddingTop = 6f;
            right.style.paddingBottom = 8f;
            right.style.backgroundColor = Theme.Bg.WithAlpha(0.92f);
            main.Add(right);

            _partyRow = UiKit.Row(5f);
            _partyRow.style.justifyContent = Justify.Center;
            right.Add(_partyRow);
            BuildParty();

            // Party HP.
            var hpRow = UiKit.Row(8f);
            hpRow.Add(UiKit.Dim("HP", 12f));
            _partyHpBar = UiKit.Bar(Theme.HpBar, 14f);
            _partyHpBar.style.flexGrow = 1f;
            hpRow.Add(_partyHpBar);
            _partyHpLabel = UiKit.Text("", 12f, true);
            hpRow.Add(_partyHpLabel);
            right.Add(hpRow);

            // Move timer.
            var timerRow = UiKit.Row(8f);
            timerRow.Add(UiKit.Dim("Move", 12f));
            _timerBar = UiKit.Bar(Theme.TimerBar, 8f);
            _timerBar.style.flexGrow = 1f;
            timerRow.Add(_timerBar);
            _timerLabel = UiKit.Text("", 12f, true, Theme.TimerBar);
            timerRow.Add(_timerLabel);
            right.Add(timerRow);

            // Board keeps a 6:5 aspect and fills remaining space.
            var boardHost = new VisualElement();
            boardHost.style.flexGrow = 1f;
            boardHost.style.alignItems = Align.Center;
            boardHost.style.justifyContent = Justify.Center;
            right.Add(boardHost);
            _board = new OrbBoardView(_source);
            boardHost.Add(_board);
            boardHost.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float w = boardHost.resolvedStyle.width;
                float h = boardHost.resolvedStyle.height;
                float cell = Mathf.Min(w / 6f, h / 5f);
                _board.style.width = cell * 6f;
                _board.style.height = cell * 5f;
            });

            // FX layer on top of everything.
            _fxLayer = new VisualElement();
            _fxLayer.pickingMode = PickingMode.Ignore;
            _fxLayer.style.position = Position.Absolute;
            _fxLayer.style.left = 0; _fxLayer.style.right = 0;
            _fxLayer.style.top = 0; _fxLayer.style.bottom = 0;
            root.Add(_fxLayer);

            _board.MatchBurst = (worldPos, color) =>
            {
                var local = _fxLayer.WorldToLocal(worldPos);
                UiFx.Burst(_fxLayer, local, Theme.OrbColorOf(color), 8, 34f);
            };

            Subscribe();
            RefreshAll();
            ShowDialogue(_stage != null ? _stage.dialogueBefore : null, null);
            MaybeBossIntro();
        }

        // ------------------------------ Widgets -------------------------------

        void BuildEnemies()
        {
            _enemyRow.Clear();
            _enemyWidgets.Clear();
            foreach (var enemy in _source.State.Enemies)
            {
                var w = new EnemyWidget();
                float size = enemy.IsBoss ? 150f : 100f;
                w.Root = UiKit.Column(4f);
                w.Root.style.alignItems = Align.Center;
                w.Root.style.width = size + 40f;

                w.TelegraphLabel = UiKit.Dim("", 11f);
                w.TelegraphLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                w.Root.Add(w.TelegraphLabel);

                var portraitHost = new VisualElement();
                portraitHost.style.alignItems = Align.Center;
                w.Portrait = new VisualElement();
                w.Portrait.style.width = size;
                w.Portrait.style.height = size * 1.05f;
                UiKit.Round(w.Portrait, Theme.Radius);
                w.Portrait.style.overflow = Overflow.Hidden;
                UiKit.Border(w.Portrait, enemy.IsBoss ? Theme.Danger : Theme.PanelLine, enemy.IsBoss ? 3f : 2f);
                w.Portrait.style.backgroundImage = new StyleBackground(
                    PlaceholderArt.Portrait(enemy.ArtRef, enemy.Element, enemy.DisplayName, enemy.IsBoss));
                var initials = UiKit.Text(PlaceholderArt.Initials(enemy.DisplayName), size * 0.3f, true, Color.white.WithAlpha(0.9f));
                initials.style.position = Position.Absolute;
                initials.style.left = 0; initials.style.right = 0;
                initials.style.top = Length.Percent(26);
                initials.style.unityTextAlign = TextAnchor.MiddleCenter;
                w.Portrait.Add(initials);

                // Countdown chip on the portrait corner.
                var cd = new VisualElement();
                cd.style.position = Position.Absolute;
                cd.style.top = 4f; cd.style.right = 4f;
                cd.style.backgroundColor = Theme.BgDeep.WithAlpha(0.9f);
                UiKit.Round(cd, 12f);
                cd.style.paddingLeft = 8f; cd.style.paddingRight = 8f;
                cd.style.paddingTop = 2f; cd.style.paddingBottom = 2f;
                w.CountdownLabel = UiKit.Text("", 15f, true, Theme.AccentWarm);
                cd.Add(w.CountdownLabel);
                w.Portrait.Add(cd);
                portraitHost.Add(w.Portrait);
                w.Root.Add(portraitHost);

                var nameRow = UiKit.Row(5f);
                nameRow.Add(UiKit.ElementBadge(enemy.Element, 15f));
                var name = UiKit.Text((enemy.IsBoss ? "☠ " : "") + enemy.DisplayName, 13f, true,
                    enemy.IsBoss ? Theme.Danger : Theme.TextMain);
                nameRow.Add(name);
                w.Root.Add(nameRow);

                w.HpBar = UiKit.Bar(Theme.EnemyHp, 9f);
                w.HpBar.style.width = size + 30f;
                w.Root.Add(w.HpBar);
                w.HpLabel = UiKit.Dim("", 10f);
                w.Root.Add(w.HpLabel);
                w.StatusLabel = UiKit.Dim("", 10f);
                w.StatusLabel.style.color = Theme.AccentWarm;
                w.Root.Add(w.StatusLabel);

                _enemyRow.Add(w.Root);
                _enemyWidgets.Add(w);
            }
        }

        void BuildParty()
        {
            _partyRow.Clear();
            _partyWidgets.Clear();
            for (int slot = 0; slot < _source.State.Party.Count; slot++)
            {
                var member = _source.State.Party[slot];
                var w = new PartyWidget();
                w.Root = UiKit.Column(3f);
                w.Root.style.alignItems = Align.Center;

                w.Portrait = UiKit.PortraitCard(member.Character, 62f, 76f);
                if (member.IsLeader)
                {
                    var crown = UiKit.Text("♛", 13f, true, Theme.GoldColor);
                    crown.style.position = Position.Absolute;
                    crown.style.top = -2f;
                    crown.style.right = 2f;
                    w.Portrait.Add(crown);
                }
                int capturedSlot = slot;
                w.Portrait.RegisterCallback<PointerDownEvent>(evt =>
                {
                    _source.CastSkill(capturedSlot);
                    evt.StopPropagation();
                });
                w.Root.Add(w.Portrait);

                w.ChargeBar = UiKit.Bar(Theme.ElementColor(member.Character.Element), 6f);
                w.ChargeBar.style.width = 60f;
                w.Root.Add(w.ChargeBar);
                w.ChargeLabel = UiKit.Dim("", 9f);
                w.Root.Add(w.ChargeLabel);

                _partyRow.Add(w.Root);
                _partyWidgets.Add(w);
            }
        }

        // ------------------------------ Events --------------------------------

        void Subscribe()
        {
            _source.AttackPerformed += OnAttack;
            _source.Healed += OnHeal;
            _source.ComboChanged += OnCombo;
            _source.EnemyActed += OnEnemyActed;
            _source.EnemyCountdownChanged += OnEnemyCountdown;
            _source.EnemyDefeated += OnEnemyDefeated;
            _source.SkillCast += OnSkillCast;
            _source.StateChanged += RefreshAll;
            _source.BattleEnded += OnBattleEnded;
            Root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                _source.AttackPerformed -= OnAttack;
                _source.Healed -= OnHeal;
                _source.ComboChanged -= OnCombo;
                _source.EnemyActed -= OnEnemyActed;
                _source.EnemyCountdownChanged -= OnEnemyCountdown;
                _source.EnemyDefeated -= OnEnemyDefeated;
                _source.SkillCast -= OnSkillCast;
                _source.StateChanged -= RefreshAll;
                _source.BattleEnded -= OnBattleEnded;
            });
        }

        void OnAttack(AttackEvent ev)
        {
            if (ev.PartySlot >= 0 && ev.PartySlot < _partyWidgets.Count)
            {
                UiFx.Punch(_partyWidgets[ev.PartySlot].Portrait, 1.12f, 140);
            }
            if (ev.TargetEnemyIndex >= 0 && ev.TargetEnemyIndex < _enemyWidgets.Count)
            {
                var target = _enemyWidgets[ev.TargetEnemyIndex];
                var pos = _fxLayer.WorldToLocal(target.Portrait.worldBound.center);
                var color = Theme.ElementColor(ev.Element);
                UiFx.Burst(_fxLayer, pos, color, ev.Critical ? 16 : 10, ev.Critical ? 64f : 44f);
                UiFx.DamageNumber(_fxLayer, pos + new Vector2(0f, -20f), ev.Damage,
                    color, ev.Critical, ev.EffectiveHit, ev.ResistedHit);
                UiFx.Punch(target.Portrait, ev.Critical ? 1.16f : 1.07f, 120);
                if (ev.EffectiveHit)
                {
                    UiFx.Flash(target.Portrait, color, 0.35f, 160);
                }
                // Escalating shake keeps rapid multi-attacks satisfying without nausea.
                float mag = Mathf.Min(2.5f + ev.SequenceIndex * 0.6f, 8f);
                UiFx.Shake(_battlefield, ev.Critical ? mag + 3f : mag, 180);
                if (ev.Critical && ev.SequenceCount <= 4)
                {
                    WorldFx.Instance.HitStop(0.05f);
                }
            }
            RefreshAll();
        }

        void OnHeal(HealEvent ev)
        {
            var pos = _fxLayer.WorldToLocal(_partyHpBar.worldBound.center);
            UiFx.HealNumber(_fxLayer, pos + new Vector2(0f, -18f), ev.Amount);
            UiFx.Flash(_partyHpBar, Theme.Success, 0.5f, 300);
            RefreshAll();
        }

        void OnCombo(int combo)
        {
            _comboLabel.text = combo >= 1 ? "COMBO ×" + combo : "";
            UiFx.Punch(_comboLabel, 1.3f, 160);
            if (combo >= 2)
            {
                var pos = _fxLayer.WorldToLocal(_board.worldBound.center) + new Vector2(-60f, -40f);
                UiFx.ComboPopup(_fxLayer, pos, combo);
            }
        }

        void OnEnemyActed(EnemyActionEvent ev)
        {
            var accent = ev.DamageToParty > 0 ? Theme.Danger : Theme.AccentWarm;
            UiFx.TelegraphBanner(_fxLayer, ev.ActionName, ev.Description, accent);
            if (ev.DamageToParty > 0)
            {
                UiFx.Shake(Root, 7f, 320);
                UiFx.Flash(Root, Theme.Danger, 0.22f, 260);
                var pos = _fxLayer.WorldToLocal(_partyHpBar.worldBound.center);
                UiFx.DamageNumber(_fxLayer, pos + new Vector2(30f, -16f), ev.DamageToParty, Theme.Danger);
            }
            if (ev.EnemyIndex >= 0 && ev.EnemyIndex < _enemyWidgets.Count)
            {
                UiFx.Punch(_enemyWidgets[ev.EnemyIndex].Portrait, 1.12f, 200);
            }
            _board.Refresh();
            RefreshAll();
        }

        void OnEnemyCountdown(int index)
        {
            if (index >= 0 && index < _enemyWidgets.Count)
            {
                UiFx.Punch(_enemyWidgets[index].CountdownLabel, 1.5f, 180);
            }
            RefreshAll();
        }

        void OnEnemyDefeated(int index)
        {
            if (index >= 0 && index < _enemyWidgets.Count)
            {
                var w = _enemyWidgets[index];
                var pos = _fxLayer.WorldToLocal(w.Portrait.worldBound.center);
                UiFx.Burst(_fxLayer, pos, Theme.AccentWarm, 22, 90f);
                w.Root.experimental.animation.Start(1f, 0f, MotionSettings.Ms(420), (e, v) =>
                {
                    e.style.opacity = v;
                    e.style.scale = new Scale(Vector2.one * (0.7f + v * 0.3f));
                });
            }
        }

        void OnSkillCast(SkillCastEvent ev)
        {
            var caster = _source.State.Party[ev.PartySlot].Character;
            if (ev.BigCutIn)
            {
                UiFx.SkillCutIn(_fxLayer, caster, ev.SkillName);
                UiFx.Shake(Root, 5f, 260);
            }
            else
            {
                UiFx.TelegraphBanner(_fxLayer, caster.DisplayName, ev.SkillName, Theme.ElementColor(ev.Element));
            }
            _board.Refresh();
            RefreshAll();
        }

        void OnBattleEnded(BattleEndEvent ev)
        {
            if (_ended) return;
            _ended = true;
            ShowDialogue(ev.Outcome == BattleOutcome.Victory && _stage != null ? _stage.dialogueAfter : null,
                () => ShowResults(ev));
        }

        void MaybeBossIntro()
        {
            foreach (var enemy in _source.State.Enemies)
            {
                if (!enemy.IsBoss) continue;
                Root.schedule.Execute(() =>
                {
                    UiFx.TelegraphBanner(_fxLayer, "⚔ BOSS: " + enemy.DisplayName + " ⚔",
                        "It looks extremely serious about this.", Theme.Danger);
                    UiFx.Shake(_battlefield, 8f, 420);
                }).ExecuteLater(600);
                break;
            }
        }

        // ------------------------------ Results --------------------------------

        void ShowResults(BattleEndEvent ev)
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0; overlay.style.right = 0;
            overlay.style.top = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = Theme.BgDeep.WithAlpha(0.75f);
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;

            var panel = UiKit.Panel(true);
            panel.style.minWidth = 340f;
            panel.style.alignItems = Align.Center;
            bool win = ev.Outcome == BattleOutcome.Victory;
            var title = UiKit.Title(win ? "VICTORY!" : "DEFEAT...", 34f);
            title.style.color = win ? Theme.AccentWarm : Theme.Danger;
            panel.Add(title);

            if (win)
            {
                var starsRow = UiKit.Row(6f);
                for (int i = 0; i < 3; i++)
                {
                    var star = UiKit.Text("★", 34f, true,
                        i < ev.StarsEarned ? Theme.Rarity5 : Theme.PanelLine);
                    starsRow.Add(star);
                    if (i < ev.StarsEarned && !MotionSettings.ReducedMotion)
                    {
                        int delay = 250 + i * 220;
                        var s = star;
                        Root.schedule.Execute(() => UiFx.Punch(s, 1.6f, 260)).ExecuteLater(delay);
                    }
                }
                panel.Add(starsRow);
                if (ev.RewardLines.Count > 0)
                {
                    panel.Add(UiKit.Dim("Rewards", 12f));
                    foreach (var line in ev.RewardLines)
                    {
                        panel.Add(UiKit.Text(line, 14f));
                    }
                }
            }
            else
            {
                panel.Add(UiKit.Dim("The party promises to take it seriously next time.", 12f));
            }

            var btn = UiKit.Button("Continue", () => Router.Pop(), true);
            btn.style.marginTop = 14f;
            panel.Add(btn);
            overlay.Add(panel);
            Root.Add(overlay);
            UiFx.Punch(panel, 1.08f, 260);
        }

        void ShowDialogue(string sceneId, Action then)
        {
            if (string.IsNullOrEmpty(sceneId) ||
                !ContentDb.Instance.DialogueScenes.TryGetValue(sceneId, out var scene))
            {
                then?.Invoke();
                return;
            }
            Root.Add(new DialogueOverlay(scene, then));
        }

        // ------------------------------ Refresh --------------------------------

        void RefreshAll()
        {
            var state = _source.State;
            _waveLabel.text = "Wave " + (state.WaveIndex + 1) + "/" + state.WaveCount;
            _resolutionsLabel.text = "Moves: " + state.BoardResolutions +
                (_stage != null ? " / ★" + _stage.resolutionsStar : "");

            if (_enemyWidgets.Count != state.Enemies.Count)
            {
                BuildEnemies();
            }
            for (int i = 0; i < _enemyWidgets.Count && i < state.Enemies.Count; i++)
            {
                var w = _enemyWidgets[i];
                var enemy = state.Enemies[i];
                UiKit.SetBar(w.HpBar, enemy.HpMax > 0 ? enemy.Hp / (float)enemy.HpMax : 0f);
                w.HpLabel.text = UiKit.FormatNumber(enemy.Hp) + " / " + UiKit.FormatNumber(enemy.HpMax);
                w.CountdownLabel.text = enemy.Hp > 0 ? enemy.Countdown.ToString() : "—";
                w.CountdownLabel.style.color = enemy.Countdown <= 1 ? Theme.Danger : Theme.AccentWarm;
                w.StatusLabel.text = string.Join("  ", enemy.ActiveStatuses);
                w.TelegraphLabel.text = string.IsNullOrEmpty(enemy.TelegraphText) || enemy.Hp <= 0
                    ? "" : "Next: " + enemy.TelegraphText;
            }

            float hpFrac = state.PartyHpMax > 0 ? state.PartyHp / (float)state.PartyHpMax : 0f;
            UiKit.SetBar(_partyHpBar, hpFrac, hpFrac < 0.3f ? Theme.HpBarLow : Theme.HpBar);
            _partyHpLabel.text = UiKit.FormatNumber(state.PartyHp) + " / " + UiKit.FormatNumber(state.PartyHpMax);

            float timerFrac = state.MoveTimeTotal > 0 ? state.MoveTimeRemaining / state.MoveTimeTotal : 0f;
            UiKit.SetBar(_timerBar, state.Moving ? timerFrac : 1f,
                timerFrac < 0.3f && state.Moving ? Theme.Danger : Theme.TimerBar);
            _timerLabel.text = state.Moving
                ? state.MoveTimeRemaining.ToString("0.0") + "s"
                : state.MoveTimeTotal.ToString("0") + "s";

            for (int i = 0; i < _partyWidgets.Count && i < state.Party.Count; i++)
            {
                var w = _partyWidgets[i];
                var member = state.Party[i];
                var skill = member.Character.ActiveSkill;
                if (skill != null)
                {
                    float frac = skill.ChargeMax > 0 ? skill.Charge / (float)skill.ChargeMax : 0f;
                    UiKit.SetBar(w.ChargeBar, frac, skill.Ready ? Theme.Accent : Theme.ElementColor(member.Character.Element));
                    w.ChargeLabel.text = skill.Ready ? "READY!" : skill.Charge + "/" + skill.ChargeMax;
                    w.ChargeLabel.style.color = skill.Ready ? Theme.Accent : Theme.TextDim;
                }
                w.Portrait.style.opacity = member.Bound ? 0.35f : 1f;
            }
        }

        public override void Tick(float deltaTime)
        {
            _pump?.Invoke(deltaTime);
        }
    }
}
