using System;
using UnityEngine;

namespace HollowDemo
{
    [CreateAssetMenu(menuName = "空洞原型/商店")]
    public sealed class ShopDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Offer
        {
            public ItemDefinition item;
            [Min(1)] public int quantity = 1;
            [Min(0)] public int price = 5;
        }
        public string displayName = "洞穴商店";
        public ItemDefinition currency;
        public Offer[] offers = Array.Empty<Offer>();
    }
}
