using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Widget factory for the whole game UI. Everything is built in code so
    /// screens stay reviewable in diffs and there is no GUID wiring; visual
    /// language comes from <see cref="Theme"/>.
    /// </summary>
    public static class UiKit
    {
        // ------------------------------ Layout ----------------------------

        public static VisualElement Row(float gap = 6f)
        {
            var el = new VisualElement();
            el.style.flexDirection = FlexDirection.Row;
            el.style.alignItems = Align.Center;
            SetGap(el, gap);
            return el;
        }

        public static VisualElement Column(float gap = 6f)
        {
            var el = new VisualElement();
            el.style.flexDirection = FlexDirection.Column;
            SetGap(el, gap);
            return el;
        }

        static void SetGap(VisualElement el, float gap)
        {
            // Unity 6 flex gap isn't universally supported; use child margins.
            el.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                bool row = el.resolvedStyle.flexDirection == FlexDirection.Row
                           || el.resolvedStyle.flexDirection == FlexDirection.RowReverse;
                for (int i = 1; i < el.childCount; i++)
                {
                    var child = el[i];
                    if (row) child.style.marginLeft = gap;
                    else child.style.marginTop = gap;
                }
            });
        }

        public static VisualElement Spacer()
        {
            var el = new VisualElement();
            el.style.flexGrow = 1f;
            return el;
        }

        public static VisualElement Panel(bool raised = false)
        {
            var el = new VisualElement();
            el.style.backgroundColor = raised ? Theme.PanelRaised : Theme.Panel;
            Round(el, Theme.Radius);
            Border(el, Theme.PanelLine, 1f);
            el.style.paddingLeft = Theme.Pad;
            el.style.paddingRight = Theme.Pad;
            el.style.paddingTop = Theme.Pad;
            el.style.paddingBottom = Theme.Pad;
            return el;
        }

        public static void Round(VisualElement el, float radius)
        {
            el.style.borderTopLeftRadius = radius;
            el.style.borderTopRightRadius = radius;
            el.style.borderBottomLeftRadius = radius;
            el.style.borderBottomRightRadius = radius;
        }

        public static void Border(VisualElement el, Color color, float width)
        {
            el.style.borderLeftColor = color;
            el.style.borderRightColor = color;
            el.style.borderTopColor = color;
            el.style.borderBottomColor = color;
            el.style.borderLeftWidth = width;
            el.style.borderRightWidth = width;
            el.style.borderTopWidth = width;
            el.style.borderBottomWidth = width;
        }

        public static void Pad(VisualElement el, float pad)
        {
            el.style.paddingLeft = pad;
            el.style.paddingRight = pad;
            el.style.paddingTop = pad;
            el.style.paddingBottom = pad;
        }

        // ------------------------------ Text -------------------------------

        public static Label Text(string text, float size = 14f, bool bold = false, Color? color = null)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = color ?? Theme.TextMain;
            label.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        public static Label Title(string text, float size = 22f)
        {
            return Text(text, size, true);
        }

        public static Label Dim(string text, float size = 12f)
        {
            return Text(text, size, false, Theme.TextDim);
        }

        // ------------------------------ Buttons ----------------------------

        public static Button Button(string text, Action onClick, bool primary = false)
        {
            var btn = new Button(onClick) { text = text };
            btn.style.backgroundColor = primary ? Theme.Accent : Theme.PanelRaised;
            btn.style.color = primary ? Theme.BgDeep : Theme.TextMain;
            btn.style.fontSize = 14f;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            Round(btn, Theme.RadiusSmall);
            Border(btn, primary ? Theme.Accent : Theme.PanelLine, 1f);
            btn.style.paddingLeft = 14f;
            btn.style.paddingRight = 14f;
            btn.style.paddingTop = 7f;
            btn.style.paddingBottom = 7f;
            // Touch-friendly minimum hit target for mobile landscape.
            btn.style.minHeight = 34f;
            btn.style.minWidth = 44f;
            btn.style.marginLeft = 0;
            btn.style.marginRight = 0;
            btn.style.marginTop = 0;
            btn.style.marginBottom = 0;
            var baseColor = primary ? Theme.Accent : Theme.PanelRaised;
            btn.RegisterCallback<PointerEnterEvent>(_ => btn.style.backgroundColor = Color.Lerp(baseColor, Color.white, 0.12f));
            btn.RegisterCallback<PointerLeaveEvent>(_ => btn.style.backgroundColor = baseColor);
            // Press feedback: quick squash so taps read instantly on touch.
            btn.RegisterCallback<PointerDownEvent>(_ =>
            {
                if (!MotionSettings.ReducedMotion) btn.style.scale = new Scale(new Vector2(0.95f, 0.95f));
            }, TrickleDown.TrickleDown);
            btn.RegisterCallback<PointerUpEvent>(_ => btn.style.scale = new Scale(Vector2.one));
            btn.RegisterCallback<PointerLeaveEvent>(_ => btn.style.scale = new Scale(Vector2.one));
            return btn;
        }

        public static Button WarmButton(string text, Action onClick)
        {
            var btn = Button(text, onClick, true);
            btn.style.backgroundColor = Theme.AccentWarm;
            Border(btn, Theme.AccentWarm, 1f);
            return btn;
        }

        // ------------------------------ Badges -----------------------------

        public static VisualElement ElementBadge(ElementId element, float size = 20f)
        {
            var el = new VisualElement();
            el.style.width = size;
            el.style.height = size;
            el.style.backgroundColor = Theme.ElementColor(element);
            Round(el, size / 2f);
            el.style.alignItems = Align.Center;
            el.style.justifyContent = Justify.Center;
            var icon = Text(Theme.ElementIcon(element), size * 0.62f, true, Theme.BgDeep);
            icon.style.marginTop = -1f;
            el.Add(icon);
            el.tooltip = element.DisplayName();
            return el;
        }

        /// <summary>Star row. The 6th (awakened) star renders prismatic-pink and larger.</summary>
        public static VisualElement Stars(int baseRarity, bool awakened, float size = 13f)
        {
            var row = Row(1f);
            int total = awakened ? 6 : baseRarity;
            for (int i = 0; i < total; i++)
            {
                bool sixth = i == 5;
                var star = Text("★", sixth ? size * 1.25f : size, true,
                    sixth ? Theme.Rarity6 : Theme.RarityColor(baseRarity));
                row.Add(star);
            }
            return row;
        }

        // ------------------------------ Bars --------------------------------

        /// <summary>Progress bar; returns the container. Update via <see cref="SetBar"/>.</summary>
        public static VisualElement Bar(Color fillColor, float height = 10f)
        {
            var track = new VisualElement { name = "bar-track" };
            track.style.height = height;
            track.style.backgroundColor = Theme.BgDeep;
            Round(track, height / 2f);
            track.style.overflow = Overflow.Hidden;
            var fill = new VisualElement { name = "bar-fill" };
            fill.style.height = Length.Percent(100);
            fill.style.width = Length.Percent(100);
            fill.style.backgroundColor = fillColor;
            Round(fill, height / 2f);
            track.Add(fill);
            return track;
        }

        public static void SetBar(VisualElement bar, float fraction, Color? color = null)
        {
            var fill = bar.Q("bar-fill");
            if (fill == null) return;
            fill.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100f);
            if (color.HasValue) fill.style.backgroundColor = color.Value;
        }

        // ------------------------------ Portraits ---------------------------

        /// <summary>Framed character portrait; frame color follows current rarity, awakened gets a glow ring.</summary>
        public static VisualElement PortraitCard(CharacterView c, float width = 84f, float height = 104f)
        {
            var card = new VisualElement();
            card.style.width = width;
            card.style.height = height;
            Round(card, Theme.RadiusSmall);
            card.style.overflow = Overflow.Hidden;
            Border(card, Theme.RarityColor(c.CurrentRarity), c.Awakened ? 3f : 2f);
            var tex = PlaceholderArt.Portrait(c.CurrentArtRef, c.Element, c.DisplayName, c.Awakened);
            card.style.backgroundImage = new StyleBackground(tex);

            var initials = Text(PlaceholderArt.Initials(c.DisplayName), width * 0.3f, true, Color.white.WithAlpha(0.85f));
            initials.style.position = Position.Absolute;
            initials.style.left = 0; initials.style.right = 0;
            initials.style.top = Length.Percent(24);
            initials.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(initials);

            var badge = ElementBadge(c.Element, 16f);
            badge.style.position = Position.Absolute;
            badge.style.top = 3f;
            badge.style.left = 3f;
            card.Add(badge);

            var stars = Stars(c.BaseRarity, c.Awakened, width * 0.11f);
            stars.style.position = Position.Absolute;
            stars.style.bottom = 2f;
            stars.style.left = 4f;
            card.Add(stars);
            return card;
        }

        // ------------------------------ Misc --------------------------------

        public static VisualElement CurrencyChip(string icon, string amount, Color color)
        {
            var chip = Row(4f);
            chip.style.backgroundColor = Theme.BgDeep.WithAlpha(0.8f);
            Round(chip, 11f);
            chip.style.paddingLeft = 9f;
            chip.style.paddingRight = 10f;
            chip.style.paddingTop = 3f;
            chip.style.paddingBottom = 3f;
            chip.Add(Text(icon, 13f, true, color));
            var value = Text(amount, 13f, true);
            value.name = "chip-value";
            chip.Add(value);
            return chip;
        }

        public static string FormatNumber(long value)
        {
            if (value >= 100000000) return (value / 1000000f).ToString("0.#") + "M";
            if (value >= 1000000) return (value / 1000000f).ToString("0.##") + "M";
            if (value >= 100000) return (value / 1000f).ToString("0.#") + "K";
            return value.ToString("N0");
        }

        public static string FormatTimeRemaining(TimeSpan span)
        {
            if (span.TotalSeconds <= 0) return "Ended";
            if (span.TotalDays >= 1) return string.Format("{0}d {1}h", (int)span.TotalDays, span.Hours);
            if (span.TotalHours >= 1) return string.Format("{0}h {1}m", (int)span.TotalHours, span.Minutes);
            return string.Format("{0}m {1}s", (int)span.TotalMinutes, span.Seconds);
        }
    }
}
