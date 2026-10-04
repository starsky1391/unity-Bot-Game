using UnityEngine;

namespace HollowDemo
{
    public sealed class RoomVolume : MonoBehaviour
    {
        public int id;
        public string title, hint;
        [HideInInspector] public Rect bounds;
        public bool explored;
        public string scenePath;
        [UnityEngine.Serialization.FormerlySerializedAs("mapOutline")]
        [HideInInspector] public Vector2[] ceilingOutline;
        public RoomMapDefinition map;
        public int[] neighbors;
    }

}
