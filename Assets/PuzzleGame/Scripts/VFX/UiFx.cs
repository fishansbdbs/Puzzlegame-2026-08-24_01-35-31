using UnityEngine;
using UnityEngine.UIElements;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Presentation.VFX
{
    /// <summary>
    /// UI-Toolkit-native juice: shakes, punches, flashes, floating damage
    /// numbers, combo popups and telegraph banners. Everything respects
    /// <see cref="MotionSettings"/> for reduced-motion play.
    /// </summary>
    public static class UiFx
    {
        // ------------------------------ Shakes -----------------------------

        /// <summary>Positional shake on any element (screen shake = shake the screen root).</summary>
        public static void Shake(VisualElement el, float magnitude = 6f, int durationMs = 260)
        {
            float juice = MotionSettings.Juice;
            if (juice <= 0f || el == null) return;
            float mag = magnitude * juice;
            el.experimental.animation.Start(0f, 1f, durationMs, (e, t) =>
            {
                float decay = 1f - t;
                float x = (Mathf.PerlinNoise(t * 30f, 0.3f) - 0.5f) * 2f * mag * decay;
                float y = (Mathf.PerlinNoise(0.7f, t * 30f) - 0.5f) * 2f * mag * decay;
                e.style.translate = new Translate(x, y);
            }).OnCompleted(() => el.style.translate = new Translate(0, 0));
        }

        /// <summary>Quick scale punch for hits, button feedback, reveals.</summary>
        public static void Punch(VisualElement el, float scaleUp = 1.15f, int durationMs = 180)
        {
            if (el == null) return;
            if (MotionSettings.ReducedMotion)
            {
                el.style.scale = new Scale(Vector2.one);
                return;
            }
            el.experimental.animation.Start(0f, 1f, durationMs, (e, t) =>
            {
                float s = t < 0.35f
                    ? Mathf.Lerp(1f, scaleUp, t / 0.35f)
                    : Mathf.Lerp(scaleUp, 1f, (t - 0.35f) / 0.65f);
                e.style.scale = new Scale(new Vector2(s, s));
            }).OnCompleted(() => el.style.scale = new Scale(Vector2.one));
        }

        /// <summary>Full-element color flash overlay (elemental hit tint, heal glow...).</summary>
        public static void Flash(VisualElement host, Color color, float maxAlpha = 0.35f, int durationMs = 220)
        {
            float juice = MotionSettings.Juice;
            if (host == null) return;
            float alpha = maxAlpha * Mathf.Max(0.25f, juice);
            var overlay = new VisualElement();
            overlay.pickingMode = PickingMode.Ignore;
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0; overlay.style.right = 0;
            overlay.style.top = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = color.WithAlpha(alpha);
            host.Add(overlay);
            overlay.experimental.animation.Start(alpha, 0f, MotionSettings.Ms(durationMs),
                (e, v) => e.style.backgroundColor = color.WithAlpha(v))
                .OnCompleted(() => overlay.RemoveFromHierarchy());
        }

        // -------------------------- Damage numbers -------------------------

        /// <summary>
        /// Floating combat number. Spawn inside a full-screen absolute layer;
        /// position is in the layer's local space.
        /// </summary>
        public static void DamageNumber(VisualElement layer, Vector2 position, long amount,
            Color color, bool critical = false, bool effective = false, bool resisted = false)
        {
            if (layer == null) return;
            string text = UiKit.FormatNumber(amount);
            float size = critical ? 30f : 21f;
            if (effective) size += 3f;
            var label = UiKit.Text(text, size, true, color);
            if (critical) label.text += "!";
            if (resisted) label.style.color = Theme.TextDim;
            label.pickingMode = PickingMode.Ignore;
            label.style.position = Position.Absolute;
            // Small horizontal scatter so rapid multi-hits stay readable.
            float jitterX = Mathf.PerlinNoise(Time.realtimeSinceStartup * 13.7f, 0.5f) * 44f - 22f;
            label.style.left = position.x + jitterX;
            label.style.top = position.y;
            label.style.unityTextOutlineWidth = 1.5f;
            label.style.unityTextOutlineColor = Theme.BgDeep;
            layer.Add(label);

            int dur = MotionSettings.Ms(critical ? 800 : 620);
            float rise = critical ? 70f : 52f;
            label.experimental.animation.Start(0f, 1f, dur, (e, t) =>
            {
                e.style.translate = new Translate(0f, -rise * EaseOut(t));
                e.style.opacity = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                if (t < 0.2f)
                {
                    float s = Mathf.Lerp(1.5f, 1f, t / 0.2f);
                    e.style.scale = new Scale(new Vector2(s, s));
                }
            }).OnCompleted(() => label.RemoveFromHierarchy());
        }

        /// <summary>Green healing number with a softer float.</summary>
        public static void HealNumber(VisualElement layer, Vector2 position, long amount)
        {
            DamageNumber(layer, position, amount, Theme.Success);
        }

        // ----------------------------- Popups -------------------------------

        /// <summary>Combo counter pop: "COMBO x4". Escalates color as the count grows.</summary>
        public static void ComboPopup(VisualElement layer, Vector2 position, int combo)
        {
            if (layer == null || combo < 2) return;
            Color c = combo >= 7 ? Theme.Rarity6 : combo >= 5 ? Theme.AccentWarm : Theme.Accent;
            var label = UiKit.Text("COMBO ×" + combo, 24f + Mathf.Min(combo, 10) * 1.4f, true, c);
            label.pickingMode = PickingMode.Ignore;
            label.style.position = Position.Absolute;
            label.style.left = position.x;
            label.style.top = position.y;
            label.style.unityTextOutlineWidth = 2f;
            label.style.unityTextOutlineColor = Theme.BgDeep;
            layer.Add(label);
            int dur = MotionSettings.Ms(520);
            label.experimental.animation.Start(0f, 1f, dur, (e, t) =>
            {
                float s = t < 0.25f ? Mathf.Lerp(0.4f, 1.15f, t / 0.25f) : Mathf.Lerp(1.15f, 1f, (t - 0.25f) / 0.75f);
                e.style.scale = new Scale(new Vector2(s, s));
                e.style.opacity = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
            }).OnCompleted(() => label.RemoveFromHierarchy());
        }

        /// <summary>Boss telegraph / enemy action banner sliding across the top of the battle area.</summary>
        public static void TelegraphBanner(VisualElement layer, string title, string detail, Color accent)
        {
            if (layer == null) return;
            var banner = UiKit.Row(8f);
            banner.pickingMode = PickingMode.Ignore;
            banner.style.position = Position.Absolute;
            banner.style.top = Length.Percent(18);
            banner.style.left = 0;
            banner.style.right = 0;
            banner.style.justifyContent = Justify.Center;
            var inner = UiKit.Row(10f);
            inner.style.backgroundColor = Theme.BgDeep.WithAlpha(0.92f);
            UiKit.Round(inner, 8f);
            UiKit.Border(inner, accent, 2f);
            UiKit.Pad(inner, 10f);
            inner.Add(UiKit.Text("⚠", 18f, true, accent));
            var col = UiKit.Column(1f);
            col.Add(UiKit.Text(title, 16f, true, accent));
            if (!string.IsNullOrEmpty(detail)) col.Add(UiKit.Dim(detail, 12f));
            inner.Add(col);
            banner.Add(inner);
            layer.Add(banner);

            int dur = MotionSettings.Ms(1600);
            banner.experimental.animation.Start(0f, 1f, dur, (e, t) =>
            {
                if (t < 0.12f)
                {
                    e.style.opacity = t / 0.12f;
                    e.style.translate = new Translate(0f, Mathf.Lerp(-24f, 0f, EaseOut(t / 0.12f)));
                }
                else if (t > 0.82f)
                {
                    e.style.opacity = 1f - (t - 0.82f) / 0.18f;
                }
                else
                {
                    e.style.opacity = 1f;
                }
            }).OnCompleted(() => banner.RemoveFromHierarchy());
        }

        /// <summary>
        /// Near-full-screen skill cut-in: element-tinted band with the
        /// character portrait and skill name sweeping across.
        /// </summary>
        public static void SkillCutIn(VisualElement layer, CharacterView caster, string skillName)
        {
            if (layer == null) return;
            var band = new VisualElement();
            band.pickingMode = PickingMode.Ignore;
            band.style.position = Position.Absolute;
            band.style.left = 0; band.style.right = 0;
            band.style.top = Length.Percent(30);
            band.style.height = Length.Percent(34);
            var ec = Theme.ElementColor(caster.Element);
            band.style.backgroundColor = Theme.BgDeep.WithAlpha(0.88f);
            band.style.borderTopColor = ec; band.style.borderBottomColor = ec;
            band.style.borderTopWidth = 3f; band.style.borderBottomWidth = 3f;
            band.style.flexDirection = FlexDirection.Row;
            band.style.alignItems = Align.Center;
            band.style.justifyContent = Justify.Center;

            var content = UiKit.Row(16f);
            content.Add(UiKit.PortraitCard(caster, 92f, 114f));
            var col = UiKit.Column(4f);
            col.Add(UiKit.Text(caster.DisplayName, 18f, true, ec));
            col.Add(UiKit.Text(skillName, 30f, true));
            content.Add(col);
            band.Add(content);
            layer.Add(band);

            int dur = MotionSettings.Ms(1100);
            band.experimental.animation.Start(0f, 1f, dur, (e, t) =>
            {
                if (t < 0.18f)
                {
                    float k = EaseOut(t / 0.18f);
                    e.style.translate = new Translate(Mathf.Lerp(-e.resolvedStyle.width, 0f, k), 0f);
                    e.style.opacity = 1f;
                }
                else if (t > 0.8f)
                {
                    float k = (t - 0.8f) / 0.2f;
                    e.style.translate = new Translate(Mathf.Lerp(0f, e.resolvedStyle.width * 0.6f, k * k), 0f);
                    e.style.opacity = 1f - k;
                }
                else
                {
                    e.style.translate = new Translate(0f, 0f);
                }
            }).OnCompleted(() => band.RemoveFromHierarchy());
        }

        /// <summary>Radial particle-ish burst made of small UI squares, tinted per element.</summary>
        public static void Burst(VisualElement layer, Vector2 position, Color color, int count = 10, float distance = 46f)
        {
            float juice = MotionSettings.Juice;
            if (layer == null || juice <= 0f) return;
            count = Mathf.Max(4, Mathf.RoundToInt(count * juice));
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + Mathf.PerlinNoise(i * 0.37f, 0.1f);
                float dist = distance * (0.7f + Mathf.PerlinNoise(0.2f, i * 0.53f) * 0.6f);
                var dot = new VisualElement();
                dot.pickingMode = PickingMode.Ignore;
                float size = 5f + (i % 3) * 2.5f;
                dot.style.width = size;
                dot.style.height = size;
                UiKit.Round(dot, size / 2f);
                dot.style.backgroundColor = i % 4 == 0 ? Color.Lerp(color, Color.white, 0.5f) : color;
                dot.style.position = Position.Absolute;
                dot.style.left = position.x;
                dot.style.top = position.y;
                layer.Add(dot);
                Vector2 target = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
                dot.experimental.animation.Start(0f, 1f, MotionSettings.Ms(420), (e, t) =>
                {
                    float k = EaseOut(t);
                    e.style.translate = new Translate(target.x * k, target.y * k);
                    e.style.opacity = 1f - t * t;
                }).OnCompleted(() => dot.RemoveFromHierarchy());
            }
        }

        public static float EaseOut(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        public static float EaseInOut(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }
    }
}
