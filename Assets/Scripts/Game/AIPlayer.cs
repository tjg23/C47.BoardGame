using System.Threading;
using System.Threading.Tasks;
using ChungToi.Core;
using ChungToi.AI;

namespace ChungToi.Game
{
	/// <summary>
	/// Wraps <see cref="MinimaxAI"/> as an <see cref="IPlayer"/>. Runs the search on a thread-pool
	/// thread via <see cref="Task.Run"/> so the Unity main thread stays responsive (otherwise the
	/// editor would freeze for the duration of every search).
	///
	/// The search itself only touches Core types (no <c>UnityEngine</c> calls), so it's safe to
	/// execute off the main thread. We pass a <see cref="GameState.Clone"/> in for belt-and-braces
	/// isolation: even if the main thread somehow held a reference, our snapshot is independent.
	///
	/// The <see cref="IsThinking"/> flag is set true around the await so the HUD can show a
	/// "thinking…" indicator. <see cref="LastStats"/> exposes the most recent search counters
	/// (nodes, leaves, terminals, α-β cutoffs, elapsed ms) for the algorithm-showcase writeup.
	/// </summary>
	public sealed class AIPlayer : IPlayer
	{
		public MinimaxAI Engine { get; }
		public bool IsThinking { get; private set; }
		public SearchStats LastStats { get; private set; }
		public string Label { get; }

		public AIPlayer(int maxDepth, string label = "AI")
		{
			Engine = new MinimaxAI(maxDepth);
			Label = label;
		}

		public async Task<Move> ChooseMove(GameState state, CancellationToken ct)
		{
			var snapshot = state.Clone();
			IsThinking = true;
			try
			{
				// Note on cancellation: Task.Run honors `ct` only at task-start time. Once the
				// search is running, it runs to completion on the worker thread regardless of
				// whether `ct` is later canceled — the await throws and we discard the result,
				// but the worker keeps churning until done. For Chung Toi at sane depths this is
				// sub-second and not worth complicating the search loop with cooperative checks.
				var move = await Task.Run(() => Engine.ChooseMove(snapshot), ct);
				LastStats = Engine.Stats;
				return move;
			}
			finally
			{
				IsThinking = false;
			}
		}
	}
}
