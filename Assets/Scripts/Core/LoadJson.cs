using System;
using System.IO;
using Newtonsoft.Json.Linq;


namespace OpenGS
{
    public static class IO
    {
        public static void WriteToFile(string filepath,string str)
        {
            if (string.IsNullOrWhiteSpace(filepath))
            {
                throw new ArgumentException("A file path is required.", nameof(filepath));
            }

            var directory = Path.GetDirectoryName(filepath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

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

        public static JObject ReadFromFile(string filepath)
        {
            if (string.IsNullOrWhiteSpace(filepath) || !File.Exists(filepath))
            {
                return new JObject();
            }

            try
            {
                var content = File.ReadAllText(filepath);
                return string.IsNullOrWhiteSpace(content)
                    ? new JObject()
                    : JObject.Parse(content);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[JsonIO] Failed to read '{filepath}': {ex.Message}");
                return new JObject();
            }
        }

        public static void WriteToFile(string filepath,JObject json)
        {
            if (string.IsNullOrWhiteSpace(filepath))
            {
                throw new ArgumentException("A file path is required.", nameof(filepath));
            }

            var directory = Path.GetDirectoryName(filepath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filepath, json?.ToString() ?? "{}");


        }



    }
}
