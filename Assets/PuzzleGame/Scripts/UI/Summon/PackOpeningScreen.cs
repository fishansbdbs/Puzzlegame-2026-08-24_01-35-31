using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using PuzzleGame.Presentation.VFX;

namespace PuzzleGame.Presentation.UI.Summon
{
    /// <summary>
    /// The pack-opening summon flow — a core identity feature.
    ///
    /// Summon confirmed → pack drops in → the player physically rips the
    /// pack by dragging → the pack bursts → cards land face-down →
    /// tap-to-reveal (or Reveal All) → results.
    ///
    /// Rarity is teased through foil tier, glow, shake and light cracks,
    /// with an occasional fake-out upgrade (pack transforms mid-rip).
    /// Presentation only: all results arrive pre-generated in the session.
    /// </summary>
    public class PackOpeningScreen : UiScreen
    {
        readonly SummonSession _session;
        readonly int _teaseSeed;

        VisualElement _stage;       // center area for pack then cards
        VisualElement _fxLayer;
        VisualElement _pack;
        VisualElement _tearStrip;
        Label _hint;
        float _ripProgress;
        bool _ripping;
        bool _ripped;
        int _cracksShown;
        bool _fakeOutPending;
        bool _fakeOutDone;

        readonly List<CardSlot> _cards = new List<CardSlot>();
        int _revealedCount;
        Button _revealAllBtn;
        Button _doneBtn;
        Label _summary;

        class CardSlot
        {
            public SummonCardResult Result;
            public VisualElement Root;      // flip container
            public bool Revealed;
            public bool Revealing;
        }

        public PackOpeningScreen(SummonSession session)
        {
            _session = session;
            // Deterministic presentation seed from the (pre-generated) results.
            int seed = 17;
            foreach (var card in session.Cards)
            {
                foreach (char c in card.CharacterId ?? "") seed = seed * 31 + c;
            }
            _teaseSeed = seed < 0 ? -seed : seed;
            // Fake-out: strong pack that first presents as a plain one.
            _fakeOutPending = session.MaxRarity >= 4 && _teaseSeed % 10 < 3 && !MotionSettings.ReducedMotion;
        }

        int TeaseTier => _session.MaxRarity >= 5 ? 2 : (_session.Cards.Count > 1 ? 1 : 0);
        int ShownTier => _fakeOutPending && !_fakeOutDone ? 0 : TeaseTier;

        protected override void Build(VisualElement root)
        {
            root.style.backgroundColor = Theme.BgDeep;

            var top = UiKit.Row(8f);
            top.style.paddingLeft = 14f;
            top.style.paddingTop = 8f;
            top.Add(UiKit.Dim(_session.Banner != null ? _session.Banner.DisplayName : "Summon", 13f));
            root.Add(top);

            _stage = new VisualElement();
            _stage.style.flexGrow = 1f;
            _stage.style.alignItems = Align.Center;
            _stage.style.justifyContent = Justify.Center;
            root.Add(_stage);

            _fxLayer = new VisualElement();
            _fxLayer.pickingMode = PickingMode.Ignore;
            _fxLayer.style.position = Position.Absolute;
            _fxLayer.style.left = 0; _fxLayer.style.right = 0;
            _fxLayer.style.top = 0; _fxLayer.style.bottom = 0;
            root.Add(_fxLayer);

            BuildPack();
        }

        // ------------------------------ Pack -----------------------------------

        void BuildPack()
        {
            bool multi = _session.Cards.Count > 1;
            float w = multi ? 210f : 160f;
            float h = multi ? 288f : 220f;

            var packHost = UiKit.Column(14f);
            packHost.style.alignItems = Align.Center;
            _stage.Add(packHost);

            _pack = new VisualElement();
            _pack.style.width = w;
            _pack.style.height = h;
            UiKit.Round(_pack, 10f);
            _pack.style.overflow = Overflow.Hidden;
            ApplyPackVisual();
            packHost.Add(_pack);

            // Tear strip along the top edge.
            _tearStrip = new VisualElement();
            _tearStrip.pickingMode = PickingMode.Ignore;
            _tearStrip.style.position = Position.Absolute;
            _tearStrip.style.left = 0; _tearStrip.style.right = 0;
            _tearStrip.style.top = 0;
            _tearStrip.style.height = Length.Percent(16);
            _tearStrip.style.backgroundColor = Color.white.WithAlpha(0.12f);
            _tearStrip.style.borderBottomColor = Color.white.WithAlpha(0.55f);
            _tearStrip.style.borderBottomWidth = 2f;
            _pack.Add(_tearStrip);

            var notch = UiKit.Text("✂ - - - - - - - - - -", 11f, false, Color.white.WithAlpha(0.7f));
            notch.pickingMode = PickingMode.Ignore;
            notch.style.position = Position.Absolute;
            notch.style.top = Length.Percent(12);
            notch.style.left = 6f;
            _pack.Add(notch);

            _hint = UiKit.Text("Hold and drag to rip the pack open!", 15f, true, Theme.Accent);
            packHost.Add(_hint);

            // Entry: drop + bounce, then idle tease.
            if (!MotionSettings.ReducedMotion)
            {
                _pack.style.translate = new Translate(0f, -260f);
                _pack.experimental.animation.Start(0f, 1f, 550, (e, t) =>
                {
                    float k = UiFx.EaseOut(t);
                    float bounce = t > 0.75f ? Mathf.Sin((t - 0.75f) / 0.25f * Mathf.PI) * 12f : 0f;
                    e.style.translate = new Translate(0f, -260f * (1f - k) - bounce);
                }).OnCompleted(StartIdleTease);
            }
            else
            {
                StartIdleTease();
            }

            _pack.RegisterCallback<PointerDownEvent>(OnRipStart);
            _pack.RegisterCallback<PointerMoveEvent>(OnRipDrag);
            _pack.RegisterCallback<PointerUpEvent>(OnRipEnd);
        }

        void ApplyPackVisual()
        {
            string art = _session.Banner != null ? _session.Banner.PackArtRef : _session.BannerId;
            _pack.style.backgroundImage = new StyleBackground(PlaceholderArt.Pack(art, ShownTier));
            Color rim = ShownTier >= 2 ? Theme.Rarity5 : ShownTier == 1 ? Theme.Rarity4 : Theme.PanelLine;
            UiKit.Border(_pack, rim, ShownTier >= 2 ? 3f : 2f);
        }

        void StartIdleTease()
        {
            if (MotionSettings.ReducedMotion || _ripped) return;
            // The richer the pack, the more it trembles with anticipation.
            int tier = ShownTier;
            float amp = 1.5f + tier * 2f;
            _pack.experimental.animation.Start(0f, 1f, 1400, (e, t) =>
            {
                if (_ripped || _ripping) return;
                e.style.translate = new Translate(
                    Mathf.Sin(t * Mathf.PI * 8f) * amp * 0.4f,
                    Mathf.Sin(t * Mathf.PI * 2f) * 3f);
            }).OnCompleted(() =>
            {
                if (!_ripped) StartIdleTease();
            });
            if (tier >= 2)
            {
                var center = _fxLayer.WorldToLocal(_pack.worldBound.center);
                UiFx.Burst(_fxLayer, center, Theme.Rarity5, 5, 60f);
            }
        }

        // ------------------------------ Ripping ---------------------------------

        void OnRipStart(PointerDownEvent evt)
        {
            if (_ripped) return;
            _ripping = true;
            _pack.CapturePointer(evt.pointerId);
            _hint.text = "Riiiip!";
        }

        void OnRipDrag(PointerMoveEvent evt)
        {
            if (!_ripping || _ripped) return;
            float delta = Mathf.Abs(evt.deltaPosition.x) + Mathf.Abs(evt.deltaPosition.y) * 0.4f;
            _ripProgress += delta / (_pack.resolvedStyle.width * 2.2f);
            UpdateRipVisual();
            if (_ripProgress >= 1f)
            {
                _ripped = true;
                _ripping = false;
                _pack.ReleasePointer(evt.pointerId);
                TearOpen();
            }
        }

        void OnRipEnd(PointerUpEvent evt)
        {
            _ripping = false;
            _pack.ReleasePointer(evt.pointerId);
            if (!_ripped) _hint.text = "Keep ripping!";
        }

        void UpdateRipVisual()
        {
            float p = Mathf.Clamp01(_ripProgress);
            _tearStrip.style.translate = new Translate(p * _pack.resolvedStyle.width * 0.55f, -p * 8f);
            _tearStrip.style.rotate = new Rotate(new Angle(p * 6f));
            UiFx.Shake(_pack, 1.5f + p * 3f + ShownTier * 1.2f, 90);

            // Light cracks escape at rip thresholds — brighter for better packs.
            int crackStage = p >= 0.85f ? 3 : p >= 0.55f ? 2 : p >= 0.3f ? 1 : 0;
            while (_cracksShown < crackStage)
            {
                _cracksShown++;
                SpawnCrack(_cracksShown);
            }
            // Fake-out: mid-rip the humble pack reveals its true foil.
            if (_fakeOutPending && !_fakeOutDone && p >= 0.55f)
            {
                _fakeOutDone = true;
                UiFx.Flash(Root, Color.white, 0.6f, 300);
                UiFx.Shake(Root, 8f, 300);
                ApplyPackVisual();
                _hint.text = "Wait... this pack is different!";
                _hint.style.color = Theme.Rarity5;
            }
        }

        void SpawnCrack(int stage)
        {
            Color glow = ShownTier >= 2 ? Theme.Rarity5 : ShownTier == 1 ? Theme.Rarity4 : Color.white;
            var crack = new VisualElement();
            crack.pickingMode = PickingMode.Ignore;
            crack.style.position = Position.Absolute;
            crack.style.top = Length.Percent(14);
            crack.style.left = Length.Percent(12 + stage * 26);
            crack.style.width = 3f;
            crack.style.height = Length.Percent(18 + stage * 8);
            crack.style.backgroundColor = glow;
            crack.style.rotate = new Rotate(new Angle(stage % 2 == 0 ? 14f : -11f));
            _pack.Add(crack);
            var center = _fxLayer.WorldToLocal(_pack.worldBound.center);
            UiFx.Burst(_fxLayer, center, glow, 6 + stage * 3, 40f + stage * 14f);
        }

        void TearOpen()
        {
            _hint.text = "";
            var center = _fxLayer.WorldToLocal(_pack.worldBound.center);
            Color glow = ShownTier >= 2 ? Theme.Rarity5 : ShownTier == 1 ? Theme.Rarity4 : Theme.Accent;
            UiFx.Burst(_fxLayer, center, glow, 26, 140f);
            UiFx.Flash(Root, glow, ShownTier >= 2 ? 0.5f : 0.3f, 350);
            UiFx.Shake(Root, 6f + ShownTier * 2f, 320);
            if (ShownTier >= 2) WorldFx.Instance.HitStop(0.06f);

            // Strip flies off, pack halves part and fade.
            if (!MotionSettings.ReducedMotion)
            {
                _tearStrip.experimental.animation.Start(0f, 1f, 400, (e, t) =>
                {
                    e.style.translate = new Translate(
                        _pack.resolvedStyle.width * (0.55f + t * 1.4f), -t * 120f);
                    e.style.rotate = new Rotate(new Angle(6f + t * 50f));
                    e.style.opacity = 1f - t;
                });
            }
            _pack.experimental.animation.Start(0f, 1f, MotionSettings.Ms(420), (e, t) =>
            {
                e.style.scale = new Scale(new Vector2(1f + t * 0.25f, 1f - t * 0.15f));
                e.style.opacity = 1f - t;
            }).OnCompleted(ShowCards);
        }

        // ------------------------------ Cards -----------------------------------

        void ShowCards()
        {
            _stage.Clear();
            var column = UiKit.Column(10f);
            column.style.alignItems = Align.Center;
            _stage.Add(column);

            int count = _session.Cards.Count;
            int perRow = count > 5 ? 5 : count;
            float cardW = count > 5 ? 104f : count > 1 ? 118f : 168f;
            float cardH = cardW * 1.3f;

            var rows = new List<VisualElement>();
            for (int i = 0; i < count; i += perRow)
            {
                var row = UiKit.Row(10f);
                row.style.justifyContent = Justify.Center;
                column.Add(row);
                rows.Add(row);
            }

            for (int i = 0; i < count; i++)
            {
                var slot = new CardSlot { Result = _session.Cards[i] };
                slot.Root = BuildCardBack(slot, cardW, cardH);
                rows[i / perRow].Add(slot.Root);
                _cards.Add(slot);
                // Cards deal out with a stagger.
                if (!MotionSettings.ReducedMotion)
                {
                    slot.Root.style.opacity = 0f;
                    slot.Root.style.translate = new Translate(0f, -60f);
                    int delay = i * 70;
                    var captured = slot.Root;
                    Root.schedule.Execute(() =>
                    {
                        captured.experimental.animation.Start(0f, 1f, 260, (e, t) =>
                        {
                            e.style.opacity = t;
                            e.style.translate = new Translate(0f, -60f * (1f - UiFx.EaseOut(t)));
                        });
                    }).ExecuteLater(delay);
                }
            }

            var footer = UiKit.Row(12f);
            footer.style.marginTop = 8f;
            _revealAllBtn = UiKit.Button("Reveal All", RevealAllSequentially, true);
            footer.Add(_revealAllBtn);
            _summary = UiKit.Dim("Tap a card to reveal it", 12f);
            footer.Add(_summary);
            _doneBtn = UiKit.WarmButton("Done", () => Router.Pop());
            _doneBtn.style.display = DisplayStyle.None;
            footer.Add(_doneBtn);
            column.Add(footer);
        }

        VisualElement BuildCardBack(CardSlot slot, float w, float h)
        {
            var card = new VisualElement();
            card.style.width = w;
            card.style.height = h;
            UiKit.Round(card, 8f);
            card.style.overflow = Overflow.Hidden;
            UiKit.Border(card, Theme.PanelLine, 2f);
            card.style.backgroundColor = Theme.Panel;
            card.style.alignItems = Align.Center;
            card.style.justifyContent = Justify.Center;

            // Uniform card back — no per-card spoilers, just an inviting sigil.
            var sigil = UiKit.Text("❖", w * 0.34f, true, Theme.PanelLine);
            card.Add(sigil);
            var brand = UiKit.Dim(GameInfo.Title, 10f);
            brand.style.position = Position.Absolute;
            brand.style.bottom = 6f;
            card.Add(brand);

            card.RegisterCallback<PointerDownEvent>(_ => Reveal(slot));
            return card;
        }

        void RevealAllSequentially()
        {
            _revealAllBtn.SetEnabled(false);
            int delay = 0;
            foreach (var slot in _cards)
            {
                if (slot.Revealed || slot.Revealing) continue;
                var captured = slot;
                Root.schedule.Execute(() => Reveal(captured)).ExecuteLater(delay);
                // 5★ reveals take longer; give them room in the sequence.
                delay += captured.Result.Character != null && captured.Result.Character.BaseRarity >= 5
                    ? MotionSettings.Ms(1500)
                    : MotionSettings.Ms(420);
            }
        }

        void Reveal(CardSlot slot)
        {
            if (slot.Revealed || slot.Revealing) return;
            slot.Revealing = true;
            var character = slot.Result.Character;
            int rarity = character != null ? character.BaseRarity : 1;

            if (rarity >= 5)
            {
                // Dramatic build-up: the card resists, glows, shakes... then erupts.
                int buildMs = MotionSettings.Ms(1000);
                UiKit.Border(slot.Root, Theme.Rarity5, 3f);
                UiFx.Shake(slot.Root, 5f, buildMs);
                var center = _fxLayer.WorldToLocal(slot.Root.worldBound.center);
                UiFx.Burst(_fxLayer, center, Theme.Rarity5, 14, 70f);
                Root.schedule.Execute(() =>
                {
                    UiFx.Flash(Root, Theme.Rarity5, 0.55f, 400);
                    UiFx.Shake(Root, 9f, 350);
                    WorldFx.Instance.HitStop(0.05f);
                    UiFx.Burst(_fxLayer, _fxLayer.WorldToLocal(slot.Root.worldBound.center), Theme.Rarity6, 30, 150f);
                    FlipCard(slot);
                }).ExecuteLater(buildMs);
            }
            else if (rarity == 4)
            {
                UiKit.Border(slot.Root, Theme.Rarity4, 3f);
                var center = _fxLayer.WorldToLocal(slot.Root.worldBound.center);
                UiFx.Burst(_fxLayer, center, Theme.Rarity4, 10, 60f);
                Root.schedule.Execute(() => FlipCard(slot)).ExecuteLater(MotionSettings.Ms(280));
            }
            else
            {
                FlipCard(slot);
            }
        }

        void FlipCard(CardSlot slot)
        {
            int half = MotionSettings.Ms(130);
            slot.Root.experimental.animation.Start(1f, 0f, half, (e, v) =>
                e.style.scale = new Scale(new Vector2(Mathf.Max(0.02f, v), 1f)))
                .OnCompleted(() =>
                {
                    BuildCardFace(slot);
                    slot.Root.experimental.animation.Start(0f, 1f, half, (e, v) =>
                        e.style.scale = new Scale(new Vector2(Mathf.Max(0.02f, v), 1f)))
                        .OnCompleted(() => FinishReveal(slot));
                });
        }

        void BuildCardFace(CardSlot slot)
        {
            var result = slot.Result;
            var character = result.Character;
            var card = slot.Root;
            card.Clear();
            card.style.backgroundColor = Theme.PanelRaised;
            int rarity = character != null ? character.CurrentRarity : 1;
            UiKit.Border(card, Theme.RarityColor(rarity), rarity >= 5 ? 3f : 2f);
            if (character == null) return;

            float w = card.resolvedStyle.width;
            var art = new VisualElement();
            art.pickingMode = PickingMode.Ignore;
            art.style.position = Position.Absolute;
            art.style.left = 0; art.style.right = 0; art.style.top = 0; art.style.bottom = 0;
            art.style.backgroundImage = new StyleBackground(
                PlaceholderArt.Portrait(character.CurrentArtRef, character.Element, character.DisplayName, character.Awakened));
            card.Add(art);

            var initials = UiKit.Text(PlaceholderArt.Initials(character.DisplayName), w * 0.3f, true, Color.white.WithAlpha(0.85f));
            initials.pickingMode = PickingMode.Ignore;
            initials.style.position = Position.Absolute;
            initials.style.left = 0; initials.style.right = 0;
            initials.style.top = Length.Percent(22);
            initials.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(initials);

            var badge = UiKit.ElementBadge(character.Element, 18f);
            badge.style.position = Position.Absolute;
            badge.style.top = 4f; badge.style.left = 4f;
            card.Add(badge);

            // NEW / duplicate banner.
            var status = UiKit.Text("", 10f, true, Theme.BgDeep);
            status.pickingMode = PickingMode.Ignore;
            status.style.position = Position.Absolute;
            status.style.top = 5f; status.style.right = 4f;
            UiKit.Round(status, 4f);
            status.style.paddingLeft = 5f; status.style.paddingRight = 5f;
            status.style.paddingTop = 1f; status.style.paddingBottom = 1f;
            if (result.IsNew)
            {
                status.text = "NEW!";
                status.style.backgroundColor = Theme.AccentWarm;
            }
            else if (result.AscensionGained)
            {
                status.text = "ASC +1 (" + result.AscensionAfter + "/5)";
                status.style.backgroundColor = Theme.Accent;
            }
            else
            {
                status.text = result.OverflowReward ?? "MAX ASC";
                status.style.backgroundColor = Theme.Rarity6;
            }
            card.Add(status);

            var bottom = UiKit.Column(1f);
            bottom.pickingMode = PickingMode.Ignore;
            bottom.style.position = Position.Absolute;
            bottom.style.bottom = 4f;
            bottom.style.left = 4f; bottom.style.right = 4f;
            bottom.style.alignItems = Align.Center;
            bottom.style.backgroundColor = Theme.BgDeep.WithAlpha(0.72f);
            UiKit.Round(bottom, 5f);
            var name = UiKit.Text(character.DisplayName, 12f, true);
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            bottom.Add(name);
            bottom.Add(UiKit.Stars(character.BaseRarity, character.Awakened, 11f));
            card.Add(bottom);
        }

        void FinishReveal(CardSlot slot)
        {
            slot.Revealed = true;
            slot.Revealing = false;
            UiFx.Punch(slot.Root, slot.Result.Character != null && slot.Result.Character.BaseRarity >= 5 ? 1.22f : 1.1f, 220);
            _revealedCount++;
            if (_revealedCount >= _cards.Count)
            {
                int newCount = 0, fiveStars = 0;
                foreach (var c in _cards)
                {
                    if (c.Result.IsNew) newCount++;
                    if (c.Result.Character != null && c.Result.Character.BaseRarity >= 5) fiveStars++;
                }
                _summary.text = newCount + " new" + (fiveStars > 0 ? " • " + fiveStars + "× 5★!" : "");
                _summary.style.color = fiveStars > 0 ? Theme.Rarity5 : Theme.TextDim;
                _revealAllBtn.style.display = DisplayStyle.None;
                _doneBtn.style.display = DisplayStyle.Flex;
                UiFx.Punch(_doneBtn, 1.15f, 240);
            }
        }
    }
}
