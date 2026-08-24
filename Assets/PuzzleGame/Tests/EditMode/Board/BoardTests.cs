using System;
using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Tests.EditMode.Board
{
    public sealed class BoardTests
    {
        [Test]
        public void Board_has_the_canonical_six_by_five_dimensions()
        {
            var board = new BoardState();

            Assert.That(board.Width, Is.EqualTo(6));
            Assert.That(board.Height, Is.EqualTo(5));
            Assert.That(BoardState.MinimumMatchSize, Is.EqualTo(3));
            Assert.That(BoardState.DefaultDragDurationSeconds, Is.EqualTo(10f));
        }

        [Test]
        public void Board_rejects_positions_outside_its_dimensions()
        {
            var board = new BoardState();

            Assert.That(() => board.Get(-1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => board.Set(6, 0, OrbType.Fire), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => board.Swap(new BoardPosition(0, 0), new BoardPosition(0, 5)), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Clone_has_independent_orb_storage()
        {
            var board = BoardFixtures.Sequential();
            var clone = board.Clone();

            clone.Set(0, 0, OrbType.Dark);

            Assert.That(board.Get(0, 0), Is.EqualTo(OrbType.Fire));
            Assert.That(clone.Get(0, 0), Is.EqualTo(OrbType.Dark));
        }

        [Test]
        public void Crossing_adjacent_cells_swaps_the_dragged_orb()
        {
            var board = BoardFixtures.Sequential();
            var drag = new DragSession(board, new BoardPosition(0, 0), 10f);

            Assert.That(drag.TryMove(new BoardPosition(1, 0), 0.25f), Is.True);
            Assert.That(board.Get(1, 0), Is.EqualTo(OrbType.Fire));
        }

        [Test]
        public void Drag_rejects_diagonal_and_non_adjacent_moves()
        {
            var board = BoardFixtures.Sequential();
            var drag = new DragSession(board, new BoardPosition(0, 0), 10f);

            Assert.That(drag.TryMove(new BoardPosition(1, 1), 0.25f), Is.False);
            Assert.That(drag.TryMove(new BoardPosition(2, 0), 0.25f), Is.False);
            Assert.That(board.Get(0, 0), Is.EqualTo(OrbType.Fire));
        }

        [Test]
        public void Drag_rejects_a_move_at_its_duration_boundary()
        {
            var board = BoardFixtures.Sequential();
            var drag = new DragSession(board, new BoardPosition(0, 0), 10f);

            Assert.That(drag.TryMove(new BoardPosition(1, 0), 10f), Is.False);
            Assert.That(board.Get(0, 0), Is.EqualTo(OrbType.Fire));
        }

        [Test]
        public void Ending_a_drag_is_idempotent_and_prevents_later_moves()
        {
            var board = BoardFixtures.Sequential();
            var drag = new DragSession(board, new BoardPosition(0, 0), 10f);

            drag.End();
            drag.End();

            Assert.That(drag.TryMove(new BoardPosition(1, 0), 0f), Is.False);
            Assert.That(board.Get(0, 0), Is.EqualTo(OrbType.Fire));
        }

        [Test]
        public void Intersecting_horizontal_and_vertical_runs_form_one_group()
        {
            var groups = MatchDetector.FindGroups(BoardFixtures.FireCross());

            Assert.That(groups, Has.Count.EqualTo(1));
            Assert.That(groups[0].OrbType, Is.EqualTo(OrbType.Fire));
            Assert.That(groups[0].Cells, Has.Count.EqualTo(5));
        }

        [Test]
        public void Disconnected_runs_of_one_type_remain_separate_in_stable_order()
        {
            var groups = MatchDetector.FindGroups(BoardFixtures.TwoFireRows());

            Assert.That(groups, Has.Count.EqualTo(2));
            Assert.That(groups[0].Id, Is.EqualTo(0));
            Assert.That(groups[1].Id, Is.EqualTo(1));
            Assert.That(groups[0].Cells[0], Is.EqualTo(new BoardPosition(0, 0)));
            Assert.That(groups[1].Cells[0], Is.EqualTo(new BoardPosition(3, 4)));
        }

        [Test]
        public void Generator_creates_a_board_without_initial_matches()
        {
            var board = new BoardGenerator(new CyclingRandomSource()).Generate();

            Assert.That(MatchDetector.FindGroups(board), Is.Empty);
        }

        [Test]
        public void Generator_stops_after_its_configured_number_of_failed_attempts()
        {
            var generator = new BoardGenerator(new ConstantRandomSource(0), 1);

            Assert.That(() => generator.Generate(), Throws.TypeOf<InvalidOperationException>());
        }
    }

    internal static class BoardFixtures
    {
        internal static readonly OrbType[] Refill =
        {
            OrbType.Light, OrbType.Dark, OrbType.Heart,
            OrbType.Light, OrbType.Dark, OrbType.Heart,
            OrbType.Light, OrbType.Dark, OrbType.Heart
        };

        internal static BoardState Sequential()
        {
            return Create((x, y) => (OrbType)((x + y) % 6));
        }

        internal static BoardState FireCross()
        {
            var board = Create((x, y) => (OrbType)((x + y) % 6));
            board.Set(1, 2, OrbType.Fire);
            board.Set(2, 2, OrbType.Fire);
            board.Set(3, 2, OrbType.Fire);
            board.Set(2, 1, OrbType.Fire);
            board.Set(2, 3, OrbType.Fire);
            board.Set(4, 2, OrbType.Water);
            board.Set(2, 4, OrbType.Water);
            return board;
        }

        internal static BoardState TwoFireRows()
        {
            var board = Create((x, y) => (OrbType)((x + y + 1) % 6));
            for (var x = 0; x < 3; x++)
            {
                board.Set(x, 0, OrbType.Fire);
            }

            for (var x = 3; x < 6; x++)
            {
                board.Set(x, 4, OrbType.Fire);
            }

            return board;
        }

        internal static BoardState Create(Func<int, int, OrbType> valueAt)
        {
            var board = new BoardState();
            for (var y = 0; y < board.Height; y++)
            {
                for (var x = 0; x < board.Width; x++)
                {
                    board.Set(x, y, valueAt(x, y));
                }
            }

            return board;
        }
    }

    internal sealed class ConstantRandomSource : IRandomSource
    {
        private readonly int value;

        internal ConstantRandomSource(int value)
        {
            this.value = value;
        }

        public int Next(int exclusiveMaximum)
        {
            return value;
        }
    }

    internal sealed class CyclingRandomSource : IRandomSource
    {
        private int next;

        public int Next(int exclusiveMaximum)
        {
            var value = next % exclusiveMaximum;
            next++;
            return value;
        }
    }
}
