using UnityEngine;

namespace HollowDemo
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class GroundSurface : MonoBehaviour
    {
        public bool canClimb = true;
    }
}
