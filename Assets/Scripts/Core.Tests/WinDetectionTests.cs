using NUnit.Framework;
using ChungToi.Core;

namespace ChungToi.Core.Tests
{
	[TestFixture]
	public class WinDetectionTests
	{
		private static Board MakeBoard(BoardSize size, params (Coord at, Player p)[] pieces)
		{
			var b = new Board(size);
			foreach (var (at, p) in pieces)
				b.Set(at, new Cell(p, Orientation.Cardinal));
			return b;
		}

		[Test]
		public void EmptyBoard_NoWinner()
		{
			Assert.AreEqual(Player.None, Rules.CheckWinner(new Board(BoardSize.ThreeByThree)));
			Assert.AreEqual(Player.None, Rules.CheckWinner(new Board(BoardSize.FourByFour)));
		}

		[Test]
		public void Row_3x3_AllPlayers()
		{
			for (int r = 0; r < 3; r++)
			{
				var b = MakeBoard(BoardSize.ThreeByThree,
					(new Coord(r, 0), Player.X), (new Coord(r, 1), Player.X), (new Coord(r, 2), Player.X));
				Assert.AreEqual(Player.X, Rules.CheckWinner(b), $"Row {r} X");
			}
		}

		[Test]
		public void Col_3x3()
		{
			for (int c = 0; c < 3; c++)
			{
				var b = MakeBoard(BoardSize.ThreeByThree,
					(new Coord(0, c), Player.O), (new Coord(1, c), Player.O), (new Coord(2, c), Player.O));
				Assert.AreEqual(Player.O, Rules.CheckWinner(b), $"Col {c} O");
			}
		}

		[Test]
		public void MainDiagonals_3x3()
		{
			var b1 = MakeBoard(BoardSize.ThreeByThree,
				(new Coord(0, 0), Player.X), (new Coord(1, 1), Player.X), (new Coord(2, 2), Player.X));
			Assert.AreEqual(Player.X, Rules.CheckWinner(b1));

			var b2 = MakeBoard(BoardSize.ThreeByThree,
				(new Coord(0, 2), Player.O), (new Coord(1, 1), Player.O), (new Coord(2, 0), Player.O));
			Assert.AreEqual(Player.O, Rules.CheckWinner(b2));
		}

		[Test]
		public void Row_4x4_RequiresFullLength()
		{
			// Three in a row is NOT a win on 4x4 — we agreed on 4-in-a-row.
			var partial = MakeBoard(BoardSize.FourByFour,
				(new Coord(0, 0), Player.X), (new Coord(0, 1), Player.X), (new Coord(0, 2), Player.X));
			Assert.AreEqual(Player.None, Rules.CheckWinner(partial));

			var full = MakeBoard(BoardSize.FourByFour,
				(new Coord(0, 0), Player.X), (new Coord(0, 1), Player.X),
				(new Coord(0, 2), Player.X), (new Coord(0, 3), Player.X));
			Assert.AreEqual(Player.X, Rules.CheckWinner(full));
		}

		[Test]
		public void Col_4x4_FullLength()
		{
			var b = MakeBoard(BoardSize.FourByFour,
				(new Coord(0, 2), Player.O), (new Coord(1, 2), Player.O),
				(new Coord(2, 2), Player.O), (new Coord(3, 2), Player.O));
			Assert.AreEqual(Player.O, Rules.CheckWinner(b));
		}

		[Test]
		public void Diagonals_4x4_FullLength()
		{
			var main = MakeBoard(BoardSize.FourByFour,
				(new Coord(0, 0), Player.X), (new Coord(1, 1), Player.X),
				(new Coord(2, 2), Player.X), (new Coord(3, 3), Player.X));
			Assert.AreEqual(Player.X, Rules.CheckWinner(main));

			var anti = MakeBoard(BoardSize.FourByFour,
				(new Coord(0, 3), Player.O), (new Coord(1, 2), Player.O),
				(new Coord(2, 1), Player.O), (new Coord(3, 0), Player.O));
			Assert.AreEqual(Player.O, Rules.CheckWinner(anti));
		}

		[Test]
		public void MixedColorLine_NoWinner()
		{
			var b = MakeBoard(BoardSize.ThreeByThree,
				(new Coord(0, 0), Player.X), (new Coord(0, 1), Player.O), (new Coord(0, 2), Player.X));
			Assert.AreEqual(Player.None, Rules.CheckWinner(b));
		}

		[Test]
		public void OrientationDoesNotAffectWin()
		{
			var b = new Board(BoardSize.ThreeByThree);
			b.Set(new Coord(0, 0), new Cell(Player.X, Orientation.Cardinal));
			b.Set(new Coord(0, 1), new Cell(Player.X, Orientation.Diagonal));
			b.Set(new Coord(0, 2), new Cell(Player.X, Orientation.Cardinal));
			Assert.AreEqual(Player.X, Rules.CheckWinner(b));
		}

		[Test]
		public void OffDiagonal_4x4_DoesNotCount()
		{
			// A "diagonal" of length 3 within the 4x4 (e.g. (0,1)-(1,2)-(2,3)) is NOT a winning line.
			var b = MakeBoard(BoardSize.FourByFour,
				(new Coord(0, 1), Player.X), (new Coord(1, 2), Player.X), (new Coord(2, 3), Player.X));
			Assert.AreEqual(Player.None, Rules.CheckWinner(b));
		}
	}
}
