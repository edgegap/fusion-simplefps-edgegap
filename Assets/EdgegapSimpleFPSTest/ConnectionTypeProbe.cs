using Fusion;
using UnityEngine;

// Test-only: logs runner.CurrentConnectionType (Direct vs Relayed), RTT, player count and scenes
// every 5 s, so headless clients and the server report them in their logs. Delete before shipping.
public sealed class ConnectionTypeProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var go = new GameObject("ConnectionTypeProbe");
        DontDestroyOnLoad(go);
        go.AddComponent<ConnectionTypeProbe>();
    }

    float _next;

    static string LoadedScenes()
    {
        var names = new System.Collections.Generic.List<string>();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            names.Add($"{s.name}#{s.buildIndex}{(s.isLoaded ? "" : "(loading)")}");
        }
        return string.Join(",", names);
    }

    void Update()
    {
        if (Time.realtimeSinceStartup < _next) return;
        _next = Time.realtimeSinceStartup + 5f;

        foreach (var runner in FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None))
        {
            if (runner.IsRunning == false) continue;
            int players = 0;
            foreach (var _ in runner.ActivePlayers) players++;
            Debug.Log($"[probe] mode={runner.GameMode} CT={runner.CurrentConnectionType} " +
                      $"rtt={runner.GetPlayerRtt(PlayerRef.None) * 1000:0}ms players={players} " +
                      $"session={runner.SessionInfo?.Name} region={runner.SessionInfo?.Region} " +
                      $"runnerScene={runner.SceneManager?.MainRunnerScene.name}#{runner.SceneManager?.MainRunnerScene.buildIndex} " +
                      $"loaded={LoadedScenes()}");
        }
    }
}
