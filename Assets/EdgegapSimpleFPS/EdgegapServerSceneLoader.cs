#if UNITY_SERVER
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Edgegap.SimpleFPS
{
    /// <summary>
    /// Dedicated server only: skip the Startup menu and load Deathmatch, whose bootstrap starts the server.
    /// The server build keeps the client's scene list (Startup = 0, Deathmatch = 1) because Fusion tells
    /// clients which scene to load by build index: a server built with only Deathmatch sends index 0,
    /// and clients load Startup instead.
    /// </summary>
    public static class EdgegapServerSceneLoader
    {
        public const string ServerScene = "Deathmatch";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void LoadServerScene()
        {
            if (SceneManager.GetActiveScene().name != ServerScene)
                SceneManager.LoadScene(ServerScene, LoadSceneMode.Single);
        }
    }
}
#endif
