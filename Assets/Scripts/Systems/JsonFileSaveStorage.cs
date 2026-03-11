using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace OpenGS
{
    public class JsonFileSaveStorage : ISaveStorage
    {
        public bool Save<T>(string fileName, T data, Formatting formatting = Formatting.Indented)
        {
            try
            {
                string path = GetPath(fileName);
                string tempPath = path + ".tmp";
                string backupPath = path + ".bak";
                string json = JsonConvert.SerializeObject(data, formatting);

                File.WriteAllText(tempPath, json, Encoding.UTF8);

                if (File.Exists(path))
                {
                    File.Copy(path, backupPath, true);
                }

                File.Copy(tempPath, path, true);
                File.Delete(tempPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonFileSaveStorage] Save failed ({fileName}): {e.Message}");
                return false;
            }
        }

        public T Load<T>(string fileName, T defaultValue = default)
        {
            if (TryLoad(fileName, out T loaded))
            {
                return loaded;
            }

            return defaultValue;
        }

        public bool TryLoad<T>(string fileName, out T data)
        {
            string path = GetPath(fileName);
            string backupPath = path + ".bak";

            if (TryLoadFromPath(path, out data))
            {
                return true;
            }

            if (TryLoadFromPath(backupPath, out data))
            {
                Debug.LogWarning($"[JsonFileSaveStorage] Loaded backup save for {fileName}");
                return true;
            }

            data = default;
            return false;
        }

        public bool Delete(string fileName)
        {
            string path = GetPath(fileName);
            string backupPath = path + ".bak";
            bool deleted = false;

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    deleted = true;
                }

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    deleted = true;
                }

                return deleted;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonFileSaveStorage] Delete failed ({fileName}): {e.Message}");
                return false;
            }
        }

        public bool Exists(string fileName)
        {
            return File.Exists(GetPath(fileName));
        }

        public string GetPath(string fileName)
        {
            return FilePathHelper.GetFilePath(fileName);
        }

        private static bool TryLoadFromPath<T>(string path, out T data)
        {
            data = default;

            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                T parsed = JsonConvert.DeserializeObject<T>(json);
                if (parsed == null)
                {
                    return false;
                }

                data = parsed;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
