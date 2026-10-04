using UnityEngine;

namespace HollowDemo
{
    public sealed class Pickup : Interactable
    {
        [HideInInspector] public string persistentId;
        public ItemDefinition item;
        [Min(1)] public int count = 1;
        public string DisplayName => item.displayName;
        public override string Prompt => "[E] 拾取 " + DisplayName + " ×" + count;
        public override void Interact(DemoGame game)
        {
            if (game.PickupCollected(persistentId)) return;
            if (!game.inventory.TryAdd(item, count)) { game.ShowNotice("背包空间不足，物品保留在原地"); return; }
            game.ShowNotice("获得 " + DisplayName + " ×" + count);
            gameObject.SetActive(false);
            game.RecordPickup(persistentId);
            game.SaveProgress();
        }
    }

}
