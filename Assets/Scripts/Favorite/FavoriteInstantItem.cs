using System.Collections.Generic;
using OpenGSCore;

namespace OpenGS
{
    public class FavoriteInstantItem
    {
        private const int SlotCount = 3;
        private readonly List<InstantItemData> data;

        public FavoriteInstantItem[] fi = new FavoriteInstantItem[SlotCount];

        public struct InstantItemData
        {
            public EInstantItemType Type;
            public string Name;

            public InstantItemData(EInstantItemType type, string name = null)
            {
                Type = type;
                Name = name ?? type.ToString();
            }
        }

        public FavoriteInstantItem()
        {
            data = new List<InstantItemData>(SlotCount);
            for (var i = 0; i < SlotCount; i++)
            {
                data.Add(new InstantItemData(EInstantItemType.None));
            }
        }

        public IReadOnlyList<InstantItemData> Data => data;

        public void FillAll(InstantItemData item)
        {
            for (var i = 0; i < SlotCount; i++)
            {
                data[i] = item;
            }
        }

        public bool SetWeapon(int i, InstantItemData item)
        {
            if (i < 0 || i >= SlotCount)
            {
                return false;
            }

            data[i] = item;
            return true;
        }
    }
}
