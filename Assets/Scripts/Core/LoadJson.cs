using System;
using System.IO;
using Newtonsoft.Json.Linq;


namespace OpenGS
{
    public static class IO
    {
        public static void WriteToFile(string filepath,string str)
        {
            File.WriteAllText(filepath, str);
        }
    }

    public static class JsonIO
    {
        public static JObject ReadFromFile()
        {


            var result = new JObject();

            return result;
        }

        public static void WriteToFile(string filepath,JObject json)
        {
            File.WriteAllText(filepath, json.ToString());


        }



    }
}
