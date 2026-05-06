using ChungToi.Core;

namespace ChungToi.AI
{
	/// <summary>
	/// Classic open-line heuristic. For each potential winning line (every row, every column,
	/// and the two main diagonals), if only one side has pieces in that line, we award
	/// that side k^2 points where k is the count. Contested lines (both sides present) score 0.
	///
	/// On 3x3 the maximum possible score is 8 lines * 9 = 72.
	/// On 4x4 the maximum possible score is 10 lines * 16 = 160.
	/// Both are far below the search's terminal score, so a real win/loss never collides
	/// with a heuristic value.
	/// </summary>
	public sealed class LineHeuristicEvaluator : IEvaluator
	{
		public int Evaluate(GameState state, Player perspective)
		{
			var board = state.Board;
			int n = board.N;
			var opp = perspective.Opponent();
			int score = 0;

			for (int r = 0; r < n; r++)
				score += LineScore(board, perspective, opp, n, r, 0, 0, 1);

			for (int c = 0; c < n; c++)
				score += LineScore(board, perspective, opp, n, 0, c, 1, 0);

			score += LineScore(board, perspective, opp, n, 0, 0, 1, 1);
			score += LineScore(board, perspective, opp, n, 0, n - 1, 1, -1);

			return score;
		}

		private static int LineScore(Board board, Player me, Player opp, int n,
									 int startR, int startC, int dr, int dc)
		{
			int mine = 0, his = 0;
			for (int i = 0; i < n; i++)
			{
				var owner = board.Get(startR + dr * i, startC + dc * i).Owner;
				if (owner == me) mine++;
				else if (owner == opp) his++;
			}
			if (mine > 0 && his > 0) return 0; // contested line
			if (mine > 0) return mine * mine;
			if (his > 0) return -(his * his);
			return 0;
		}
	}
}
