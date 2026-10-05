using UnityEngine;

namespace HollowDemo
{
    public sealed class GrapplePoint : MonoBehaviour
    {
        public bool isAvailable = true;
        public Color highlightColor = new Color(1, .85f, .25f);
        public bool IsHighlighted { get; private set; }
        SpriteRenderer visual;
        Color originalColor;
        void Awake() { visual = GetComponent<SpriteRenderer>(); if (visual != null) originalColor = visual.color; }
        public void SetHighlighted(bool highlighted)
        {
            IsHighlighted = highlighted;
            if (visual != null) visual.color = highlighted ? highlightColor : originalColor;
        }
        void OnDisable() => SetHighlighted(false);
    }
}
