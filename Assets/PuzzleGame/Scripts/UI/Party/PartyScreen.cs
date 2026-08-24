using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PuzzleGame.Presentation.UI.Party
{
    /// <summary>
    /// Owned-character roster. Cards show rarity frame, element, level and
    /// awakened treatment at a glance; tap opens the character detail page.
    /// </summary>
    public class PartyScreen : UiScreen
    {
        ScrollView _grid;
        Label _countLabel;
        string _elementFilter = "";

        protected override void Build(VisualElement root)
        {
            root.Add(Header("Characters"));

            var toolbar = UiKit.Row(8f);
            toolbar.style.paddingLeft = 14f;
            toolbar.style.paddingRight = 14f;
            toolbar.style.paddingTop = 8f;
            _countLabel = UiKit.Dim("", 12f);
            toolbar.Add(_countLabel);
            toolbar.Add(UiKit.Spacer());
            toolbar.Add(FilterButton("All", ""));
            foreach (ElementId element in System.Enum.GetValues(typeof(ElementId)))
            {
                toolbar.Add(FilterButton(Theme.ElementIcon(element), element.ToString()));
            }
            root.Add(toolbar);

            _grid = new ScrollView(ScrollViewMode.Vertical);
            _grid.style.flexGrow = 1f;
            _grid.contentContainer.style.flexDirection = FlexDirection.Row;
            _grid.contentContainer.style.flexWrap = Wrap.Wrap;
            _grid.contentContainer.style.justifyContent = Justify.Center;
            _grid.style.paddingTop = 6f;
            root.Add(_grid);

            var roster = PresentationServices.Get<IRosterSource>();
            roster.RosterChanged += Rebuild;
            root.RegisterCallback<DetachFromPanelEvent>(_ => roster.RosterChanged -= Rebuild);
        }

        Button FilterButton(string label, string filter)
        {
            return UiKit.Button(label, () =>
            {
                _elementFilter = filter;
                Rebuild();
            });
        }

        public override void OnEnter()
        {
            Rebuild();
        }

        void Rebuild()
        {
            _grid.Clear();
            var roster = PresentationServices.Get<IRosterSource>();
            IEnumerable<CharacterView> owned = roster.GetOwned();
            if (!string.IsNullOrEmpty(_elementFilter))
            {
                owned = owned.Where(c => c.Element.ToString() == _elementFilter);
            }
            var list = owned.ToList();
            _countLabel.text = list.Count + " owned";

            foreach (var character in list)
            {
                var card = UiKit.Column(3f);
                card.style.alignItems = Align.Center;
                card.style.marginLeft = 7f;
                card.style.marginRight = 7f;
                card.style.marginBottom = 12f;

                var portrait = UiKit.PortraitCard(character, 96f, 118f);
                var lv = UiKit.Text("Lv." + character.Level, 11f, true,
                    character.Level >= character.LevelMax ? Theme.GoldColor : Theme.TextMain);
                lv.style.position = Position.Absolute;
                lv.style.top = 3f;
                lv.style.right = 5f;
                lv.style.unityTextOutlineWidth = 1f;
                lv.style.unityTextOutlineColor = Theme.BgDeep;
                portrait.Add(lv);
                if (character.Awakened)
                {
                    var badge = UiKit.Text("AWAKENED", 8f, true, Theme.Rarity6);
                    badge.style.position = Position.Absolute;
                    badge.style.bottom = 16f;
                    badge.style.left = 0; badge.style.right = 0;
                    badge.style.unityTextAlign = TextAnchor.MiddleCenter;
                    portrait.Add(badge);
                }
                card.Add(portrait);
                var name = UiKit.Text(character.DisplayName, 12f, true);
                name.style.maxWidth = 100f;
                name.style.unityTextAlign = TextAnchor.MiddleCenter;
                card.Add(name);
                if (character.Ascension > 0)
                {
                    card.Add(UiKit.Dim("Asc. " + character.Ascension + "/" + character.AscensionCap, 10f));
                }

                var captured = character;
                portrait.RegisterCallback<PointerDownEvent>(_ =>
                    Router.Push(new CharacterDetailScreen(captured.Id)));
                _grid.Add(card);
            }
        }
    }
}
