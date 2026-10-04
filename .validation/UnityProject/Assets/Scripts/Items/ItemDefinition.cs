using UnityEngine;

namespace HollowDemo
{
    [CreateAssetMenu(menuName = "空洞原型/物品")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Range(1, 99)] public int stackLimit = 99;
        [Min(0)] public int healing;
        public Color color = Color.white;
    }
}
