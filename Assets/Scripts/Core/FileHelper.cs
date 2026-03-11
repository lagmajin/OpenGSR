using System.IO;
using UnityEngine;


namespace OpenGS
{
    public static class FilePathHelper
    {
        public static string GetWritablePath()
        {
            string path = Application.isEditor
                ? Path.Combine(Application.dataPath, "../SavedData") // エディタ用
                : Application.persistentDataPath; // ランタイム用

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        public static string GetFilePath(string fileName)
        {
            return Path.Combine(GetWritablePath(), fileName);
        }
    }

}