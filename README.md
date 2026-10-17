# Conkist.GDK.Services

<p>
  <img src="https://img.shields.io/badge/Unity-6000.3.14f1-black?logo=unity" />
  <img src="https://img.shields.io/badge/C%23-dev-239120?logo=csharp" />
  <img src="https://img.shields.io/badge/version-1.0.0-blue" />
  <img src="https://img.shields.io/badge/license-MIT-green" />
</p>

Backend integration layer of the **Conkist Game Development Kit** — a provider-agnostic framework for authentication, cloud data and analytics in Unity games.

```json
"me.conkist.gdk.services": "https://github.com/Conkist/Conkist.GDK.Services.git"
```

**Requires:** `Conkist.GDK.Core` · `UniTask`

---

## Architecture

A two-layer design that decouples game logic from backend providers.

### Layer 1 — Standardization

Abstract C# interfaces your game code talks to — provider-agnostic by design:

| Interface | Responsibility |
|---|---|
| `IAuthService` | Anonymous login, user/password, account linking, logout |
| `IRemoteDataService` | Save, load and sync player profile and progress in the cloud |
| `IAnalyticsService` | Track custom events, progression and game telemetry |

### Layer 2 — Providers

Concrete implementations per backend, swappable without changing call sites:

| Provider | Backend |
|---|---|
| `ConkistAuthService` / `ConkistRemoteDataService` | Conkist Backend |
| UGS extensions | Unity Gaming Services (Authentication, Cloud Save, Analytics) |
| PlayFab extensions | Microsoft PlayFab |

---

## Unified Facade

`GDKServices` is the single access point. Register a provider once at startup; everything else consumes the abstraction.

**Bootstrap:**
```csharp
using Conkist.GDK.Services;

public class ServiceBootstrap : MonoBehaviour
{
    private void Awake()
    {
        GDKServices.RegisterAuth(new ConkistAuthService());
        GDKServices.RegisterRemoteData(new ConkistRemoteDataService());
    }
}
```

**In-game usage (provider-agnostic):**
```csharp
using Conkist.GDK.Services;
using Cysharp.Threading.Tasks;

public class LoginController : MonoBehaviour
{
    public async UniTaskVoid Login()
    {
        bool success = await GDKServices.Auth.LoginAnonymousAsync();

        if (success)
        {
            var data = await GDKServices.RemoteData.LoadDataAsync("PlayerProgress");
        }
    }
}
```

Swapping from Conkist Backend to Unity Gaming Services requires changing only the `RegisterAuth` / `RegisterRemoteData` calls — zero changes in game logic.

---

## Install

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "me.conkist.gdk.core": "https://github.com/Conkist/Conkist.GDK.Core.git",
    "me.conkist.gdk.services": "https://github.com/Conkist/Conkist.GDK.Services.git",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11"
  }
}
```

---

## Samples

Included in `Samples~/` (visible via Package Manager UI):

1. **Bootstrap** — how to initialize and configure providers
2. **Provider swap** — switching between Unity and Conkist backends from the same facade

---

## Part of

**[Conkist GDK](https://github.com/Conkist)** — Game Development Kit for Unity · [conkist.me](https://conkist.me)
