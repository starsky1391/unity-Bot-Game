using UnityEngine;

namespace HollowDemo
{
    public sealed class MapViewport
    {
        public const float DefaultScale = 32;
        public Vector2 Center { get; private set; }
        public float Scale { get; private set; } = DefaultScale;
        public void Open(Vector2 player) { Center = player; Scale = DefaultScale; }
        public void Recenter(Vector2 player) => Center = player;
        public Vector2 Project(Vector2 position, Rect viewport)
        {
            Vector2 delta = position - Center;
            return viewport.center + new Vector2(delta.x, -delta.y) * Scale;
        }
        public void Pan(Vector2 pixels) => Center -= new Vector2(pixels.x, -pixels.y) / Scale;
        public void Zoom(float scroll, Vector2 mouse, Rect viewport)
        {
            Vector2 offset = mouse - viewport.center;
            offset.y = -offset.y;
            Vector2 anchor = Center + offset / Scale;
            Scale = Mathf.Clamp(Scale * Mathf.Pow(1.15f, -scroll), 8, 96);
            Center = anchor - offset / Scale;
        }
    }
}
