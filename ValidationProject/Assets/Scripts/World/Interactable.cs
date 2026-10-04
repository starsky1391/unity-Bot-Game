using UnityEngine;

namespace HollowDemo
{
    public abstract class Interactable : MonoBehaviour
    {
        public float interactionRange = 1.65f;
        public abstract string Prompt { get; }
        public abstract void Interact(DemoGame game);
    }
}
