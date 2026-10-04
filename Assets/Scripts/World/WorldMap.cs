using System;
using UnityEngine;

namespace HollowDemo
{
    public sealed class WorldMap : MonoBehaviour
    {
        [Serializable] public struct LegacyFogRegion
        {
            public int id;
            public Rect oldBounds, mapBounds;
        }
        public Transform startPoint, finish, fallBoundary;
        public Vector2[] outline;
        [Min(.01f)] public float mapScale = .4f;
        [HideInInspector] public LegacyFogRegion[] legacyFogRegions;
        public Vector2 MapPosition(Vector3 worldPosition) => (Vector2)transform.InverseTransformPoint(worldPosition) * mapScale;
        public float FallY => fallBoundary.position.y;

        public Rect MapView(Camera camera)
        {
            Vector2 min = MapPosition(camera.ViewportToWorldPoint(new Vector3(0, 0, -camera.transform.position.z)));
            Vector2 max = MapPosition(camera.ViewportToWorldPoint(new Vector3(1, 1, -camera.transform.position.z)));
            Vector2 outlineMin = outline[0] * mapScale, outlineMax = outlineMin;
            foreach (var point in outline)
            {
                outlineMin = Vector2.Min(outlineMin, point * mapScale);
                outlineMax = Vector2.Max(outlineMax, point * mapScale);
            }
            return Rect.MinMaxRect(Mathf.Max(min.x, outlineMin.x), Mathf.Max(min.y, outlineMin.y),
                Mathf.Min(max.x, outlineMax.x), Mathf.Min(max.y, outlineMax.y));
        }

        public bool TryGetSpawn(Transform point, out Vector2 position)
        {
            Physics2D.SyncTransforms();
            position = point.position;
            var floor = Physics2D.BoxCast(position + Vector2.up * .1f, new Vector2(.65f, .1f), 0,
                Vector2.down, 4, 1 << 8);
            if (floor.collider == null || floor.normal.y < .5f) return false;
            position.y = Mathf.Max(position.y, floor.point.y + .72f);
            return position.y > FallY && Physics2D.OverlapBox(position, new Vector2(.65f, 1.3f), 0, 1 << 8) == null;
        }

        public void RestoreFog(MapFog fog, SaveData data)
        {
            if (data.version >= 2) { fog.Restore(data.exploredMapCells); return; }
            if (data.exploredMapCells == null) return;
            foreach (var cell in data.exploredMapCells)
                foreach (var region in legacyFogRegions)
                    if (region.id == cell.room)
                    {
                        Vector2 min = new Vector2(cell.x, cell.y) * MapFog.CellSize;
                        Vector2 max = min + Vector2.one * MapFog.CellSize;
                        fog.Reveal(0, Rect.MinMaxRect(
                            Mathf.Lerp(region.mapBounds.xMin, region.mapBounds.xMax, Mathf.InverseLerp(region.oldBounds.xMin, region.oldBounds.xMax, min.x)),
                            Mathf.Lerp(region.mapBounds.yMin, region.mapBounds.yMax, Mathf.InverseLerp(region.oldBounds.yMin, region.oldBounds.yMax, min.y)),
                            Mathf.Lerp(region.mapBounds.xMin, region.mapBounds.xMax, Mathf.InverseLerp(region.oldBounds.xMin, region.oldBounds.xMax, max.x)),
                            Mathf.Lerp(region.mapBounds.yMin, region.mapBounds.yMax, Mathf.InverseLerp(region.oldBounds.yMin, region.oldBounds.yMax, max.y))));
                        break;
                    }
        }

        void OnDrawGizmosSelected()
        {
            if (startPoint != null) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(startPoint.position, .4f); }
            if (fallBoundary != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(fallBoundary.position + Vector3.left * 150, fallBoundary.position + Vector3.right * 150);
            }
        }
    }
}
