namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Single source of truth for user-facing game naming.
    /// The final game name is undecided; every screen must read the title
    /// from here so renaming is a one-line change.
    /// </summary>
    public static class GameInfo
    {
        public const string WorkingTitle = "PuzzleGame";

        /// <summary>User-facing title. Swap this when the final name lands.</summary>
        public static string Title => WorkingTitle;
    }
}
