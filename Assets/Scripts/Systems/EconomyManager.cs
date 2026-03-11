using UnityEngine;

namespace OpenGS
{
    // JSON保存用のデータクラス
    [System.Serializable]
    public class EconomyData
    {
        public int credits = 1000;
    }

    /// <summary>
    /// ゲーム内通貨（クレジット）の管理を行う。
    /// JsonStorageを利用した永続化。
    /// </summary>
    public static class EconomyManager
    {
        private const string SAVE_FILE = "Economy.json";
        private static EconomyData data;

        private static void LoadData()
        {
            if (data == null)
            {
                data = JsonStorage.Load<EconomyData>(SAVE_FILE, new EconomyData());
            }
        }

        private static void SaveData()
        {
            JsonStorage.Save(SAVE_FILE, data);
        }

        public static int GetCredits()
        {
            LoadData();
            return data.credits;
        }

        public static void AddCredits(int amount)
        {
            LoadData();
            data.credits += amount;
            SaveData();
        }

        public static bool SpendCredits(int amount)
        {
            LoadData();
            if (data.credits >= amount)
            {
                data.credits -= amount;
                SaveData();
                return true;
            }
            return false;
        }

        public static bool CanAfford(int amount)
        {
            return GetCredits() >= amount;
        }
    }
}
