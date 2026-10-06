using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace HollowDemo
{
    public sealed class WorldActivation : MonoBehaviour
    {
        public Vector2 chunkSize = new Vector2(20, 16);
        public Vector2 preloadMargin = new Vector2(12, 10);
        [Min(0)] public float unloadMargin = 8;
        [Min(.02f)] public float checkInterval = .2f;
        sealed class Chunk
        {
            public GameObject root;
            public Bounds bounds;
            public readonly List<Transform> movers = new List<Transform>();
            public BossArena arena;
        }
        readonly List<Chunk> chunks = new List<Chunk>();
        bool built;
        float nextCheck;
        public int ChunkCount => chunks.Count;
        public int ActiveChunkCount => chunks.Count(c => c.root.activeSelf);
        public void RestoreAll()
        {
            foreach (var chunk in chunks)
                if (chunk.root != null && !chunk.root.activeSelf) { foreach (var mover in chunk.movers) mover.GetComponent<EnemyBrain>().ResumeForRegion(); chunk.root.SetActive(true); }
        }
        public void Build()
        {
            if (built) return;
            var candidates = new HashSet<Transform>();
            foreach (var component in FindObjectsOfType<MonoBehaviour>(true))
            {
                if (!(component is GroundSurface || component is EnemyBrain || component is Pickup || component is AbilityUnlock || component is Npc || component is Checkpoint || component is GrapplePoint || component is DamageWater || component is InstantDeathZone || component is DropPlatform || component is BossArena)) continue;
                if (component.transform.parent != null && !component.transform.parent.gameObject.activeInHierarchy) continue;
                candidates.Add(component.transform);
            }
            var groups = new Dictionary<(Transform, int, int), Chunk>();
            Physics2D.SyncTransforms();
            foreach (var target in candidates)
            {
                bool nested = false;
                for (var parent = target.parent; parent != null; parent = parent.parent)
                    if (candidates.Contains(parent)) { nested = true; break; }
                if (nested) continue;
                var originalParent = target.parent;
                var position = originalParent == null ? target.position : originalParent.InverseTransformPoint(target.position);
                int x = Mathf.FloorToInt(position.x / Mathf.Max(1, chunkSize.x)), y = Mathf.FloorToInt(position.y / Mathf.Max(1, chunkSize.y));
                var key = (originalParent, x, y);
                if (!groups.TryGetValue(key, out var chunk))
                {
                    chunk = new Chunk { root = new GameObject("运行区域 " + x + "," + y), bounds = new Bounds(position, Vector3.zero) };
                    chunk.root.transform.SetParent(originalParent, false);
                    groups.Add(key, chunk); chunks.Add(chunk);
                }
                var colliders = target.GetComponentsInChildren<Collider2D>(true);
                foreach (var collider in colliders)
                {
                    var bounds = collider.bounds;
                    if (bounds.size.sqrMagnitude == 0) bounds = new Bounds(collider.transform.position, Vector3.one * 2);
                    Vector3 min = chunk.root.transform.InverseTransformPoint(bounds.min), max = chunk.root.transform.InverseTransformPoint(bounds.max);
                    chunk.bounds.Encapsulate(min); chunk.bounds.Encapsulate(max);
                }
                if (colliders.Length == 0) { chunk.bounds.Encapsulate(position - Vector3.one); chunk.bounds.Encapsulate(position + Vector3.one); }
                target.SetParent(chunk.root.transform, true);
                if (target.GetComponent<EnemyBrain>() != null) chunk.movers.Add(target);
                var arena = target.GetComponent<BossArena>(); if (arena != null) chunk.arena = arena;
            }
            built = true;
        }
        Rect ViewAt(Vector2 position)
        {
            var camera = Camera.main;
            float height = camera == null ? 15 : camera.orthographicSize * 2;
            float width = camera == null ? 26 : height * camera.aspect;
            return new Rect(position - new Vector2(width, height) * .5f - preloadMargin, new Vector2(width, height) + preloadMargin * 2);
        }
        public void ActivateAt(Vector2 position)
        {
            var view = ViewAt(position);
            foreach (var chunk in chunks)
            {
                var local = chunk.bounds;
                foreach (var mover in chunk.movers) local.Encapsulate(chunk.root.transform.InverseTransformPoint(mover.position));
                var a = chunk.root.transform.TransformPoint(local.min); var b = chunk.root.transform.TransformPoint(local.max);
                var bounds = Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
                var range = view;
                if (chunk.root.activeSelf) { range.xMin -= unloadMargin; range.yMin -= unloadMargin; range.xMax += unloadMargin; range.yMax += unloadMargin; }
                bool active = bounds.Overlaps(range, true) || (chunk.arena != null && DemoGame.Instance.ActiveBoss == chunk.arena);
                if (chunk.root.activeSelf != active)
                {
                    foreach (var mover in chunk.movers) { var enemy = mover.GetComponent<EnemyBrain>(); if (active) enemy.ResumeForRegion(); else enemy.PauseForRegion(); }
                    chunk.root.SetActive(active);
                }
            }
            Physics2D.SyncTransforms();
        }
        void Update()
        {
            var game = DemoGame.Instance;
            if (!built || !game.HasRun || game.Paused || game.Player.Dead || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + checkInterval;
            ActivateAt(game.Player.transform.position);
        }
        void OnDisable() { if (Application.isPlaying) RestoreAll(); }
    }
}
