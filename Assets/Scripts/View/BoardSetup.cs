using UnityEngine;
using ChungToi.Core;

namespace ChungToi.View
{
	/// <summary>
	/// Bootstraps a Chung Toi board for visual verification (Step 3 of the build plan):
	/// no input, no AI, just renders a hardcoded test position so you can confirm the BoardView
	/// pipeline works end-to-end. Wire this onto an empty GameObject in the scene; on Play it
	/// builds a <see cref="BoardView"/> as a child, places the camera overhead, and renders the
	/// chosen test board.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class BoardSetup : MonoBehaviour
	{
		public enum TestPosition
		{
			Empty,
			MidPlacement3x3,
			MidGame3x3,
			MidGame4x4,
		}

		[Header("Board")]
		public BoardSize Size = BoardSize.ThreeByThree;
		public TestPosition Position = TestPosition.MidGame3x3;

		[Header("Camera")]
		[Tooltip("If set, this camera is repositioned to look down at the board. Falls back to Camera.main.")]
		public Camera CameraToFrame;
		public float CameraMargin = 1.5f;

		private BoardView _view;
		private Board _board;

		private void Awake()
		{
			EnsureView();
			_board = BuildTestBoard();
			FrameCamera();
		}

		private void Start()
		{
			// Render in Start so all Awake logic (incl. camera) has settled.
			_view.Render(_board);
		}

		// Public so an editor menu / a future input layer can rebuild on demand.
		public void Rebuild()
		{
			EnsureView();
			_board = BuildTestBoard();
			FrameCamera();
			_view.Render(_board);
		}

		// ---- internals ----

		private void EnsureView()
		{
			if (_view != null) return;
			_view = GetComponentInChildren<BoardView>();
			if (_view != null) return;

			var go = new GameObject("BoardView");
			go.transform.SetParent(transform, false);
			_view = go.AddComponent<BoardView>();
		}

		private Board BuildTestBoard()
		{
			var board = Position switch
			{
				TestPosition.Empty => new Board(Size),
				TestPosition.MidPlacement3x3 => MakeMidPlacement3x3(),
				TestPosition.MidGame3x3 => MakeMidGame3x3(),
				TestPosition.MidGame4x4 => MakeMidGame4x4(),
				_ => new Board(Size),
			};
			Size = board.Size; // sync Size so FrameCamera uses the actual board's dimensions
			return board;
		}

		private static Board MakeMidPlacement3x3()
		{
			// Two pieces placed, partway through placement.
			var b = new Board(BoardSize.ThreeByThree);
			b.Set(new Coord(0, 0), new Cell(Player.X, Orientation.Cardinal));
			b.Set(new Coord(2, 2), new Cell(Player.O, Orientation.Diagonal));
			return b;
		}

		private static Board MakeMidGame3x3()
		{
			// Movement-phase-shaped 3x3 with three pieces each, mixed orientations.
			var b = new Board(BoardSize.ThreeByThree);
			b.Set(new Coord(0, 0), new Cell(Player.X, Orientation.Cardinal));
			b.Set(new Coord(0, 1), new Cell(Player.X, Orientation.Diagonal));
			b.Set(new Coord(2, 2), new Cell(Player.X, Orientation.Cardinal));
			b.Set(new Coord(0, 2), new Cell(Player.O, Orientation.Diagonal));
			b.Set(new Coord(1, 0), new Cell(Player.O, Orientation.Cardinal));
			b.Set(new Coord(2, 1), new Cell(Player.O, Orientation.Cardinal));
			return b;
		}

		private static Board MakeMidGame4x4()
		{
			var b = new Board(BoardSize.FourByFour);
			b.Set(new Coord(0, 0), new Cell(Player.X, Orientation.Cardinal));
			b.Set(new Coord(1, 1), new Cell(Player.X, Orientation.Diagonal));
			b.Set(new Coord(2, 2), new Cell(Player.X, Orientation.Cardinal));
			b.Set(new Coord(0, 3), new Cell(Player.X, Orientation.Diagonal));
			b.Set(new Coord(3, 0), new Cell(Player.X, Orientation.Cardinal));
			b.Set(new Coord(0, 1), new Cell(Player.O, Orientation.Cardinal));
			b.Set(new Coord(1, 2), new Cell(Player.O, Orientation.Cardinal));
			b.Set(new Coord(2, 1), new Cell(Player.O, Orientation.Diagonal));
			b.Set(new Coord(3, 2), new Cell(Player.O, Orientation.Cardinal));
			b.Set(new Coord(2, 3), new Cell(Player.O, Orientation.Diagonal));
			return b;
		}

		private void FrameCamera()
		{
			var cam = CameraToFrame != null ? CameraToFrame : Camera.main;
			if (cam == null)
			{
				Debug.LogWarning("BoardSetup: no camera to frame (set CameraToFrame or tag a camera as MainCamera).");
				return;
			}

			int n = (int)Size;
			float pitch = _view != null ? _view.CellSize + _view.CellGap : 1.05f;
			float halfBoard = n * 0.5f * pitch;

			cam.orthographic = true;
			cam.orthographicSize = halfBoard + CameraMargin;

			cam.transform.SetPositionAndRotation(transform.position + new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
		}
	}
}
