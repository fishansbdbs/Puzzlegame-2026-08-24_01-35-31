using System;

namespace PuzzleGame.Core.Gacha
{
    public sealed class BannerRuntimeState
    {
        private readonly string bannerId;
        private readonly string rotationId;
        private readonly int stepCount;
        private int nextStepIndex;

        public BannerRuntimeState(string bannerId, int stepCount)
            : this(bannerId, bannerId, stepCount, 0)
        {
        }

        public BannerRuntimeState(string bannerId, string rotationId, int stepCount)
            : this(bannerId, rotationId, stepCount, 0)
        {
        }

        public BannerRuntimeState(string bannerId, string rotationId, int stepCount, int nextStepIndex)
        {
            if (string.IsNullOrWhiteSpace(bannerId)) throw new ArgumentException("Banner ID is required.", "bannerId");
            if (string.IsNullOrWhiteSpace(rotationId)) throw new ArgumentException("Rotation ID is required.", "rotationId");
            if (stepCount < 0) throw new ArgumentOutOfRangeException("stepCount");
            if (nextStepIndex < 0 || nextStepIndex > stepCount) throw new ArgumentOutOfRangeException("nextStepIndex");
            this.bannerId = bannerId; this.rotationId = rotationId;
            this.stepCount = stepCount;
            this.nextStepIndex = nextStepIndex;
        }

        public string BannerId { get { return bannerId; } }
        public string RotationId { get { return rotationId; } }
        public int StepCount { get { return stepCount; } }
        public int NextStepIndex { get { return nextStepIndex; } }
        public bool IsComplete { get { return nextStepIndex >= stepCount; } }

        internal void AssertMatches(string id, string rotation, int authoredStepCount)
        {
            if (!string.Equals(bannerId, id, StringComparison.Ordinal)) throw new ArgumentException("Runtime state belongs to a different banner.", "id");
            if (!string.Equals(rotationId, rotation, StringComparison.Ordinal)) throw new ArgumentException("Runtime state belongs to a different rotation.", "rotation");
            if (stepCount != authoredStepCount) throw new ArgumentException("Runtime state step count does not match authored content.", "authoredStepCount");
        }

        internal void Advance()
        {
            if (IsComplete) throw new InvalidOperationException("Banner rotation is complete.");
            nextStepIndex++;
        }
    }
}
