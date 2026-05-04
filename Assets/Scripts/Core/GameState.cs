namespace ChungToi.Core
{
	public enum GamePhase : byte
	{
		Placement = 0,
		Movement = 1,
		GameOver = 2
	}

	/// <summary>
	/// Full snapshot of a Chung Toi game: board + whose turn + phase + remaining-to-place + winner.
	/// Cloneable. The AI search will <see cref="Clone"/> this and apply moves to the copy.
	/// </summary>
	public sealed class GameState
	{
		public Board Board { get; private set; }
		public Player ToMove { get; private set; }
		public GamePhase Phase { get; private set; }
		public Player Winner { get; private set; }

		// How many pieces each player still has to place during the placement phase.
		public int PiecesToPlaceX { get; private set; }
		public int PiecesToPlaceO { get; private set; }

		// Move counter — primarily so Rules can enforce the 3x3 "no center on first move" rule.
		public int MoveNumber { get; private set; }

		public GameState(BoardSize size, Player firstToMove = Player.X)
		{
			Board = new Board(size);
			ToMove = firstToMove;
			Phase = GamePhase.Placement;
			Winner = Player.None;
			int per = size.PiecesPerPlayer();
			PiecesToPlaceX = per;
			PiecesToPlaceO = per;
			MoveNumber = 0;
		}

		private GameState() { }

		public GameState Clone()
		{
			return new GameState
			{
				Board = Board.Clone(),
				ToMove = ToMove,
				Phase = Phase,
				Winner = Winner,
				PiecesToPlaceX = PiecesToPlaceX,
				PiecesToPlaceO = PiecesToPlaceO,
				MoveNumber = MoveNumber
			};
		}

		public int PiecesToPlace(Player p) => p switch
		{
			Player.X => PiecesToPlaceX,
			Player.O => PiecesToPlaceO,
			_ => 0
		};

		// -- mutators (used only by Rules.Apply) --

		internal void DecrementPiecesToPlace(Player p)
		{
			if (p == Player.X) PiecesToPlaceX--;
			else if (p == Player.O) PiecesToPlaceO--;
		}

		internal void SwitchTurn() => ToMove = ToMove.Opponent();
		internal void IncrementMoveNumber() => MoveNumber++;
		internal void SetPhase(GamePhase phase) => Phase = phase;
		internal void SetWinner(Player winner) => Winner = winner;

		/// <summary>
		/// Test-only factory: build a state directly from a Board and metadata, bypassing the placement
		/// phase. Useful for movement-phase rule tests where scripting the placement sequence might
		/// accidentally trigger a win.
		/// </summary>
		internal static GameState CreateForTesting(Board board, Player toMove, GamePhase phase, int moveNumber = 100)
		{
			return new GameState
			{
				Board = board,
				ToMove = toMove,
				Phase = phase,
				Winner = Player.None,
				PiecesToPlaceX = 0,
				PiecesToPlaceO = 0,
				MoveNumber = moveNumber
			};
		}
	}
}
