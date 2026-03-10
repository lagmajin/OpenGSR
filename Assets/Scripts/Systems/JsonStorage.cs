using System;
using System.IO;
using System.Text;
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
            try
            {
                string path = FilePathHelper.GetFilePath(fileName);
                string json = JsonConvert.SerializeObject(data, formatting);
                File.WriteAllText(path, json, Encoding.UTF8);
                Debug.Log($"[JsonStorage] Saved: {path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonStorage] Save Failed ({fileName}): {e.Message}");
                return false;
            }
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
            string path = FilePathHelper.GetFilePath(fileName);

            if (!File.Exists(path))
            {
                // ファイルが無い場合はデフォルトを返す
                return defaultValue;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                T data = JsonConvert.DeserializeObject<T>(json);

                // もし中身が空っぽ等で null になった場合はデフォルト値を返す
                if (data == null)
                {
                    return defaultValue;
                }

                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonStorage] Load Failed ({fileName}): {e.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// セーブファイルを削除します。
        /// </summary>
        public static bool Delete(string fileName)
        {
            string path = FilePathHelper.GetFilePath(fileName);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    Debug.Log($"[JsonStorage] Deleted: {path}");
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[JsonStorage] Delete Failed ({fileName}): {e.Message}");
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// セーブファイルが存在するかどうかを確認します。
        /// </summary>
        public static bool Exists(string fileName)
        {
            string path = FilePathHelper.GetFilePath(fileName);
            return File.Exists(path);
        }
    }
}
