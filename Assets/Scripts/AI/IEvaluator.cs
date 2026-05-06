using ChungToi.Core;

namespace ChungToi.AI
{
	/// <summary>
	/// Position evaluator. Returns a score in centipawn-like units from
	/// <paramref name="perspective"/>'s point of view: positive is good for that player.
	/// Called only at non-terminal leaves; terminal scoring is handled inside the search.
	/// </summary>
	public interface IEvaluator
	{
		int Evaluate(GameState state, Player perspective);
	}
}
