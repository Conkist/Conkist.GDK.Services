# Architecture

## Layers

```
Game code
   |
ConkistBackendService (MonoBehaviour)            Runtime/Extensions/Conkist/
   |-- ConkistApiClient   (UnityWebRequest, one method per route, returns ApiResponse<T>)
   |-- ConkistTelemetryBuffer  (queue in memory: batch, capacity, re-enqueue on failure)
   |-- ConkistSessionData / IPlayerCredentialStore (player id + secret, 5-minute token)
   |-- ConkistCodeParser  (cleans a typed code or a QR link, no network)
   |
BaseBackendService -> SingletonBehaviour (package me.conkist.gdk.core)
```

Other backends use the same base class: `UnityBackendService` (Unity Gaming Services), `PlayfabBackendService`, `CustomBackendService`. Each is a separate assembly. Game code that needs the Conkist Platform uses `ConkistBackendService`.

UI helpers (components): `ConkistLinkPolling` (polls the status of the shown code), `ConkistQRCodeDisplay` (QR, time bar, auto-renew).

## Flow: session

1. `LoginAsync` loads the saved player (`playerId`, `playerSecret`) from the credential store.
2. `POST /v1/sdk/initialize` with the public key (and the saved player, if any).
3. The API answers with `playerId`, a 5-minute `sessionToken`, and, for a new player only, the `playerSecret`. The service saves it. The API keeps only a hash.
4. If the API refuses the saved secret, the service drops the saved player and makes a new one.
5. Each call renews the token when it is close to the end.

## Flow: game shows a code

1. After login the service calls `POST /v1/sdk/links`. It gets an 8-digit code and a deep link. The code lives 5 minutes.
2. `OnConnectionCodeReceived` fires. The game draws the code and the QR.
3. An account types the code in the Conkist App. The game sees `confirmed` by polling `GET /v1/sdk/links/{id}/status`.

## Flow: player enters a code

1. `RedeemCodeAsync` cleans the text with `ConkistCodeParser`. If it is not a code, the call stops with 400 and no network call.
2. `POST /v1/sdk/codes/redeem`. The server tells the kind by the format: 8 digits = account link, 8 characters with a letter = room.
3. The answer has `kind`: `link` (connection confirmed) or `room` (the player joined `room`).
4. Five wrong codes lock the player for 15 minutes (429). Wrong, expired and closed codes give the same 400 message.

## Flow: telemetry

1. `TrackEvent` adds to the buffer. After login a loop sends a batch (10 events) every 10 seconds, or at once on `FlushAsync`.
2. `POST /v1/sdk/telemetry`. The server keeps only events on the project's approved list and answers 202.
3. A failed batch goes back to the buffer. The oldest event drops when the buffer is full (1000).

## Error strategy

Network methods return `ApiResponse<T>`. They do not throw for network or HTTP errors. Check `IsSuccess`, then `IsRateLimited`, `IsUnauthorized`, `IsNetworkError`. Programming errors (null arguments) throw.

## Assemblies

| Assembly | Content |
|---|---|
| `Conkist.GDK.Services` | Conkist, base and custom backends |
| `Conkist.GDK.Services.Playfab` | PlayFab provider |
| `Conkist.Services.Unity` | Unity Gaming Services provider |
| `Conkist.GDK.Services.Tests` | Tests (PlayMode) |

`Conkist.GDK.Services` compiles only when UniTask and `me.conkist.gdk.core` are present (define constraints `CONKIST_UNITASK`, `CONKIST_GDK_CORE`).

Server contract: `conkist-api`, `SdkController`.
