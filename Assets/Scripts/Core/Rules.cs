using System;
using System.Collections.Generic;

namespace ChungToi.Core
{
	/// <summary>
	/// Pure, static rules engine. All methods take a <see cref="GameState"/> and either return
	/// information about it or mutate it in place. Callers wanting a separate state must
	/// <see cref="GameState.Clone"/> first — this is the contract the minimax search relies on.
	/// </summary>
	public static class Rules
	{
		private static readonly (int dr, int dc)[] CardinalDirs =
			{ (-1, 0), (1, 0), (0, -1), (0, 1) };

		private static readonly (int dr, int dc)[] DiagonalDirs =
			{ (-1, -1), (-1, 1), (1, -1), (1, 1) };

		public static List<Move> GetLegalMoves(GameState state)
		{
			var moves = new List<Move>();
			if (state.Phase == GamePhase.GameOver) return moves;

			if (state.Phase == GamePhase.Placement) AddPlacementMoves(state, moves);
			else AddMovementMoves(state, moves);

			return moves;
		}

		public static bool HasAnyLegalMove(GameState state)
		{
			// Equivalent to GetLegalMoves(state).Count > 0 but short-circuits on first hit.
			if (state.Phase == GamePhase.GameOver) return false;
			if (state.Phase == GamePhase.Placement)
			{
				bool restrictCenter = state.MoveNumber == 0 && state.Board.Size == BoardSize.ThreeByThree;
				var center = new Coord(1, 1);
				foreach (var c in state.Board.AllCoords())
				{
					if (!state.Board.IsEmpty(c)) continue;
					if (restrictCenter && c == center) continue;
					return true;
				}
				return false;
			}
			// Movement: rotation in place is always legal for any owned piece, so a single owned piece is enough.
			return state.Board.CountOwned(state.ToMove) > 0;
		}

		public static bool IsLegal(GameState state, Move move)
		{
			var legal = GetLegalMoves(state);
			return legal.Contains(move);
		}

		/// <summary>Applies <paramref name="move"/> to <paramref name="state"/> in place.</summary>
		public static void Apply(GameState state, Move move)
		{
			if (state.Phase == GamePhase.GameOver)
				throw new InvalidOperationException("Cannot apply a move to a finished game.");

			var mover = state.ToMove;
			var board = state.Board;

			switch (move.Kind)
			{
				case MoveKind.Place:
					if (state.Phase != GamePhase.Placement)
						throw new InvalidOperationException("Place move outside placement phase.");
					board.Set(move.From, new Cell(mover, move.NewOrient));
					state.DecrementPiecesToPlace(mover);
					break;

				case MoveKind.Rotate:
					{
						if (state.Phase != GamePhase.Movement)
							throw new InvalidOperationException("Rotate move outside movement phase.");
						var cell = board.Get(move.From);
						board.Set(move.From, new Cell(cell.Owner, move.NewOrient));
						break;
					}

				case MoveKind.Slide:
					{
						if (state.Phase != GamePhase.Movement)
							throw new InvalidOperationException("Slide move outside movement phase.");
						var cell = board.Get(move.From);
						board.Set(move.From, Cell.Empty);
						board.Set(move.To, new Cell(cell.Owner, move.NewOrient));
						break;
					}
			}

			var winner = CheckWinner(board);
			if (winner != Player.None)
			{
				state.SetWinner(winner);
				state.SetPhase(GamePhase.GameOver);
				return;
			}

			state.IncrementMoveNumber();
			state.SwitchTurn();

			if (state.Phase == GamePhase.Placement
				&& state.PiecesToPlaceX == 0 && state.PiecesToPlaceO == 0)
			{
				state.SetPhase(GamePhase.Movement);
			}

			// Stuck-player-loses: if the player about to move can't move, they lose.
			if (state.Phase == GamePhase.Movement && !HasAnyLegalMove(state))
			{
				state.SetWinner(state.ToMove.Opponent());
				state.SetPhase(GamePhase.GameOver);
			}
		}

		public static Player CheckWinner(Board board)
		{
			int n = board.N;

			for (int r = 0; r < n; r++)
			{
				var first = board.Get(r, 0).Owner;
				if (first == Player.None) continue;
				bool all = true;
				for (int c = 1; c < n; c++)
					if (board.Get(r, c).Owner != first) { all = false; break; }
				if (all) return first;
			}

			for (int c = 0; c < n; c++)
			{
				var first = board.Get(0, c).Owner;
				if (first == Player.None) continue;
				bool all = true;
				for (int r = 1; r < n; r++)
					if (board.Get(r, c).Owner != first) { all = false; break; }
				if (all) return first;
			}

			var d1 = board.Get(0, 0).Owner;
			if (d1 != Player.None)
			{
				bool all = true;
				for (int i = 1; i < n; i++)
					if (board.Get(i, i).Owner != d1) { all = false; break; }
				if (all) return d1;
			}

			var d2 = board.Get(0, n - 1).Owner;
			if (d2 != Player.None)
			{
				bool all = true;
				for (int i = 1; i < n; i++)
					if (board.Get(i, n - 1 - i).Owner != d2) { all = false; break; }
				if (all) return d2;
			}

			return Player.None;
		}

		private static void AddPlacementMoves(GameState state, List<Move> moves)
		{
			var board = state.Board;
			int n = board.N;
			bool restrictCenter = state.MoveNumber == 0 && board.Size == BoardSize.ThreeByThree;
			var center = new Coord(1, 1);

			for (int r = 0; r < n; r++)
				for (int c = 0; c < n; c++)
				{
					var coord = new Coord(r, c);
					if (!board.IsEmpty(coord)) continue;
					if (restrictCenter && coord == center) continue;
					moves.Add(Move.Place(coord, Orientation.Cardinal));
					moves.Add(Move.Place(coord, Orientation.Diagonal));
				}
		}

		private static void AddMovementMoves(GameState state, List<Move> moves)
		{
			var board = state.Board;
			int n = board.N;
			var player = state.ToMove;

			for (int r = 0; r < n; r++)
				for (int c = 0; c < n; c++)
				{
					var from = new Coord(r, c);
					var cell = board.Get(from);
					if (cell.Owner != player) continue;

					var toggled = cell.Orient == Orientation.Cardinal ? Orientation.Diagonal : Orientation.Cardinal;

					// Rotate in place — must change orientation (a no-op rotate is not a move).
					moves.Add(Move.Rotate(from, toggled));

					var dirs = cell.Orient == Orientation.Cardinal ? CardinalDirs : DiagonalDirs;
					foreach (var (dr, dc) in dirs)
					{
						int dist = 1;
						while (true)
						{
							int nr = from.Row + dr * dist;
							int nc = from.Col + dc * dist;
							if (nr < 0 || nr >= n || nc < 0 || nc >= n) break;
							var to = new Coord(nr, nc);
							if (board.IsEmpty(to))
							{
								moves.Add(Move.Slide(from, to, cell.Orient));
								moves.Add(Move.Slide(from, to, toggled));
							}
							dist++;
						}
					}
				}
		}
	}
}
