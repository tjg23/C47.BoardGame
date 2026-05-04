using System;

namespace ChungToi.Core
{
	public readonly struct Coord : IEquatable<Coord>
	{
		public readonly int Row;
		public readonly int Col;

		public Coord(int row, int col)
		{
			Row = row;
			Col = col;
		}

		public bool InBounds(BoardSize size)
		{
			int n = (int)size;
			return Row >= 0 && Row < n && Col >= 0 && Col < n;
		}

		public Coord Offset(int dRow, int dCol) => new(Row + dRow, Col + dCol);

		public bool Equals(Coord other) => Row == other.Row && Col == other.Col;
		public override bool Equals(object obj) => obj is Coord c && Equals(c);
		public override int GetHashCode() => (Row * 397) ^ Col;
		public override string ToString() => $"({Row},{Col})";

		public static bool operator ==(Coord a, Coord b) => a.Equals(b);
		public static bool operator !=(Coord a, Coord b) => !a.Equals(b);
	}
}
