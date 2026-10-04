using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HollowDemo
{
    public sealed class MapFog
    {
        public const float CellSize = .5f;
        readonly HashSet<Vector3Int> cells = new HashSet<Vector3Int>();

        public void Clear() => cells.Clear();
        public bool Visible(int room, Vector2 point) => cells.Contains(new Vector3Int(room,
            Mathf.FloorToInt(point.x / CellSize), Mathf.FloorToInt(point.y / CellSize)));

        public bool Reveal(int room, Rect view)
        {
            bool changed = false;
            for (int x = Mathf.FloorToInt(view.xMin / CellSize); x <= Mathf.FloorToInt(view.xMax / CellSize); x++)
                for (int y = Mathf.FloorToInt(view.yMin / CellSize); y <= Mathf.FloorToInt(view.yMax / CellSize); y++)
                    changed |= cells.Add(new Vector3Int(room, x, y));
            return changed;
        }

        public SaveData.MapCell[] Save() => cells.Select(c => new SaveData.MapCell { room = c.x, x = c.y, y = c.z }).ToArray();
        public void Restore(SaveData.MapCell[] saved)
        {
            cells.Clear();
            if (saved != null)
                foreach (var cell in saved) cells.Add(new Vector3Int(cell.room, cell.x, cell.y));
        }

        public IEnumerable<Vector2[]> VisibleSegments(int room, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            var cuts = new List<float> { 0, 1 };
            // 在网格边界切分线段，避免整条房间边缘泄露到未探索区域。
            for (int axis = 0; axis < 2; axis++)
            {
                if (Mathf.Abs(delta[axis]) < .001f) continue;
                int first = Mathf.FloorToInt(Mathf.Min(from[axis], to[axis]) / CellSize) + 1;
                int last = Mathf.CeilToInt(Mathf.Max(from[axis], to[axis]) / CellSize) - 1;
                for (int cell = first; cell <= last; cell++) cuts.Add((cell * CellSize - from[axis]) / delta[axis]);
            }
            cuts.Sort();
            float start = -1;
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                if (cuts[i + 1] - cuts[i] < .0001f) continue;
                bool visible = Visible(room, from + delta * ((cuts[i] + cuts[i + 1]) / 2));
                if (visible && start < 0) start = cuts[i];
                if (!visible && start >= 0)
                {
                    yield return new[] { from + delta * start, from + delta * cuts[i] };
                    start = -1;
                }
            }
            if (start >= 0) yield return new[] { from + delta * start, to };
        }
    }
}
