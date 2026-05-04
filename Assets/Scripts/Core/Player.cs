namespace ChungToi.Core
{
	public enum Player : byte
	{
		None = 0,
		X = 1,
		O = 2
	}

	public static class PlayerExtensions
	{
		public static Player Opponent(this Player p) => p switch
		{
			Player.X => Player.O,
			Player.O => Player.X,
			_ => Player.None
		};
	}
}
