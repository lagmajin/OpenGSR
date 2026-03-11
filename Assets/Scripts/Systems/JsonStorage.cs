using System;
using UnityEngine;
using Newtonsoft.Json;

namespace OpenGS
{
    /// <summary>
    /// 汎用的なJSONの永続化（保存・読み込み）を行うユーティリティ。
    /// FilePathHelperを利用しているため、エディタでは /SavedData フォルダに、
    /// ビルド後の実機では Application.persistentDataPath に保存されます。
    /// </summary>
    public static class JsonStorage
    {
        [Serializable]
        private class VersionedPayload<T>
        {
            public int version;
            public T payload;
        }

        private static ISaveStorage storage = new JsonFileSaveStorage();

        public static ISaveStorage Storage => storage;

        public static void SetStorage(ISaveStorage customStorage)
        {
            storage = customStorage ?? new JsonFileSaveStorage();
        }

        /// <summary>
        /// オブジェクトをJSON形式で保存します。
        /// </summary>
        /// <typeparam name="T">保存するオブジェクトの型</typeparam>
        /// <param name="fileName">保存ファイル名 (例: mydata.json)</param>
        /// <param name="data">保存するデータ</param>
        /// <param name="formatting">JSONのフォーマット（デフォルトはきれいに改行されるIndented）</param>
        /// <returns>保存に成功したかどうか</returns>
        public static bool Save<T>(string fileName, T data, Formatting formatting = Formatting.Indented)
        {
            if (!storage.Save(fileName, data, formatting))
            {
                Debug.LogError($"[JsonStorage] Save failed ({BuildContext(fileName)})");
                return false;
            }

            Debug.Log($"[JsonStorage] Saved ({BuildContext(fileName)})");
            return true;
        }

        /// <summary>
        /// 指定したファイルからJSONを読み込み、指定の型にデシリアライズして返します。
        /// ファイルが存在しない場合やエラーが発生した場合は、デフォルト値を返します。
        /// </summary>
        /// <typeparam name="T">読み込むデータの型</typeparam>
        /// <param name="fileName">読み込むファイル名</param>
        /// <param name="defaultValue">ファイルが見つからないなどの場合のデフォルト値。指定しなければ型の既定値（new T() や null）</param>
        /// <returns>読み込んだデータ、またはデフォルト値</returns>
        public static T Load<T>(string fileName, T defaultValue = default)
        {
            try
            {
                return storage.Load(fileName, defaultValue);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonStorage] Load failed ({BuildContext(fileName)}): {e.Message}");
                return defaultValue;
            }
        }

        public static bool TryLoad<T>(string fileName, out T data)
        {
            try
            {
                return storage.TryLoad(fileName, out data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonStorage] TryLoad failed ({BuildContext(fileName)}): {e.Message}");
                data = default;
                return false;
            }
        }

        public static bool SaveVersioned<T>(string fileName, T data, int version, Formatting formatting = Formatting.Indented)
        {
            var payload = new VersionedPayload<T>
            {
                version = version,
                payload = data
            };
            bool ok = Save(fileName, payload, formatting);
            if (ok)
            {
                Debug.Log($"[JsonStorage] Versioned save complete ({BuildContext(fileName, version)})");
            }
            return ok;
        }

        public static T LoadVersioned<T>(
            string fileName,
            int currentVersion,
            Func<int, T, T> migrate,
            T defaultValue = default)
        {
            if (!TryLoad(fileName, out VersionedPayload<T> loaded) || loaded == null)
            {
                return defaultValue;
            }

            if (loaded.version == currentVersion)
            {
                Debug.Log($"[JsonStorage] Versioned load hit ({BuildContext(fileName, currentVersion)})");
                return loaded.payload;
            }

            if (migrate == null)
            {
                Debug.LogWarning($"[JsonStorage] Version mismatch ({BuildContext(fileName, currentVersion)}): from={loaded.version}, migrator=null");
                return defaultValue;
            }

            try
            {
                T migrated = migrate(loaded.version, loaded.payload);
                SaveVersioned(fileName, migrated, currentVersion);
                Debug.Log($"[JsonStorage] Migration complete ({BuildContext(fileName, currentVersion)}): from={loaded.version}");
                return migrated;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonStorage] Migration failed ({BuildContext(fileName, currentVersion)}): from={loaded.version}, error={ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// セーブファイルを削除します。
        /// </summary>
        public static bool Delete(string fileName)
        {
            if (storage.Delete(fileName))
            {
                Debug.Log($"[JsonStorage] Deleted ({BuildContext(fileName)})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// セーブファイルが存在するかどうかを確認します。
        /// </summary>
        public static bool Exists(string fileName)
        {
            return storage.Exists(fileName);
        }

        private static string BuildContext(string fileName, int? version = null)
        {
            string path = "(unknown)";
            try
            {
                path = storage.GetPath(fileName);
            }
            catch
            {
            }

            string versionText = version.HasValue ? version.Value.ToString() : "-";
            return $"file={fileName}, version={versionText}, path={path}";
        }
    }
}
