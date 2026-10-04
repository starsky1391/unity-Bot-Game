using UnityEngine;

namespace HollowDemo
{
    public sealed class CameraFollow : MonoBehaviour
    {
        public PlayerMotor player;
        public float viewSize = 6.5f;
        public Vector2 levelMin = new Vector2(-5, -3), levelMax = new Vector2(188, 17);
        Vector3 velocity;
        void LateUpdate()
        {
            if (player == null || DemoGame.Instance.Paused) return;
            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, .16f);
            DemoGame.Instance.RevealCameraMap(GetComponent<Camera>());
        }
        Vector3 DesiredPosition()
        {
            var camera = GetComponent<Camera>();
            var bounds = DemoGame.Instance.ActiveRoom.Bounds;
            camera.orthographicSize = Mathf.Min(viewSize, bounds.height / 2, bounds.width / (2 * camera.aspect));
            levelMin = bounds.min;
            levelMax = bounds.max;
            float height = camera.orthographicSize, width = height * camera.aspect;
            return new Vector3(levelMax.x - levelMin.x < width * 2 ? bounds.center.x :
                Mathf.Clamp(player.transform.position.x + player.Facing * 1.4f, levelMin.x + width, levelMax.x - width),
                levelMax.y - levelMin.y < height * 2 ? bounds.center.y :
                Mathf.Clamp(player.transform.position.y + 2, levelMin.y + height, levelMax.y - height), -10);
        }
        public void Snap()
        {
            velocity = Vector3.zero;
            transform.position = DesiredPosition();
            DemoGame.Instance.RevealCameraMap(GetComponent<Camera>());
        }
    }
}
