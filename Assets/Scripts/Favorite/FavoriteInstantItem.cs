using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenGSCore;

namespace OpenGS
{
    public class FavoriteInstantItem
    {
        private List<InstantItemData> data;
        public FavoriteInstantItem[] fi = new FavoriteInstantItem[3];

        public struct InstantItemData
        {
            EInstantItemType type;
            string name;

        }

        public FavoriteInstantItem()
        {

        }


        void FillAll(InstantItemData data)
        {


        }



        void SetWeapon(int i, InstantItemData data)
        {

        }

    }
}
