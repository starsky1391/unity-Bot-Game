using UnityEngine;

namespace HollowDemo
{
    public enum ItemKind { Consumable, Material, RefillableFlask, EmptyFlask, Currency, ManaFlask }
    [CreateAssetMenu(menuName = "空洞原型/物品")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public ItemKind kind;
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Range(1, 99)] public int stackLimit = 99;
        [Min(0)] public int healing;
        [Min(0)] public int manaRecovery = 50;
        public Color color = Color.white;
        public Sprite icon;
        [Tooltip("耗尽后保留装备图标；关闭时耗尽会清空装备槽")]
        public bool retainWhenEmpty;
    }
}
