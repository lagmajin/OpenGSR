using System;
using System.Collections.Generic;
using System.Text;
using NUlid.Rng;


namespace OpenGSCore
{
    public class GenerateUniqueID
    {

        public static string GenerateId()
        {


            var result = Guid.NewGuid().ToString();


            return result;
        }

    }
}
