using NUnit.Framework;
using ChungToi.Core;

namespace ChungToi.Core.Tests
{
	[TestFixture]
	public class BoardTests
	{
		[Test]
		public void NewBoard_IsAllEmpty_3x3()
		{
			var b = new Board(BoardSize.ThreeByThree);
			Assert.AreEqual(3, b.N);
			foreach (var c in b.AllCoords())
				Assert.IsTrue(b.IsEmpty(c));
		}

		[Test]
		public void NewBoard_IsAllEmpty_4x4()
		{
			var b = new Board(BoardSize.FourByFour);
			Assert.AreEqual(4, b.N);
			foreach (var c in b.AllCoords())
				Assert.IsTrue(b.IsEmpty(c));
		}

		[Test]
		public void Set_Get_Roundtrip()
		{
			var b = new Board(BoardSize.ThreeByThree);
			var cell = new Cell(Player.X, Orientation.Diagonal);
			b.Set(new Coord(1, 2), cell);
			Assert.AreEqual(cell, b.Get(new Coord(1, 2)));
			Assert.IsTrue(b.IsEmpty(new Coord(0, 0)));
		}

		[Test]
		public void Clone_IsIndependent()
		{
			var a = new Board(BoardSize.FourByFour);
			a.Set(new Coord(0, 0), new Cell(Player.X, Orientation.Cardinal));
			var b = a.Clone();
			b.Set(new Coord(0, 0), new Cell(Player.O, Orientation.Diagonal));
			Assert.AreEqual(Player.X, a.Get(new Coord(0, 0)).Owner);
			Assert.AreEqual(Player.O, b.Get(new Coord(0, 0)).Owner);
		}

		[Test]
		public void Equality_IsValueBased()
		{
			var a = new Board(BoardSize.ThreeByThree);
			var b = new Board(BoardSize.ThreeByThree);
			Assert.AreEqual(a, b);
			a.Set(new Coord(1, 1), new Cell(Player.X, Orientation.Cardinal));
			Assert.AreNotEqual(a, b);
			b.Set(new Coord(1, 1), new Cell(Player.X, Orientation.Cardinal));
			Assert.AreEqual(a, b);
		}

		[Test]
		public void DifferentSizes_AreNotEqual()
		{
			var a = new Board(BoardSize.ThreeByThree);
			var b = new Board(BoardSize.FourByFour);
			Assert.AreNotEqual(a, b);
		}

		[Test]
		public void CountOwned_Works()
		{
			var b = new Board(BoardSize.ThreeByThree);
			b.Set(new Coord(0, 0), new Cell(Player.X, Orientation.Cardinal));
			b.Set(new Coord(0, 1), new Cell(Player.X, Orientation.Diagonal));
			b.Set(new Coord(0, 2), new Cell(Player.O, Orientation.Cardinal));
			Assert.AreEqual(2, b.CountOwned(Player.X));
			Assert.AreEqual(1, b.CountOwned(Player.O));
			Assert.AreEqual(6, b.CountOwned(Player.None));
		}

		[Test]
		public void Coord_InBounds()
		{
			var size = BoardSize.ThreeByThree;
			Assert.IsTrue(new Coord(0, 0).InBounds(size));
			Assert.IsTrue(new Coord(2, 2).InBounds(size));
			Assert.IsFalse(new Coord(-1, 0).InBounds(size));
			Assert.IsFalse(new Coord(0, 3).InBounds(size));
		}

		[Test]
		public void BoardSizeExtensions_PiecesPerPlayer()
		{
			Assert.AreEqual(3, BoardSize.ThreeByThree.PiecesPerPlayer());
			Assert.AreEqual(5, BoardSize.FourByFour.PiecesPerPlayer());
		}
	}
}
