using System.Collections.Generic;
using UnityEngine;
using ChungToi.Core;

namespace ChungToi.View
{
	/// <summary>
	/// Renders a <see cref="Board"/> as a top-down 3D grid using runtime primitives only — no
	/// prefabs or sprite assets required. Cells are flat quads (checkerboard); pieces are short
	/// cubes whose Y-rotation indicates orientation (0° = Cardinal, 45° = Diagonal).
	///
	/// One-way data flow: call <see cref="Render"/> with any <see cref="Board"/> and the visuals
	/// are updated. The view itself is stateless beyond the cell grid it builds on first render.
	/// Step 4+ will add input layers; this script knows nothing about input.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class BoardView : MonoBehaviour
	{
		[Header("Layout")]
		public float CellSize = 1.0f;
		public float CellGap = 0.05f;       // visible gap between cells
		public float PieceHeight = 0.4f;
		public float PieceInsetXZ = 0.2f;    // how much smaller than a cell the piece is, total

		[Header("Colors")]
		public Color CellLight = new(0.85f, 0.85f, 0.80f);
		public Color CellDark = new(0.55f, 0.55f, 0.50f);
		public Color XColor = new(0.20f, 0.55f, 0.95f);
		public Color OColor = new(0.95f, 0.35f, 0.30f);

		// Cell GameObjects, indexed [row, col]. Built once per board size.
		private GameObject[,] _cells;
		private BoardSize _builtForSize;
		private bool _gridBuilt;

		// Pieces are recreated on each Render. Simpler than tracking deltas; the boards are tiny.
		private readonly List<GameObject> _pieces = new();

		// Cached unlit material so all primitives share one instance per color (cheap, URP-friendly).
		private Material _materialTemplate;

		private void Awake()
		{
			_materialTemplate = FindUnlitMaterialTemplate();
		}

		public void Render(Board board)
		{
			if (board == null)
			{
				Debug.LogWarning("BoardView.Render called with null board");
				return;
			}

			if (!_gridBuilt || _builtForSize != board.Size)
				BuildGrid(board.Size);

			ClearPieces();
			foreach (var coord in board.AllCoords())
			{
				var cell = board.Get(coord);
				if (cell.IsEmpty) continue;
				_pieces.Add(BuildPiece(coord, cell));
			}
		}

		public Vector3 WorldPosForCell(Coord coord, BoardSize size)
		{
			int n = (int)size;
			float pitch = CellSize + CellGap;
			float originX = -(n - 1) * 0.5f * pitch;
			float originZ = (n - 1) * 0.5f * pitch;
			// Row 0 is at the "top" of the board (positive Z); col 0 is on the left (negative X).
			float x = originX + coord.Col * pitch;
			float z = originZ - coord.Row * pitch;
			return new Vector3(x, 0f, z) + transform.position;
		}

		// ---- internals ----

		private void BuildGrid(BoardSize size)
		{
			// Wipe previous grid (if any).
			if (_cells != null)
			{
				foreach (var go in _cells)
					if (go != null) DestroyImmediate(go);
			}
			ClearPieces();

			int n = (int)size;
			_cells = new GameObject[n, n];

			for (int r = 0; r < n; r++)
				for (int c = 0; c < n; c++)
				{
					var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
					quad.name = $"Cell ({r},{c})";
					quad.transform.SetParent(transform, false);
					// Quads face +Z by default; rotate 90° X so they face up (+Y).
					quad.transform.SetPositionAndRotation(WorldPosForCell(new Coord(r, c), size), Quaternion.Euler(90f, 0f, 0f));
					quad.transform.localScale = new Vector3(CellSize, CellSize, 1f);

					bool light = ((r + c) % 2) == 0;
					ApplyColor(quad, light ? CellLight : CellDark);

					// Tag the quad so the input layer can map a raycast hit back to a Coord.
					quad.AddComponent<CellRef>().Set(new Coord(r, c));

					// The quad's collider is a MeshCollider on a single quad — fine for input later.
					_cells[r, c] = quad;
				}

			_builtForSize = size;
			_gridBuilt = true;
		}

		private GameObject BuildPiece(Coord coord, Cell cell)
		{
			var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
			piece.name = $"Piece {cell.Owner} ({coord.Row},{coord.Col}) {cell.Orient}";
			piece.transform.SetParent(transform, false);

			float size = CellSize - PieceInsetXZ;
			piece.transform.localScale = new Vector3(size, PieceHeight, size);

			var pos = WorldPosForCell(coord, _builtForSize);
			pos.y = PieceHeight * 0.5f + 0.01f; // sit on top of the cell quad
			piece.transform.position = pos;

			float yRot = cell.Orient == Orientation.Cardinal ? 0f : 45f;
			piece.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

			ApplyColor(piece, cell.Owner == Player.X ? XColor : OColor);
			return piece;
		}

		private void ClearPieces()
		{
			for (int i = 0; i < _pieces.Count; i++)
				if (_pieces[i] != null) DestroyImmediate(_pieces[i]);
			_pieces.Clear();
		}

		private void ApplyColor(GameObject go, Color c)
		{
			var mr = go.GetComponent<MeshRenderer>();
			// Each primitive gets its own material instance (cheap; small board). Using sharedMaterial
			// would cause cross-cell color bleed.
			var mat = new Material(_materialTemplate != null ? _materialTemplate : DefaultShaderMaterial())
			{
				color = c
			};
			mr.sharedMaterial = mat;
		}

		private static Material FindUnlitMaterialTemplate()
		{
			// URP's "Universal Render Pipeline/Unlit" gives flat colors — exactly what we want for
			// a board-game readout. Fall back to "Unlit/Color" then to the default shader if not found.
			var shader = Shader.Find("Universal Render Pipeline/Unlit")
					  ?? Shader.Find("Unlit/Color")
					  ?? Shader.Find("Standard");
			return new Material(shader);
		}

		private static Material DefaultShaderMaterial()
		{
			return new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));
		}

		private void OnDestroy()
		{
			// Materials we new'd up don't get cleaned automatically; release them.
			// (Only relevant in the editor; player builds tear down the whole heap on quit.)
			ReleaseMaterials(_pieces);
			if (_cells != null)
			{
				foreach (var go in _cells)
					ReleaseMaterial(go);
			}
		}

		private static void ReleaseMaterials(IEnumerable<GameObject> gos)
		{
			foreach (var go in gos) ReleaseMaterial(go);
		}

		private static void ReleaseMaterial(GameObject go)
		{
			if (go == null) return;
			var mr = go.GetComponent<MeshRenderer>();
			if (mr == null || mr.sharedMaterial == null) return;
			Destroy(mr.sharedMaterial);
		}
	}
}
