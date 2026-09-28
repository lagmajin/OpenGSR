using Newtonsoft.Json.Linq;
using System;

namespace OpenGSCore
{
    /// <summary>
    /// ネットワーク同期可能なオブジェクトのインターフェイス
    /// クライアント・サーバー間でゲーム状態を効率的に同期するための基本定義を提供します。
    /// </summary>
    public interface ISyncable
    {
        /// <summary>
        /// オブジェクトの現在の状態をJSON形式で取得します。
        /// ネットワーク送信用のデータ形式に変換します。
        /// </summary>
        /// <returns>JSON形式のオブジェクト状態</returns>
        JObject ToJSon();

        /// <summary>
        /// オブジェクトの状態が前回の同期以降に変化したかどうかを判定します。
        /// 変化していない場合はネットワーク送信をスキップできます（帯域幅最適化）。
        /// </summary>
        /// <returns>true なら状態が変化している、false なら変化していない</returns>
        bool HasChanged();

        /// <summary>
        /// 現在の状態を「前回の同期状態」として記録します。
        /// HasChanged() の判定用に呼び出されます。
        /// </summary>
        void SaveSyncState();
    }

    /// <summary>
    /// ISyncable の実装をサポートするユーティリティクラス
    /// </summary>
    public static class SyncHelper
    {
        /// <summary>
        /// オブジェクトのリストをJSON配列に変換します。
        /// 変化したオブジェクトのみを含めることで帯域幅を最適化できます。
        /// </summary>
        public static JArray SyncableListToJson(System.Collections.Generic.IEnumerable<ISyncable> items, bool onlyChanged = true)
        {
            var array = new JArray();

            foreach (var item in items)
            {
                if (onlyChanged && !item.HasChanged())
                {
                    continue;
                }

                array.Add(item.ToJSon());
            }

            return array;
        }

        /// <summary>
        /// リスト内の全オブジェクトの同期状態を保存します。
        /// </summary>
        public static void SaveSyncStateForAll(System.Collections.Generic.IEnumerable<ISyncable> items)
        {
            foreach (var item in items)
            {
                item.SaveSyncState();
            }
        }
    }
}
