using UnityEngine;

namespace HollowDemo
{
    public sealed class RoomContent : MonoBehaviour
    {
        public int roomId;
        public Transform cameraMin, cameraMax, finish;
        public Checkpoint checkpoint;
        public RoomEntrance startEntrance;
        public Rect Bounds => Rect.MinMaxRect(cameraMin.position.x, cameraMin.position.y, cameraMax.position.x, cameraMax.position.y);
        public Rect MapViewBounds(RoomMapDefinition map, Camera camera)
        {
            Vector2 min = transform.InverseTransformPoint(cameraMin.position);
            Vector2 max = transform.InverseTransformPoint(cameraMax.position);
            Vector2 viewMin = transform.InverseTransformPoint(camera.ViewportToWorldPoint(new Vector3(0, 0, -camera.transform.position.z)));
            Vector2 viewMax = transform.InverseTransformPoint(camera.ViewportToWorldPoint(new Vector3(1, 1, -camera.transform.position.z)));
            // 相机平滑移动会无限接近限制位置，将贴近边界的视野按实际房间边界处理。
            if (Mathf.Abs(viewMin.x - min.x) < .01f) viewMin.x = min.x;
            if (Mathf.Abs(viewMin.y - min.y) < .01f) viewMin.y = min.y;
            if (Mathf.Abs(viewMax.x - max.x) < .01f) viewMax.x = max.x;
            if (Mathf.Abs(viewMax.y - max.y) < .01f) viewMax.y = max.y;
            Vector2 mapMin = map.outline[0], mapMax = mapMin;
            foreach (var point in map.outline)
            {
                mapMin = Vector2.Min(mapMin, point);
                mapMax = Vector2.Max(mapMax, point);
            }
            // 与当前位置标记使用同一比例，但不将视野边缘吸附到通道轮廓。
            return Rect.MinMaxRect(
                Mathf.Lerp(mapMin.x, mapMax.x, Mathf.InverseLerp(min.x, max.x, viewMin.x)),
                Mathf.Max(mapMin.y, Mathf.LerpUnclamped(mapMin.y, mapMax.y, viewMin.y / max.y)),
                Mathf.Lerp(mapMin.x, mapMax.x, Mathf.InverseLerp(min.x, max.x, viewMax.x)),
                Mathf.Min(mapMax.y, Mathf.LerpUnclamped(mapMin.y, mapMax.y, viewMax.y / max.y)));
        }
        public Vector2 MapPosition(RoomMapDefinition map, Vector2 worldPosition)
        {
            Vector2 min = transform.InverseTransformPoint(cameraMin.position);
            Vector2 max = transform.InverseTransformPoint(cameraMax.position);
            return map.PlayerPosition(transform.InverseTransformPoint(worldPosition), Rect.MinMaxRect(min.x, min.y, max.x, max.y));
        }
    }
}
