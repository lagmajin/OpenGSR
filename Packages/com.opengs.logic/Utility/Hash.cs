using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace OpenGSCore
{
    public class Hash
    {
        static readonly string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
        public static string CreateSalt(int length)
        {
            StringBuilder sb = new StringBuilder(length);
            var r = new Random();
            for (int i = 0; i < length; i++)
            {
                int pos = r.Next(chars.Length);
                char c = chars[pos];
                sb.Append(c);
            }
            return sb.ToString();
        }




        public static string CreateHash(in string str)
        {
            var csp = new SHA256CryptoServiceProvider();

            var bytes = Encoding.UTF8.GetBytes(str);

            var hash = csp.ComputeHash(bytes);

            var hashStr = new StringBuilder();
            foreach (var hashByte in hash)
            {
                hashStr.Append(hashByte.ToString("x2"));
            }

            return hashStr.ToString();

        }

        public static string CreateHashWithSalt(in string str, in string salt)
        {

            var st = salt + str;

            var csp = new SHA256CryptoServiceProvider();

            var bytes = Encoding.UTF8.GetBytes(str);

            var hash = csp.ComputeHash(bytes);

            var hashStr = new StringBuilder();
            foreach (var hashByte in hash)
            {
                hashStr.Append(hashByte.ToString("x2"));
            }

            return hashStr.ToString();

        }



    }
}
