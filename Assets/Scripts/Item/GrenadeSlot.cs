
using System.Collections.Generic;
//using MoreMountains.Tools;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{

    public class GrenadeSlotItem
    {
        //private bool 

        EGrenadeType? _type=null;
        public InstantItemThumbnailMasterData data;

        public GrenadeSlotItem(EGrenadeType? type =null)
        {
            _type = type;
        }

        public EGrenadeType? GrenadeType()
        {
            return _type;
        }

        public void SetGrenadeType(EGrenadeType type)
        {
            _type = type;
        }

        public void Use()
        {


            _type = null;





        }

        public bool IsEmpty()
        {
            return _type == null;
        }

        public void Clear()
        {
            _type = null;
        }

        public override string ToString()
        {
            return _type?.ToString() ?? EGrenadeType.Empty.ToString();
        }

        public string DebugString()
        {
            return ToString();
        }

    }

    public class GrenadeSlots
    {

        private const int SlotCount = 3;
        private readonly List<GrenadeSlotItem> items = new List<GrenadeSlotItem>();
        private readonly GrenadeSlotItem[] slots = new GrenadeSlotItem[SlotCount];

        public GrenadeSlots()
        {
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = new GrenadeSlotItem();
            }
        }

        public bool IsEmpty()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty())
                {
                    return false;
                }
            }


            return true;

        }



        public void FillGrenade(EGrenadeType type = EGrenadeType.Normal)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty())
                {
                    slots[i] = new GrenadeSlotItem(type);
                }
            }


        }

        public void FillNormalGrenade()
        {
            FillGrenade(EGrenadeType.Normal);
        }

        public void RemoveAll()
        {
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i].Clear();
            }
            items.Clear();
        }

        public int Size()
        {
            return slots.Length;
        }

        public int Count()
        {
            var count = 0;
            for (var i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty())
                {
                    count++;
                }
            }
            return count;
        }

        public GrenadeSlotItem Use(int i = 0)
        {
            if (i < 0 || i >= slots.Length || slots[i].IsEmpty())
            {
                return new GrenadeSlotItem();
            }

            var used = slots[i];
            slots[i] = new GrenadeSlotItem();
            return used;
        }


        public string DebugString()
        {
            return $"GrenadeSlot {Count()}/{Size()}";
        }


    }

}
