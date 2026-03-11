using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OpenGSCore;

namespace OpenGS
{
    [System.Serializable]
    public class EquipInstantItemData
    {
        // 保持できるアイテムの最大数
        public const int MAX_ITEMS = 3;

        // 保持しているアイテムのリスト
        private List<EInstantItemType> items = new List<EInstantItemType>();

        // アイテムを追加（最大3つまで）
        public bool AddItem(EInstantItemType item)
        {
            if (items.Count >= MAX_ITEMS)
            {
                Debug.LogWarning($"アイテムは最大{MAX_ITEMS}つまでしか保持できません");
                return false;
            }

            items.Add(item);
            return true;
        }

        // アイテムを削除
        public bool RemoveItem(EInstantItemType item)
        {
            return items.Remove(item);
        }

        // 指定位置のアイテムを取得
        public EInstantItemType GetItemAt(int index)
        {
            if (index < 0 || index >= items.Count)
            {
                Debug.LogError($"無効なインデックス: {index}");
                return EInstantItemType.None;// 適切なデフォルト値
            }
            return items[index];
        }

        // 現在保持しているアイテムのリストを取得
        public IReadOnlyList<EInstantItemType> GetItems()
        {
            return items.AsReadOnly();
        }

        // 保持しているアイテムの数
        public int ItemCount => items.Count;

        // アイテムをクリア
        public void ClearItems()
        {
            items.Clear();
        }
    }
    public class EquipmentSaveManager
    {
        private static readonly string SAVE_FILE_NAME = "equipment_data.json";
        public EquipmentSaveManager()
        {

        }

        private string GetSaveFilePath()
        {
            // Application.persistentDataPath はプラットフォームごとに適切な永続データディレクトリを返す
            // エディタ: <Project>/Assets/../Library/Application Support/<company name>/<product name>
            // Windows: C:/Users/<username>/AppData/LocalLow/<company name>/<product name>
            // Mac: ~/Library/Application Support/company name/product name
            // iOS/Android: アプリのサンドボックス内の永続データディレクトリ
            return Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        }
        public bool IsSaveFileValid()
        {
            string filePath = GetSaveFilePath();

            // ファイルが存在しない場合
            if (!File.Exists(filePath))
            {
                Debug.Log("セーブファイルが存在しません");
                return false;
            }

            // ファイルが空の場合
            if (new FileInfo(filePath).Length == 0)
            {
                Debug.LogWarning("セーブファイルが空です");
                return false;
            }

            return true;

        }
        public void SaveEquipmentData()
        {
              
            if(!IsSaveFileValid())
            {


            }


        }


    }


}