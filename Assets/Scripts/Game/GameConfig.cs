using ChungToi.Core;

namespace ChungToi.Game
{
	/// <summary>
	/// Five-step difficulty ramp. Backed by search depth — higher = stronger and slower.
	/// The integer value is the search depth so callers can do <c>(int)Difficulty</c>.
	///
	/// We deliberately leave heuristic selection out of this enum: <see cref="MinimaxAI"/>
	/// already takes an <see cref="AI.IEvaluator"/>, so a future "Expert uses a smarter
	/// evaluator" tweak is one constructor change in <see cref="AIPlayer"/>.
	/// </summary>
	public enum AIDifficulty
	{
		Beginner = 1,
		Easy = 2,
		Medium = 3,
		Hard = 4,
		Expert = 5,
	}

	/// <summary>
	/// Plain settings bag passed from the main menu to the game controller. Mutable; the menu
	/// builds it up as the user clicks options, then hands it off when they press Start.
	/// </summary>
	public sealed class GameConfig
	{
		public BoardSize Size;
		public GameController.SideController XControl;
		public GameController.SideController OControl;
		public AIDifficulty Difficulty;

		public int AIDepth => (int)Difficulty;

		public static GameConfig Default => new()
		{
			Size = BoardSize.ThreeByThree,
			XControl = GameController.SideController.Human,
			OControl = GameController.SideController.AI,
			Difficulty = AIDifficulty.Medium,
		};

		public GameConfig Clone() => new()
		{
			Size = Size,
			XControl = XControl,
			OControl = OControl,
			Difficulty = Difficulty,
		};
	}
}
