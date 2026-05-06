using System.Threading;
using System.Threading.Tasks;
using ChungToi.Core;

namespace ChungToi.Game
{
    /// <summary>
    /// Anything that can produce a <see cref="Move"/> for the side currently to move. The game
    /// loop in <see cref="GameController"/> awaits this without caring whether the implementation
    /// is a human reading mouse clicks or an AI running minimax.
    ///
    /// Contract:
    ///   - Must return a move that's legal in <paramref name="state"/>.
    ///   - Must respect <paramref name="ct"/> by canceling the returned task if signalled.
    ///   - Must never mutate <paramref name="state"/>.
    /// </summary>
    public interface IPlayer
    {
        Task<Move> ChooseMove(GameState state, CancellationToken ct);
    }
}
