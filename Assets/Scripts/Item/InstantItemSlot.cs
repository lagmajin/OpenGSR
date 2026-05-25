using System.Collections.Generic;
using System.Linq;
using OpenGSCore;

namespace OpenGS
{
    public abstract class AbstractInstantItem
    {
        private bool used;

        protected EInstantItemType type = EInstantItemType.None;

        public EInstantItemType Type => type;

        public bool CanUse()
        {
            return !used;
        }

        public void Use()
        {
            used = true;
        }

        public bool Used()
        {
            return used;
        }
    }

    public class InstantClusterGrenadePack : AbstractInstantItem
    {
        public InstantClusterGrenadePack()
        {
            type = EInstantItemType.ClusterGrenadePack;
        }
    }

    public class InstantPowerGrenadePack : AbstractInstantItem
    {
        public InstantPowerGrenadePack()
        {
            type = EInstantItemType.PowerGrenadePack;
        }
    }

    public class InstantMagneticGrenadePack : AbstractInstantItem
    {
        public InstantMagneticGrenadePack()
        {
            type = EInstantItemType.MagnetGrenadePack;
        }
    }

    public class InstantBandAid : AbstractInstantItem
    {
        public InstantBandAid()
        {
            type = EInstantItemType.HealthKit;
        }
    }

    public class InstantItemSlots
    {
        private readonly List<AbstractInstantItem> items = new List<AbstractInstantItem>(3);

        public InstantItemSlots()
        {
            for (var index = 0; index < 3; index++)
            {
                items.Add(null);
            }
        }

        public void InsertInstantItem(int i, EInstantItemType type)
        {
            if (i < 0 || i >= items.Count)
            {
                return;
            }

            items[i] = CreateItem(type);
        }

        public void SetFromEquippedItems(IEnumerable<EInstantItemType> equippedItems)
        {
            Clear();

            if (equippedItems == null)
            {
                return;
            }

            var index = 0;
            foreach (var type in equippedItems)
            {
                if (index >= items.Count)
                {
                    break;
                }

                InsertInstantItem(index, type);
                index++;
            }
        }

        public void Clear()
        {
            for (var index = 0; index < items.Count; index++)
            {
                items[index] = null;
            }
        }

        public bool TryUse(int i, out EInstantItemType type)
        {
            type = EInstantItemType.None;

            if (i < 0 || i >= items.Count)
            {
                return false;
            }

            var item = items[i];
            if (item == null || !item.CanUse())
            {
                return false;
            }

            type = item.Type;
            item.Use();
            return true;
        }

        public EInstantItemType GetSlotType(int i)
        {
            if (i < 0 || i >= items.Count || items[i] == null)
            {
                return EInstantItemType.None;
            }

            return items[i].Type;
        }

        public bool IsUsed(int i)
        {
            if (i < 0 || i >= items.Count || items[i] == null)
            {
                return true;
            }

            return items[i].Used();
        }

        public bool CanUse()
        {
            return items.Any(item => item != null && item.CanUse());
        }

        public int Count()
        {
            return items.Count;
        }

        public IReadOnlyList<AbstractInstantItem> GetItems()
        {
            return items;
        }

        public AbstractInstantItem GetItemFromSlot(int i = 0)
        {
            if (i < 0 || i >= items.Count)
            {
                UnityEngine.Debug.LogWarning($"[InstantItemSlots] Slot index out of range: {i}");
                return null;
            }

            var item = items[i];
            if (item == null)
            {
                UnityEngine.Debug.LogWarning($"[InstantItemSlots] No item in slot: {i}");
            }

            return item;
        }

        private static AbstractInstantItem CreateItem(EInstantItemType type)
        {
            return type switch
            {
                EInstantItemType.ClusterGrenadePack => new InstantClusterGrenadePack(),
                EInstantItemType.PowerGrenadePack => new InstantPowerGrenadePack(),
                EInstantItemType.MagnetGrenadePack => new InstantMagneticGrenadePack(),
                EInstantItemType.HealthKit => new InstantBandAid(),
                EInstantItemType.None => null,
                _ => null
            };
        }
    }
}
