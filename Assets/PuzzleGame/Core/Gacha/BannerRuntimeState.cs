using System;

namespace PuzzleGame.Core.Gacha
{
    public sealed class BannerRuntimeState
    {
        private readonly string bannerId;
        private readonly int stepCount;
        private int nextStepIndex;

        public BannerRuntimeState(string bannerId, int stepCount)
        {
            if (string.IsNullOrWhiteSpace(bannerId)) throw new ArgumentException("Banner ID is required.", "bannerId");
            if (stepCount < 0) throw new ArgumentOutOfRangeException("stepCount");
            this.bannerId = bannerId;
            this.stepCount = stepCount;
        }

        public string BannerId { get { return bannerId; } }
        public int NextStepIndex { get { return nextStepIndex; } }
        public bool IsComplete { get { return nextStepIndex >= stepCount; } }

        internal void AssertMatches(string id)
        {
            if (!string.Equals(bannerId, id, StringComparison.Ordinal)) throw new ArgumentException("Runtime state belongs to a different banner.", "id");
        }

        internal void Advance()
        {
            if (IsComplete) throw new InvalidOperationException("Banner rotation is complete.");
            nextStepIndex++;
        }
    }
}
