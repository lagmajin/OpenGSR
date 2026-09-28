using System;
using System.Collections.Generic;
using System.Text;

using System.Security.Cryptography;


namespace OpenGSCore
{
    public class Encrypt
    {
        static string EncryptAES(string plainText, byte[] key, byte[] iv)
        {
            var src = Encoding.Unicode.GetBytes(plainText);

            using (AesManaged am = new AesManaged())
            {



            }


            string result="";

            return result;
        }

        static string DecryptAES(string text)
        {
            string result = "";


            return result;
        }


    }
}
