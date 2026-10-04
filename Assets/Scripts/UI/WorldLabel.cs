using UnityEngine;
using UnityEngine.UI;
namespace HollowDemo
{
    public sealed class WorldLabel : MonoBehaviour
    {
        public string text;
        public Text label;
        void Awake()
        {
            if (label == null) return;
            label.font = ChineseFont.Shared;
            var npc = GetComponentInParent<Npc>();
            label.text = npc == null ? text : npc.displayName;
        }
        void OnValidate()
        {
            if (label != null) label.text = text;
        }
    }
}
