using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Board
{
    public interface IOrbSource
    {
        OrbType NextOrb();
    }

    public sealed class CascadeLayer
    {
        private readonly IReadOnlyList<MatchGroup> groups;

        internal CascadeLayer(IReadOnlyList<MatchGroup> groups)
        {
            this.groups = groups;
        }

        public IReadOnlyList<MatchGroup> Groups { get { return groups; } }
    }

    public sealed class BoardResolution
    {
        private readonly IReadOnlyList<CascadeLayer> cascadeLayers;

        internal BoardResolution(List<CascadeLayer> cascadeLayers, BoardState finalBoard)
        {
            this.cascadeLayers = cascadeLayers.AsReadOnly();
            FinalBoard = finalBoard;
        }

        public IReadOnlyList<CascadeLayer> CascadeLayers { get { return cascadeLayers; } }
        public BoardState FinalBoard { get; private set; }
    }

    public sealed class BoardResolver
    {
        public const int DefaultMaximumCascadeLayers = 100;

        private readonly int maximumCascadeLayers;

        public BoardResolver(int maximumCascadeLayers = DefaultMaximumCascadeLayers)
        {
            if (maximumCascadeLayers <= 0)
            {
                throw new ArgumentOutOfRangeException("maximumCascadeLayers");
            }

            this.maximumCascadeLayers = maximumCascadeLayers;
        }

        public BoardResolution Resolve(BoardState board, IOrbSource orbSource)
        {
            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            if (orbSource == null)
            {
                throw new ArgumentNullException("orbSource");
            }

            var workingBoard = board.Clone();
            var layers = new List<CascadeLayer>();
            while (true)
            {
                var groups = MatchDetector.FindGroups(workingBoard);
                if (groups.Count == 0)
                {
                    return new BoardResolution(layers, workingBoard);
                }

                if (layers.Count >= maximumCascadeLayers)
                {
                    throw new InvalidOperationException("Board resolution exceeded the maximum cascade layer count.");
                }

                layers.Add(new CascadeLayer(groups));
                ApplyGravityAndRefill(workingBoard, groups, orbSource);
            }
        }

        private static void ApplyGravityAndRefill(BoardState board, IReadOnlyList<MatchGroup> groups, IOrbSource orbSource)
        {
            var cleared = new bool[BoardState.Columns * BoardState.Rows];
            for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                var cells = groups[groupIndex].Cells;
                for (var cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                {
                    cleared[board.GetIndex(cells[cellIndex].X, cells[cellIndex].Y)] = true;
                }
            }

            for (var x = 0; x < BoardState.Columns; x++)
            {
                var nextEmptyRow = 0;
                for (var y = 0; y < BoardState.Rows; y++)
                {
                    if (!cleared[board.GetIndex(x, y)])
                    {
                        board.Set(x, nextEmptyRow, board.Get(x, y));
                        nextEmptyRow++;
                    }
                }

                for (var y = nextEmptyRow; y < BoardState.Rows; y++)
                {
                    var orbType = orbSource.NextOrb();
                    BoardState.ValidateOrbType(orbType, "orbSource");
                    board.Set(x, y, orbType);
                }
            }
        }
    }
}
