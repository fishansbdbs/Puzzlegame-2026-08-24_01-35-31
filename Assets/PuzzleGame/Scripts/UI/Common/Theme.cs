using UnityEngine;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Central visual language: element colors, rarity treatments, panel
    /// palette, spacing. All screens and VFX pull colors from here so the
    /// game reads as one system and a future art pass is a single edit.
    /// </summary>
    public static class Theme
    {
        // ------------------------------ Elements --------------------------
        public static readonly Color Fire = FromHex(0xFF5A3C);
        public static readonly Color Water = FromHex(0x3CA8FF);
        public static readonly Color Nature = FromHex(0x43D06A);
        public static readonly Color Light = FromHex(0xFFDD57);
        public static readonly Color Dark = FromHex(0xB066FF);
        public static readonly Color Heart = FromHex(0xFF7EB6);

        // ------------------------------ Rarity ----------------------------
        public static readonly Color Rarity1 = FromHex(0x9AA3AD);   // common gray
        public static readonly Color Rarity2 = FromHex(0x7FBF7F);   // green
        public static readonly Color Rarity3 = FromHex(0x5FA8E8);   // blue
        public static readonly Color Rarity4 = FromHex(0xC98BF0);   // purple
        public static readonly Color Rarity5 = FromHex(0xFFC24B);   // gold
        public static readonly Color Rarity6 = FromHex(0xFF8BE8);   // awakened prismatic pink

        // ------------------------------ Chrome -----------------------------
        public static readonly Color Bg = FromHex(0x171A26);
        public static readonly Color BgDeep = FromHex(0x0F1119);
        public static readonly Color Panel = FromHex(0x232838);
        public static readonly Color PanelRaised = FromHex(0x2C3247);
        public static readonly Color PanelLine = FromHex(0x3B4360);
        public static readonly Color TextMain = FromHex(0xF2F4FA);
        public static readonly Color TextDim = FromHex(0xA7AFC4);
        public static readonly Color Accent = FromHex(0x53E0C4);    // mint accent
        public static readonly Color AccentWarm = FromHex(0xFFB25A);
        public static readonly Color Danger = FromHex(0xFF5470);
        public static readonly Color Success = FromHex(0x5AE07E);
        public static readonly Color HpBar = FromHex(0x5AE07E);
        public static readonly Color HpBarLow = FromHex(0xFF5470);
        public static readonly Color EnemyHp = FromHex(0xFF6A5A);
        public static readonly Color TimerBar = FromHex(0x53C7E0);
        public static readonly Color GoldColor = FromHex(0xFFD873);
        public static readonly Color GemColor = FromHex(0x7FE8FF);

        // ------------------------------ Metrics ----------------------------
        public const float Radius = 10f;
        public const float RadiusSmall = 6f;
        public const float Pad = 10f;

        public static Color ElementColor(ElementId element)
        {
            switch (element)
            {
                case ElementId.Fire: return Fire;
                case ElementId.Water: return Water;
                case ElementId.Nature: return Nature;
                case ElementId.Light: return Light;
                default: return Dark;
            }
        }

        public static Color OrbColorOf(OrbColor color)
        {
            switch (color)
            {
                case OrbColor.Fire: return Fire;
                case OrbColor.Water: return Water;
                case OrbColor.Nature: return Nature;
                case OrbColor.Light: return Light;
                case OrbColor.Dark: return Dark;
                default: return Heart;
            }
        }

        public static Color RarityColor(int stars)
        {
            switch (stars)
            {
                case 1: return Rarity1;
                case 2: return Rarity2;
                case 3: return Rarity3;
                case 4: return Rarity4;
                case 5: return Rarity5;
                default: return Rarity6;
            }
        }

        public static string ElementIcon(ElementId element)
        {
            switch (element)
            {
                case ElementId.Fire: return "♨";   // hot springs ~ flame
                case ElementId.Water: return "☔";
                case ElementId.Nature: return "⚘";
                case ElementId.Light: return "☀";
                default: return "☽";
            }
        }

        public static Color FromHex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                1f);
        }

        public static Color WithAlpha(this Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
