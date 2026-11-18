# Changelog

All notable changes to **Conkist GDK Services** (`me.conkist.gdk.services`) are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-11-18

### Added

- **One code door.** `RedeemCodeAsync(codeOrLink)` accepts an account code (8 digits) and a room code (8 characters with a letter), typed or taken from a QR link. A text that is not a code is refused with 400 and no network call (`ConkistCodeParser`).
- Rooms: `GetRoomsAsync`, `LeaveRoomAsync`, models `SdkRoom` and `RedeemCodeResponse`.
- Persistent anonymous player (`playerId` + `playerSecret`) with `IPlayerCredentialStore` and a `PlayerPrefs` default.
- Links: `GetLinksAsync`, `RevokeLinkAsync`, `RedeemAccountCodeAsync`, `CheckLinkStatusAsync`.
- `ConkistLinkPolling` and `ConkistQRCodeDisplay` components.
- **Documentation** in `Documentation~/`: index, guide for AI agents, getting started, connection codes, telemetry, API reference, troubleshooting, testing.
- Samples: **Quick Start** (new) and **Account Link** (the enter-code scene now takes account and room codes).
- Tests: `ConkistCodeParserTests`, `ConkistLinkContractTests`, and the opt-in `ConkistLiveConnectionTests` (needs a local API).

### Deprecated

- `ConkistApiService` is `[Obsolete]`. Most of its routes do not exist on the server. It will be removed in the next minor version. Use `ConkistBackendService`.
- `GetConnectionQRCode` (sync) is `[Obsolete]`. Use `GetConnectionQRCodeAsync`.

### Fixed

- README, SERVICES_README and ARCHITECTURE described types that do not exist (`GDKServices`, `IAuthService`, `ConkistAuthService` and others). They are rewritten from the code.
- The version in `README.md` and `CHANGELOG.md` now matches `package.json`.

### Compatibility

- Requires Unity 6000.3 or newer, `me.conkist.gdk.core` 0.1.0, Newtonsoft.Json 3.2.2 (automatic) and UniTask 2.5.x (add it by git URL; it is not in the Unity registry).

## [Unreleased]

[Unreleased]: https://github.com/Conkist/Conkist.GDK.Services/compare/0.2.0...HEAD
[0.2.0]: https://github.com/Conkist/Conkist.GDK.Services/releases/tag/0.2.0
