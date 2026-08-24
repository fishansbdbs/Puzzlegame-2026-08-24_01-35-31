using System;

namespace PuzzleGame.Core.Board
{
    public struct BoardPosition : IEquatable<BoardPosition>, IComparable<BoardPosition>
    {
        public readonly int X;
        public readonly int Y;

        public BoardPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(BoardPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardPosition && Equals((BoardPosition)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public int CompareTo(BoardPosition other)
        {
            var byRow = Y.CompareTo(other.Y);
            return byRow != 0 ? byRow : X.CompareTo(other.X);
        }

        public static bool operator ==(BoardPosition left, BoardPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoardPosition left, BoardPosition right)
        {
            return !left.Equals(right);
        }
    }
}
