---
name: avalonia-desktop
description: 'Create, organize, write and review the RaidManager desktop companion in Avalonia UI 12 on .NET 10 (ADR-0032):
  the two projects and their .csproj files, central package versions, the feature-folder layout, MVVM without a
  framework, compiled bindings, the theme and embedded font, the generic host and dependency injection, the desktop
  lifetime, tray icon and single instance, the UI thread and async commands, disposal, Windows-only code (DPAPI) behind
  a seam, HTTP and configuration, accessibility, logging, the self-contained single-file publish and the telemetry
  opt-out. Use for any companion .axaml, .axaml.cs, view model, project file or publish change. Tests: avalonia-tests.'
---

# Avalonia desktop companion

Use this skill for every change to the desktop companion: `src/Containers/UI/Core/RaidManager.Companion.Client` and
`src/Containers/UI/Hosting/RaidManager.Companion`. Read first:

- `raidmanager-conventions`, then ADR-0032 (the companion in Avalonia), ADR-0030 (pairing and the device token),
  ADR-0002 (why a companion exists) and ADR-0019 (the design system).
- `avalonia-tests` for every test, `penpot-mockups` for every window, and the `clean-code-*`,
  `csharp-xml-documentation` and `csharp-regions` skills for the C# itself. Where those skills and the existing code
  disagree, `raidmanager-conventions` §4 decides.

Every version-dependent fact below was checked on 2026-10-04 against the Avalonia 12.1.3 documentation and a throwaway
prototype built with this repository's `Directory.Build.props`, analyzers and `.editorconfig`, on Windows and in the
`mcr.microsoft.com/dotnet/sdk:10.0` Linux image (#521). When the Avalonia major version changes, check its breaking
changes page before relying on this skill.

## 1. Non-negotiable rules

- **Avalonia 12 on `net10.0`, never `net10.0-windows`.** Both projects target the repository's `net10.0`. No WPF or
  Windows Forms type, no `UseWPF`, no `UseWindowsForms`: they would stop the tests from running on Linux CI
  (ADR-0032).
- **The product ships for Windows only; the tests run on Linux.** Every Windows-only API sits behind an interface in
  the host, in a class marked `[SupportedOSPlatform("windows")]` (section 9).
- **A mockup comes first.** Every window, state and tray menu has a board in a `docs/mockups/companion-*.penpot` file,
  confirmed by the owner, before it is built (ADR-0017, `penpot-mockups`). Each state is compared with its board
  before handover (`avalonia-tests` §6).
- **The client project has no Avalonia reference.** View models, commands, API clients and state machines live in
  `RaidManager.Companion.Client`, which references no UI package; the compiler then enforces "no Avalonia type in a
  view model".
- **Compiled bindings only.** Every view root and `DataTemplate` declares `x:DataType`. Never write
  `ReflectionBinding`, never set `x:CompileBindings="False"`, never set `AvaloniaUseCompiledBindingsByDefault` to
  `false` (it is `true` by default since Avalonia 12).
- **No logic in code-behind.** A `.axaml.cs` file holds `InitializeComponent()` and, at most, view-only event wiring
  such as hiding a window on close. Anything that decides, formats, calls a service or holds state is in a view model.
- **Colors, sizes and fonts only from the theme** (section 6). No hexadecimal color, font name or magic size in a view.
- **Secrets never leave the token store.** The device token and the device code are never logged, shown, put in a
  view model property, written under the WoW folder or kept in a static field (ADR-0030).
- **Avalonia telemetry is off everywhere** (owner decision on #520): `Avalonia.BuildServices` sends anonymous build
  data on every build unless `AVALONIA_TELEMETRY_OPTOUT=1` is set. Every workflow step that builds the companion sets
  it in `env:`, and the README tells the owner to set it once as a Windows user variable.

## 2. Create the projects

Create the projects by hand, from this section. Don't install `Avalonia.Templates` on the owner's computer: the
templates add a reflection `ViewLocator`, the Inter font and `CommunityToolkit.Mvvm`, which this repository doesn't
use (ADR-0032).

### Central package versions

Add the packages to `Directory.Packages.props`, all Avalonia packages on the same version line:

```xml
<PackageVersion Include="Avalonia" Version="12.*" />
<PackageVersion Include="Avalonia.Desktop" Version="12.*" />
<PackageVersion Include="Avalonia.Themes.Fluent" Version="12.*" />
<PackageVersion Include="Avalonia.Headless.XUnit" Version="12.*" />
<PackageVersion Include="System.Security.Cryptography.ProtectedData" Version="10.*" />
<PackageVersion Include="xunit.v3" Version="3.*" />
```

`Avalonia.Headless.XUnit` 12 requires xUnit v3: keep `xunit.v3` on major version 3, which Avalonia 12.1 is built
against, until a release of `Avalonia.Headless.XUnit` names version 4. `xunit` (version 2) stays for the other test
projects. Floating versions follow the repository's existing practice (an open conflict, `raidmanager-conventions` §4).

### The client project

`src/Containers/UI/Core/RaidManager.Companion.Client/RaidManager.Companion.Client.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <PackageId>RaidManager.Companion.Client</PackageId>
    <Version>1.0.0</Version>
    <PackageTags>companion;viewmodels;pairing;raid-management</PackageTags>
    <Description>View models, API client and pairing logic of the RaidManager desktop companion.</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
  </ItemGroup>
</Project>
```

### The host project

`src/Containers/UI/Hosting/RaidManager.Companion/RaidManager.Companion.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <AssemblyName>RaidManager.Companion</AssemblyName>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <ApplicationIcon>Assets/raidmanager.ico</ApplicationIcon>
    <IsPackable>false</IsPackable>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <PackageId>RaidManager.Companion</PackageId>
    <Version>1.0.0</Version>
    <PackageTags>companion;desktop;avalonia;raid-management</PackageTags>
    <Description>The RaidManager desktop companion: pairs with the player's account and syncs character data.</Description>
  </PropertyGroup>
  <ItemGroup>
    <AvaloniaResource Include="Assets/**" />
    <Content Include="appsettings*.json" CopyToOutputDirectory="PreserveNewest" ExcludeFromSingleFile="true" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" />
    <PackageReference Include="Avalonia.Desktop" />
    <PackageReference Include="Avalonia.Themes.Fluent" />
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="System.Security.Cryptography.ProtectedData" />
    <ProjectReference Include="../../Core/RaidManager.Companion.Client/RaidManager.Companion.Client.csproj" />
  </ItemGroup>
  <Target Name="PublishOnlyTheExecutable" AfterTargets="ComputeResolvedFilesToPublishList">
    <ItemGroup>
      <ResolvedFileToPublish Remove="@(ResolvedFileToPublish)"
                             Condition="'%(Extension)' == '.pdb' Or '%(Extension)' == '.xml'" />
    </ItemGroup>
  </Target>
</Project>
```

The last target keeps the native SkiaSharp and HarfBuzz symbol files (about 100 MB) and the XML documentation out of
the published folder; `DebugType=none` alone doesn't. Don't set `GenerateDocumentationFile` to `false` instead:
StyleCop then fails the build with `SA0001`.

Also add `app.manifest` (supported OS and per-monitor DPI awareness) next to the project file. Add both projects and
their test projects to `RaidManager.sln`, check the evaluated metadata as `raidmanager-project-packaging` says, and
build the solution: the XAML compiler's generated code passes the repository's analyzers, so any warning is yours.

### Program and application

```csharp
internal static class Program
{
    [STAThread]
    public static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // The designer and the tests call this method; keep it public, static and free of side effects.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
```

`UsePlatformDetect()` also registers the text shaper. If a builder names a renderer itself (`UseSkia()`), it must
also call `UseHarfBuzz()`, or Avalonia 12 throws "No text shaping system configured". `WithInterFont()` needs the
`Avalonia.Fonts.Inter` package and isn't used here: the companion embeds Open Sans (section 6). `Main` can't run
under a test, so keep it to that one line.

## 3. Layout by feature

```text
src/Containers/UI/Core/RaidManager.Companion.Client/
├── AssemblyReference.cs
├── Configuration/CompanionOptions.cs        ← API and website addresses, validated at start
└── Features/
    ├── Pairing/                             ← PairingViewModel, PairingState, IPairingApi, PairingApi, ...
    ├── Tokens/                              ← ITokenStore, TokenStore, ITokenProtector, StoredToken
    ├── Tray/                                ← TrayViewModel
    └── Shared/                              ← ViewModelBase, RelayCommand, AsyncRelayCommand, IBrowserLauncher

src/Containers/UI/Hosting/RaidManager.Companion/
├── Program.cs  App.axaml  App.axaml.cs  app.manifest  appsettings.json  appsettings.<Environment>.json
├── Assets/                                  ← raidmanager.ico, Fonts/OpenSans-*.ttf
├── Theme/                                   ← Palette.axaml (colors, brushes, fonts, sizes), Controls.axaml (styles)
├── Composition/ServiceCollectionExtensions.cs
├── Features/
│   ├── Shell/MainWindow.axaml(.cs)          ← the window; shows the current feature view
│   ├── Pairing/PairingView.axaml(.cs)       ← states 5 to 8 of the companion-pairing mockup
│   ├── Tokens/DpapiTokenProtector.cs        ← Windows only
│   └── Shared/AvaloniaBrowserLauncher.cs, SingleInstance.cs
└── Properties/PublishProfiles/win-x64.pubxml
```

- One type per file, the file named after the type. A feature folder holds everything that feature needs; a type
  moves to `Shared` only when a second feature uses it.
- Views end in `View`, windows in `Window`, view models in `ViewModel`, and each view's view model has the same
  feature folder name in the client project (`Features/Pairing/PairingView` shows `Features/Pairing/PairingViewModel`).
- Map view models to views with typed `DataTemplate`s in `App.axaml`, not with the template's reflection
  `ViewLocator` (no compile-time check, not trimming-safe):

  ```xml
  <Application.DataTemplates>
    <DataTemplate DataType="pairing:PairingViewModel">
      <pairingViews:PairingView />
    </DataTemplate>
  </Application.DataTemplates>
  ```

## 4. View models and commands

- `ViewModelBase` implements `INotifyPropertyChanged` with one `SetProperty<T>(ref T field, T value,
  [CallerMemberName] string? name = null)` that raises only on a real change. View models are `sealed` and get their
  services through the constructor.
- Commands implement `System.Windows.Input.ICommand` (`RelayCommand`, `AsyncRelayCommand` in `Features/Shared`).
  `AsyncRelayCommand` disables itself while running, catches every failure into view model state (an error state the
  mockup shows), never lets an exception reach the dispatcher, and passes a `CancellationToken` it cancels on
  disposal.
- A screen with several states exposes one state value (an enum or a small record) plus derived `bool` properties
  for the view (`IsWaiting`, `IsPaired`); the view never compares strings.
- Time comes from an injected `TimeProvider` (countdowns, expiry, polling), never `DateTime.Now`, so tests control it.
- A view model that opens the browser calls an `IBrowserLauncher` from the client project; the host implements it with
  `TopLevel.GetTopLevel(control)?.Launcher.LaunchUriAsync(uri)`, which opens the default browser on Windows.

Bad: logic in code-behind, and a view model that knows Avalonia.

```csharp
private async void OnPairClick(object? sender, RoutedEventArgs e)
{
    var response = await new HttpClient().PostAsync(...);   // service call in the view
    CodeText.Text = ...;                                      // view state set by hand
    Dispatcher.UIThread.Post(() => ...);
}
```

Good: the view binds, the view model decides.

```xml
<Button Classes="primary" Content="Get a new code" Command="{Binding RequestCodeCommand}" />
```

## 5. XAML

- Files are `.axaml`; their code-behind is `.axaml.cs`, `sealed partial`, and documented like any type.
- Every view root sets `x:DataType`; bindings are plain `{Binding Path}`, which compile. A binding the compiler rejects
  is fixed in the view model, never by switching to reflection.
- Give `x:Name` only to elements a test or the code-behind must find, in PascalCase. Prefer finding controls by
  `AutomationProperties.Name` in tests.
- Styles use selectors and classes defined in `Theme/Controls.axaml` (`Classes="primary"`), never inline setters
  repeated across views.
- Resource lookups: `{DynamicResource Key}` for anything in a `ThemeDictionaries` dictionary (a `StaticResource`
  can't find those and throws at run time); `{StaticResource Key}` for the rest.
- `TextBox` uses `PlaceholderText` (Avalonia 12 renamed `Watermark`). Find a window's `TopLevel` with
  `TopLevel.GetTopLevel(visual)`, never by casting the root visual.

## 6. Theme and font

- `Theme/Palette.axaml` holds every color, brush, font family and size of ADR-0019, named after the variables of
  `raidmanager-theme.css` in PascalCase (`SurfaceCardBrush` for `--rm-surface-card`, `StatusDangerBrush` for
  `--rm-status-danger`). When the website palette changes, change both in the same pull request.
- `App.axaml` sets `RequestedThemeVariant="Dark"`, includes `<FluentTheme />` and then the two theme files, so the
  theme wins over Fluent defaults.
- **Embed Open Sans as static `.ttf` files.** Avalonia loads only TrueType and OpenType files and doesn't support
  variable fonts, so neither the website's `.woff2` files nor the default (variable) Open Sans download work. Put
  the static instances the mockups use (Regular, SemiBold, Bold) in `Assets/Fonts/` with the font's `OFL.txt`, add a
  `*.ttf binary` rule to `.gitattributes`, and declare
  `<FontFamily x:Key="BodyFont">avares://RaidManager.Companion/Assets/Fonts#Open Sans</FontFamily>`. Without an
  embedded font, Windows and Linux fall back to different system fonts and rendered frames differ (seen in the #521
  prototype).

## 7. Startup, dependency injection and lifetime

- `App.OnFrameworkInitializationCompleted` builds the services once: `Host.CreateApplicationBuilder()` for
  configuration (`appsettings.json`, `appsettings.<Environment>.json`), logging and `IHttpClientFactory`, then
  `services.AddCompanion(configuration)` from `Composition/ServiceCollectionExtensions.cs`. Resolve the root view
  models there only; everything else gets its dependencies through constructors. No service locator, no static
  service provider.
- Set `desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown`, set `desktop.MainWindow`, and dispose the service
  provider in `desktop.Exit`.
- **Close hides to the tray** (owner decision on #514): `MainWindow` overrides `OnClosing(WindowClosingEventArgs e)`;
  unless the application is quitting, it sets `e.Cancel = true` and calls `Hide()`. Quit in the tray menu calls
  `desktop.Shutdown()`. This is the one piece of behavior allowed in code-behind, because it is about the window
  itself.
- **Unhandled exceptions:** `App` subscribes to `Dispatcher.UIThread.UnhandledException` and
  `TaskScheduler.UnobservedTaskException` to log them. It doesn't mark them handled: a command catches its own
  failures (section 4), so an exception that reaches the dispatcher is a bug, and the application must not run on in
  an unknown state.
- **Tray icon:** declared in `App.axaml` under `TrayIcon.Icons`, with a `NativeMenu` (the Avalonia `Menu` control
  doesn't work there) and an `.ico` icon included as `AvaloniaResource`. `App.axaml` sets
  `x:DataType="tray:TrayViewModel"` and `App` sets its `DataContext` to the tray view model, so the menu's compiled
  bindings resolve (verified in the #521 prototype). On Windows a left click runs `TrayIcon.Command` and a right click
  opens the menu; bind both Open entries to the same command.
- **One instance per Windows user:** `Program.Main` takes a named `Mutex` (`Local\RaidManager.Companion`) before
  starting Avalonia; a second start signals the first to show its window, then exits. The signal is Windows-only
  code (section 9).

## 8. Threading, async and disposal

- Avalonia has one UI thread. An `await` started on it resumes on it, so view models update their properties after
  `await` without dispatching.
- `Dispatcher.UIThread` appears only in the host, when a callback really arrives on another thread (a timer from the
  operating system, the single-instance signal). The client project never uses it.
- CPU-bound or blocking work runs in `Task.Run`; nothing blocks the UI thread: no `.Result`, `.Wait()` or
  `Thread.Sleep`.
- `async void` only for event handlers in code-behind, which then contain a single awaited call wrapped in
  `try`/`catch`.
- Polling loops await `Task.Delay(interval, timeProvider, cancellationToken)` and stop when their token is canceled.
- Any view model that owns a loop, a timer, a subscription or a `CancellationTokenSource` implements `IDisposable` (or
  `IAsyncDisposable`) and is disposed by its owner; the application disposes the service provider on exit.

## 9. Windows-only code and the token store

The C# platform analyzer (CA1416) fails the build on any Windows-only call that Linux could reach. Never suppress it;
do this instead (verified in the #521 prototype, build green on both systems):

```csharp
[SupportedOSPlatform("windows")]
public sealed class DpapiTokenProtector : ITokenProtector
{
    public byte[] Protect(byte[] data) => ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);

    public byte[] Unprotect(byte[] data) => ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
}
```

- The composition root registers it inside `if (OperatingSystem.IsWindows())`; on any other system it registers
  nothing, and the companion refuses to start with a message (it ships for Windows only).
- `TokenStore` (client project) writes `%LOCALAPPDATA%\RaidManager\companion.dat`
  (`Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`) through a temporary file in the same
  folder and `File.Move(temporary, target, overwrite: true)`, so a crash never leaves half a file, and deletes it on a
  401 for a revoked or expired token (ADR-0030).
- Its tests use a fake protector; the DPAPI round trip is a Windows-only test (`avalonia-tests` §4).

## 10. HTTP and configuration

- One typed client per API area (`PairingApi` for `/companion/pairings`), registered with `AddHttpClient<TClient>`
  and the base address from `CompanionOptions.ApiBaseUrl`. JSON goes through `System.Net.Http.Json` with a
  source-generated `JsonSerializerContext`.
- No automatic retry of a POST that creates something (`POST /companion/pairings`): a retry would start a second
  pairing. The polling loop owns its own back-off (ADR-0032). A timeout on every client is fine.
- Read failures as problem details and switch on their code (`CompanionPairing.Pending`, `Companion.Revoked`), never
  on the message text.
- The bearer token is added per request from the token store, never as a default header of a shared client.
- `CompanionOptions` is bound from configuration, validated with data annotations and `ValidateOnStart()`. Addresses
  are never hard-coded; the environment of a publish is ADR-0032's choice.

## 11. Accessibility and logging

- Every control without visible text, and the pairing code, has `AutomationProperties.Name`. The tab order follows
  the reading order, the main action has focus when a state appears, and every action works from the keyboard.
- Contrast comes from the theme, which meets WCAG AA (ADR-0019); never lower it in a view.
- Log with `ILogger<T>` and message templates, at the level the event deserves. Never log the device token, the
  device code, the token file's content or the account folder name (ADR-0030).

## 12. Publish

`Properties/PublishProfiles/win-x64.pubxml` holds: `RuntimeIdentifier` `win-x64`, `SelfContained` `true`,
`PublishSingleFile` `true`, `IncludeNativeLibrariesForSelfExtract` `true`, `DebugType` `none`. Publish from any
system, Linux included:

```bash
AVALONIA_TELEMETRY_OPTOUT=1 dotnet publish src/Containers/UI/Hosting/RaidManager.Companion -c Release -p:PublishProfile=win-x64
```

The result is one executable of about 100 MB plus the `appsettings*.json` files next to it. It isn't signed:
Windows SmartScreen warns on first start until a code-signing ADR exists. Trimming and native AOT are not enabled;
turning them on needs its own test of every view.

## 13. Review checklist

- [ ] Both projects target `net10.0`; no WPF, Windows Forms or `-windows` target framework; telemetry opt-out set in
      every workflow step that builds them.
- [ ] Each window and state matches a confirmed mockup board, compared with a rendered frame.
- [ ] The client project references no Avalonia package; code-behind holds no logic.
- [ ] Every view root and data template has `x:DataType`; no `ReflectionBinding`.
- [ ] No color, font or size outside `Theme/`; Open Sans embedded.
- [ ] Services injected through constructors; one composition root; the provider disposed on exit.
- [ ] Commands catch their failures into state; no `.Result`, `.Wait()` or stray `async void`.
- [ ] Windows-only code marked `[SupportedOSPlatform("windows")]` behind an interface; CA1416 not suppressed.
- [ ] No token, device code or account folder name in logs, view models or files other than `companion.dat`.
- [ ] Tests as `avalonia-tests` requires, and the coverage gate passes.

## Sources

- Avalonia 12 documentation (`docs.avaloniaui.net`): breaking changes, compiled bindings, view locator, application
  lifetimes, threading model, launcher, theme variants, TrayIcon, Windows platform guide, dependency injection.
- The `Avalonia.BuildServices` package README (telemetry and `AVALONIA_TELEMETRY_OPTOUT`).
- ADR-0002, ADR-0017, ADR-0019, ADR-0030 and ADR-0032; owner decisions on #514 and #520; the prototype of #521.
