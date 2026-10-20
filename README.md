# Simple FPS dedicated servers on Edgegap

Run Photon's [Simple FPS Multiplayer (Photon Fusion)](https://assetstore.unity.com/packages/templates/tutorials/simple-fps-multiplayer-photon-fusion-272941) sample as a dedicated server on [Edgegap](https://edgegap.com), one deployment per match, close to the players. Players join from the sample's **Party** menu by typing the deployment's address and Photon region.

Copy this kit into your Simple FPS project. It doesn't edit any Simple FPS or Fusion file, and it doesn't include any. You still get Simple FPS from the Unity Asset Store under your own licence.

> **Tested** on 2026-10-20 with Simple FPS 2.1.1, Fusion 2.1.1 (build 2177) and Unity 6000.3.11f1 on Windows: a server built with this kit on an unmodified Simple FPS project ran on Edgegap in Newark, and two clients joined it through the Party menu. Both connected **Direct** (11 to 29 ms), with no exceptions. See [What was tested](#what-was-tested).

## What's in the kit

| Path | What it does |
|---|---|
| `Assets/EdgegapSimpleFPS/EdgegapFusionBootstrap.cs` | Replaces `FusionBootstrap` on the Deathmatch scene. In a server build, it starts a Fusion server on port 7777, names the session after the deployment (`<request id>.pr.edgegap.net`), marks the scene active on load so clients use it, and logs the session and Photon region. Hosts, clients and editor play are unchanged. |
| `Assets/EdgegapSimpleFPS/EdgegapPartyMenu.cs` | Replaces `FusionMenuUIParty` on the Startup scene. **Join** takes the deployment address and a Photon region code. **Create Game** is unchanged. |
| `Assets/EdgegapSimpleFPS/EdgegapServerSceneLoader.cs` | Server builds only: skips the Startup menu and loads Deathmatch. |
| `Assets/EdgegapSimpleFPS/Editor/SimpleFpsEdgegapSetup.cs` | Adds **Tools > Edgegap Simple FPS**: check the project, set it up, build the Linux server and a Windows client. You can also call it from the Unity CLI. |
| `Assets/EdgegapSimpleFPSTest/` | Test-only: a probe that logs the connection type, and a driver that joins through the Party menu in headless clients. Delete it before shipping. |
| `edgegap/Dockerfile`, `edgegap/start-server.sh` | An Ubuntu 22.04 image that runs the server as a non-root user. It never prints the environment, and it stops the deployment when the server exits. |
| `.dockerignore` | Sends only the server build to Docker. |
| `scripts/` | Set up and build, package and push, run locally, and test with headless clients. |

## What you need

- Simple FPS Multiplayer (Photon Fusion) from the Asset Store, imported into a new Unity project. It's a complete project: it brings its own Project Settings.
- Unity 6000.3 with **Linux Dedicated Server Build Support** and **Linux Build Support (IL2CPP)**. For clients, add **Windows Build Support (IL2CPP)**, which needs Visual Studio's C++ workload.
- The server must use **IL2CPP**, the sample's default. To build Linux IL2CPP on Windows, add these packages in **Window > Package Manager > + > Add package by name**: `com.unity.toolchain.win-x86_64-linux` and `com.unity.sdk.linux-x86_64`.
- A Photon Fusion App ID from the [Photon dashboard](https://dashboard.photonengine.com).
- An Edgegap account, and Docker Desktop.

## 1. Add the kit to the project

Copy `Assets/EdgegapSimpleFPS`, `edgegap/`, `scripts/`, `.dockerignore` and `.gitattributes` into the project root, next to `Assets/`. To run the scripted client test, also copy `Assets/EdgegapSimpleFPSTest`.

Then in Unity:

1. In **Tools > Fusion > Realtime Settings**, paste your Fusion App ID. The Asset Store package also ships a `PhotonAppSettings` asset in `Assets/AssetStoreTools/Resources/` with the publisher's App ID: put yours there too, or delete that copy.
2. In **Tools > Fusion > Network Project Config**, set **Peer Mode** to `Single`.
3. Run **Tools > Edgegap Simple FPS > Check Project** and fix anything it reports.
4. Run **Tools > Edgegap Simple FPS > Set Up Project**.

Set Up Project changes only scene and build settings, and you can run it again safely:

- Deathmatch: `FusionBootstrap` becomes `EdgegapFusionBootstrap`, keeping its settings, and Start Mode becomes **User Interface**.
- Startup: the Party menu's `FusionMenuUIParty` becomes `EdgegapPartyMenu`. The menu prefab instance is unpacked to allow this.
- The Join card's code field accepts up to 30 characters (letters, digits and dots) with AllCaps off, and gets a new **Region** field next to it.
- The build profile is set to Startup (0), then Deathmatch (1).

## 2. Build, package and upload

From a terminal (Git Bash on Windows), with the editor closed on the project:

```bash
scripts/build.sh server
scripts/package.sh registry.edgegap.com/<your-registry-project>/simplefps <new-tag>
```

The first IL2CPP server build takes about 13 minutes. Log in first with `docker login registry.edgegap.com`, using the Container Registry credentials from the Edgegap dashboard. Use a new tag for every build.

From the editor: **Tools > Edgegap Simple FPS > Build Linux Server** builds the same thing to `Builds/EdgegapServer`. If you package it with the Edgegap Unity plugin, set its **Path To Dockerfile** to `edgegap/Dockerfile`. The plugin's default Dockerfile prints the environment, which includes the deployment's delete token.

In the Edgegap dashboard, create an application and a version with:

- your image and tag, 1 vCPU and 2 GB to start
- port `7777`, protocol **UDP**, with the port check off
- your registry username and token in the version's registry settings

## 3. Test locally first

```bash
scripts/package.sh
scripts/run-local.sh
```

The server logs `[edgegap-simplefps] server started: session=f68e011bfb01.pr.edgegap.net photon_region=<code>`. Join it from a client with that session and region (see step 4). Locally the connection shows as Relayed because of Docker Desktop's network; on Edgegap it's Direct.

## 4. Start a match and connect

1. Deploy your version from the Edgegap dashboard (or API), close to your players.
2. In the deployment's logs, find `server started: session=<fqdn> photon_region=<code>`.
3. Build a client with `scripts/build.sh client` (or your usual Windows build). Launch it, then on the main menu click **Party Menu**.
4. On the Join card, type the deployment FQDN (for example `f68e011bfb01.pr.edgegap.net`) in the code field and the region code (for example `cae`) in the Region field. Click **Join**.

If the client doesn't connect, the region most likely doesn't match the one in the server log.

## Runtime variables

| Variable | Set by | Effect |
|---|---|---|
| `ARBITRIUM_REQUEST_ID` | Edgegap | The session name becomes `<request id>.pr.edgegap.net`, the deployment's FQDN. |
| `ARBITRIUM_PORT_GAMEPORT_INTERNAL` | Edgegap | The UDP port the server binds (7777 by default). It binds `0.0.0.0`, so clients connect Direct. |
| `PHOTON_REGION` | You (version or deployment) | A fixed Photon region, for example `us`. When it isn't set, the server uses the region with the best ping from where it runs: a Toronto deployment picked `cae`. |
| `ARBITRIUM_DELETE_URL`, `ARBITRIUM_DELETE_TOKEN` | Edgegap | When the server process exits on its own, the start script uses them to stop the deployment. |

## Why the Startup scene is in the server build

Fusion tells clients which scene to load by **build index**. If the server is built with Deathmatch only, Deathmatch is index 0 there, while index 0 is Startup on clients. Clients then never find the game objects and throw `NullReferenceException` in `Player.FixedUpdateNetwork` every tick. So both builds use Startup (0) and Deathmatch (1), and the server skips Startup.

For the same reason, the server marks Deathmatch **active on load**, as the sample's own host does. Without it, clients load Deathmatch but keep Startup active, with the same exception.

## Tests

```bash
scripts/build.sh client
scripts/test-clients.sh <fqdn> <region>
```

This starts two headless clients that type the address and region into the Party menu and press Join. After 60 seconds it prints what each client reports, for example:

```
client 1: [probe] mode=Client CT=Direct rtt=11ms players=2 session=<fqdn> region=cae runnerScene=Deathmatch#1 ... | exceptions=0
```

It exits 0 when every client sees every player and logged no exception.

## What was tested

On 2026-10-20, with Simple FPS 2.1.1, Fusion 2.1.1 (build 2177) and Unity 6000.3.11f1 on Windows 11:

- A Linux IL2CPP server and a Windows IL2CPP client, built from this kit on an unmodified Simple FPS project.
- A local container with two clients: both joined, Relayed (expected locally), no exceptions.
- An Edgegap deployment in Newark (Photon region `us`) with two clients that joined through the Party menu, run with `scripts/test-clients.sh`: CT **Direct**, 11 to 29 ms, two players, Deathmatch loaded, no exceptions on the clients or the server. An earlier run with the same changes, made by hand, in Toronto (`cae`): Direct, 11 to 24 ms.

Not tested: more than two players, a match played to its end, whether the server exits after a match, the CPU a full match needs, macOS, and the Edgegap plugin's build button.

## Licence

The kit is MIT licensed (see `LICENSE`). Simple FPS, Photon Fusion and the sample's third-party assets are under their own licences and are not included.
