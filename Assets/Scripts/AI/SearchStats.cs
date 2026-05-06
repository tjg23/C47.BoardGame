namespace ChungToi.AI
{
	/// <summary>
	/// Per-search counters. Reset at the start of each <see cref="MinimaxAI.ChooseMove"/> call.
	/// The cutoff and node counts are the headline numbers for the algorithm-showcase writeup:
	/// running the same position with <see cref="MinimaxAI.UseAlphaBeta"/> on vs. off gives
	/// you the pruning-vs-no-pruning comparison.
	/// </summary>
	public sealed class SearchStats
	{
		public long NodesVisited;
		public long LeavesEvaluated;
		public long TerminalNodes;
		public long AlphaBetaCutoffs;
		public int MaxDepth;
		public long ElapsedMilliseconds;

		public override string ToString() =>
			$"depth={MaxDepth} nodes={NodesVisited} leaves={LeavesEvaluated} " +
			$"terminals={TerminalNodes} cutoffs={AlphaBetaCutoffs} time={ElapsedMilliseconds}ms";
	}
}
