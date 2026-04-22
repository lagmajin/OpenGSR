using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenGS
{
    public class EncryptManager
    {
        public static EncryptManager Instance { get; set; } = new();

        private RSACryptoServiceProvider rsa;

        private EncryptManager()
        {
            rsa = new RSACryptoServiceProvider(1024);
        }

        public void SetRSAPublicKey(in string str)
        {
            rsa.FromXmlString(str);




        }

        public string GetRSAPublicKey()
        {
            return rsa.ToXmlString(false);


        }

        public byte[] Encrypt(in string data)
        {
            var content=Encoding.UTF8.GetBytes(data);

            byte[] encryptedData = rsa.Encrypt(content,false);


            return encryptedData;
        }


        


    }
}
