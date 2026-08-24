using UnityEngine;
using UnityEngine.UIElements;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Player-facing rendering for data-driven stage modifiers
    /// ("start_locks:2", "combo_shield:3", ...). Core interprets the
    /// mechanics; this maps the same strings to readable chips so stage
    /// select and the battle HUD can warn the player up front.
    /// </summary>
    public static class ModifierDisplay
    {
        public static bool TryDescribe(string modifier, out string icon, out string label, out Color color)
        {
            var parts = (modifier ?? "").Split(':');
            string arg = parts.Length > 1 ? parts[1] : "";
            switch (parts[0])
            {
                case "elite":
                    icon = "☆"; label = "Elite encounter"; color = Theme.AccentWarm; return true;
                case "start_locks":
                    icon = "🔒"; label = arg + " orbs start locked"; color = Theme.TextDim; return true;
                case "start_poison":
                    icon = "☠"; label = arg + " orbs start poisoned"; color = Theme.Dark; return true;
                case "start_blockers":
                    icon = "▦"; label = arg + " blockers on board"; color = Theme.TextDim; return true;
                case "move_time_minus":
                    icon = "⏱"; label = "-" + arg + "s move time"; color = Theme.Danger; return true;
                case "combo_shield":
                    icon = "◈"; label = arg + "+ combos required"; color = Theme.Accent; return true;
                case "enemy_haste":
                    icon = "≫"; label = "Hastened enemies"; color = Theme.Danger; return true;
                case "no_heart_orbs":
                    icon = "♡"; label = "No Heart orbs"; color = Theme.Danger; return true;
                case "mono_element_only":
                    icon = "◉"; label = "One element only"; color = Theme.Rarity6; return true;
                case "element_bonus":
                    icon = "▲"; label = Capitalize(arg) + " boosted"; color = Theme.Success; return true;
                case "healing_reduced":
                    icon = "✚"; label = "Healing halved"; color = Theme.Danger; return true;
                default:
                    icon = "?"; label = modifier; color = Theme.TextDim; return false;
            }
        }

        /// <summary>Small chip element for one modifier.</summary>
        public static VisualElement Chip(string modifier, float fontSize = 10f)
        {
            TryDescribe(modifier, out var icon, out var label, out var color);
            var chip = UiKit.Row(3f);
            chip.style.backgroundColor = Theme.BgDeep.WithAlpha(0.85f);
            UiKit.Round(chip, 4f);
            chip.style.paddingLeft = 5f; chip.style.paddingRight = 6f;
            chip.style.paddingTop = 1f; chip.style.paddingBottom = 1f;
            chip.Add(UiKit.Text(icon, fontSize, true, color));
            chip.Add(UiKit.Text(label, fontSize, false, Theme.TextMain));
            return chip;
        }

        static string Capitalize(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
