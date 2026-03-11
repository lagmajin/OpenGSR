using System;

namespace OpenGS
{
    public static class GameEventBrokerExtensions
    {
        public static void PublishGameEvent(this AbstractGameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                return;
            }

            GameEventBroker.Publish(gameEvent);
        }

        public static IDisposable SubscribeGameEvent<TEvent>(Action<TEvent> onMessageReceived)
            where TEvent : AbstractGameEvent
        {
            return GameEventBroker.Subscribe(onMessageReceived);
        }

        public static IDisposable SubscribeGameEvent<TEvent>(
            Action<TEvent> onMessageReceived,
            GameEventBroker.SubscriptionBag bag)
            where TEvent : AbstractGameEvent
        {
            return GameEventBroker.Subscribe(onMessageReceived, bag);
        }
    }
}
