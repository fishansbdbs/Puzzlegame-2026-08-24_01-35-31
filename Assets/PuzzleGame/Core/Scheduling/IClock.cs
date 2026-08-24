using System;

namespace PuzzleGame.Core.Scheduling
{
    public interface IClock
    {
        DateTimeOffset Now { get; }
    }
}
