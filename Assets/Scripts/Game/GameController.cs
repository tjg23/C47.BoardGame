using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using ChungToi.Core;
using ChungToi.AI;
using ChungToi.View;

namespace ChungToi.Game
{
	/// <summary>
	/// Top-level orchestrator. Owns the <see cref="GameState"/>, the <see cref="BoardView"/>, the
	/// <see cref="InputController"/>, the <see cref="HighlightOverlay"/>, and an <see cref="IPlayer"/>
	/// for each side. Runs the game loop: ask the side-to-move player for a move, apply, render,
	/// repeat.
	///
	/// Two startup modes:
	/// • <see cref="AutoStart"/> = true (default): kicks off a game in <c>Start</c> using whatever
	///   field values are set in the Inspector. Convenient for headless / debug runs.
	/// • <see cref="AutoStart"/> = false: stays idle until <see cref="StartWithConfig"/> or
	///   <see cref="StartNewGame"/> is called. The main menu uses this mode.
	///
	/// Hotkeys: <b>N</b> starts a new game with current settings; <b>M</b> raises
	/// <see cref="MenuRequested"/> so the menu can take over.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class GameController : MonoBehaviour
	{
		public enum SideController { Human, AI }

		[Header("Board")]
		public BoardSize Size = BoardSize.ThreeByThree;

		[Header("Players")]
		public SideController XControl = SideController.Human;
		public SideController OControl = SideController.AI;
		[Tooltip("Search depth used by every AIPlayer this controller spawns.")]
		public int AIDepth = 3;

		[Header("Camera")]
		[Tooltip("Falls back to Camera.main if unset.")]
		public Camera RaycastCamera;
		public float CameraMargin = 1.5f;

		[Header("Lifecycle")]
		[Tooltip("If true, kicks off a game in Start(). Set false when a main menu drives launch.")]
		public bool AutoStart = true;

		public GameState State { get; private set; }

		public IPlayer XPlayer { get; private set; }
		public IPlayer OPlayer { get; private set; }
		public HumanPlayer HumanX { get; private set; }
		public HumanPlayer HumanO { get; private set; }

		// Most recent AI search result for display in the HUD. Set after every AI move.
		public SearchStats LastAIStats { get; private set; }
		public string LastAILabel { get; private set; }

		/// <summary>Raised when the user presses M during play. The main menu listens for this.</summary>
		public event Action MenuRequested;

		private BoardView _view;
		private InputController _input;
		private HighlightOverlay _highlight;
		private CancellationTokenSource _cts;

		public IPlayer CurrentPlayer
		{
			get
			{
				if (State == null) return null;
				return State.ToMove == Player.X ? XPlayer : OPlayer;
			}
		}

		public HumanPlayer CurrentHuman => CurrentPlayer as HumanPlayer;
		public AIPlayer CurrentAI => CurrentPlayer as AIPlayer;

		public bool IsRunning => State != null && State.Phase != GamePhase.GameOver;

		// ---- lifecycle ----

		private void Awake()
		{
			EnsureSubsystems();
		}

		private void Start()
		{
			if (AutoStart) StartNewGame();
		}

		private void Update()
		{
			// Don't read game-control hotkeys while no game is running (menu is up).
			if (State == null) return;

			var kb = Keyboard.current;
			if (kb == null) return;

			if (kb.nKey.wasPressedThisFrame)
				StartNewGame();
			if (kb.mKey.wasPressedThisFrame)
				MenuRequested?.Invoke();
		}

		private void OnDestroy()
		{
			_cts?.Cancel();
			_cts?.Dispose();
			if (HumanX != null) HumanX.SelectionChanged -= OnSelectionChanged;
			if (HumanO != null) HumanO.SelectionChanged -= OnSelectionChanged;
			HumanX?.Dispose();
			HumanO?.Dispose();
		}

		// ---- public commands ----

		/// <summary>Apply settings from a <see cref="GameConfig"/> and start a new game.</summary>
		public void StartWithConfig(GameConfig config)
		{
			if (config == null) throw new ArgumentNullException(nameof(config));
			Size = config.Size;
			XControl = config.XControl;
			OControl = config.OControl;
			AIDepth = config.AIDepth;
			StartNewGame();
		}

		/// <summary>Start a new game with the controller's current Size / Control / Depth settings.</summary>
		public void StartNewGame()
		{
			CancelLoop();
			HumanX?.ClearSelection();
			HumanO?.ClearSelection();
			LastAIStats = null;
			LastAILabel = null;

			FrameCamera(); // Size may have changed since EnsureSubsystems ran.
			BindPlayerSlots();

			_cts = new CancellationTokenSource();
			_ = RunWithErrorHandlingAsync(_cts.Token);
		}

		/// <summary>
		/// Cancel any in-flight game loop and clear all transient state. The main menu calls this
		/// before showing itself, so the next render is a clean slate.
		/// </summary>
		public void Stop()
		{
			CancelLoop();
			HumanX?.ClearSelection();
			HumanO?.ClearSelection();
			State = null;
			LastAIStats = null;
			LastAILabel = null;
			_highlight?.Clear();
			if (_view != null) _view.Render(new Board(Size));
		}

		// ---- main loop ----

		private async Task RunWithErrorHandlingAsync(CancellationToken ct)
		{
			try
			{
				await RunGameAsync(ct);
			}
			catch (OperationCanceledException) { /* expected on quit/restart/menu */ }
			catch (Exception ex)
			{
				Debug.LogError($"Game loop crashed: {ex}");
			}
		}

		private async Task RunGameAsync(CancellationToken ct)
		{
			State = new GameState(Size);
			_view.Render(State.Board);
			RefreshHighlights();

			while (State.Phase != GamePhase.GameOver)
			{
				ct.ThrowIfCancellationRequested();
				var current = CurrentPlayer;
				var move = await current.ChooseMove(State, ct);
				ct.ThrowIfCancellationRequested();

				Rules.Apply(State, move);
				_view.Render(State.Board);
				RefreshHighlights();

				if (current is AIPlayer ai)
				{
					LastAIStats = ai.LastStats;
					LastAILabel = ai.Label;
					if (ai.LastStats != null)
						Debug.Log($"[{ai.Label}] {ai.LastStats}");
				}
			}

			_highlight.Clear();
			Debug.Log(State.Winner == Player.None
					? "Game ended in a draw."
					: $"Game over — {State.Winner} wins.");
		}

		// ---- bootstrapping ----

		private void EnsureSubsystems()
		{
			// BoardView (child)
			_view = GetComponentInChildren<BoardView>();
			if (_view == null)
			{
				var go = new GameObject("BoardView");
				go.transform.SetParent(transform, false);
				_view = go.AddComponent<BoardView>();
			}

			// InputController (child)
			_input = GetComponentInChildren<InputController>();
			if (_input == null)
			{
				var go = new GameObject("InputController");
				go.transform.SetParent(transform, false);
				_input = go.AddComponent<InputController>();
			}

			// HighlightOverlay (child of BoardView so it inherits the board's transform)
			_highlight = GetComponentInChildren<HighlightOverlay>();
			if (_highlight == null)
			{
				var go = new GameObject("Highlights");
				go.transform.SetParent(_view.transform, false);
				_highlight = go.AddComponent<HighlightOverlay>();
			}
			_highlight.View = _view;

			// Camera
			if (RaycastCamera == null) RaycastCamera = Camera.main;
			if (RaycastCamera == null)
			{
				var go = new GameObject("Main Camera")
				{
					tag = "MainCamera"
				};
				RaycastCamera = go.AddComponent<Camera>();
				go.AddComponent<AudioListener>();
			}
			_input.RaycastCamera = RaycastCamera;
			FrameCamera();

			// HumanPlayer instances are stable across restarts (they hold input subscriptions).
			HumanX = new HumanPlayer(_input);
			HumanO = new HumanPlayer(_input);
			HumanX.SelectionChanged += OnSelectionChanged;
			HumanO.SelectionChanged += OnSelectionChanged;
		}

		private void BindPlayerSlots()
		{
			XPlayer = XControl == SideController.AI ? new AIPlayer(AIDepth, "AI X") : HumanX;
			OPlayer = OControl == SideController.AI ? new AIPlayer(AIDepth, "AI O") : HumanO;
		}

		private void OnSelectionChanged() => RefreshHighlights();

		private void RefreshHighlights()
		{
			if (_highlight == null || State == null) return;
			if (State.Phase == GamePhase.GameOver) { _highlight.Clear(); return; }

			var human = CurrentHuman;
			if (human == null || !human.SelectedPiece.HasValue)
				_highlight.Clear();
			else
				_highlight.Show(human.SelectedPiece, human.SlideDestinations, State.Board.Size);
		}

		private void CancelLoop()
		{
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;
		}

		private void FrameCamera()
		{
			if (RaycastCamera == null || _view == null) return;
			int n = (int)Size;
			float pitch = _view.CellSize + _view.CellGap;
			float halfBoard = n * 0.5f * pitch;

			RaycastCamera.orthographic = true;
			RaycastCamera.orthographicSize = halfBoard + CameraMargin;
			RaycastCamera.transform.SetPositionAndRotation(
					transform.position + new Vector3(0f, 10f, 0f),
					Quaternion.Euler(90f, 0f, 0f));
			RaycastCamera.clearFlags = CameraClearFlags.SolidColor;
			RaycastCamera.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
		}
	}
}
