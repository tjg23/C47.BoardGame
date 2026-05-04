using System;

namespace ChungToi.Core
{
	public enum Orientation : byte
	{
		Cardinal = 0,
		Diagonal = 1
	}

	public readonly struct Cell : IEquatable<Cell>
	{
		public readonly Player Owner;
		public readonly Orientation Orient;

		public Cell(Player owner, Orientation orient)
		{
			Owner = owner;
			Orient = orient;
		}

		public bool IsEmpty => Owner == Player.None;
		public static Cell Empty => default;

		public bool Equals(Cell other) => Owner == other.Owner && Orient == other.Orient;
		public override bool Equals(object obj) => obj is Cell c && Equals(c);
		public override int GetHashCode() => ((int)Owner << 1) | (int)Orient;

		public static bool operator ==(Cell a, Cell b) => a.Equals(b);
		public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
	}
}
