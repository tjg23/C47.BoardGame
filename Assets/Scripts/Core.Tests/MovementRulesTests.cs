using System.Linq;
using NUnit.Framework;
using ChungToi.Core;

namespace ChungToi.Core.Tests
{
	[TestFixture]
	public class MovementRulesTests
	{
		private static GameState MovementState(BoardSize size, Player toMove, params (Coord at, Player p, Orientation o)[] pieces)
		{
			var b = new Board(size);
			foreach (var (at, p, o) in pieces)
				b.Set(at, new Cell(p, o));
			return GameState.CreateForTesting(b, toMove, GamePhase.Movement);
		}

		[Test]
		public void RotateInPlace_IsAlwaysLegal()
		{
			var s = MovementState(BoardSize.ThreeByThree, Player.X,
				(new Coord(0, 0), Player.X, Orientation.Cardinal),
				(new Coord(2, 2), Player.X, Orientation.Diagonal),
				(new Coord(0, 2), Player.X, Orientation.Cardinal),
				(new Coord(1, 0), Player.O, Orientation.Cardinal),
				(new Coord(1, 1), Player.O, Orientation.Diagonal),
				(new Coord(2, 0), Player.O, Orientation.Cardinal)
			);

			var moves = Rules.GetLegalMoves(s);
			Assert.IsTrue(moves.Any(m => m.Kind == MoveKind.Rotate && m.From == new Coord(0, 0) && m.NewOrient == Orientation.Diagonal));
			Assert.IsTrue(moves.Any(m => m.Kind == MoveKind.Rotate && m.From == new Coord(2, 2) && m.NewOrient == Orientation.Cardinal));
			// Should not generate rotate moves for opponent pieces.
			Assert.IsFalse(moves.Any(m => m.Kind == MoveKind.Rotate && m.From == new Coord(1, 0)));
		}

		[Test]
		public void CardinalPiece_SlidesOnlyOrthogonally()
		{
			var s = MovementState(BoardSize.ThreeByThree, Player.X,
				(new Coord(1, 1), Player.X, Orientation.Cardinal),
				(new Coord(0, 1), Player.O, Orientation.Cardinal) // blocks N
			);

			var slides = Rules.GetLegalMoves(s)
				.Where(m => m.Kind == MoveKind.Slide && m.From == new Coord(1, 1))
				.Select(m => m.To)
				.Distinct()
				.ToList();

			CollectionAssert.Contains(slides, new Coord(2, 1));
			CollectionAssert.Contains(slides, new Coord(1, 0));
			CollectionAssert.Contains(slides, new Coord(1, 2));
			CollectionAssert.DoesNotContain(slides, new Coord(0, 0));
			CollectionAssert.DoesNotContain(slides, new Coord(2, 2));
			CollectionAssert.DoesNotContain(slides, new Coord(0, 1));
		}

		[Test]
		public void DiagonalPiece_SlidesOnlyDiagonally()
		{
			var s = MovementState(BoardSize.ThreeByThree, Player.X,
				(new Coord(1, 1), Player.X, Orientation.Diagonal),
				(new Coord(2, 0), Player.O, Orientation.Cardinal) // blocks SW
			);

			var slides = Rules.GetLegalMoves(s)
				.Where(m => m.Kind == MoveKind.Slide && m.From == new Coord(1, 1))
				.Select(m => m.To)
				.Distinct()
				.ToList();

			CollectionAssert.Contains(slides, new Coord(0, 0));
			CollectionAssert.Contains(slides, new Coord(0, 2));
			CollectionAssert.Contains(slides, new Coord(2, 2));
			CollectionAssert.DoesNotContain(slides, new Coord(2, 0));
			CollectionAssert.DoesNotContain(slides, new Coord(1, 0));
			CollectionAssert.DoesNotContain(slides, new Coord(0, 1));
		}

		[Test]
		public void Slide_BlockedByIntermediatePieces()
		{
			var s = MovementState(BoardSize.FourByFour, Player.X,
				(new Coord(0, 0), Player.X, Orientation.Cardinal),
				(new Coord(0, 2), Player.O, Orientation.Cardinal)
			);

			var eastDestinations = Rules.GetLegalMoves(s)
				.Where(m => m.Kind == MoveKind.Slide && m.From == new Coord(0, 0) && m.To.Row == 0)
				.Select(m => m.To)
				.Distinct()
				.ToList();

			CollectionAssert.Contains(eastDestinations, new Coord(0, 1));
			CollectionAssert.DoesNotContain(eastDestinations, new Coord(0, 2));
			CollectionAssert.DoesNotContain(eastDestinations, new Coord(0, 3));
		}

		[Test]
		public void CannotLandOnOccupiedSquare()
		{
			var s = MovementState(BoardSize.ThreeByThree, Player.X,
				(new Coord(0, 0), Player.X, Orientation.Cardinal),
				(new Coord(0, 1), Player.O, Orientation.Cardinal),
				(new Coord(1, 0), Player.O, Orientation.Cardinal)
			);

			var slides = Rules.GetLegalMoves(s)
				.Where(m => m.Kind == MoveKind.Slide && m.From == new Coord(0, 0))
				.Select(m => m.To)
				.ToList();

			CollectionAssert.DoesNotContain(slides, new Coord(0, 1));
			CollectionAssert.DoesNotContain(slides, new Coord(1, 0));
		}

		[Test]
		public void Slide_ProducesTwoMovesPerDestination_OneEachOrientation()
		{
			var s = MovementState(BoardSize.ThreeByThree, Player.X,
				(new Coord(0, 0), Player.X, Orientation.Cardinal)
			);

			var slidesTo01 = Rules.GetLegalMoves(s)
				.Where(m => m.Kind == MoveKind.Slide && m.From == new Coord(0, 0) && m.To == new Coord(0, 1))
				.ToList();

			Assert.AreEqual(2, slidesTo01.Count);
			Assert.IsTrue(slidesTo01.Any(m => m.NewOrient == Orientation.Cardinal));
			Assert.IsTrue(slidesTo01.Any(m => m.NewOrient == Orientation.Diagonal));
		}

		[Test]
		public void RotateMove_OnApply_TogglesOrientation_AndSwitchesTurn()
		{
			var s = MovementState(BoardSize.ThreeByThree, Player.X,
				(new Coord(0, 0), Player.X, Orientation.Cardinal),
				(new Coord(2, 2), Player.O, Orientation.Cardinal)
			);
			Rules.Apply(s, Move.Rotate(new Coord(0, 0), Orientation.Diagonal));
			Assert.AreEqual(Orientation.Diagonal, s.Board.Get(new Coord(0, 0)).Orient);
			Assert.AreEqual(Player.X, s.Board.Get(new Coord(0, 0)).Owner);
			Assert.AreEqual(Player.O, s.ToMove);
		}

		[Test]
		public void SlideMove_OnApply_MovesPiece()
		{
			var s = MovementState(BoardSize.ThreeByThree, Player.X,
				(new Coord(0, 0), Player.X, Orientation.Cardinal),
				(new Coord(2, 2), Player.O, Orientation.Cardinal)
			);
			Rules.Apply(s, Move.Slide(new Coord(0, 0), new Coord(0, 2), Orientation.Diagonal));
			Assert.IsTrue(s.Board.IsEmpty(new Coord(0, 0)));
			Assert.AreEqual(new Cell(Player.X, Orientation.Diagonal), s.Board.Get(new Coord(0, 2)));
			Assert.AreEqual(Player.O, s.ToMove);
		}
	}
}
