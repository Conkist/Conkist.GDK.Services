# Conkist GDK Services

`me.conkist.gdk.services` connects a Unity game to the **Conkist Platform**: an anonymous player session, connection codes (account link and class room, by typed code or QR), and telemetry.

**Start here:** [`Documentation~/index.md`](Documentation~/index.md). AI agents: read [`Documentation~/AGENTS.md`](Documentation~/AGENTS.md) first.

| | |
|---|---|
| Version | 0.2.0 (see [CHANGELOG](CHANGELOG.md)) |
| Unity | 6000.3 or newer |
| Depends on | `me.conkist.gdk.core`, `com.unity.nuget.newtonsoft-json` (automatic). `com.cysharp.unitask` (add it by git URL). |
| Main class | `Conkist.GDK.Services.Conkist.ConkistBackendService` |

## Install

`Packages/manifest.json`:

```json
{
  "dependencies": {
    "me.conkist.gdk.core": "https://github.com/Conkist/Conkist.GDK.Core.git",
    "me.conkist.gdk.services": "https://github.com/Conkist/Conkist.GDK.Services.git#0.2.0",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11"
  }
}
```

Use a tag. Without a tag, Unity follows the default branch.

## What it does

| Feature | Where to read |
|---|---|
| Session. Anonymous player, 5-minute token, renewed on its own. | [getting-started](Documentation~/getting-started.md) |
| Codes. One door (`RedeemCodeAsync`) for an account code (8 digits) and a room code (8 characters). The game can also show a code and a QR. | [connection-codes](Documentation~/connection-codes.md) |
| Rooms and links. List, leave, cancel. | [connection-codes](Documentation~/connection-codes.md) |
| Telemetry. Buffered events sent in batches. The server drops events that the project did not approve. | [telemetry](Documentation~/telemetry.md) |

## Minimal code

```csharp
using System.Collections.Generic;
using Conkist.GDK.Services.Conkist;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Example : MonoBehaviour
{
    [SerializeField] ConkistBackendService conkist;   // component on a GameObject, public key filled

    async void Start()
    {
        await UniTask.WaitUntil(() => conkist.IsInitialized);
        conkist.TrackEvent("session_lifecycle", new Dictionary<string, object> { { "phase", "start" } });

        var result = await conkist.RedeemCodeAsync("ABCD-2345");   // or a QR link
        if (result.IsSuccess && result.Data.IsRoom) Debug.Log(result.Data.Room.Name);
    }
}
```

## Samples

Package Manager > Conkist GDK Services > Samples.

- **Quick Start**: session and telemetry in one script.
- **Account Link**: show a code or type a code (account and room).

## Deprecated

`ConkistApiService` is deprecated in 0.2.0 and will be removed in the next minor version. Use `ConkistBackendService`.

## Security

Use only the public key (`pk_...`) in a game. Never commit a key. Never send personal data in events: the player can be a child.

---

Part of **[Conkist GDK](https://github.com/Conkist)** · [conkist.me](https://conkist.me)
