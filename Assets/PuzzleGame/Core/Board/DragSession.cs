using System;

namespace PuzzleGame.Core.Board
{
    public sealed class DragSession
    {
        private readonly BoardState board;
        private readonly float durationSeconds;
        private BoardPosition currentPosition;
        private bool ended;

        public DragSession(BoardState board, BoardPosition start, float durationSeconds = BoardState.DefaultDragDurationSeconds)
        {
            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            board.Get(start);
            if (durationSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException("durationSeconds");
            }

            this.board = board;
            this.durationSeconds = durationSeconds;
            currentPosition = start;
        }

        public bool TryMove(BoardPosition next, float elapsedSeconds)
        {
            if (ended || elapsedSeconds >= durationSeconds)
            {
                return false;
            }

            if (next.X < 0 || next.X >= BoardState.Columns || next.Y < 0 || next.Y >= BoardState.Rows)
            {
                return false;
            }

            var distance = Math.Abs(next.X - currentPosition.X) + Math.Abs(next.Y - currentPosition.Y);
            if (distance != 1)
            {
                return false;
            }

            board.Swap(currentPosition, next);
            currentPosition = next;
            return true;
        }

        public void End()
        {
            ended = true;
        }
    }
}
