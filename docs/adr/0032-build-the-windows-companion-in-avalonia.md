# ADR-0032: Build the Windows companion in Avalonia with a window and a tray icon

- Status: Accepted
- Date: 2026-10-04
- Deciders: Gihed Annabi
- Accepted: 2026-10-04, with its first implementation in #514 (owner approval recorded on #514)

## Context

The desktop companion pairs with the player's account and uploads character snapshots
([ADR-0002](0002-use-desktop-companion-for-character-sync.md)). [ADR-0030](0030-pair-the-companion-with-a-confirmed-code.md)
fixed its pairing protocol and that it runs on Windows only at first, keeping its device token with the Windows Data
Protection API (DPAPI). Task #514 builds the pairing part (mockup `companion-pairing`, states 5 to 8); folder
discovery and uploads follow in #384 (mockup `companion-sync`).

The owner first chose WPF with a window and a tray icon. WPF runs only on Windows, while continuous integration runs
on `ubuntu-latest`: a WPF project compiles there but neither it nor its tests can run, and the coverage gate leaves
out a `src/` project that no test loads (ADR-0015), so the window code would go untested. The owner then chose Avalonia
UI, a cross-platform .NET UI framework close to WPF, so every companion test runs in the existing job (owner decision
on #514).

The public API host of each environment serves only `/companion/...` (`deploy/server/raidmanager.Caddyfile`), for
example `https://api.raidmanager-dev.pivotsoftwares.com`, and the website serves `/companion/pair?code=`.

## Decision

### Avalonia UI, run on Windows, tested on Linux

The companion is an Avalonia application (MIT license): XAML views, compiled bindings and MVVM, as in WPF. Its window
and tray icon use Avalonia's own `Window` and `TrayIcon`, so no Windows Forms or third-party tray package is needed.
The product ships for Windows only (ADR-0030); Linux runs only the tests, through `Avalonia.Headless.XUnit`, which
renders views without a screen.

### Two projects and their tests

| Project | Target | Holds |
| --- | --- | --- |
| `src/Containers/UI/Core/RaidManager.Companion.Client` | `net10.0` | The API client of the `/companion` routes, the pairing state machine and polling, the token store over a protector seam, the view models, the environment options; no UI framework |
| `src/Containers/UI/Hosting/RaidManager.Companion` | `net10.0` | The Avalonia application: `App`, the window and its views for states 5 to 8, the tray icon and menu, the DPAPI protector, the theme, the composition root |

Tests mirror them, both in the existing Linux job and coverage gate:

- `test/Containers/UI/Core/RaidManager.Companion.Client.Tests`: the behavior of every state, on the repository's
  xUnit v2 with Reqnroll where it is business behavior.
- `test/Containers/UI/Hosting/RaidManager.Companion.Tests`: headless view and tray tests on xUnit v3, which
  `Avalonia.Headless.XUnit` 12 requires. They use headless drawing, so CI needs no native rendering library.

Only the DPAPI protector can't run on Linux: it is marked `[SupportedOSPlatform("windows")]` for the platform
analyzer, and its round-trip test runs on Windows and is skipped elsewhere.

### The application

- **MVVM without a framework:** view models implement `INotifyPropertyChanged` themselves and commands use a small
  `RelayCommand` in the client project. The generic host (`Microsoft.Extensions.Hosting`, already in
  `Directory.Packages.props`) gives dependency injection, configuration, logging and `IHttpClientFactory` with
  `Microsoft.Extensions.Http.Resilience`.
- **Tray:** closing the window hides it to the tray, and the tray menu (Open, Quit) brings it back or exits (owner
  decision on #514), so #384 can keep watching folders. Its look is a board of the `companion-pairing` mockup.
- **One instance per Windows user:** a second start brings the running window forward through a named `Mutex` and
  event.
- **Theme:** theme resource dictionaries hold the palette and type scale of
  [ADR-0019](0019-dark-design-system-with-an-app-shell.md), named after the variables of `raidmanager-theme.css`;
  views use only their resources. Open Sans is embedded as static `.ttf` files, because Avalonia reads neither the
  website's `.woff2` files nor variable fonts.
- **Build telemetry off:** Avalonia's build package sends anonymous build data unless `AVALONIA_TELEMETRY_OPTOUT=1` is
  set. Every workflow step that builds the companion sets it, and the owner sets it on their computer (owner
  decision on #520).

### Pairing client (ADR-0030)

1. `POST /companion/pairings` with the computer's label (`Environment.MachineName`). The answer gives the device
   code, the pairing code, its expiry and the polling interval.
2. The window shows the code and a countdown (state 5) and opens
   `<website>/companion/pair?code=<pairing code>` in the default browser.
3. It polls `POST /companion/pairings/token` at the interval the API gives, never under five seconds: `Pending` keeps
   polling, a 429 adds five seconds to the interval (as RFC 8628 `slow_down`), `Expired` or `Invalid` show state 7,
   and the token shows state 6. A network failure keeps polling with the same back-off until the code expires.
4. At start, a stored token is checked with `GET /companion/me`. A 401 (`Companion.Revoked`, `Companion.Expired` or
   `Companion.TokenUnknown`) deletes the token and shows state 8 with Pair again.

### Token storage

The token is encrypted with `ProtectedData.Protect` (`System.Security.Cryptography.ProtectedData`,
`DataProtectionScope.CurrentUser`, an application entropy) and written to `%LOCALAPPDATA%\RaidManager\companion.dat`
through a temporary file and a replace, so a crash never leaves half a file. The file holds the companion id, the
player's name and the protected token. The token is never logged, never shown, and never written under the WoW folder.
The store sits behind an `ITokenProtector` seam, so its tests use a fake protector.

### Environments

The companion reads `ApiBaseUrl` and `WebsiteBaseUrl` from `appsettings.json` next to the executable, with one
`appsettings.<Environment>.json` per environment (`Development` for the local Aspire ports, `Dev`, `Test`,
`Production`). A publish selects its environment; the player never types an address.

### Packaging

- **Project metadata** follows `raidmanager-project-packaging`: both projects set `IsPackable` and
  `GeneratePackageOnBuild` to `false` (the client is not a reusable library); the host's assembly name is
  `RaidManager.Companion` and it carries the RaidManager icon.
- **Build output:** a publish profile makes a self-contained, single-file `win-x64` executable, so players need no
  .NET runtime (owner decision on #514). It is not signed yet; Windows SmartScreen warns on first start until a
  code-signing decision, in its own ADR, is made.
- **Continuous integration:** the existing Linux job builds and tests both projects. A `companion` job publishes the
  `win-x64` executable (cross-published from Linux) and uploads it as a workflow artifact.
- **Distribution** to players (installer, release assets, automatic updates) is outside this ADR and is decided before
  the first release that ships the companion.

## Consequences

**Positive**

- Every companion test, views included, runs in the existing Linux job and counts in the coverage gate.
- The tray icon is part of the UI framework; no Windows Forms dependency.
- A macOS or Linux companion later needs only a platform token protector, not a new UI.
- The token is unreadable to other Windows users and on other computers, as ADR-0030 requires.

**Negative**

- New dependencies: the Avalonia packages, `System.Security.Cryptography.ProtectedData` and `xunit.v3`, added to
  `Directory.Packages.props`; the host's tests use xUnit v3 while the other test projects stay on v2.
- Avalonia isn't WPF: some WPF habits (styles, triggers, some controls) differ.
- An unsigned executable shows a SmartScreen warning.

## Alternatives considered

- **WPF with a Windows CI job:** the owner's first choice; it keeps a Windows-only framework but needs a second,
  slower CI job and a second coverage report.
- **WPF tested only locally:** one job, but the window code would leave the coverage gate.
- **Uno Platform:** cross-platform too, but heavier and further from WPF.
- **.NET MAUI or WinUI 3:** no Linux support for tests, no in-box tray icon, and MSIX packaging for WinUI.
- **MSIX or ClickOnce packaging:** cleaner installs and updates, but MSIX needs a signing certificate, and both are a
  distribution choice deferred above.
- **Windows Credential Manager for the token:** rejected in ADR-0030.
