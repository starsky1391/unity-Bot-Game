using UnityEngine;

namespace HollowDemo
{
    public sealed class CameraFollow : MonoBehaviour
    {
        public PlayerMotor player;
        public float viewSize = 6.5f;
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
            camera.orthographicSize = viewSize;
            return new Vector3(player.transform.position.x + player.Facing * 1.4f,
                player.transform.position.y + 2, -10);
        }
        public void Snap()
        {
            player = DemoGame.Instance.Player;
            velocity = Vector3.zero;
            transform.position = DesiredPosition();
            DemoGame.Instance.RevealCameraMap(GetComponent<Camera>());
        }
    }
}
