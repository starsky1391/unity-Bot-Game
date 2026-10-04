using UnityEngine;

namespace HollowDemo
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class RoomExit : MonoBehaviour
    {
        public int targetRoom;
        public string targetEntrance;
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerMotor>() != null)
                DemoGame.Instance.EnterRoom(targetRoom, targetEntrance);
        }
    }
}
