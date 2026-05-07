using System;
using System.Collections.Generic;
using System.Text;

namespace ChungToi.Core
{
	public enum BoardSize
	{
		ThreeByThree = 3,
		FourByFour = 4
	}

	public static class BoardSizeExtensions
	{
		public static int PiecesPerPlayer(this BoardSize size) => size switch
		{
			BoardSize.ThreeByThree => 3,
			BoardSize.FourByFour => 4,
			_ => 0
		};
	}

	/// <summary>
	/// Pure board state: a grid of <see cref="Cell"/>. Cloneable; value-equal for testing.
	/// All mutation goes through <see cref="Set"/> — callers are expected to clone first
	/// when they want a separate state (the AI search will rely on this).
	/// </summary>
	public sealed class Board : IEquatable<Board>
	{
		public BoardSize Size { get; }
		public int N => (int)Size;

		private readonly Cell[] _cells;

		public Board(BoardSize size)
		{
			Size = size;
			_cells = new Cell[N * N];
		}

		private Board(BoardSize size, Cell[] cells)
		{
			Size = size;
			_cells = cells;
		}

		private int Index(int row, int col) => row * N + col;

		public Cell Get(Coord c) => _cells[Index(c.Row, c.Col)];
		public Cell Get(int row, int col) => _cells[Index(row, col)];

		public void Set(Coord c, Cell cell) => _cells[Index(c.Row, c.Col)] = cell;

		public bool IsEmpty(Coord c) => Get(c).IsEmpty;

		public IEnumerable<Coord> AllCoords()
		{
			for (int r = 0; r < N; r++)
				for (int col = 0; col < N; col++)
					yield return new Coord(r, col);
		}

		public Board Clone()
		{
			var copy = new Cell[_cells.Length];
			Array.Copy(_cells, copy, _cells.Length);
			return new Board(Size, copy);
		}

		public int CountOwned(Player player)
		{
			int n = 0;
			for (int i = 0; i < _cells.Length; i++)
				if (_cells[i].Owner == player) n++;
			return n;
		}

		public bool Equals(Board other)
		{
			if (other is null) return false;
			if (Size != other.Size) return false;
			for (int i = 0; i < _cells.Length; i++)
				if (_cells[i] != other._cells[i]) return false;
			return true;
		}

		public override bool Equals(object obj) => obj is Board b && Equals(b);

		public override int GetHashCode()
		{
			unchecked
			{
				int h = (int)Size;
				for (int i = 0; i < _cells.Length; i++)
					h = h * 31 + _cells[i].GetHashCode();
				return h;
			}
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine($"Board {N}x{N}:");
			for (int r = 0; r < N; r++)
			{
				for (int c = 0; c < N; c++)
				{
					var cell = Get(r, c);
					char owner = cell.Owner switch { Player.X => 'X', Player.O => 'O', _ => '.' };
					char orient = cell.IsEmpty ? ' ' : (cell.Orient == Orientation.Cardinal ? '+' : 'x');
					sb.Append(owner).Append(orient).Append(' ');
				}
				sb.AppendLine();
			}
			return sb.ToString();
		}
	}
}
