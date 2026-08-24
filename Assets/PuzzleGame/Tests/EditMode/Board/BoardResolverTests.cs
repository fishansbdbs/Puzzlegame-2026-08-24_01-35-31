using NUnit.Framework;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Tests.EditMode.Board
{
    public sealed class BoardResolverTests
    {
        [Test]
        public void Resolver_keeps_two_disconnected_fire_matches_separate()
        {
            var result = new BoardResolver().Resolve(BoardFixtures.TwoFireRows(), new QueueOrbSource(BoardFixtures.Refill));

            Assert.That(result.CascadeLayers[0].Groups.Count, Is.EqualTo(2));
            Assert.That(result.CascadeLayers[0].Groups[0].OrbType, Is.EqualTo(OrbType.Fire));
            Assert.That(result.CascadeLayers[0].Groups[1].OrbType, Is.EqualTo(OrbType.Fire));
        }

        [Test]
        public void Resolver_clears_all_groups_in_a_layer_before_applying_gravity()
        {
            var board = BoardFixtures.TwoFireRows();
            var result = new BoardResolver().Resolve(board, new QueueOrbSource(BoardFixtures.Refill));

            Assert.That(result.CascadeLayers, Has.Count.EqualTo(1));
            Assert.That(result.FinalBoard.Get(0, 4), Is.EqualTo(OrbType.Light));
            Assert.That(result.FinalBoard.Get(3, 4), Is.EqualTo(OrbType.Light));
        }

        [Test]
        public void Resolver_compacts_each_column_toward_row_zero()
        {
            var board = BoardFixtures.Sequential();
            board.Set(0, 0, OrbType.Fire);
            board.Set(0, 1, OrbType.Fire);
            board.Set(0, 2, OrbType.Fire);
            board.Set(0, 3, OrbType.Water);
            var result = new BoardResolver().Resolve(board, new QueueOrbSource(BoardFixtures.Refill));

            Assert.That(result.FinalBoard.Get(0, 0), Is.EqualTo(OrbType.Water));
        }

        [Test]
        public void Resolver_refills_empty_cells_in_column_then_row_order()
        {
            var board = BoardFixtures.Sequential();
            board.Set(0, 0, OrbType.Fire);
            board.Set(0, 1, OrbType.Fire);
            board.Set(0, 2, OrbType.Fire);
            var result = new BoardResolver().Resolve(board, new QueueOrbSource(new[] { OrbType.Light, OrbType.Dark, OrbType.Heart }));

            Assert.That(result.FinalBoard.Get(0, 2), Is.EqualTo(OrbType.Light));
            Assert.That(result.FinalBoard.Get(0, 3), Is.EqualTo(OrbType.Dark));
            Assert.That(result.FinalBoard.Get(0, 4), Is.EqualTo(OrbType.Heart));
        }

        [Test]
        public void Resolver_repeats_until_a_second_cascade_layer_is_stable()
        {
            var board = BoardFixtures.Sequential();
            board.Set(0, 0, OrbType.Fire);
            board.Set(0, 1, OrbType.Fire);
            board.Set(0, 2, OrbType.Fire);
            board.Set(0, 3, OrbType.Water);
            board.Set(1, 0, OrbType.Water);
            board.Set(2, 0, OrbType.Water);

            var result = new BoardResolver().Resolve(board, new QueueOrbSource(BoardFixtures.Refill));

            Assert.That(result.CascadeLayers, Has.Count.EqualTo(2));
            Assert.That(result.CascadeLayers[1].Groups[0].OrbType, Is.EqualTo(OrbType.Water));
            Assert.That(MatchDetector.FindGroups(result.FinalBoard), Is.Empty);
        }
    }

    internal sealed class QueueOrbSource : IOrbSource
    {
        private readonly OrbType[] values;
        private int index;

        internal QueueOrbSource(OrbType[] values)
        {
            this.values = values;
        }

        public OrbType NextOrb()
        {
            var value = values[index % values.Length];
            index++;
            return value;
        }
    }
}
