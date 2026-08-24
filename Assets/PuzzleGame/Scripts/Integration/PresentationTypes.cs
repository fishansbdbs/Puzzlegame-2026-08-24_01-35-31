using System;

namespace PuzzleGame.Presentation
{
    // ---------------------------------------------------------------------
    // Presentation-side enums.
    //
    // These mirror the vocabulary of the master design spec
    // (docs/superpowers/specs/2026-08-24-puzzlegame-design.md).
    // Codex owns the authoritative core-side contracts; when those land,
    // add a small mapping shim in Integration/ rather than editing every
    // presentation call site. Do NOT add gameplay rules here.
    // ---------------------------------------------------------------------

    /// <summary>Playable character elements. Heart is orb-only, never a character element.</summary>
    public enum ElementId
    {
        Fire,
        Water,
        Nature,
        Light,
        Dark
    }

    /// <summary>The six orb colors on the 6x5 board.</summary>
    public enum OrbColor
    {
        Fire,
        Water,
        Nature,
        Light,
        Dark,
        Heart
    }

    /// <summary>
    /// Visual state layered on top of an orb color. Flags so an orb can be
    /// e.g. Poisoned + Selected at once.
    /// </summary>
    [Flags]
    public enum OrbStateFlags
    {
        None = 0,
        Selected = 1 << 0,
        Dragging = 1 << 1,
        Matched = 1 << 2,
        Locked = 1 << 3,
        Poisoned = 1 << 4,
        Blocked = 1 << 5
    }

    /// <summary>Banner archetypes from the design spec.</summary>
    public enum BannerKind
    {
        Standard,
        Featured,
        GatherIn,
        StepUp
    }

    /// <summary>High-level rotating content categories.</summary>
    public enum ScheduledContentKind
    {
        Banner,
        EventChapter,
        MaterialDungeon,
        AwakeningStage,
        ChallengeTower,
        BossRush
    }

    /// <summary>Currencies presentation needs to display.</summary>
    public enum CurrencyId
    {
        Gold,
        Gems,
        Ticket,
        EventToken
    }

    public static class ElementUtil
    {
        public static OrbColor ToOrb(this ElementId element)
        {
            switch (element)
            {
                case ElementId.Fire: return OrbColor.Fire;
                case ElementId.Water: return OrbColor.Water;
                case ElementId.Nature: return OrbColor.Nature;
                case ElementId.Light: return OrbColor.Light;
                default: return OrbColor.Dark;
            }
        }

        /// <summary>Display name for an element.</summary>
        public static string DisplayName(this ElementId element)
        {
            return element.ToString();
        }

        /// <summary>Display name for an orb color.</summary>
        public static string DisplayName(this OrbColor color)
        {
            return color.ToString();
        }
    }
}
