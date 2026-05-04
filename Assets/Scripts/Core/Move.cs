using System;

namespace ChungToi.Core
{
	public enum MoveKind : byte
	{
		Place = 0,
		Rotate = 1,
		Slide = 2
	}

	/// <summary>
	/// Tagged-union move. Use the static factories instead of the raw constructor.
	/// </summary>
	public readonly struct Move : IEquatable<Move>
	{
		public readonly MoveKind Kind;
		public readonly Coord From;
		public readonly Coord To;
		public readonly Orientation NewOrient;

		private Move(MoveKind kind, Coord from, Coord to, Orientation newOrient)
		{
			Kind = kind;
			From = from;
			To = to;
			NewOrient = newOrient;
		}

		public static Move Place(Coord at, Orientation orient) =>
			new(MoveKind.Place, at, at, orient);

		public static Move Rotate(Coord at, Orientation newOrient) =>
			new(MoveKind.Rotate, at, at, newOrient);

		public static Move Slide(Coord from, Coord to, Orientation newOrient) =>
			new(MoveKind.Slide, from, to, newOrient);

		public bool Equals(Move other) =>
			Kind == other.Kind && From == other.From && To == other.To && NewOrient == other.NewOrient;

		public override bool Equals(object obj) => obj is Move m && Equals(m);
		public override int GetHashCode() =>
			((int)Kind * 31 + From.GetHashCode()) * 31 + To.GetHashCode() * 31 + (int)NewOrient;

		public override string ToString() => Kind switch
		{
			MoveKind.Place => $"Place {From} {NewOrient}",
			MoveKind.Rotate => $"Rotate {From} -> {NewOrient}",
			MoveKind.Slide => $"Slide {From} -> {To} ({NewOrient})",
			_ => "?"
		};

		public static bool operator ==(Move a, Move b) => a.Equals(b);
		public static bool operator !=(Move a, Move b) => !a.Equals(b);
	}
}
