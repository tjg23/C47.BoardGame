using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using ChungToi.Core;
using ChungToi.View;

namespace ChungToi.Game
{
    /// <summary>
    /// Top-level orchestrator. Owns the <see cref="GameState"/>, the <see cref="BoardView"/>, the
    /// <see cref="InputController"/>, the <see cref="HighlightOverlay"/>, and an <see cref="IPlayer"/>
    /// for each side. Runs the game loop: ask the side-to-move player for a move, apply, render,
    /// repeat.
    ///
    /// Highlight refreshes are driven by <see cref="HumanPlayer.SelectionChanged"/> — both humans'
    /// events route through a single handler that redraws based on whichever side is currently
    /// expected to move.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameController : MonoBehaviour
    {
        [Header("Board")]
        public BoardSize Size = BoardSize.ThreeByThree;

        [Header("Camera")]
        [Tooltip("Falls back to Camera.main if unset.")]
        public Camera RaycastCamera;
        public float CameraMargin = 1.5f;

        public GameState State { get; private set; }
        public HumanPlayer HumanX { get; private set; }
        public HumanPlayer HumanO { get; private set; }

        public IPlayer XPlayer { get; private set; }
        public IPlayer OPlayer { get; private set; }

        private BoardView _view;
        private InputController _input;
        private HighlightOverlay _highlight;
        private CancellationTokenSource _cts;

        public HumanPlayer CurrentHuman
        {
            get
            {
                if (State == null) return null;
                var p = State.ToMove == Player.X ? XPlayer : OPlayer;
                return p as HumanPlayer;
            }
        }

        private void Awake()
        {
            EnsureSubsystems();
        }

        private async void Start()
        {
            _cts = new CancellationTokenSource();
            try
            {
                await RunGameAsync(_cts.Token);
            }
            catch (OperationCanceledException) { /* expected on quit */ }
            catch (Exception ex)
            {
                Debug.LogError($"Game loop crashed: {ex}");
            }
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

        // ---- main loop ----

        private async Task RunGameAsync(CancellationToken ct)
        {
            State = new GameState(Size);
            _view.Render(State.Board);
            RefreshHighlights();

            while (State.Phase != GamePhase.GameOver)
            {
                ct.ThrowIfCancellationRequested();
                var current = State.ToMove == Player.X ? XPlayer : OPlayer;
                var move = await current.ChooseMove(State, ct);

                Rules.Apply(State, move);
                _view.Render(State.Board);
                RefreshHighlights();
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

            // Players (Step 4: humans on both sides)
            HumanX = new HumanPlayer(_input);
            HumanO = new HumanPlayer(_input);
            XPlayer = HumanX;
            OPlayer = HumanO;

            HumanX.SelectionChanged += OnSelectionChanged;
            HumanO.SelectionChanged += OnSelectionChanged;
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
