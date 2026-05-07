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
    /// Per-side <see cref="SideController"/> picks Human or AI for X and O. Step 6 default is
    /// X = Human, O = AI at depth 3. Press <b>N</b> to start a new game.
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

        public GameState State { get; private set; }

        public IPlayer XPlayer { get; private set; }
        public IPlayer OPlayer { get; private set; }
        public HumanPlayer HumanX { get; private set; }
        public HumanPlayer HumanO { get; private set; }

        // Most recent AI search result for display in the HUD. Set after every AI move.
        public SearchStats LastAIStats { get; private set; }
        public string LastAILabel { get; private set; }

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
        public AIPlayer    CurrentAI    => CurrentPlayer as AIPlayer;

        // ---- lifecycle ----

        private void Awake()
        {
            EnsureSubsystems();
        }

        private void Start()
        {
            StartNewGame();
        }

        private void Update()
        {
            // N = new game. Works mid-game (cancels in-flight AI search) and after game over.
            var kb = Keyboard.current;
            if (kb != null && kb.nKey.wasPressedThisFrame)
                StartNewGame();
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

        public void StartNewGame()
        {
            // Cancel any in-flight loop; the awaiting task's continuation will throw OCE and exit
            // cleanly. The AI worker (if any) keeps running until done but its result is discarded.
            _cts?.Cancel();
            _cts?.Dispose();

            HumanX?.ClearSelection();
            HumanO?.ClearSelection();
            LastAIStats = null;
            LastAILabel = null;

            // Re-bind player slots — lets you flip XControl/OControl in the Inspector and press N.
            BindPlayerSlots();

            _cts = new CancellationTokenSource();
            _ = RunWithErrorHandlingAsync(_cts.Token);
        }

        // ---- main loop ----

        private async Task RunWithErrorHandlingAsync(CancellationToken ct)
        {
            try
            {
                await RunGameAsync(ct);
            }
            catch (OperationCanceledException) { /* expected on quit/restart */ }
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
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
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
            XPlayer = XControl == SideController.AI ? new AIPlayer(AIDepth, "AI X") : (IPlayer)HumanX;
            OPlayer = OControl == SideController.AI ? new AIPlayer(AIDepth, "AI O") : (IPlayer)HumanO;
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

        private void FrameCamera()
        {
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
