using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Board
{
    public sealed class MatchGroup
    {
        private readonly IReadOnlyList<BoardPosition> cells;

        internal MatchGroup(int id, OrbType orbType, List<BoardPosition> cells)
        {
            Id = id;
            OrbType = orbType;
            this.cells = cells.AsReadOnly();
        }

        public int Id { get; private set; }
        public OrbType OrbType { get; private set; }
        public IReadOnlyList<BoardPosition> Cells { get { return cells; } }
    }

    public static class MatchDetector
    {
        public static IReadOnlyList<MatchGroup> FindGroups(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            var runs = new List<Run>();
            CollectHorizontalRuns(board, runs);
            CollectVerticalRuns(board, runs);

            var parents = new int[runs.Count];
            for (var index = 0; index < parents.Length; index++)
            {
                parents[index] = index;
            }

            for (var left = 0; left < runs.Count; left++)
            {
                for (var right = left + 1; right < runs.Count; right++)
                {
                    if (runs[left].OrbType == runs[right].OrbType && Overlaps(runs[left], runs[right]))
                    {
                        Union(parents, left, right);
                    }
                }
            }

            var components = new Dictionary<int, List<BoardPosition>>();
            for (var index = 0; index < runs.Count; index++)
            {
                var root = Find(parents, index);
                List<BoardPosition> cells;
                if (!components.TryGetValue(root, out cells))
                {
                    cells = new List<BoardPosition>();
                    components.Add(root, cells);
                }

                AddUnique(cells, runs[index].Cells);
            }

            var pendingGroups = new List<PendingGroup>();
            foreach (var component in components)
            {
                var cells = component.Value;
                cells.Sort();
                pendingGroups.Add(new PendingGroup(runs[component.Key].OrbType, cells));
            }

            pendingGroups.Sort(delegate(PendingGroup left, PendingGroup right)
            {
                var byPosition = left.Cells[0].CompareTo(right.Cells[0]);
                return byPosition != 0 ? byPosition : left.OrbType.CompareTo(right.OrbType);
            });

            var result = new List<MatchGroup>(pendingGroups.Count);
            for (var index = 0; index < pendingGroups.Count; index++)
            {
                result.Add(new MatchGroup(index, pendingGroups[index].OrbType, pendingGroups[index].Cells));
            }

            return result.AsReadOnly();
        }

        private static void CollectHorizontalRuns(BoardState board, List<Run> runs)
        {
            for (var y = 0; y < BoardState.Rows; y++)
            {
                var x = 0;
                while (x < BoardState.Columns)
                {
                    var orbType = board.Get(x, y);
                    var end = x + 1;
                    while (end < BoardState.Columns && board.Get(end, y) == orbType)
                    {
                        end++;
                    }

                    if (end - x >= BoardState.MinimumMatchSize)
                    {
                        runs.Add(Run.Horizontal(orbType, x, end, y));
                    }

                    x = end;
                }
            }
        }

        private static void CollectVerticalRuns(BoardState board, List<Run> runs)
        {
            for (var x = 0; x < BoardState.Columns; x++)
            {
                var y = 0;
                while (y < BoardState.Rows)
                {
                    var orbType = board.Get(x, y);
                    var end = y + 1;
                    while (end < BoardState.Rows && board.Get(x, end) == orbType)
                    {
                        end++;
                    }

                    if (end - y >= BoardState.MinimumMatchSize)
                    {
                        runs.Add(Run.Vertical(orbType, x, y, end));
                    }

                    y = end;
                }
            }
        }

        private static bool Overlaps(Run left, Run right)
        {
            for (var leftIndex = 0; leftIndex < left.Cells.Count; leftIndex++)
            {
                for (var rightIndex = 0; rightIndex < right.Cells.Count; rightIndex++)
                {
                    if (left.Cells[leftIndex] == right.Cells[rightIndex])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void AddUnique(List<BoardPosition> destination, List<BoardPosition> source)
        {
            for (var index = 0; index < source.Count; index++)
            {
                if (!destination.Contains(source[index]))
                {
                    destination.Add(source[index]);
                }
            }
        }

        private static int Find(int[] parents, int item)
        {
            while (parents[item] != item)
            {
                parents[item] = parents[parents[item]];
                item = parents[item];
            }

            return item;
        }

        private static void Union(int[] parents, int left, int right)
        {
            left = Find(parents, left);
            right = Find(parents, right);
            if (left != right)
            {
                parents[right] = left;
            }
        }

        private sealed class Run
        {
            internal readonly OrbType OrbType;
            internal readonly List<BoardPosition> Cells;

            private Run(OrbType orbType, List<BoardPosition> cells)
            {
                OrbType = orbType;
                Cells = cells;
            }

            internal static Run Horizontal(OrbType orbType, int start, int end, int y)
            {
                var cells = new List<BoardPosition>();
                for (var x = start; x < end; x++)
                {
                    cells.Add(new BoardPosition(x, y));
                }

                return new Run(orbType, cells);
            }

            internal static Run Vertical(OrbType orbType, int x, int start, int end)
            {
                var cells = new List<BoardPosition>();
                for (var y = start; y < end; y++)
                {
                    cells.Add(new BoardPosition(x, y));
                }

                return new Run(orbType, cells);
            }
        }

        private sealed class PendingGroup
        {
            internal readonly OrbType OrbType;
            internal readonly List<BoardPosition> Cells;

            internal PendingGroup(OrbType orbType, List<BoardPosition> cells)
            {
                OrbType = orbType;
                Cells = cells;
            }
        }
    }
}
