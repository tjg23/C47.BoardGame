using System.Collections.Generic;
using UnityEngine;
using ChungToi.Core;

namespace ChungToi.View
{
    /// <summary>
    /// Draws transient board overlays for the input layer: a "selected" tint underneath the
    /// active piece and "legal target" tints on slide destinations. Each overlay is a thin quad
    /// hovering just above its cell (and below pieces) so the player still sees the piece on top.
    ///
    /// Overlay quads have their colliders stripped so raycasts continue to hit the cell quad
    /// underneath — the input layer never has to know overlays exist.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HighlightOverlay : MonoBehaviour
    {
        public BoardView View;

        [Header("Colors")]
        public Color SelectionColor   = new Color(1.0f, 0.85f, 0.20f);
        public Color DestinationColor = new Color(0.30f, 0.85f, 0.40f);

        [Tooltip("Y offset above the cell quad. Must be smaller than the piece's resting Y.")]
        public float OverlayY = 0.02f;

        private readonly List<GameObject> _quads = new List<GameObject>();
        private Material _materialTemplate;

        private void Awake()
        {
            _materialTemplate = MakeUnlitMaterial();
        }

        public void Show(Coord? selection, IReadOnlyList<Coord> destinations, BoardSize size)
        {
            ClearQuads();

            if (selection.HasValue)
                _quads.Add(MakeQuad(selection.Value, size, SelectionColor, "Selection"));

            if (destinations != null)
            {
                for (int i = 0; i < destinations.Count; i++)
                    _quads.Add(MakeQuad(destinations[i], size, DestinationColor, "Destination"));
            }
        }

        public void Clear() => ClearQuads();

        // ---- internals ----

        private GameObject MakeQuad(Coord coord, BoardSize size, Color color, string label)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = $"{label} ({coord.Row},{coord.Col})";
            quad.transform.SetParent(transform, false);

            // Strip the auto-attached MeshCollider so input still raycasts to the cell underneath.
            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            float pitch = View != null ? View.CellSize : 1f;
            var pos = View != null ? View.WorldPosForCell(coord, size) : new Vector3(coord.Col, 0f, -coord.Row);
            pos.y = OverlayY;
            quad.transform.SetPositionAndRotation(pos, Quaternion.Euler(90f, 0f, 0f));
            quad.transform.localScale = new Vector3(pitch, pitch, 1f);

            var mr = quad.GetComponent<MeshRenderer>();
            var mat = new Material(_materialTemplate) { color = color };
            mr.sharedMaterial = mat;
            return quad;
        }

        private void ClearQuads()
        {
            for (int i = 0; i < _quads.Count; i++)
            {
                var q = _quads[i];
                if (q == null) continue;
                var mr = q.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterial != null) Destroy(mr.sharedMaterial);
                Destroy(q);
            }
            _quads.Clear();
        }

        private static Material MakeUnlitMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Color")
                      ?? Shader.Find("Standard");
            return new Material(shader);
        }

        private void OnDestroy()
        {
            ClearQuads();
            if (_materialTemplate != null) Destroy(_materialTemplate);
        }
    }
}
