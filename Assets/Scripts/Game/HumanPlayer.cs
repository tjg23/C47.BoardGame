using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChungToi.Core;

namespace ChungToi.Game
{
	/// <summary>
	/// A keyboard + mouse player. Listens to <see cref="InputController"/> events and resolves
	/// the <see cref="IPlayer.ChooseMove"/> task when the user produces a legal move.
	///
	/// Placement phase:
	///   - R toggles <see cref="CurrentOrientation"/>
	///   - Click an empty legal cell → emit Move.Place
	///
	/// Movement phase (state machine):
	///   Idle:
	///     click own piece               → Selected(coord, slideOrient = piece.Orient)
	///     other clicks                  → ignore
	///   Selected:
	///     R                             → toggle slideOrient (preview-rotate-on-slide)
	///     click selected piece          → emit Rotate(coord, opposite)
	///     click highlighted destination → emit Slide(coord, dest, slideOrient)
	///     click another own piece       → switch selection
	///     click anything else / Esc     → Idle
	///
	/// Listeners can subscribe to <see cref="SelectionChanged"/> to redraw highlights.
	/// </summary>
	public sealed class HumanPlayer : IPlayer, IDisposable
	{
		private readonly InputController _input;

		// Placement-phase mode-state.
		public Orientation CurrentOrientation { get; private set; } = Orientation.Cardinal;

		// Movement-phase mode-state.
		public Coord? SelectedPiece { get; private set; }
		public Orientation SlideOrientation { get; private set; }
		public IReadOnlyList<Coord> SlideDestinations => _slideDestinationsView;

		private readonly List<Coord> _slideDestinations = new();
		private readonly IReadOnlyList<Coord> _slideDestinationsView;

		// Fired any time the selection state changes (selected piece, slide destinations, or
		// slide orientation). Listeners should redraw highlights / HUD on this event.
		public event Action SelectionChanged;

		// ChooseMove call-state. Populated only while a ChooseMove task is in flight.
		private GameState _state;
		private TaskCompletionSource<Move> _tcs;
		private CancellationTokenRegistration _ctReg;

		public HumanPlayer(InputController input)
		{
			_input = input ?? throw new ArgumentNullException(nameof(input));
			_slideDestinationsView = _slideDestinations.AsReadOnly();

			_input.CellLeftClicked += OnCellLeftClicked;
			_input.RotatePressed += OnRotatePressed;
			_input.CancelPressed += OnCancelPressed;
		}

		public Task<Move> ChooseMove(GameState state, CancellationToken ct)
		{
			if (_tcs != null && !_tcs.Task.IsCompleted)
				throw new InvalidOperationException("ChooseMove called while a previous call is still pending.");

			_state = state;
			_tcs = new TaskCompletionSource<Move>(TaskCreationOptions.RunContinuationsAsynchronously);
			_ctReg = ct.Register(() => _tcs.TrySetCanceled(ct));
			// Selection always starts clean for a new turn.
			ResetSelection(silent: true);
			return _tcs.Task;
		}

		public void Dispose()
		{
			_input.CellLeftClicked -= OnCellLeftClicked;
			_input.RotatePressed -= OnRotatePressed;
			_input.CancelPressed -= OnCancelPressed;
			_ctReg.Dispose();
			_tcs?.TrySetCanceled();
		}

		// ---- input handlers ----

		private void OnCellLeftClicked(Coord coord)
		{
			if (!IsAwaiting()) return;

			switch (_state.Phase)
			{
				case GamePhase.Placement:
					HandlePlacementClick(coord);
					break;
				case GamePhase.Movement:
					HandleMovementClick(coord);
					break;
			}
		}

		private void OnRotatePressed()
		{
			if (!IsAwaiting()) return;

			if (_state.Phase == GamePhase.Placement)
			{
				CurrentOrientation = Toggle(CurrentOrientation);
				// Placement rotation isn't part of selection state, but listeners may want to
				// refresh the HUD label. Fire SelectionChanged so a single subscription suffices.
				SelectionChanged?.Invoke();
			}
			else if (_state.Phase == GamePhase.Movement && SelectedPiece.HasValue)
			{
				SlideOrientation = Toggle(SlideOrientation);
				SelectionChanged?.Invoke();
			}
		}

		private void OnCancelPressed()
		{
			if (!IsAwaiting()) return;
			if (_state.Phase == GamePhase.Movement && SelectedPiece.HasValue)
				ResetSelection(silent: false);
		}

		// ---- placement ----

		private void HandlePlacementClick(Coord coord)
		{
			var move = Move.Place(coord, CurrentOrientation);
			if (Rules.IsLegal(_state, move))
				Complete(move);
			// else: ignore (illegal placement)
		}

		// ---- movement ----

		private void HandleMovementClick(Coord coord)
		{
			var meIsCurrent = _state.ToMove;
			var clickedCell = _state.Board.Get(coord);

			// Idle: only "click my own piece" matters.
			if (!SelectedPiece.HasValue)
			{
				if (clickedCell.Owner == meIsCurrent)
					Select(coord);
				return;
			}

			// Selected:
			var selected = SelectedPiece.Value;

			if (coord == selected)
			{
				// Click the selected piece itself → rotate-in-place.
				var newOrient = Toggle(clickedCell.Orient);
				var rotateMove = Move.Rotate(coord, newOrient);
				if (Rules.IsLegal(_state, rotateMove))
					Complete(rotateMove);
				return;
			}

			if (clickedCell.Owner == meIsCurrent)
			{
				// Switching selection to another own piece.
				Select(coord);
				return;
			}

			// Empty / opponent cell. If it's a legal slide destination, commit the slide.
			if (clickedCell.IsEmpty && IsSlideDestination(coord))
			{
				var slide = Move.Slide(selected, coord, SlideOrientation);
				if (Rules.IsLegal(_state, slide))
				{
					Complete(slide);
					return;
				}
			}

			// Anything else → deselect.
			ResetSelection(silent: false);
		}

		private void Select(Coord coord)
		{
			SelectedPiece = coord;
			SlideOrientation = _state.Board.Get(coord).Orient; // default to "no rotation on slide"
			RecomputeSlideDestinations(coord);
			SelectionChanged?.Invoke();
		}

		private void RecomputeSlideDestinations(Coord from)
		{
			_slideDestinations.Clear();
			var legal = Rules.GetLegalMoves(_state);
			for (int i = 0; i < legal.Count; i++)
			{
				var m = legal[i];
				if (m.Kind == MoveKind.Slide && m.From == from && !ContainsCoord(_slideDestinations, m.To))
					_slideDestinations.Add(m.To);
			}
		}

		private bool IsSlideDestination(Coord coord) => ContainsCoord(_slideDestinations, coord);

		private static bool ContainsCoord(List<Coord> list, Coord c)
		{
			for (int i = 0; i < list.Count; i++)
				if (list[i] == c) return true;
			return false;
		}

		// ---- helpers ----

		private bool IsAwaiting() => _tcs != null && !_tcs.Task.IsCompleted;

		private static Orientation Toggle(Orientation o) =>
				o == Orientation.Cardinal ? Orientation.Diagonal : Orientation.Cardinal;

		private void ResetSelection(bool silent)
		{
			bool hadSelection = SelectedPiece.HasValue || _slideDestinations.Count > 0;
			SelectedPiece = null;
			_slideDestinations.Clear();
			// SlideOrientation is intentionally left at its previous value — it'll be re-set on next Select().
			if (!silent && hadSelection) SelectionChanged?.Invoke();
		}

		/// <summary>Public hook for restarts: drop any in-flight selection.</summary>
		public void ClearSelection() => ResetSelection(silent: false);

		private void Complete(Move move)
		{
			var tcs = _tcs;
			_tcs = null;
			_state = null;
			_ctReg.Dispose();
			ResetSelection(silent: false);
			tcs.TrySetResult(move);
		}
	}
}
