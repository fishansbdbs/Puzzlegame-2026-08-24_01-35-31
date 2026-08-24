using System;
using System.Collections.Generic;

namespace PuzzleGame.Core.Gacha
{
    public interface IRandomSource
    {
        int NextInt(int exclusiveUpperBound);
    }

    internal static class GachaRandom
    {
        internal static int SelectIndex(IReadOnlyList<long> weights, IRandomSource random)
        {
            if (weights == null) throw new ArgumentNullException("weights");
            if (random == null) throw new ArgumentNullException("random");
            long total = 0;
            for (var index = 0; index < weights.Count; index++)
            {
                if (weights[index] <= 0) throw new ArgumentException("Weights must be positive.", "weights");
                try { total = checked(total + weights[index]); }
                catch (OverflowException) { throw new ArgumentException("Total weight overflows Int64.", "weights"); }
            }
            if (total > int.MaxValue) throw new ArgumentException("Total weight exceeds deterministic random range.", "weights");
            var roll = random.NextInt((int)total);
            if (roll < 0 || roll >= total) throw new InvalidOperationException("Random source returned a value outside the requested range.");
            long cumulative = 0;
            for (var index = 0; index < weights.Count; index++)
            {
                cumulative += weights[index];
                if (roll < cumulative) return index;
            }
            throw new InvalidOperationException("Weighted selection failed unexpectedly.");
        }
    }
}
