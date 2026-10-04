using System.Linq;
using UnityEngine;

namespace HollowDemo
{
    public static class MapGeometry
    {
        public static RoomVolume[] VisibleRooms(RoomVolume[] rooms) => rooms.Where(r => r.explored).ToArray();
        public static Rect KnownBounds(RoomVolume[] rooms)
        {
            var points = VisibleRooms(rooms).SelectMany(r => r.map.outline.Select(p => p + r.map.origin)).ToArray();
            if (points.Length == 0) return new Rect(0, 0, 1, 1);
            return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
        }
        public static System.Collections.Generic.IEnumerable<Vector2[]> Walls(RoomMapDefinition map)
        {
            for (int i = 0; i < map.outline.Length; i++)
            {
                Vector2 a = map.outline[i], b = map.outline[(i + 1) % map.outline.Length];
                Vector2 delta = b - a;
                float length = delta.magnitude;
                var openings = map.exits.Select(e => new { exit = e, t = Vector2.Dot(e.position - a, delta) / delta.sqrMagnitude })
                    .Where(e => e.t >= 0 && e.t <= 1 && Vector2.Distance(a + delta * e.t, e.exit.position) < .01f)
                    .OrderBy(e => e.t);
                float previous = 0;
                foreach (var opening in openings)
                {
                    float start = Mathf.Clamp01(opening.t - .55f / length);
                    if (start > previous) yield return new[] { a + delta * previous + map.origin, a + delta * start + map.origin };
                    previous = Mathf.Max(previous, Mathf.Clamp01(opening.t + .55f / length));
                }
                if (previous < 1) yield return new[] { a + delta * previous + map.origin, b + map.origin };
            }
        }
    }
}
