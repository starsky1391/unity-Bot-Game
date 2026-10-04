using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowDemo
{
    [CreateAssetMenu(menuName = "洞穴 Demo/房间地图")]
    public sealed class RoomMapDefinition : ScriptableObject
    {
        [Serializable] public sealed class Exit
        {
            public int targetRoom;
            public Vector2 position, direction;
        }
        [Serializable] public sealed class Landmark
        {
            public string id, title;
            public Vector2 position;
        }
        public Vector2 origin, checkpointPosition;
        public Vector2[] outline;
        public Exit[] exits;
        public Landmark[] merchants;

        public Vector2 PlayerPosition(Vector2 worldPosition, Rect worldBounds)
        {
            float left = outline[0].x, right = left;
            float bottom = outline[0].y, top = bottom;
            foreach (var p in outline)
            {
                left = Mathf.Min(left, p.x); right = Mathf.Max(right, p.x);
                bottom = Mathf.Min(bottom, p.y); top = Mathf.Max(top, p.y);
            }
            float x = Mathf.Lerp(left + .15f, right - .15f, Mathf.InverseLerp(worldBounds.xMin, worldBounds.xMax, worldPosition.x));
            var crossings = new List<float>();
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i], b = outline[(i + 1) % outline.Length];
                if (a.x == b.x || x < Mathf.Min(a.x, b.x) || x > Mathf.Max(a.x, b.x)) continue;
                float y = Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x));
                crossings.Add(y);
            }
            float mappedY = Mathf.Lerp(bottom, top, Mathf.InverseLerp(0, worldBounds.yMax, worldPosition.y));
            crossings.Sort();
            float markerY = crossings[0], distance = float.PositiveInfinity;
            for (int i = 0; i + 1 < crossings.Count; i += 2)
            {
                float candidate = Mathf.Clamp(mappedY, crossings[i] + .15f, crossings[i + 1] - .15f);
                float difference = Mathf.Abs(candidate - mappedY);
                if (difference < distance) { distance = difference; markerY = candidate; }
            }
            return origin + new Vector2(x, markerY);
        }
    }
}
