using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HollowDemo
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapCanvasGraphic : MaskableGraphic, IDragHandler, IScrollHandler
    {
        [UnityEngine.Serialization.FormerlySerializedAs("currentRoomColor")]
        public Color outlineColor = new Color(.5f, .95f, .9f);
        public Color checkpointColor = new Color(.4f, 1, .65f);
        public Color merchantColor = new Color(.9f, .68f, .32f);
        public Color playerColor = Color.white;
        public float lineWidth = 2, iconSize = 12;
        readonly MapViewport view = new MapViewport();
        DemoGame Game => DemoGame.Instance;
        Vector2 PlayerPosition => Game.World.MapPosition(Game.Player.transform.position);
        Rect ViewRect => new Rect(0, 0, rectTransform.rect.width, rectTransform.rect.height);
        protected override void OnEnable()
        {
            base.OnEnable();
            if (Application.isPlaying && Game != null && Game.World != null) view.Open(PlayerPosition);
        }
        public void Recenter() { view.Recenter(PlayerPosition); SetVerticesDirty(); }
        public void OnDrag(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.pressEventCamera, out var now);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position - data.delta, data.pressEventCamera, out var before);
            Vector2 delta = now - before;
            view.Pan(new Vector2(delta.x, -delta.y));
            SetVerticesDirty();
        }
        public void OnScroll(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.enterEventCamera, out var point);
            var rect = rectTransform.rect;
            view.Zoom(-data.scrollDelta.y, new Vector2(point.x - rect.xMin, rect.yMax - point.y), ViewRect);
            SetVerticesDirty();
        }
        Vector2 Project(Vector2 point)
        {
            Vector2 p = view.Project(point, ViewRect);
            return new Vector2(rectTransform.rect.xMin + p.x, rectTransform.rect.yMax - p.y);
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!Application.isPlaying || Game == null || Game.World == null) return;
            var world = Game.World;
            for (int i = 0; i < world.outline.Length; i++)
            {
                Vector2 a = world.outline[i] * world.mapScale;
                Vector2 b = world.outline[(i + 1) % world.outline.Length] * world.mapScale;
                foreach (var segment in Game.MapFog.VisibleSegments(0, a, b))
                    Line(mesh, Project(segment[0]), Project(segment[1]), outlineColor);
            }
            foreach (var checkpoint in Game.Checkpoints)
            {
                Vector2 point = world.MapPosition(checkpoint.transform.position);
                if (Game.CheckpointActivated(checkpoint.persistentId) && Game.MapFog.Visible(0, point))
                    Icon(mesh, Project(point), checkpointColor, true);
            }
            foreach (var npc in Game.Npcs)
            {
                Vector2 point = world.MapPosition(npc.transform.position);
                if (Game.LandmarkDiscovered(npc.mapLandmarkId) && Game.MapFog.Visible(0, point))
                    Icon(mesh, Project(point), merchantColor, false);
            }
            Icon(mesh, Project(PlayerPosition), playerColor, false);
        }
        void Line(VertexHelper mesh, Vector2 from, Vector2 to, Color ink)
        {
            Vector2 normal = new Vector2(-(to - from).y, (to - from).x).normalized * lineWidth / 2;
            Quad(mesh, from - normal, from + normal, to + normal, to - normal, ink);
        }
        void Icon(VertexHelper mesh, Vector2 p, Color ink, bool diamond)
        {
            float r = iconSize / 2;
            if (diamond) Quad(mesh, p + Vector2.left * r, p + Vector2.up * r, p + Vector2.right * r, p + Vector2.down * r, ink);
            else Quad(mesh, p + new Vector2(-r,-r), p + new Vector2(-r,r), p + new Vector2(r,r), p + new Vector2(r,-r), ink);
        }
        static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ink)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, ink, Vector2.zero); mesh.AddVert(b, ink, Vector2.zero);
            mesh.AddVert(c, ink, Vector2.zero); mesh.AddVert(d, ink, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
