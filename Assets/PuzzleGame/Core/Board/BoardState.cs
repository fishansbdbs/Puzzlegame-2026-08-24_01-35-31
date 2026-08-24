using System;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Board
{
    public sealed class BoardState
    {
        public const int Columns = 6;
        public const int Rows = 5;
        public const int MinimumMatchSize = 3;
        public const float DefaultDragDurationSeconds = 10f;

        private readonly OrbType[] cells;

        public BoardState()
        {
            cells = new OrbType[Columns * Rows];
        }

        private BoardState(OrbType[] cells)
        {
            this.cells = cells;
        }

        public int Width { get { return Columns; } }
        public int Height { get { return Rows; } }

        public OrbType Get(int x, int y)
        {
            return cells[GetIndex(x, y)];
        }

        public OrbType Get(BoardPosition position)
        {
            return Get(position.X, position.Y);
        }

        public void Set(int x, int y, OrbType orbType)
        {
            cells[GetIndex(x, y)] = orbType;
        }

        public void Set(BoardPosition position, OrbType orbType)
        {
            Set(position.X, position.Y, orbType);
        }

        public void Swap(BoardPosition first, BoardPosition second)
        {
            var firstIndex = GetIndex(first.X, first.Y);
            var secondIndex = GetIndex(second.X, second.Y);
            var temporary = cells[firstIndex];
            cells[firstIndex] = cells[secondIndex];
            cells[secondIndex] = temporary;
        }

        public BoardState Clone()
        {
            return new BoardState((OrbType[])cells.Clone());
        }

        internal int GetIndex(int x, int y)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows)
            {
                throw new ArgumentOutOfRangeException("position", "Board position is outside the board.");
            }

            return y * Columns + x;
        }
    }
}
