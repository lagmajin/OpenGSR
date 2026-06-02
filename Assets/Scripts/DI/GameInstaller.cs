using OpenGS;
using OpenGSCore;
using UnityEngine;
using Zenject;

namespace OpenGS
{
    public class GameInstaller : MonoInstaller
    {
        public OnlineLoadingSceneNetworkManager onlineLoadingSceneManagerGO;
        public override void InstallBindings()
        {
            Debug.Log("[GameInstaller] ProjectContext に ClientSessionData を登録");
            var effectPrefabs = Resources.Load<EffectPrefabMasterData>("MasterData/Effect/EffectPrefab");
            if (effectPrefabs != null)
            {
                Container.BindInstance(effectPrefabs).AsSingle();
            }
            Container.Bind<IEffectService>().To<EffectService>().AsSingle();
            Container.Bind<ISoundService>().To<SoundService>().AsSingle().WithArguments(
                Resources.Load<SoundMasterData>("MasterData/SoundMasterData"),
                Resources.Load<BGMMasterData>("MasterData/BGMMasterData"));
            Container.Bind<OnlineLoadingSceneNetworkManager>()
 .FromComponentInHierarchy()
 .AsSingle();
            // ClientSessionData をシングルトンとして登録
            //Container.Bind<ClientSessionData>().AsSingle().NonLazy();
            Container.BindInstance(DependencyInjectionConfig.Resolve<MatchRoomManager>()).AsSingle();
            Container.BindInstance(DependencyInjectionConfig.Resolve<MatchRUDPServerNetworkManager>()).AsSingle();
            Container.BindInstance(DependencyInjectionConfig.Resolve<OnlineLoadingManager>()).AsSingle();
            Container.BindInstance(DependencyInjectionConfig.Resolve<GeneralServerNetworkManager>()).AsSingle();
            Container.BindInstance(DependencyInjectionConfig.Resolve<EquipmentSaveManager>()).AsSingle();
            Container.BindInstance(DependencyInjectionConfig.Resolve<PlayerMatchManager>()).AsSingle();
            Container.Bind<IShopService>().To<OnlineShopService>().AsSingle();
            // BindInstance は任意だけど、Resolve に使うならここで Bind
            //var manager = DependencyInjectionConfig.Resolve<OnlineLoadingSceneNetworkManager>();
            //Container.BindInstance(manager).AsSingle();
   
        }


        public override void Start()
        {
            // Bind が終わったので、Scene上のオブジェクトに Inject
           // Container.Inject(onlineLoadingSceneManagerGO);
        }
    }
}

