using UnityEngine;

namespace HollowDemo
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class RoomExit : MonoBehaviour
    {
        public int targetRoom;
        public string targetEntrance;
    }
}
