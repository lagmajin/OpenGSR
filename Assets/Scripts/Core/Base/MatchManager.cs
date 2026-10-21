

using System;
using System.Collections.Generic;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    internal interface IMatchManager
    {

    }

    internal class MatchManagerImpl
    {

    }

    internal class CaptureTheFlagImpl : MatchManagerImpl
    {

    }






internal class MatchManager
    {
        private MatchManagerImpl imp;

        private readonly List<IAbstractMatchMainScript> mainScriptSubscribers = new();

        private List<IPlayer> subscribePlayers = new();
        public static MatchManager Instance { get; } = new();

        private readonly object subscribePlayerLockObject = new();

        public List<PlayerStatus> status;

        public PlayerDatabase MatchPlayerDatabase { get; set; }=new();

        private MatchManager()
        {
            //MatchPlayerInfo
        }

        /// <summary>
        /// Registers a match script to receive this match's events.
        /// <para>
        /// This held one script rather than a list of them, so a second script
        /// replaced the first instead of joining it, and any script unsubscribing
        /// cleared whatever had registered after it. A CTF scene registers both its
        /// own script and the base class's, which is exactly the case that lost
        /// events.
        /// </para>
        /// </summary>
        public void SubscribeEvent(IAbstractMatchMainScript script)
        {
            if (script == null)
            {
                return;
            }

            if (!mainScriptSubscribers.Contains(script))
            {
                mainScriptSubscribers.Add(script);
            }

            Debug.Log($"<color=blue>SubscribeEvent</color> ({mainScriptSubscribers.Count})");
        }

        /// <summary>
        /// Removes one script, and only that one.
        /// <para>
        /// It used to clear the single field whatever was passed in, so a script
        /// leaving a match took every other script's subscription with it.
        /// </para>
        /// </summary>
        public void UnSubscribeEvent(IAbstractMatchMainScript script)
        {
            if (script == null)
            {
                return;
            }

            var removed = mainScriptSubscribers.Remove(script);

            Debug.Log($"<color=red>UnSubscribeEvent</color> (removed={removed}, {mainScriptSubscribers.Count})");
        }

        public void SubscribeEvent(IPlayer player)
        {
            lock (subscribePlayerLockObject)
            {
                if (!subscribePlayers.Contains(player))
                {
                    subscribePlayers.Add(player);
                }
            }

        }

        public void UnSubscribeEvent(IPlayer player)
        {
            lock (subscribePlayerLockObject)
            {
                subscribePlayers.Remove(player);
            }



        }

        /// <summary>
        /// Hands an event to every script registered for this match.
        /// <para>
        /// Each one is called on its own, and a script that throws is logged and
        /// skipped rather than ending the delivery to the rest: these are the
        /// handlers a mode's whole match state moves through, so one of them
        /// faulting used to stop the event reaching the scripts after it.
        /// </para>
        /// </summary>
        public void SendEvent(AbstractGameEvent ev)
        {
            if (ev == null)
            {
                return;
            }

            IAbstractMatchMainScript[] subscribers;
            lock (subscribePlayerLockObject)
            {
                if (mainScriptSubscribers.Count == 0)
                {
                    Debug.LogWarning($"[MatchManager] Dropped {ev.GetType().Name}: no match subscriber is registered.");
                    return;
                }

                subscribers = mainScriptSubscribers.ToArray();
            }

            foreach (var subscriber in subscribers)
            {
                if (subscriber == null)
                {
                    continue;
                }

                try
                {
                    subscriber.PostEvent(ev);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MatchManager] {subscriber.GetType().Name} failed to handle {ev.GetType().Name}: {ex}");
                }
            }
        }

        public void ClearAllSubscribe()
        {
            mainScriptSubscribers.Clear();
        }

        /// <summary>
        /// Drops everything this match subscribed.
        /// <para>
        /// The manager is a process wide singleton and the subscriptions are
        /// per match, so without this a match's players and main script stayed
        /// registered into the next one: the next match's events went to a scene
        /// that had already been unloaded, and the player list grew by a whole
        /// match every time.
        /// <para>
        /// The database is replaced rather than emptied, because PlayerDatabase
        /// has no clear of its own and the property is settable.
        /// </para>
        /// </summary>
        public void Clear()
        {
            mainScriptSubscribers.Clear();

            lock (subscribePlayerLockObject)
            {
                subscribePlayers.Clear();
            }

            MatchPlayerDatabase = new PlayerDatabase();
            status?.Clear();
        }

    }


}
