using UnityEngine;
using ChungToi.Core;

namespace ChungToi.View
{
    /// <summary>
    /// Tiny tag component attached to each board cell GameObject. Lets a raycast hit be mapped
    /// back to a logical <see cref="Coord"/> without parsing object names.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CellRef : MonoBehaviour
    {
        public int Row;
        public int Col;
        public Coord Coord => new Coord(Row, Col);

        public void Set(Coord c)
        {
            Row = c.Row;
            Col = c.Col;
        }
    }
}
