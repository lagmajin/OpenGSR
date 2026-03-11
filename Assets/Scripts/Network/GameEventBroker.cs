using System;
using System.Collections.Generic;
using System.Threading;
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
        
        public static IDisposable Subscribe<T>(Action<T> onMessageReceived, SubscriptionBag bag)
        {
            var sub = Subscribe(onMessageReceived);
            bag?.Add(sub);
            return sub;
        }

        public static IDisposable SubscribeOnce<T>(Action<T> onMessageReceived)
        {
            if (onMessageReceived == null) throw new ArgumentNullException(nameof(onMessageReceived));

            IDisposable sub = null;
            sub = Subscribe<T>(message =>
            {
                sub?.Dispose();
                onMessageReceived(message);
            });
            return sub;
        }

        public static int SubscriberCount<T>()
        {
            lock (Locker)
            {
                if (Handlers.TryGetValue(typeof(T), out var list))
                {
                    return list.Count;
                }
            }

            return 0;
        }

        public static void UnsubscribeAll<T>()
        {
            lock (Locker)
            {
                Handlers.Remove(typeof(T));
            }
        }

        public static SubscriptionBag CreateSubscriptionBag()
        {
            return new SubscriptionBag();
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
            private int _disposed;

            public Subscription(Action<T> handler)
            {
                _handler = handler;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 1)
                {
                    return;
                }

                if (_handler == null)
                {
                    return;
                }

                Unsubscribe(_handler);
                _handler = null;
            }
        }

        public sealed class SubscriptionBag : IDisposable
        {
            private readonly List<IDisposable> subscriptions = new List<IDisposable>();
            private bool disposed;

            public void Add(IDisposable subscription)
            {
                if (subscription == null)
                {
                    return;
                }

                if (disposed)
                {
                    subscription.Dispose();
                    return;
                }

                subscriptions.Add(subscription);
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                for (int i = 0; i < subscriptions.Count; i++)
                {
                    subscriptions[i]?.Dispose();
                }
                subscriptions.Clear();
            }
        }
    }
}
