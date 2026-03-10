using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Autofac;
using OpenGSCore;
using UnityEngine;



namespace OpenGS
{
    public static class DependencyInjectionConfig
    {
        private static IContainer _container;
        private static IContainer _serverContainer;
        private static IContainer _testContainer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticForDomainReloadless()
        {
            _container = null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void InitializeDI( )
        {
            // Ensure debug settings are loaded early because InitializeDI may run before other RuntimeInitialize methods.
            try
            {
                DebugSettingsManager.EnsureLoaded();
                Debug.Log($"DependencyInjectionConfig: DebugSettings loaded early: testMode={DebugSettingsManager.settings?.localServerTestMode}, port={DebugSettingsManager.settings?.localTCPPort}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DependencyInjectionConfig: Failed to ensure debug settings loaded: {ex.Message}");
            }
            if (_container != null)
            {
                _container.Dispose(); // 前のコンテナを破棄（任意）
                _container = null;
            }
            var builder = new ContainerBuilder();

            // DIコンテナに登録
            //builder.RegisterType<PlayerService>().As<IPlayerService>().SingleInstance();
            //builder.RegisterType<EnemySpawner>().As<IEnemySpawner>().InstancePerDependency();
            builder.RegisterType<GameGeneralManager>().SingleInstance();
            builder.RegisterType<OnlineGameModeSelect>().SingleInstance();
            builder.RegisterType<GeneralServerNetworkManager>().SingleInstance();
            builder.RegisterType<MatchRUDPServerNetworkManager>().SingleInstance();
            builder.RegisterType<EquipmentSaveManager>().SingleInstance();

            builder.RegisterType<OnlineLoadingManager>().SingleInstance();
            
            //builder.RegisterType<ClientSessionInfo>().SingleInstance();
            builder.RegisterType<PlayerSessionInfo>().SingleInstance();

            builder.RegisterType<PlayerStatus>().SingleInstance();
            builder.RegisterType<PlayerData>().SingleInstance();


            builder.RegisterType<WaitRoomManager>().SingleInstance();
            builder.RegisterType<MatchRoomManager>().SingleInstance();
            
            builder.RegisterType<WaitRoom>().InstancePerDependency();
            builder.RegisterType<MatchData>().InstancePerDependency();
            builder.RegisterType<PlayerMatchManager>().SingleInstance();
            builder.RegisterType<LocalTestMatchRUDPServer>().SingleInstance();
            //builder.RegisterType<OnlineLoadingSceneNetworkManager>().SingleInstance();

            _container = builder.Build();

            // Eagerly resolve critical singletons so they are created now and can be traced in logs.
            try
            {
                var gsn = _container.Resolve<GeneralServerNetworkManager>();
                Debug.Log($"DependencyInjectionConfig: Eagerly resolved GeneralServerNetworkManager instanceHash={gsn.GetHashCode()}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DependencyInjectionConfig: Failed to eagerly resolve GeneralServerNetworkManager: {ex.Message}");
            }

            try
            {
                var rudp = _container.Resolve<MatchRUDPServerNetworkManager>();
                Debug.Log($"DependencyInjectionConfig: Eagerly resolved MatchRUDPServerNetworkManager instanceHash={rudp.GetHashCode()}");
            }
            catch { }

            var testServerBuilder = new ContainerBuilder();
            testServerBuilder.RegisterType<LocalTestTcpServer>().SingleInstance();
            testServerBuilder.RegisterType<LocalTestMatchRUDPServer>().SingleInstance();

            _testContainer = testServerBuilder.Build();

            // If a local test TCP server is registered in the test container, start it automatically
            try
            {
                var localTestServer = _testContainer.Resolve<LocalTestTcpServer>();
                if (localTestServer != null)
                {
                    // Use debug settings if available to determine whether to start and which port to use.
                    try
                    {
                        // Ensure debug settings are loaded before using them. Load order of RuntimeInitializeOnLoadMethod
                        // is not guaranteed, so call EnsureLoaded explicitly here.
                        DebugSettingsManager.EnsureLoaded();
                        var settings = DebugSettingsManager.settings;
                        // Only start the local test server when test mode is enabled AND a valid port (>0) is provided.
                        if (settings != null && settings.localServerTestMode && settings.localTCPPort > 0)
                        {
                            localTestServer.port = settings.localTCPPort;

                            _ = localTestServer.StartAsync(localTestServer.port);

                            Application.quitting += () =>
                            {
                                try { localTestServer.Stop(); } catch { }
                            };

                            Debug.Log($"DependencyInjectionConfig: LocalTestTcpServer started on port {localTestServer.port} (from DebugSettings)");
                        }
                        else
                        {
                            Debug.Log($"DependencyInjectionConfig: LocalTestTcpServer not started. testMode={settings?.localServerTestMode}, localTCPPort={settings?.localTCPPort}");
                        }
                    }
                    catch (Exception exInner)
                    {
                        Debug.LogWarning($"DependencyInjectionConfig: Error applying DebugSettings to LocalTestTcpServer: {exInner.Message}");
                        // Fallback: do not start automatically
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DependencyInjectionConfig: Failed to resolve LocalTestTcpServer from test container: {ex.Message}");
            }



            var method = MethodBase.GetCurrentMethod();
            var className = method.DeclaringType?.FullName ?? "Unknown Class";
            var functionName = method.Name;

            Debug.Log(className);

            // ファイル名と行番号も含めて出力
            //Debug.Log($"{className}.{functionName}() - {System.IO.Path.GetFileName(file)}:{line} - Autofac Container Initialized");
        }

        public static void RecreateServerContainer()
        {
            _testContainer?.Dispose();

            // 新しいコンテナを作る
            var builder = new ContainerBuilder();

            // ここでテストサーバー系を登録
            builder.RegisterType<LocalTestTcpServer>().SingleInstance();
            builder.RegisterType<LocalTestMatchRUDPServer>().SingleInstance();

            // 必要に応じて他のサービスも登録
            // builder.RegisterType<SomeOtherService>().SingleInstance();

            // ビルドして _testContainer にセット
            _testContainer = builder.Build();
        }

        // Ensure the DI container is initialized. This can be called by callers that need the container
        // and are unsure whether RuntimeInitializeOnLoadMethod has already run.
        public static void EnsureInitialized()
        {
            if (_container == null)
            {
                Debug.LogWarning("DependencyInjectionConfig: DI container not initialized. Attempting to initialize now.");
                try
                {
                    InitializeDI();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"DependencyInjectionConfig: InitializeDI failed during EnsureInitialized: {ex.Message}");
                }
            }
        }

        public static T Resolve<T>()
        {
            try
            {
                if (_container == null)
                {
                    Debug.LogError("DependencyInjectionConfig.Resolve: DI container is not initialized. Call EnsureInitialized or resolve in Awake/Start.");
                    EnsureInitialized();
                    if (_container == null)
                    {
                        throw new InvalidOperationException("DI container is not initialized.");
                    }
                }

                return _container.Resolve<T>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to resolve {typeof(T)}: {ex.Message}");
                throw;
            }
        }

        public static T ResolveTest<T>()
        {
            try
            {
                if (_testContainer == null)
                {
                    Debug.LogError("DependencyInjectionConfig.ResolveTest: test DI container is not initialized.");
                    throw new InvalidOperationException("Test DI container is not initialized.");
                }

                return _testContainer.Resolve<T>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to resolve {typeof(T)}: {ex.Message}");
                throw;
            }
        }
    }
}