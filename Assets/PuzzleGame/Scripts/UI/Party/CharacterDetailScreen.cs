using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using PuzzleGame.Presentation.VFX;

namespace PuzzleGame.Presentation.UI.Party
{
    /// <summary>
    /// Character detail: stats, element, rarity, level-up, Ascension pips,
    /// Leader/Active/Passive skills, tags and the 6★ Awakening panel with a
    /// base-vs-awakened preview toggle. Awakening triggers the ceremony.
    /// </summary>
    public class CharacterDetailScreen : UiScreen
    {
        readonly string _characterId;
        CharacterView _character;
        bool _previewAwakened;

        VisualElement _content;

        public CharacterDetailScreen(string characterId)
        {
            _characterId = characterId;
        }

        protected override void Build(VisualElement root)
        {
            root.Add(Header("Character"));
            _content = new VisualElement();
            _content.style.flexGrow = 1f;
            root.Add(_content);

            var roster = PresentationServices.Get<IRosterSource>();
            roster.RosterChanged += Rebuild;
            root.RegisterCallback<DetachFromPanelEvent>(_ => roster.RosterChanged -= Rebuild);
            Rebuild();
        }

        void Rebuild()
        {
            var roster = PresentationServices.Get<IRosterSource>();
            _character = roster.GetOwned(_characterId)
                         ?? PresentationServices.Get<IContentLibrary>().GetCharacter(_characterId);
            if (_character == null) return;
            _content.Clear();

            var main = UiKit.Row(14f);
            main.style.flexGrow = 1f;
            main.style.paddingLeft = 16f;
            main.style.paddingRight = 16f;
            main.style.paddingTop = 10f;
            main.style.paddingBottom = 10f;
            main.style.alignItems = Align.FlexStart;
            _content.Add(main);

            main.Add(BuildPortraitColumn());

            var right = new ScrollView(ScrollViewMode.Vertical);
            right.style.flexGrow = 1f;
            var col = UiKit.Column(10f);
            right.Add(col);
            main.Add(right);

            col.Add(BuildIdentityPanel());
            col.Add(BuildStatsPanel());
            col.Add(BuildSkillsPanel());
            col.Add(BuildAscensionPanel());
            col.Add(BuildAwakeningPanel());
        }

        VisualElement BuildPortraitColumn()
        {
            var col = UiKit.Column(8f);
            col.style.alignItems = Align.Center;
            col.style.flexShrink = 0f;

            bool showAwakened = _character.Awakened || _previewAwakened;
            var display = _character;
            if (_previewAwakened && !_character.Awakened)
            {
                // Preview view: same data, awakened art/frame.
                display = PresentationServices.Get<IContentLibrary>().GetCharacter(_characterId);
                if (display != null) display.Awakened = true;
                else display = _character;
            }
            var portrait = UiKit.PortraitCard(display, 190f, 236f);
            col.Add(portrait);
            if (showAwakened && !MotionSettings.ReducedMotion)
            {
                // Gentle idle pulse on awakened frames.
                portrait.experimental.animation.Start(0f, 1f, 1600, (e, t) =>
                {
                    float pulse = 0.5f + Mathf.Sin(t * Mathf.PI * 2f) * 0.5f;
                    UiKit.Border(e, Color.Lerp(Theme.Rarity6, Color.white, pulse * 0.5f), 3f);
                });
            }

            bool canPreview = !_character.Awakened && !string.IsNullOrEmpty(_character.AwakenedArtRef);
            if (canPreview)
            {
                col.Add(UiKit.Button(_previewAwakened ? "View Base Form" : "Preview 6★ Form", () =>
                {
                    _previewAwakened = !_previewAwakened;
                    Rebuild();
                }));
            }
            if (_character.Awakened)
            {
                var tag = UiKit.Text("★ AWAKENED ★", 13f, true, Theme.Rarity6);
                col.Add(tag);
            }
            return col;
        }

        VisualElement BuildIdentityPanel()
        {
            var panel = UiKit.Panel();
            var nameRow = UiKit.Row(8f);
            nameRow.Add(UiKit.ElementBadge(_character.Element, 22f));
            nameRow.Add(UiKit.Title(_character.DisplayName, 22f));
            nameRow.Add(UiKit.Stars(_character.BaseRarity, _character.Awakened, 15f));
            panel.Add(nameRow);
            if (!string.IsNullOrEmpty(_character.Epithet))
            {
                panel.Add(UiKit.Dim("“" + _character.Epithet + "”", 13f));
            }
            if (_character.Tags.Count > 0)
            {
                var tagRow = UiKit.Row(5f);
                tagRow.style.flexWrap = Wrap.Wrap;
                foreach (var tag in _character.Tags)
                {
                    var chip = UiKit.Dim(tag, 11f);
                    chip.style.backgroundColor = Theme.BgDeep;
                    UiKit.Round(chip, 8f);
                    chip.style.paddingLeft = 7f; chip.style.paddingRight = 7f;
                    chip.style.paddingTop = 2f; chip.style.paddingBottom = 2f;
                    tagRow.Add(chip);
                }
                panel.Add(tagRow);
            }
            if (!string.IsNullOrEmpty(_character.Flavor))
            {
                var flavor = UiKit.Dim(_character.Flavor, 12f);
                flavor.style.marginTop = 4f;
                panel.Add(flavor);
            }
            return panel;
        }

        VisualElement BuildStatsPanel()
        {
            var panel = UiKit.Panel();
            var header = UiKit.Row(8f);
            header.Add(UiKit.Text("Level " + _character.Level + " / " + _character.LevelMax, 15f, true));
            header.Add(UiKit.Spacer());
            bool maxed = _character.Level >= _character.LevelMax;
            if (!maxed)
            {
                var roster = PresentationServices.Get<IRosterSource>();
                header.Add(UiKit.Button("+1", () => TryLevel(1)));
                header.Add(UiKit.Button("+10", () => TryLevel(10)));
            }
            else
            {
                header.Add(UiKit.Text("MAX", 13f, true, Theme.GoldColor));
            }
            panel.Add(header);

            var bar = UiKit.Bar(Theme.Accent, 7f);
            UiKit.SetBar(bar, _character.LevelMax > 0 ? _character.Level / (float)_character.LevelMax : 0f);
            panel.Add(bar);

            var stats = UiKit.Row(20f);
            stats.style.marginTop = 6f;
            stats.Add(StatBlock("HP", _character.Hp, Theme.HpBar));
            stats.Add(StatBlock("ATK", _character.Atk, Theme.Danger));
            stats.Add(StatBlock("REC", _character.Rec, Theme.Success));
            panel.Add(stats);
            return panel;
        }

        void TryLevel(int levels)
        {
            var roster = PresentationServices.Get<IRosterSource>();
            if (roster.TryLevelUp(_characterId, levels))
            {
                UiFx.Flash(Root, Theme.Accent, 0.15f, 200);
            }
            else
            {
                UiFx.Shake(Root, 4f, 200);
            }
        }

        static VisualElement StatBlock(string label, int value, Color color)
        {
            var col = UiKit.Column(2f);
            col.Add(UiKit.Dim(label, 11f));
            col.Add(UiKit.Text(UiKit.FormatNumber(value), 19f, true, color));
            return col;
        }

        VisualElement BuildSkillsPanel()
        {
            var panel = UiKit.Panel();
            if (_character.LeaderSkill != null)
            {
                panel.Add(SkillRow("♛ Leader Skill", _character.LeaderSkill.Name,
                    _character.LeaderSkill.Description, Theme.GoldColor));
            }
            if (_character.ActiveSkill != null)
            {
                string chargeInfo = " (charges from " + _character.Element.DisplayName() + " matches, " +
                                    _character.ActiveSkill.ChargeMax + " orbs)";
                panel.Add(SkillRow("⚡ Active Skill", _character.ActiveSkill.Name,
                    _character.ActiveSkill.Description + chargeInfo, Theme.Accent));
            }
            if (!string.IsNullOrEmpty(_character.PassiveName))
            {
                panel.Add(SkillRow("◈ Passive", _character.PassiveName,
                    _character.PassiveDescription, Theme.TextDim));
            }
            return panel;
        }

        static VisualElement SkillRow(string kind, string name, string desc, Color accent)
        {
            var col = UiKit.Column(2f);
            col.style.marginBottom = 8f;
            var head = UiKit.Row(6f);
            head.Add(UiKit.Text(kind, 12f, true, accent));
            head.Add(UiKit.Text(name, 14f, true));
            col.Add(head);
            col.Add(UiKit.Dim(desc, 12f));
            return col;
        }

        VisualElement BuildAscensionPanel()
        {
            var panel = UiKit.Panel();
            var head = UiKit.Row(8f);
            head.Add(UiKit.Text("Ascension", 15f, true));
            head.Add(UiKit.Spacer());
            head.Add(UiKit.Dim(_character.Ascension + " / " + _character.AscensionCap, 13f));
            panel.Add(head);

            var pips = UiKit.Row(6f);
            for (int i = 1; i <= _character.AscensionCap; i++)
            {
                var pip = new VisualElement();
                pip.style.width = 20f;
                pip.style.height = 20f;
                UiKit.Round(pip, 10f);
                bool filled = i <= _character.Ascension;
                pip.style.backgroundColor = filled ? Theme.AccentWarm : Theme.BgDeep;
                UiKit.Border(pip, filled ? Theme.AccentWarm : Theme.PanelLine, 1.5f);
                pips.Add(pip);
            }
            panel.Add(pips);
            panel.Add(UiKit.Dim(
                "Duplicate pulls raise Ascension. Early levels power up the Active Skill; " +
                "later levels improve the Passive and add stats. Extra duplicates past max " +
                "convert to Spark Essence.", 11f));
            return panel;
        }

        VisualElement BuildAwakeningPanel()
        {
            var panel = UiKit.Panel();
            UiKit.Border(panel, _character.Awakened ? Theme.Rarity6 : Theme.PanelLine, 1.5f);
            var head = UiKit.Row(8f);
            head.Add(UiKit.Text("6★ Awakening", 15f, true, Theme.Rarity6));
            panel.Add(head);

            if (_character.Awakened)
            {
                panel.Add(UiKit.Text("This character has awakened to their true form.", 12f));
                return panel;
            }

            var roster = PresentationServices.Get<IRosterSource>();
            var lines = new List<string>();
            bool ready = roster.GetAwakenRequirements(_characterId, lines);
            foreach (var line in lines)
            {
                var label = UiKit.Text(line, 12f,
                    line.StartsWith("✓"), line.StartsWith("✓") ? Theme.Success : Theme.TextMain);
                panel.Add(label);
            }
            if (ready)
            {
                var btn = UiKit.WarmButton("⭒ AWAKEN ⭒", StartAwakening);
                btn.style.marginTop = 8f;
                btn.style.alignSelf = Align.Center;
                panel.Add(btn);
            }
            return panel;
        }

        void StartAwakening()
        {
            var roster = PresentationServices.Get<IRosterSource>();
            var before = roster.GetOwned(_characterId);
            if (before == null) return;
            Root.Add(new AwakeningCeremony(before, () =>
            {
                if (!roster.TryAwaken(_characterId))
                {
                    UiFx.Shake(Root, 4f, 200);
                    return null;
                }
                return roster.GetOwned(_characterId);
            }, Rebuild));
        }
    }
}
