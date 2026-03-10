using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// リザルト画面で表示する1行分のプレイヤーデータ
    /// </summary>
    public class PlayerMatchResultData
    {
        public string PlayerId;
        public string PlayerName;
        public string Team;
        public int Kills;
        public int Deaths;
        public int Score;
    }

    /// <summary>
    /// マッチ結果のUIの表示（リスト表示）を統括するマネージャー。
    /// AbstractResultScene またはその継承先からデータを渡されることで、プレハブをInstantiateしてリストを構築する。
    /// </summary>
    public class AbstractMatchResultUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("各プレイヤーの結果行（プレハブ）を追加する親オブジェクト (ScrollRect の Content等)")]
        public Transform contentContainer;
        
        [Tooltip("MatchResultRowUI がアタッチされた行のプレハブ")]
        public GameObject resultRowPrefab; 

        private List<GameObject> spawnedRows = new List<GameObject>();

        /// <summary>
        /// プレイヤーデータのリストを受け取り、UI行を生成・表示する
        /// </summary>
        public void UpdateResultList(List<PlayerMatchResultData> playersData)
        {
            // まず過去の行を消す
            ClearRows();

            if (playersData == null || resultRowPrefab == null || contentContainer == null)
            {
                return;
            }

            // スコアが高い順（同じスコアならキル数順）にソート
            playersData.Sort((a, b) => 
            {
                int scoreCompare = b.Score.CompareTo(a.Score);
                if (scoreCompare == 0) return b.Kills.CompareTo(a.Kills);
                return scoreCompare;
            });

            // プレハブを生成してデータを流し込む
            foreach (var playerData in playersData)
            {
                var go = Instantiate(resultRowPrefab, contentContainer);
                var rowUI = go.GetComponent<MatchResultRowUI>();
                if (rowUI != null)
                {
                    rowUI.SetData(playerData);
                }
                spawnedRows.Add(go);
            }
        }

        public void ClearRows()
        {
            foreach (var row in spawnedRows)
            {
                if (row != null) Destroy(row);
            }
            spawnedRows.Clear();
        }
    }
}
