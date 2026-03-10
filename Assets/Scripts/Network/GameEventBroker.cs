using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Lightweight in-process event broker.
    /// Avoids external dependencies while keeping a simple Publish/Subscribe API.
    /// </summary>
    public static class GameEventBroker
    {
        private static readonly object Locker = new object();
        private static readonly Dictionary<Type, List<Delegate>> Handlers = new Dictionary<Type, List<Delegate>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void InitializeMessagePipe()
        {
            lock (Locker)
            {
                Handlers.Clear();
            }
        }

        public static void Publish<T>(T message)
        {
            List<Delegate> snapshot = null;
            lock (Locker)
            {
                if (Handlers.TryGetValue(typeof(T), out var list))
                {
                    snapshot = new List<Delegate>(list);
                }
            }

            if (snapshot == null) return;

            foreach (var d in snapshot)
            {
                try
                {
                    ((Action<T>)d)?.Invoke(message);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameEventBroker] Publish<{typeof(T).Name}> failed: {ex.Message}");
                }
            }
        }

        public static IDisposable Subscribe<T>(Action<T> onMessageReceived)
        {
            if (onMessageReceived == null) throw new ArgumentNullException(nameof(onMessageReceived));

            lock (Locker)
            {
                if (!Handlers.TryGetValue(typeof(T), out var list))
                {
                    list = new List<Delegate>();
                    Handlers[typeof(T)] = list;
                }

                list.Add(onMessageReceived);
            }

            return new Subscription<T>(onMessageReceived);
        }

        private static void Unsubscribe<T>(Action<T> handler)
        {
            lock (Locker)
            {
                if (!Handlers.TryGetValue(typeof(T), out var list)) return;

                list.Remove(handler);
                if (list.Count == 0)
                {
                    Handlers.Remove(typeof(T));
                }
            }
        }

        private sealed class Subscription<T> : IDisposable
        {
            private Action<T> _handler;

            public Subscription(Action<T> handler)
            {
                _handler = handler;
            }

            public void Dispose()
            {
                if (_handler == null) return;
                Unsubscribe(_handler);
                _handler = null;
            }
        }
    }
}
