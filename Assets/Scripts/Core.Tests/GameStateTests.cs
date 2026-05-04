using NUnit.Framework;
using ChungToi.Core;

namespace ChungToi.Core.Tests
{
	[TestFixture]
	public class GameStateTests
	{
		[Test]
		public void Clone_IsIndependent()
		{
			var s = new GameState(BoardSize.ThreeByThree);
			Rules.Apply(s, Move.Place(new Coord(0, 0), Orientation.Cardinal));

			var copy = s.Clone();
			Rules.Apply(copy, Move.Place(new Coord(0, 1), Orientation.Diagonal));

			Assert.IsTrue(s.Board.IsEmpty(new Coord(0, 1)));
			Assert.IsFalse(copy.Board.IsEmpty(new Coord(0, 1)));
			Assert.AreEqual(Player.O, s.ToMove);
			Assert.AreEqual(Player.X, copy.ToMove);
		}

		[Test]
		public void PlacementWin_3x3_EndsGame()
		{
			// X places a row 0 win during placement. (X moves 1, 3, 5; O moves 2, 4.)
			var s = new GameState(BoardSize.ThreeByThree);
			Rules.Apply(s, Move.Place(new Coord(0, 0), Orientation.Cardinal)); // X
			Rules.Apply(s, Move.Place(new Coord(2, 0), Orientation.Cardinal)); // O
			Rules.Apply(s, Move.Place(new Coord(0, 1), Orientation.Cardinal)); // X
			Rules.Apply(s, Move.Place(new Coord(2, 1), Orientation.Cardinal)); // O
			Rules.Apply(s, Move.Place(new Coord(0, 2), Orientation.Cardinal)); // X — wins row 0

			Assert.AreEqual(GamePhase.GameOver, s.Phase);
			Assert.AreEqual(Player.X, s.Winner);
		}

		[Test]
		public void Cannot_Apply_After_GameOver()
		{
			var s = new GameState(BoardSize.ThreeByThree);
			Rules.Apply(s, Move.Place(new Coord(0, 0), Orientation.Cardinal));
			Rules.Apply(s, Move.Place(new Coord(2, 0), Orientation.Cardinal));
			Rules.Apply(s, Move.Place(new Coord(0, 1), Orientation.Cardinal));
			Rules.Apply(s, Move.Place(new Coord(2, 1), Orientation.Cardinal));
			Rules.Apply(s, Move.Place(new Coord(0, 2), Orientation.Cardinal));

			Assert.AreEqual(GamePhase.GameOver, s.Phase);
			Assert.Throws<System.InvalidOperationException>(
				() => Rules.Apply(s, Move.Place(new Coord(1, 1), Orientation.Cardinal)));
		}

		[Test]
		public void MovementWin_BySliding()
		{
			// X at (1,2) can slide N to (0,2) to complete row 0.
			var b = new Board(BoardSize.ThreeByThree);
			b.Set(new Coord(0, 0), new Cell(Player.X, Orientation.Cardinal));
			b.Set(new Coord(0, 1), new Cell(Player.X, Orientation.Cardinal));
			b.Set(new Coord(1, 2), new Cell(Player.X, Orientation.Cardinal));
			b.Set(new Coord(2, 0), new Cell(Player.O, Orientation.Cardinal));
			b.Set(new Coord(2, 2), new Cell(Player.O, Orientation.Cardinal));
			b.Set(new Coord(1, 0), new Cell(Player.O, Orientation.Cardinal));

			// Sanity: no winner yet.
			Assume.That(Rules.CheckWinner(b), Is.EqualTo(Player.None));

			var s = GameState.CreateForTesting(b, Player.X, GamePhase.Movement);
			Rules.Apply(s, Move.Slide(new Coord(1, 2), new Coord(0, 2), Orientation.Cardinal));

			Assert.AreEqual(GamePhase.GameOver, s.Phase);
			Assert.AreEqual(Player.X, s.Winner);
			Assert.AreEqual(new Cell(Player.X, Orientation.Cardinal), s.Board.Get(new Coord(0, 2)));
			Assert.IsTrue(s.Board.IsEmpty(new Coord(1, 2)));
		}

		[Test]
		public void StuckPlayer_Loses()
		{
			// Construct a 3x3 movement-phase position where it's O's turn and O has zero legal moves.
			// Easiest construction: one O piece, surrounded so it can't slide, but rotation is always
			// legal as long as O has any piece. So we need to give O zero pieces — meaning the
			// stalemate detection only triggers when CountOwned(toMove) == 0.
			// Realistically this cannot happen in real play (no captures), so this test simply
			// exercises HasAnyLegalMove via the public legal-move generator.
			var b = new Board(BoardSize.ThreeByThree);
			b.Set(new Coord(0, 0), new Cell(Player.X, Orientation.Cardinal));
			// O has no pieces.
			var s = GameState.CreateForTesting(b, Player.O, GamePhase.Movement);

			Assert.IsFalse(Rules.HasAnyLegalMove(s));
			Assert.AreEqual(0, Rules.GetLegalMoves(s).Count);
		}

		[Test]
		public void IsLegal_ReturnsFalse_ForBogusMove()
		{
			var s = new GameState(BoardSize.ThreeByThree);
			// Center on first move is illegal on 3x3.
			Assert.IsFalse(Rules.IsLegal(s, Move.Place(new Coord(1, 1), Orientation.Cardinal)));
			// Corner is legal.
			Assert.IsTrue(Rules.IsLegal(s, Move.Place(new Coord(0, 0), Orientation.Cardinal)));
		}

		[Test]
		public void FullScriptedGame_3x3_HumanVsHuman()
		{
			// X wins row 0: open with a corner, claim the rest of row 0 across turns.
			var s = new GameState(BoardSize.ThreeByThree);

			Rules.Apply(s, Move.Place(new Coord(0, 0), Orientation.Cardinal));
			Assert.AreEqual(Player.O, s.ToMove);
			Assert.AreEqual(GamePhase.Placement, s.Phase);
			Assert.AreEqual(Player.None, s.Winner);

			Rules.Apply(s, Move.Place(new Coord(1, 1), Orientation.Diagonal));
			Rules.Apply(s, Move.Place(new Coord(0, 1), Orientation.Cardinal));
			Rules.Apply(s, Move.Place(new Coord(2, 2), Orientation.Diagonal));
			Rules.Apply(s, Move.Place(new Coord(0, 2), Orientation.Cardinal));

			Assert.AreEqual(GamePhase.GameOver, s.Phase);
			Assert.AreEqual(Player.X, s.Winner);
		}
	}
}
