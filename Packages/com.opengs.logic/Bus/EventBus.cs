using System;
using System.Collections.Generic;

namespace OpenGSCore
{
    public abstract class AbstractEventBus
    {
        // 部屋のIDをキーにして、任意の型のイベントを受け取るハンドラをリストとして保持
        private readonly Dictionary<string, List<Action<object>>> _subscriptions = new Dictionary<string, List<Action<object>>>();
        private readonly object _lock = new object();

        // IDにサブスクライブする (ここでobject型を使うことで任意の型に対応)
        public void Subscribe(string id, Action<object> handler)
        {
            lock (_lock)
            {
                if (!_subscriptions.ContainsKey(id))
                {
                    _subscriptions[id] = new List<Action<object>>();
                }
                _subscriptions[id].Add(handler);
            }
        }

        // IDのサブスクリプションを解除する
        public void Unsubscribe(string id, Action<object> handler)
        {
            lock (_lock)
            {
                if (_subscriptions.ContainsKey(id))
                {
                    _subscriptions[id].Remove(handler);
                    if (_subscriptions[id].Count == 0)
                    {
                        _subscriptions.Remove(id);
                    }
                }
            }
        }

        // IDにイベントを発行 (任意の型のデータを受け取る)
        public void Publish(string id, object message)
        {
            List<Action<object>> handlers;
            lock (_lock)
            {
                if (!_subscriptions.ContainsKey(id))
                {
                    return;
                }
                // コピーを作成してロック外で実行
                handlers = new List<Action<object>>(_subscriptions[id]);
            }

            foreach (var handler in handlers)
            {
                handler(message);
            }
        }
    }

}