using PuzzleGame.Core.Board;
using UnityEngine;

namespace PuzzleGame.Unity.Battle
{
    public static class BoardLayout
    {
        public static BoardPosition? ScreenToCell(Vector2 screenPosition, Rect boardRect)
        {
            if (!IsFinite(screenPosition.x) || !IsFinite(screenPosition.y) ||
                !IsFinite(boardRect.x) || !IsFinite(boardRect.y) ||
                !IsFinite(boardRect.width) || !IsFinite(boardRect.height) ||
                boardRect.width <= 0f || boardRect.height <= 0f)
                return null;

            if (screenPosition.x < boardRect.xMin || screenPosition.x > boardRect.xMax ||
                screenPosition.y < boardRect.yMin || screenPosition.y > boardRect.yMax)
                return null;

            var normalizedX = (screenPosition.x - boardRect.xMin) / boardRect.width;
            var normalizedY = (screenPosition.y - boardRect.yMin) / boardRect.height;
            var x = normalizedX >= 1f ? BoardState.Columns - 1 : Mathf.FloorToInt(normalizedX * BoardState.Columns);
            var y = normalizedY >= 1f ? BoardState.Rows - 1 : Mathf.FloorToInt(normalizedY * BoardState.Rows);
            return new BoardPosition(x, y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
