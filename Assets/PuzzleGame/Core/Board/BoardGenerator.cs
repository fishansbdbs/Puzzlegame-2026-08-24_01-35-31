using System;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Board
{
    public interface IRandomSource
    {
        int Next(int exclusiveMaximum);
    }

    public sealed class BoardGenerator
    {
        private const int DefaultMaximumAttempts = 100;

        private readonly IRandomSource random;
        private readonly int maximumAttempts;

        public BoardGenerator(IRandomSource random, int maximumAttempts = DefaultMaximumAttempts)
        {
            if (random == null)
            {
                throw new ArgumentNullException("random");
            }

            if (maximumAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException("maximumAttempts");
            }

            this.random = random;
            this.maximumAttempts = maximumAttempts;
        }

        public BoardState Generate()
        {
            for (var attempt = 0; attempt < maximumAttempts; attempt++)
            {
                var board = CreateShuffledBoard();
                if (MatchDetector.FindGroups(board).Count == 0)
                {
                    return board;
                }
            }

            throw new InvalidOperationException("Could not generate a board without initial matches.");
        }

        private BoardState CreateShuffledBoard()
        {
            var values = new OrbType[BoardState.Columns * BoardState.Rows];
            var orbTypes = (OrbType[])Enum.GetValues(typeof(OrbType));
            for (var index = 0; index < values.Length; index++)
            {
                values[index] = orbTypes[index % orbTypes.Length];
            }

            for (var index = values.Length - 1; index > 0; index--)
            {
                var otherIndex = random.Next(index + 1);
                if (otherIndex < 0 || otherIndex > index)
                {
                    throw new InvalidOperationException("Random source returned a value outside the requested range.");
                }

                var temporary = values[index];
                values[index] = values[otherIndex];
                values[otherIndex] = temporary;
            }

            var board = new BoardState();
            for (var y = 0; y < BoardState.Rows; y++)
            {
                for (var x = 0; x < BoardState.Columns; x++)
                {
                    board.Set(x, y, values[y * BoardState.Columns + x]);
                }
            }

            return board;
        }
    }
}
