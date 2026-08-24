using System;

namespace PuzzleGame.Core.Scheduling
{
    public sealed class SystemLocalClock : IClock
    {
        public DateTimeOffset Now
        {
            get { return DateTimeOffset.Now; }
        }
    }
}
