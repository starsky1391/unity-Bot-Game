using System;
using System.Linq;
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
        public Vector2[] outline = Array.Empty<Vector2>();
        [Min(.01f)] public float mapScale = .4f;
        [HideInInspector] public LegacyFogRegion[] legacyFogRegions = Array.Empty<LegacyFogRegion>();
        GroundSurface[] terrain = Array.Empty<GroundSurface>();
        bool usesCheckpointSpawn;
        float fallOffsetY;
        public Vector2 MapPosition(Vector3 worldPosition) => (Vector2)transform.InverseTransformPoint(worldPosition) * mapScale;
        public float FallY => fallBoundary != null ? fallBoundary.position.y : transform.position.y + fallOffsetY;

        public bool Initialize(Checkpoint[] checkpoints, GroundSurface[] surfaces, out string error)
        {
            error = null;
            terrain = surfaces.Where(g => g.isActiveAndEnabled && g.GetComponent<Collider2D>().enabled).ToArray();
            if (terrain.Length == 0) { error = "关卡没有地面，请放入 Ground 或 Platform 预制件。"; return false; }
            fallOffsetY = terrain.Min(g => g.GetComponent<Collider2D>().bounds.min.y) - transform.position.y - 5;
            var initial = checkpoints.Where(p => p.isInitialSpawn && p.gameObject.activeInHierarchy).ToArray();
            if (initial.Length > 1) { error = "多个检查点勾选了初始出生点，请只保留一个。"; return false; }
            if (initial.Length == 1)
            {
                startPoint = initial[0].spawnPoint;
                usesCheckpointSpawn = true;
            }
            else if (usesCheckpointSpawn) startPoint = null;
            if (startPoint == null) { error = "请放入检查点，勾选初始出生点并配置 Spawn Point。"; return false; }
            if (outline == null || outline.Length < 3)
            {
                Physics2D.SyncTransforms();
                var bounds = terrain[0].GetComponent<Collider2D>().bounds;
                foreach (var ground in terrain.Skip(1)) bounds.Encapsulate(ground.GetComponent<Collider2D>().bounds);
                Vector2 min = transform.InverseTransformPoint(bounds.min - new Vector3(1, 1));
                Vector2 max = transform.InverseTransformPoint(bounds.max + new Vector3(1, 12));
                outline = new[] { min, new Vector2(min.x, max.y), max, new Vector2(max.x, min.y) };
            }
            return true;
        }

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
            position = point == null ? Vector2.zero : (Vector2)point.position;
            if (point == null) return false;
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
