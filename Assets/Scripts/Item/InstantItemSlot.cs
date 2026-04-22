using System.Linq;
using System.Collections.Generic;
using OpenGSCore;


namespace OpenGS
{


    public class AbstractInstantItem
    {
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
        private bool used = false;

        protected EInstantItemType type = EInstantItemType.None;

        

    }

    public class InstantClusterGrenadePack : AbstractInstantItem
    {
        public InstantClusterGrenadePack()
        {
            type = EInstantItemType.None;

        }

    }

    public class InstantPowerGrenadePack : AbstractInstantItem
    {

        public InstantPowerGrenadePack()
        {

        }


    }

    public class InstantMagneticGrenadePack : AbstractInstantItem
    {

    }

    public class InstantBandAid : AbstractInstantItem
    {

    }


    public class InstantItemSlots
    {
        List<AbstractInstantItem> items = new List<AbstractInstantItem>();

        public InstantItemSlots()
        {
            
        }

        public void InsertInstantItem(int i,EInstantItemType type)
        {

        }

        public bool IsUsed(int i)
        {
            return true;
        }

        public bool CanUse()
        {
            return false;
        }

        public int Count()
        {
            return items.Count();
        }

        public AbstractInstantItem GetItemFromSlot(int i = 0)
        {

            return items[i];
        }


    }
}
