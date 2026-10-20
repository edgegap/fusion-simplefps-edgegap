using System;
using System.Collections;
using System.Reflection;
using System.Text;
using Edgegap.SimpleFPS;
using TMPro;
using UnityEngine;

// Test-only: plays the Party menu like a player, for headless clients. Delete before shipping.
//   SimpleFPS.exe -batchmode -nographics -edgegapJoin <deployment fqdn> -edgegapRegion <photon region> -logFile <path>
// Types the FQDN and region into the Join card fields (their character limit and validation apply) and presses Join.
public sealed class PartyJoinDriver : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (Arg("-edgegapJoin") == null) return;
        var go = new GameObject("PartyJoinDriver");
        DontDestroyOnLoad(go);
        go.AddComponent<PartyJoinDriver>();
    }

    IEnumerator Start()
    {
        string session = Arg("-edgegapJoin"), region = Arg("-edgegapRegion") ?? "";
        yield return new WaitForSeconds(2f);

        EdgegapPartyMenu party = null;
        while (party == null)
        {
            foreach (var p in Resources.FindObjectsOfTypeAll<EdgegapPartyMenu>())
                if (p.gameObject.scene.IsValid()) party = p;
            yield return null;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var type = typeof(EdgegapPartyMenu);
        var code = (TMP_InputField)type.GetField("_sessionCodeField", flags).GetValue(party);
        var reg = (TMP_InputField)type.GetField("_regionCodeField", flags).GetValue(party);
        if (reg == null) { Debug.LogError("[join] the Region field is not assigned: run Tools > Edgegap Simple FPS > Set Up Project"); yield break; }

        code.text = Type(code, session);
        reg.text = Type(reg, region);
        Debug.Log($"[join] session field='{code.text}' region field='{reg.text}' -> pressing Join");
        type.GetMethod("OnJoinButtonPressed", flags).Invoke(party, null);
    }

    // Applies the field's character limit and per-character validation, like typing does.
    static string Type(TMP_InputField field, string value)
    {
        var validate = typeof(TMP_InputField).GetMethod("Validate", BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(string), typeof(int), typeof(char) }, null);
        var sb = new StringBuilder();
        foreach (char c in value)
        {
            if (field.characterLimit > 0 && sb.Length >= field.characterLimit) break;
            char ok = validate != null ? (char)validate.Invoke(field, new object[] { sb.ToString(), sb.Length, c }) : c;
            if (ok != '\0') sb.Append(ok);
        }
        return sb.ToString();
    }

    static string Arg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
