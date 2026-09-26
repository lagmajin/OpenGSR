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
            // Share the Autofac-owned service so event/stateful caches are not split across containers.
            TryBindResolvedInstance<ISoundService>();
            Container.Bind<OnlineLoadingSceneNetworkManager>()
 .FromComponentInHierarchy()
 .AsSingle();
            Container.Bind<UnityInputService>().AsSingle();
            Container.BindInterfacesAndSelfTo<VirtualInputService>().AsSingle();
            Container.Bind<ReplaySession>().AsSingle();
            Container.Bind<ReplayInputService>().AsSingle();
            Container.Bind<ITickable>().To<ReplayInputService>().FromResolve();
            Container.Bind<ILateTickable>().To<ReplayInputService>().FromResolve();
            // ClientSessionData をシングルトンとして登録
            //Container.Bind<ClientSessionData>().AsSingle().NonLazy();
            TryBindResolvedInstance<MatchRoomManager>();
            TryBindResolvedInstance<MatchRUDPServerNetworkManager>();
            TryBindResolvedInstance<OnlineLoadingManager>();
            TryBindResolvedInstance<GeneralServerNetworkManager>();
            TryBindResolvedInstance<EquipmentSaveManager>();
            TryBindResolvedInstance<PlayerMatchManager>();
            // Keep the same shop service instance used by the network layer.
            TryBindResolvedInstance<IShopService>();
            // BindInstance は任意だけど、Resolve に使うならここで Bind
            //var manager = DependencyInjectionConfig.Resolve<OnlineLoadingSceneNetworkManager>();
            //Container.BindInstance(manager).AsSingle();
   
        }


        public override void Start()
        {
            // Bind が終わったので、Scene上のオブジェクトに Inject
           // Container.Inject(onlineLoadingSceneManagerGO);
        }

        private void TryBindResolvedInstance<T>()
        {
            try
            {
                var instance = DependencyInjectionConfig.Resolve<T>();
                if (instance is object)
                {
                    Container.BindInstance(instance).AsSingle();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameInstaller] Optional shared service {typeof(T).Name} was not bound: {ex.Message}");
            }
        }
    }
}

