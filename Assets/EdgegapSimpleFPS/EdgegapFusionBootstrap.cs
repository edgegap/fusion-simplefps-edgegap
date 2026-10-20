using System;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Edgegap.SimpleFPS
{
    /// <summary>
    /// Replaces FusionBootstrap on the Deathmatch scene (Tools > Edgegap Simple FPS > Set Up Project does
    /// the swap and keeps every inspector value). Changes only how a server starts:
    /// - in a dedicated server build (UNITY_SERVER) it starts automatically as a Server on
    ///   ARBITRIUM_PORT_GAMEPORT_INTERNAL (7777 by default), so the scene keeps its client setting
    ///   (Start Mode = User Interface) and one project builds both sides;
    /// - the session name is the deployment FQDN, &lt;ARBITRIUM_REQUEST_ID&gt;.pr.edgegap.net;
    /// - the scene is marked active on load, as the menu's host does with LoadScene(..., setActiveOnLoad: true).
    ///   Without it, clients load Deathmatch but keep Startup active, SceneObjects finds no Gameplay and
    ///   Player.FixedUpdateNetwork throws a NullReferenceException every tick;
    /// - the Photon region is fixed when PHOTON_REGION is set, otherwise Fusion picks the best ping;
    /// - the session name and region are logged, so you know what players must type in the Party menu.
    /// Hosts, clients and the editor's debug start behave exactly like FusionBootstrap.
    /// </summary>
    [DisallowMultipleComponent]
    public class EdgegapFusionBootstrap : FusionBootstrap
    {
        public const ushort DefaultServerPort = 7777;

#if UNITY_SERVER
        protected override void Start()
        {
            StartMode = StartModes.Automatic;
            AutoStartAs = GameMode.Server;
            ServerPort = ushort.TryParse(Environment.GetEnvironmentVariable("ARBITRIUM_PORT_GAMEPORT_INTERNAL"), out var port)
                ? port
                : DefaultServerPort;
            base.Start();
        }
#endif

        protected override Task InitializeNetworkRunner(NetworkRunner runner, GameMode gameMode, NetAddress address,
            SceneRef scene, Action<NetworkRunner> onGameStarted, INetworkRunnerUpdater updater = null)
        {
            if (gameMode != GameMode.Server)
                return base.InitializeNetworkRunner(runner, gameMode, address, scene, onGameStarted, updater);

            var sceneManager = runner.GetComponent<INetworkSceneManager>()
                               ?? runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            var objectProvider = runner.GetComponent<INetworkObjectProvider>()
                                 ?? runner.gameObject.AddComponent<NetworkObjectProviderDefault>();

            var sceneInfo = new NetworkSceneInfo();
            if (scene.IsValid)
                sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive, LocalPhysicsMode.None, activeOnLoad: true);

            string requestId = Environment.GetEnvironmentVariable("ARBITRIUM_REQUEST_ID");
            if (!string.IsNullOrEmpty(requestId))
                DefaultRoomName = $"{requestId}.pr.edgegap.net";

            FusionAppSettings appSettings = null;
            string region = Environment.GetEnvironmentVariable("PHOTON_REGION");
            if (!string.IsNullOrEmpty(region))
            {
                appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
                appSettings.FixedRegion = region.Trim().ToLowerInvariant();
            }

            return runner.StartGame(new StartGameArgs
            {
                GameMode = gameMode,
                Address = address, // NetAddress.Any(port): bind 0.0.0.0 so clients connect Direct
                Scene = sceneInfo,
                SessionName = DefaultRoomName,
                CustomPhotonAppSettings = appSettings,
                OnGameStarted = r =>
                {
                    Debug.Log($"[edgegap-simplefps] server started: session={r.SessionInfo?.Name} " +
                              $"photon_region={r.SessionInfo?.Region} bind=0.0.0.0:{ServerPort}/udp");
                    onGameStarted?.Invoke(r);
                },
                SceneManager = sceneManager,
                Updater = updater,
                ObjectProvider = objectProvider,
            });
        }
    }
}
