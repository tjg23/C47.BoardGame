using System.Linq;
using NUnit.Framework;
using ChungToi.Core;

namespace ChungToi.Core.Tests
{
	[TestFixture]
	public class PlacementRulesTests
	{
		[Test]
		public void Initial3x3_LegalMoves_ExcludeCenter()
		{
			var s = new GameState(BoardSize.ThreeByThree);
			var moves = Rules.GetLegalMoves(s);
			// 8 empty non-center cells × 2 orientations
			Assert.AreEqual(16, moves.Count);
			Assert.IsFalse(moves.Any(m => m.From == new Coord(1, 1)));
		}

		[Test]
		public void Initial4x4_LegalMoves_AllowAllCells()
		{
			var s = new GameState(BoardSize.FourByFour);
			var moves = Rules.GetLegalMoves(s);
			// 16 cells × 2 orientations
			Assert.AreEqual(32, moves.Count);
		}

		[Test]
		public void AfterFirstMove3x3_CenterBecomesLegal()
		{
			var s = new GameState(BoardSize.ThreeByThree);
			Rules.Apply(s, Move.Place(new Coord(0, 0), Orientation.Cardinal));
			var moves = Rules.GetLegalMoves(s);
			Assert.IsTrue(moves.Any(m => m.From == new Coord(1, 1)));
		}

		[Test]
		public void OccupiedCellsRejected()
		{
			var s = new GameState(BoardSize.ThreeByThree);
			Rules.Apply(s, Move.Place(new Coord(0, 0), Orientation.Cardinal));
			var moves = Rules.GetLegalMoves(s);
			Assert.IsFalse(moves.Any(m => m.From == new Coord(0, 0)));
		}

		[Test]
		public void Place_DecrementsCorrectCounter_AndSwitchesTurn()
		{
			var s = new GameState(BoardSize.ThreeByThree);
			Assert.AreEqual(Player.X, s.ToMove);
			Assert.AreEqual(3, s.PiecesToPlace(Player.X));

			Rules.Apply(s, Move.Place(new Coord(0, 0), Orientation.Cardinal));
			Assert.AreEqual(Player.O, s.ToMove);
			Assert.AreEqual(2, s.PiecesToPlace(Player.X));
			Assert.AreEqual(3, s.PiecesToPlace(Player.O));
		}

		[Test]
		public void TransitionToMovementPhase_AfterAllPlaced_3x3()
		{
			var s = new GameState(BoardSize.ThreeByThree);
			// Six placements that don't form a winning line.
			Rules.Apply(s, Move.Place(new Coord(0, 0), Orientation.Cardinal)); // X
			Rules.Apply(s, Move.Place(new Coord(0, 1), Orientation.Cardinal)); // O
			Rules.Apply(s, Move.Place(new Coord(0, 2), Orientation.Cardinal)); // X
			Rules.Apply(s, Move.Place(new Coord(1, 0), Orientation.Cardinal)); // O
			Rules.Apply(s, Move.Place(new Coord(1, 2), Orientation.Cardinal)); // X
			Rules.Apply(s, Move.Place(new Coord(2, 0), Orientation.Cardinal)); // O
			Assert.AreEqual(GamePhase.Movement, s.Phase);
			Assert.AreEqual(0, s.PiecesToPlace(Player.X));
			Assert.AreEqual(0, s.PiecesToPlace(Player.O));
		}

		[Test]
		public void TransitionToMovementPhase_AfterAllPlaced_4x4()
		{
			var s = new GameState(BoardSize.FourByFour);
			var seq = new[]
			{
				new Coord(0, 0), new Coord(0, 1), // X, O
                new Coord(0, 2), new Coord(0, 3), // X, O
                new Coord(1, 1), new Coord(1, 0), // X, O
                new Coord(2, 2), new Coord(1, 2), // X, O
                new Coord(2, 1), new Coord(1, 3), // X, O
            };
			foreach (var c in seq)
				Rules.Apply(s, Move.Place(c, Orientation.Cardinal));
			Assert.AreEqual(GamePhase.Movement, s.Phase);
			Assert.AreEqual(0, s.PiecesToPlace(Player.X));
			Assert.AreEqual(0, s.PiecesToPlace(Player.O));
		}
	}
}
