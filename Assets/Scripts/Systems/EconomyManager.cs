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
        private const int SAVE_VERSION = 1;
        private const string SAVE_FILE = "Economy.json";
        private static EconomyData data;

        private static void LoadData()
        {
            if (data == null)
            {
                data = JsonStorage.LoadVersioned<EconomyData>(
                    SAVE_FILE,
                    SAVE_VERSION,
                    Migrate,
                    null);

                if (data == null)
                {
                    data = JsonStorage.Load<EconomyData>(SAVE_FILE, new EconomyData());
                }

                NormalizeData();
                JsonStorage.SaveVersioned(SAVE_FILE, data, SAVE_VERSION);
            }
        }

        private static void NormalizeData()
        {
            if (data == null)
            {
                data = new EconomyData();
            }

            data.credits = Mathf.Max(0, data.credits);
        }

        private static void SaveData()
        {
            NormalizeData();
            JsonStorage.SaveVersioned(SAVE_FILE, data, SAVE_VERSION);
        }

        private static EconomyData Migrate(int fromVersion, EconomyData oldData)
        {
            return oldData ?? new EconomyData();
        }

        public static int GetCredits()
        {
            LoadData();
            return data.credits;
        }

        public static void AddCredits(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            LoadData();
            data.credits = amount > int.MaxValue - data.credits
                ? int.MaxValue
                : Mathf.Max(0, data.credits + amount);
            SaveData();
        }

        public static void SetCredits(int amount)
        {
            LoadData();
            data.credits = Mathf.Max(0, amount);
            SaveData();
        }

        public static bool SpendCredits(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

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
            return amount > 0 && GetCredits() >= amount;
        }
    }
}
