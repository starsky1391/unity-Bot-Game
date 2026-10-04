using UnityEngine;

namespace HollowDemo
{
    public sealed class WorldLabel : MonoBehaviour
    {
        public string text;
        void Awake()
        {
            var mesh = gameObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = ChineseFont.Shared;
            mesh.fontSize = 48;
            mesh.characterSize = .075f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.color = new Color(.5f, .73f, .8f);
            GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
        }
    }
}
