# Changelog

All notable changes to **Conkist GDK Services** (`me.conkist.gdk.services`) are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - Unreleased

First public release. Consolidates the backend-integration framework developed on top of the internal `0.1.0` prototype.

### Added

- **Two-layer architecture** separating the standardization layer from concrete providers.
- **Standardization layer** — `IAuthService`, `IRemoteDataService`, `IAnalyticsService`: the contracts game code consumes.
- **Provider layer** — implementations for the Conkist Backend Service, Unity Gaming Services (UGS) and PlayFab.
- **`GDKServices` facade** — a global, configurable access point (`RegisterAuth`, `RegisterRemoteData`, `Auth`, `RemoteData`) that lets consumers swap providers without touching call sites.
- **Conkist API client** for the Conkist backend.
- **Telemetry buffer** for batching analytics / telemetry events.
- **QR code display service** for account-linking / connection codes.
- Assembly definitions: `Conkist.GDK.Services`, `Conkist.GDK.Services.Playfab`, `Conkist.Services.Unity`, plus `Conkist.GDK.Services.Tests`.
- Assembly-definition version defines for UniTask and GDK Core integration.
- Sample demonstrating bootstrap and provider switching.

### Changed

- Backend service classes migrated from `Singleton` to `SingletonBehaviour` for clearer Unity lifecycle management.
- Added a Newtonsoft.Json assembly reference.

### Dependencies

- `me.conkist.gdk.core` 1.0.0
- UniTask

### Compatibility

- Now requires Unity 6000.3+ (previously declared 2022.3, but the GDK Core dependency already required 6000.3).

[Unreleased]: https://github.com/Conkist/Conkist.GDK.Services/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Conkist/Conkist.GDK.Services/releases/tag/v1.0.0
