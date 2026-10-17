# Conkist GDK Services

Complete backend integration for Conkist Platform, providing game data management, user profiles, account linking, and analytics.

## Features

- **Game Data Management** — Fetch server-side game configuration (read-only)
- **User Profiles** — Load and save user data with custom fields and statistics
- **Account Linking** — Connect game users with Conkist Platform accounts
- **Organization Access** — ReferralToken system for project-specific access
- **Analytics** — Event tracking and telemetry integration
- **Authentication** — Anonymous login with time-refreshed tokens
- **Error Handling** — Dual strategy: ApiResponse for expected errors, Exceptions for extraordinary cases

## Quick Start

### Basic Setup

```csharp
using Conkist.GDK.Services.Backend;

public class GameManager : MonoBehaviour
{
    private ConkistApiService _apiService;

    async void Start()
    {
        _apiService = GetComponent<ConkistApiService>();
        
        // Initialize with API credentials from Keys/conkist-api.json
        await _apiService.InitializeAsync(apiKey, gameId);
        
        // Login anonymously
        var userId = await _apiService.LoginAsync(anonymous: true);
    }
}
```

### Account Linking Flow (Game → Conkist App)

```csharp
// Game generates token for user to scan
var tokenLink = await _apiService.GenerateTokenLinkAsync(gameId, gameUserId);
Debug.Log($"Scan this: {tokenLink.LinkToken}");

// Conkist App validates token and confirms link
// On confirmation, push/hook triggers game to update connection state
var confirmedLinks = await _apiService.GetLinksAsync(userId);
```

### Organization Project Access

```csharp
// User enters referral token in game/app
var response = await _apiService.ValidateReferralTokenAsync(
    projectId: "org-project-001",
    tokenValue: "static-referral-token",
    conkistUserId: "conkist-user-001"
);

if (response?.IsValid ?? false)
{
    // User connected to organization
    // Apply customizations from response.OrgCustomizations
}
```

## 11 API Methods

| # | Method | Purpose |
|---|--------|---------|
| 1 | `InitializeAsync` | Setup connection |
| 2 | `LoginAsync` | Anonymous login |
| 3 | `FetchGameDataAsync` | Server config |
| 4 | `FetchUserDataAsync` | User profile |
| 5 | `SaveUserDataAsync` | Update user |
| 6 | `GenerateTokenLinkAsync` | Create linking token |
| 7 | `EventTriggerAsync` | Send events |
| 8 | `GetLinksAsync` | List connections |
| 9 | `RemoveLinkAsync` | Revoke link |
| 10 | `RefreshAuthTokenAsync` | Refresh token |
| 11 | `ValidateReferralTokenAsync` | Org access |

## Error Handling

**Expected Errors** (Network, validation) → Return null/false in ApiResponse
```csharp
var data = await _apiService.FetchUserDataAsync(userId);
if (data == null) { /* handle gracefully */ }
```

**Extraordinary Errors** (Null refs) → Throw Exception
```csharp
try
{
    await _apiService.InitializeAsync(null, gameId); // throws ArgumentNullException
}
catch (ArgumentNullException) { /* critical error */ }
```

## Tests

Unit tests: `Tests/ConkistApiServiceTests.cs`

Run in Unity: Window → Testing → Test Runner

## Next: conkist-api Implementation

Services is ready. Next step: implement endpoints in **conkist-api**.
