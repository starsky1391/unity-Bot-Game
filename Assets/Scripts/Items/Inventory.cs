using System;

namespace HollowDemo
{
    [Serializable]
    public sealed class Inventory
    {
        public const int Capacity = 16;
        public const int StackLimit = 99;
        [Serializable]
        public sealed class Slot
        {
            public ItemDefinition item;
            public int count;
        }
        public Slot[] slots = new Slot[Capacity];

        public bool CanAdd(ItemDefinition item, int count)
        {
            int available = 0;
            foreach (var slot in slots)
                available += slot == null ? item.stackLimit : slot.item == item ? item.stackLimit - slot.count : 0;
            return count > 0 && count <= available;
        }

        public bool TryAdd(ItemDefinition item, int count)
        {
            if (!CanAdd(item, count)) return false;
            for (int i = 0; i < slots.Length && count > 0; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.item != item) continue;
                int add = Math.Min(count, item.stackLimit - slot.count);
                slot.count += add;
                count -= add;
            }
            for (int i = 0; i < slots.Length && count > 0; i++)
            {
                if (slots[i] != null) continue;
                int add = Math.Min(count, item.stackLimit);
                slots[i] = new Slot { item = item, count = add };
                count -= add;
            }
            return true;
        }

        public int Count(ItemDefinition item)
        {
            int total = 0;
            foreach (var slot in slots) if (slot != null && slot.item == item) total += slot.count;
            return total;
        }

        public bool TryRemove(ItemDefinition item, int count)
        {
            if (count < 0 || Count(item) < count) return false;
            for (int i = 0; i < slots.Length && count > 0; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.item != item) continue;
                int remove = Math.Min(count, slot.count);
                slot.count -= remove;
                count -= remove;
                if (slot.count == 0) slots[i] = null;
            }
            return true;
        }

        public bool TryBuy(ItemDefinition item, int count, ItemDefinition currency, int price)
        {
            if (price < 0 || Count(currency) < price) return false;
            var transaction = new Inventory();
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null) transaction.slots[i] = new Slot { item = slots[i].item, count = slots[i].count };
            transaction.TryRemove(currency, price);
            if (!transaction.TryAdd(item, count)) return false;
            slots = transaction.slots;
            return true;
        }

        public bool Use(int index, PlayerMotor player)
        {
            var slot = slots[index];
            if (slot == null || slot.item.healing <= 0 || player.Health >= player.maxHealth) return false;
            player.Heal(slot.item.healing);
            if (--slot.count == 0) slots[index] = null;
            return true;
        }
    }
}
