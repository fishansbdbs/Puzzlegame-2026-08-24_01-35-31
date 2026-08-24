using PuzzleGame.Presentation.Content;

namespace PuzzleGame.Presentation.Mock
{
    /// <summary>
    /// Installs the demo mocks for every integration seam that has no real
    /// implementation registered. Core systems (Codex) register the real
    /// sources before UI boots; anything they cover is left untouched.
    /// </summary>
    public static class MockInstaller
    {
        public static void InstallMissing()
        {
            var db = ContentDb.Instance;
            if (!PresentationServices.Has<IContentLibrary>())
            {
                PresentationServices.Register<IContentLibrary>(db);
            }

            MockEconomy economy = null;
            if (!PresentationServices.Has<IEconomySource>())
            {
                economy = new MockEconomy();
                PresentationServices.Register<IEconomySource>(economy);
            }

            MockSchedule schedule = null;
            if (!PresentationServices.Has<IScheduleSource>())
            {
                schedule = new MockSchedule(db);
                PresentationServices.Register<IScheduleSource>(schedule);
            }

            MockRoster roster = null;
            if (!PresentationServices.Has<IRosterSource>() && economy != null)
            {
                roster = new MockRoster(db, economy);
                PresentationServices.Register<IRosterSource>(roster);
            }

            if (!PresentationServices.Has<ISummonSource>() && economy != null && roster != null && schedule != null)
            {
                PresentationServices.Register<ISummonSource>(new MockSummons(db, economy, roster, schedule));
            }
        }
    }
}
