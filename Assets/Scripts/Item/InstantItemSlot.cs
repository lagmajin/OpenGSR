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

        public AbstractInstantItem GetItemFromSlot(int i = 0)
        {
            if (i < 0 || i >= items.Count)
            {
                return null;
            }

            return items[i];
        }

        private static AbstractInstantItem CreateItem(EInstantItemType type)
        {
            return type switch
            {
                EInstantItemType.ClusterGrenadePack => new InstantClusterGrenadePack(),
                EInstantItemType.PowerGrenadePack => new InstantPowerGrenadePack(),
                EInstantItemType.MagnetGrenadePack => new InstantMagneticGrenadePack(),
                EInstantItemType.HealthKit => new InstantBandAid(),
                _ => new InstantBandAid()
            };
        }
    }
}
