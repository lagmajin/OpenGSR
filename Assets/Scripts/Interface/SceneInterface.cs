using System;
using UniRx;
using UnityEngine;

namespace OpenGS
{

    public interface IGameSettingScene
    {

    }
    public interface IOfflineQuestLoadingScene
    {

    }
    public interface IOnlineLoadingScene
    {
        public void OnMatchLoadingCompleted();

        public void OnEnterMapAllowed();
        public void OnLoadingFailed();

        IReadOnlyReactiveProperty<float> Progress { get; }
    }
}