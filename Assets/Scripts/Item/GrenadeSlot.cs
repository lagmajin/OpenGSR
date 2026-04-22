
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

        }

        public override string ToString()
        {

            return _type.ToString();
        }

        public string DebugString()
        {
            return "";
        }

    }

    public class GrenadeSlots
    {

        List<GrenadeSlotItem> items = new List<GrenadeSlotItem>();

        GrenadeSlotItem[] slots = new GrenadeSlotItem[3];

        public bool IsEmpty()
        {
            for (int i = 0; i < 3; i++)
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
            for (int i = 0; i < 2; i++)
            {
                if (!slots[i].IsEmpty())
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

        }

        public int Size()
        {
            return 2;
        }

        public int Count()
        {
            return 3;
        }

        public GrenadeSlotItem Use(int i = 0)
        {
            if (slots[i].IsEmpty())
            {

            }
            else
            {

            }

            return new GrenadeSlotItem();
        }


        public string DebugString()
        {

            return "GrenadeSlot";
        }


    }

}
