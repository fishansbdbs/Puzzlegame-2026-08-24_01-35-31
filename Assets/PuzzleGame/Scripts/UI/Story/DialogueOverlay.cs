using System;
using UnityEngine;
using UnityEngine.UIElements;
using PuzzleGame.Presentation.Content;
using PuzzleGame.Presentation.VFX;

namespace PuzzleGame.Presentation.UI.Story
{
    /// <summary>
    /// Visual-novel style dialogue overlay used before/after stages and in
    /// event scenes. Tap/click anywhere to advance; Skip always available.
    /// Speakers slide in on the left/right with placeholder portraits.
    /// </summary>
    public class DialogueOverlay : VisualElement
    {
        readonly DialogueSceneDto _scene;
        readonly Action _onFinished;
        int _index = -1;

        VisualElement _leftPortrait;
        VisualElement _rightPortrait;
        Label _speakerLabel;
        Label _textLabel;
        VisualElement _textPanel;

        public DialogueOverlay(DialogueSceneDto scene, Action onFinished)
        {
            _scene = scene;
            _onFinished = onFinished;
            style.position = Position.Absolute;
            style.left = 0; style.right = 0; style.top = 0; style.bottom = 0;
            style.backgroundColor = Theme.BgDeep.WithAlpha(0.55f);

            _leftPortrait = MakePortraitSlot(true);
            _rightPortrait = MakePortraitSlot(false);
            Add(_leftPortrait);
            Add(_rightPortrait);

            _textPanel = UiKit.Panel(true);
            _textPanel.style.position = Position.Absolute;
            _textPanel.style.left = Length.Percent(8);
            _textPanel.style.right = Length.Percent(8);
            _textPanel.style.bottom = Length.Percent(5);
            _textPanel.style.minHeight = 96f;
            _speakerLabel = UiKit.Text("", 16f, true, Theme.Accent);
            _textLabel = UiKit.Text("", 15f);
            _textLabel.style.marginTop = 6f;
            _textPanel.Add(_speakerLabel);
            _textPanel.Add(_textLabel);
            var hint = UiKit.Dim("tap to continue ▸", 11f);
            hint.style.alignSelf = Align.FlexEnd;
            hint.style.marginTop = 4f;
            _textPanel.Add(hint);
            Add(_textPanel);

            var skip = UiKit.Button("Skip ≫", Finish);
            skip.style.position = Position.Absolute;
            skip.style.top = 10f;
            skip.style.right = 12f;
            Add(skip);

            RegisterCallback<PointerDownEvent>(_ => Advance());
            Advance();
        }

        VisualElement MakePortraitSlot(bool left)
        {
            var slot = new VisualElement();
            slot.pickingMode = PickingMode.Ignore;
            slot.style.position = Position.Absolute;
            slot.style.bottom = Length.Percent(24);
            if (left) slot.style.left = Length.Percent(6);
            else slot.style.right = Length.Percent(6);
            slot.style.opacity = 0.35f;
            return slot;
        }

        void Advance()
        {
            _index++;
            if (_scene == null || _index >= _scene.lines.Count)
            {
                Finish();
                return;
            }
            var line = _scene.lines[_index];
            bool narrator = string.IsNullOrEmpty(line.speaker);
            _speakerLabel.text = narrator ? "— Narration —" : line.speaker;
            _speakerLabel.style.color = narrator ? Theme.TextDim : Theme.Accent;
            _textLabel.text = line.text;
            UiFx.Punch(_textPanel, 1.02f, 120);

            bool left = line.side != "right";
            var active = left ? _leftPortrait : _rightPortrait;
            var inactive = left ? _rightPortrait : _leftPortrait;
            active.style.opacity = 1f;
            inactive.style.opacity = 0.35f;
            active.Clear();
            if (!narrator)
            {
                var card = new VisualElement();
                card.style.width = 120f;
                card.style.height = 150f;
                UiKit.Round(card, Theme.Radius);
                card.style.overflow = Overflow.Hidden;
                UiKit.Border(card, Theme.PanelLine, 2f);
                var artKey = string.IsNullOrEmpty(line.portrait) ? line.speaker : line.portrait;
                card.style.backgroundImage = new StyleBackground(
                    PlaceholderArt.Portrait(artKey, GuessElement(artKey), line.speaker, false));
                var initials = UiKit.Text(PlaceholderArt.Initials(line.speaker), 34f, true, Color.white.WithAlpha(0.9f));
                initials.style.position = Position.Absolute;
                initials.style.left = 0; initials.style.right = 0;
                initials.style.top = Length.Percent(28);
                initials.style.unityTextAlign = TextAnchor.MiddleCenter;
                card.Add(initials);
                var name = UiKit.Text(line.speaker, 12f, true);
                name.style.position = Position.Absolute;
                name.style.bottom = 4f;
                name.style.left = 0; name.style.right = 0;
                name.style.unityTextAlign = TextAnchor.MiddleCenter;
                card.Add(name);
                active.Add(card);
                if (!MotionSettings.ReducedMotion)
                {
                    card.style.translate = new Translate(left ? -30f : 30f, 0f);
                    card.experimental.animation.Start(0f, 1f, 160, (e, t) =>
                        e.style.translate = new Translate((left ? -30f : 30f) * (1f - UiFx.EaseOut(t)), 0f));
                }
            }
        }

        static ElementId GuessElement(string key)
        {
            int hash = 0;
            foreach (char c in key ?? "") hash = hash * 31 + c;
            var values = (ElementId[])Enum.GetValues(typeof(ElementId));
            return values[Mathf.Abs(hash) % values.Length];
        }

        void Finish()
        {
            RemoveFromHierarchy();
            _onFinished?.Invoke();
        }
    }
}
