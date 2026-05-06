using System;
using System.Threading;
using System.Threading.Tasks;
using ChungToi.Core;

namespace ChungToi.Game
{
    /// <summary>
    /// A keyboard + mouse player. Listens to <see cref="InputController"/> events and resolves
    /// the <see cref="IPlayer.ChooseMove"/> task when the user produces a legal move.
    ///
    /// Step 4 (this commit) handles the placement phase only:
    ///   - R toggles <see cref="CurrentOrientation"/>
    ///   - Click an empty legal cell → emit Move.Place
    ///   - Illegal clicks (occupied, or center on first 3x3 move) are silently ignored
    ///
    /// Step 5 will extend ChooseMove with the movement-phase selection state machine.
    /// </summary>
    public sealed class HumanPlayer : IPlayer, IDisposable
    {
        private readonly InputController _input;

        // Mode-state (placement only for Step 4): the orientation a Place move will use.
        public Orientation CurrentOrientation { get; private set; } = Orientation.Cardinal;

        // ChooseMove call-state. Populated only while a ChooseMove task is in flight.
        private GameState _state;
        private TaskCompletionSource<Move> _tcs;
        private CancellationTokenRegistration _ctReg;

        public HumanPlayer(InputController input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _input.CellLeftClicked += OnCellLeftClicked;
            _input.RotatePressed   += OnRotatePressed;
            _input.CancelPressed   += OnCancelPressed;
        }

        public Task<Move> ChooseMove(GameState state, CancellationToken ct)
        {
            if (_tcs != null && !_tcs.Task.IsCompleted)
                throw new InvalidOperationException("ChooseMove called while a previous call is still pending.");

            _state = state;
            _tcs = new TaskCompletionSource<Move>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ctReg = ct.Register(() => _tcs.TrySetCanceled(ct));
            return _tcs.Task;
        }

        public void Dispose()
        {
            _input.CellLeftClicked -= OnCellLeftClicked;
            _input.RotatePressed   -= OnRotatePressed;
            _input.CancelPressed   -= OnCancelPressed;
            _ctReg.Dispose();
            _tcs?.TrySetCanceled();
        }

        // ---- input handlers ----

        private void OnCellLeftClicked(Coord coord)
        {
            if (!IsAwaiting()) return;

            if (_state.Phase == GamePhase.Placement)
            {
                var move = Move.Place(coord, CurrentOrientation);
                if (Rules.IsLegal(_state, move))
                    Complete(move);
                // else: ignore. (UI feedback for illegal clicks can come later.)
            }
            // Step 5 will branch here on GamePhase.Movement.
        }

        private void OnRotatePressed()
        {
            // Only toggle for the player currently expected to move; otherwise an R press
            // during X's turn would also flip O's orientation behind the scenes.
            if (!IsAwaiting()) return;
            CurrentOrientation = CurrentOrientation == Orientation.Cardinal
                ? Orientation.Diagonal
                : Orientation.Cardinal;
        }

        private void OnCancelPressed()
        {
            if (!IsAwaiting()) return;
            // No-op in placement phase. Step 5 uses this to deselect the active piece.
        }

        // ---- helpers ----

        private bool IsAwaiting() => _tcs != null && !_tcs.Task.IsCompleted;

        private void Complete(Move move)
        {
            var tcs = _tcs;
            _tcs = null;
            _state = null;
            _ctReg.Dispose();
            tcs.TrySetResult(move);
        }
    }
}
