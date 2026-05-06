using System;
using System.Collections.Generic;
using System.Diagnostics;
using ChungToi.Core;

namespace ChungToi.AI
{
	/// <summary>
	/// Fixed-depth minimax search with optional alpha-beta pruning.
	///
	/// Algorithm shape (kept as explicit max/min rather than negamax for readability — the project
	/// goal is to *showcase* the algorithm):
	///
	///   Minimax(state, depth, α, β, root):
	///     if state is terminal:           return ±TerminalScore adjusted by ply
	///     if depth == 0:                  return Evaluator(state, root)
	///     if state.ToMove == root:        // maximize
	///         best = -∞
	///         for each move in legal moves of state:
	///             child = Apply(state.Clone(), move)
	///             best = max(best, Minimax(child, depth-1, α, β, root))
	///             α = max(α, best)
	///             if α ≥ β: cutoff; break
	///         return best
	///     else:                           // minimize
	///         best = +∞
	///         for each move in legal moves of state:
	///             child = Apply(state.Clone(), move)
	///             best = min(best, Minimax(child, depth-1, α, β, root))
	///             β = min(β, best)
	///             if α ≥ β: cutoff; break
	///         return best
	///
	/// Notes:
	/// - We clone-and-apply rather than apply/undo. Simpler and correct; revisit only if profiling demands.
	/// - Terminal scores are scaled by depth so the search prefers fast wins / slow losses
	///   (a mate-in-1 must beat a mate-in-3).
	/// - <see cref="UseAlphaBeta"/> can be flipped off at runtime to compare node counts —
	///   with it off the search becomes plain minimax (still correct, just slower).
	/// - Move ordering is left as generation order. Adding a "winning moves first" pass would
	///   significantly improve pruning on Chung Toi but is intentionally omitted from v1.
	/// </summary>
	public sealed class MinimaxAI
	{
		public int MaxDepth { get; }
		public bool UseAlphaBeta { get; set; } = true;
		public IEvaluator Evaluator { get; }
		public SearchStats Stats { get; private set; } = new SearchStats();

		// Sentinel score for terminal positions. Must be much larger than any heuristic value
		// the evaluator can produce so that a real win/loss is never overridden by heuristic noise.
		private const int TerminalScore = 1_000_000;

		// We use -inf+1 / +inf-1 so that negation never overflows and the asymmetry of
		// int.MinValue (whose absolute value doesn't fit in int) doesn't bite us.
		private const int Infinity = int.MaxValue / 2;

		public MinimaxAI(int maxDepth, IEvaluator evaluator = null)
		{
			if (maxDepth < 1) throw new ArgumentOutOfRangeException(nameof(maxDepth), "Depth must be ≥ 1.");
			MaxDepth = maxDepth;
			Evaluator = evaluator ?? new LineHeuristicEvaluator();
		}

		public Move ChooseMove(GameState state)
		{
			if (state.Phase == GamePhase.GameOver)
				throw new InvalidOperationException("Game is already over.");

			Stats = new SearchStats { MaxDepth = MaxDepth };
			var sw = Stopwatch.StartNew();

			var moves = Rules.GetLegalMoves(state);
			if (moves.Count == 0)
				throw new InvalidOperationException("No legal moves from this state.");

			var root = state.ToMove;
			int alpha = -Infinity;
			int beta = Infinity;

			Move bestMove = moves[0];
			int bestScore = -Infinity;

			// Root acts as a max node from the root player's perspective.
			foreach (var move in moves)
			{
				var child = state.Clone();
				Rules.Apply(child, move);
				int score = Minimax(child, MaxDepth - 1, alpha, beta, root);

				if (score > bestScore)
				{
					bestScore = score;
					bestMove = move;
				}
				if (UseAlphaBeta && bestScore > alpha) alpha = bestScore;
				// No β cutoff at the root: β is +∞.
			}

			sw.Stop();
			Stats.ElapsedMilliseconds = sw.ElapsedMilliseconds;
			return bestMove;
		}

		private int Minimax(GameState state, int depth, int alpha, int beta, Player root)
		{
			Stats.NodesVisited++;

			if (state.Phase == GamePhase.GameOver)
			{
				Stats.TerminalNodes++;
				int pliesFromRoot = MaxDepth - depth;
				if (state.Winner == root) return TerminalScore - pliesFromRoot;
				if (state.Winner != Player.None) return -TerminalScore + pliesFromRoot;
				return 0; // draw — currently unreachable in Chung Toi but defensive.
			}

			if (depth == 0)
			{
				Stats.LeavesEvaluated++;
				return Evaluator.Evaluate(state, root);
			}

			var moves = Rules.GetLegalMoves(state);
			// Stuck-player detection lives in Rules.Apply, so an in-progress state always has moves.
			// Defensive fallback if that invariant ever breaks: treat empty as a loss for the side to move.
			if (moves.Count == 0)
			{
				int pliesFromRoot = MaxDepth - depth;
				bool sideToMoveIsRoot = state.ToMove == root;
				return sideToMoveIsRoot ? -TerminalScore + pliesFromRoot : TerminalScore - pliesFromRoot;
			}

			bool maximizing = state.ToMove == root;
			return maximizing
				? MaxNode(state, moves, depth, alpha, beta, root)
				: MinNode(state, moves, depth, alpha, beta, root);
		}

		private int MaxNode(GameState state, List<Move> moves, int depth, int alpha, int beta, Player root)
		{
			int best = -Infinity;
			foreach (var move in moves)
			{
				var child = state.Clone();
				Rules.Apply(child, move);
				int score = Minimax(child, depth - 1, alpha, beta, root);

				if (score > best) best = score;
				if (UseAlphaBeta)
				{
					if (best > alpha) alpha = best;
					if (alpha >= beta) { Stats.AlphaBetaCutoffs++; break; }
				}
			}
			return best;
		}

		private int MinNode(GameState state, List<Move> moves, int depth, int alpha, int beta, Player root)
		{
			int best = Infinity;
			foreach (var move in moves)
			{
				var child = state.Clone();
				Rules.Apply(child, move);
				int score = Minimax(child, depth - 1, alpha, beta, root);

				if (score < best) best = score;
				if (UseAlphaBeta)
				{
					if (best < beta) beta = best;
					if (alpha >= beta) { Stats.AlphaBetaCutoffs++; break; }
				}
			}
			return best;
		}
	}
}
