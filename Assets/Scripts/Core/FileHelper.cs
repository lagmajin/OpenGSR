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
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new System.ArgumentException("A save file name is required.", nameof(fileName));
            }

            if (!string.Equals(Path.GetFileName(fileName), fileName, System.StringComparison.Ordinal))
            {
                throw new System.ArgumentException("Save file name must not contain a directory path.", nameof(fileName));
            }

            return Path.Combine(GetWritablePath(), fileName);
        }
    }

}
