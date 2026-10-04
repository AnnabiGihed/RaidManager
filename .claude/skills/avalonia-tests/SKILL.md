---
name: avalonia-tests
description: 'Test the RaidManager Avalonia desktop companion (ADR-0032) on Linux CI and Windows: the two test projects
  and their stacks (xUnit v2 with Reqnroll for the client, xUnit v3 with Avalonia.Headless.XUnit for the host), the
  headless test application, view, binding, command and tray tests, simulated input, Windows-only tests, time and
  HTTP fakes, rendered frames for the mockup comparison, and the coverage gate. Use whenever you write or review a
  companion test, or compare a companion window with its mockup.'
---

# Avalonia companion tests

Use this skill with `avalonia-desktop` (how the companion is built), `dotnet-unit-tests` (the house test rules:
Reqnroll for business behavior, xUnit for technical tests, Shouldly and Moq) and `gherkin-scenarios`. Facts below were
checked on 2026-10-04 against Avalonia 12.1.3 in a throwaway prototype, on Windows and in the
`mcr.microsoft.com/dotnet/sdk:10.0` Linux image (#521).

## 1. Two test projects, two stacks

| Test project | Tests | Stack |
| --- | --- | --- |
| `test/Containers/UI/Core/RaidManager.Companion.Client.Tests` | View models, commands, pairing and polling, token store, API client | The repository's stack: `xunit` (version 2), `Reqnroll.xUnit` for business behavior, Shouldly, Moq |
| `test/Containers/UI/Hosting/RaidManager.Companion.Tests` | Views and bindings, the window, tray menu, composition root, DPAPI protector | `xunit.v3` (major version 3), `Avalonia.Headless.XUnit`, Shouldly, Moq |

- The host's tests need xUnit v3 because `Avalonia.Headless.XUnit` 12 is built on it. The client's tests need no
  Avalonia, so they keep the repository's xUnit v2 and Reqnroll packages. Don't move the client tests to v3 to make
  the two match.
- Business rules of the companion (a code expires after 10 minutes, a revoked companion must pair again, polling
  never runs faster than every five seconds) are Reqnroll scenarios in the client tests, as `dotnet-unit-tests`
  requires. Views, wiring and platform code are technical xUnit tests.
- Both projects reference `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio` and `coverlet.collector`, and run with
  `dotnet test RaidManager.sln --settings coverage.runsettings` like every other project.

Host test project file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <PackageId>RaidManager.Companion.Tests</PackageId>
    <Version>1.0.0</Version>
    <PackageTags>tests;companion;raid-management</PackageTags>
    <Description>Headless tests of the RaidManager desktop companion's views, tray and platform code.</Description>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../../../../../src/Containers/UI/Hosting/RaidManager.Companion/RaidManager.Companion.csproj" />
    <PackageReference Include="Avalonia.Headless.XUnit" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio"><PrivateAssets>all</PrivateAssets></PackageReference>
    <PackageReference Include="coverlet.collector"><PrivateAssets>all</PrivateAssets></PackageReference>
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Moq" />
  </ItemGroup>
</Project>
```

xUnit v3 adds no global `using Xunit;`: write it in each file that uses `Assert` or `[Fact]`.

## 2. The headless test application

One file per host test project wires the tests to the real `App`, so the theme, data templates and tray icon load as
they do for players:

```csharp
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace RaidManager.Companion.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
```

- The default headless drawing produces no pixels and needs no native library, so tests run on any Linux runner.
  Keep it that way in committed tests; rendered frames are for the mockup comparison only (section 6).
- `App` must not need the generic host or the network to load: tests set each window's `DataContext` themselves. If
  `App.OnFrameworkInitializationCompleted` builds services, test the composition root separately (section 5).
- Isolation is per test by default: each test gets a new `Application` and dispatcher. Keep it; shared state between
  view tests hides ordering bugs.

## 3. View tests

Mark each test `[AvaloniaFact]` or `[AvaloniaTheory]`: they run on the headless dispatcher, so awaits resume on the
UI thread. A test shows the real view with a real view model whose services are fakes, then asserts what the player
would see.

```csharp
[AvaloniaFact]
public void PairingView_WhenTheCodeExpires_ShowsGetANewCode()
{
    var viewModel = PairingViewModels.Expired();
    var window = new Window { Content = new PairingView { DataContext = viewModel } };
    window.Show();

    window.GetVisualDescendants().OfType<Button>()
        .Single(button => AutomationProperties.GetName(button) == "Get a new code")
        .IsVisible.ShouldBeTrue();
}
```

- **Bindings:** change the view model, then assert the control (`text.Text.ShouldBe("ABC-DEF")`); a compiled binding
  updates synchronously. Call `Dispatcher.UIThread.RunJobs()` before asserting anything that layout or a posted
  callback changes.
- **Commands:** prove the binding, not the command (the client tests cover the command): click through input or
  invoke the bound command and assert the fake service saw the call. `button.RaiseEvent(new
  RoutedEventArgs(Button.ClickEvent))` does not run a bound command; simulate input instead:
  `window.MouseDown(point, MouseButton.Left)` then `window.MouseUp(...)`, or `button.Focus()` then
  `window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None)`. Type text with `window.KeyTextInput("...")`.
- **One test per mockup state** (states 5 to 8 of `companion-pairing`): the right texts, the visible actions, the
  focused control, and `AutomationProperties.Name` on controls without text.
- **Tray:** read `TrayIcon.GetIcons(Application.Current!)`, check the menu entries against the tray board, and
  execute an entry's `Command` to check it reaches the tray view model (verified in the #521 prototype).
- **Window close:** call `window.Close()` and assert the window is hidden and the application still runs, unless the
  test first sets the quitting state.
- Never assert on private fields, generated names or template parts the mockup doesn't show.

## 4. Platform-specific tests

Windows-only code (the DPAPI protector, the single-instance signal) has a technical test that runs on Windows and is
skipped elsewhere. Both attributes are needed: `SupportedOSPlatform` satisfies the platform analyzer (CA1416), and
`Assert.SkipUnless` (xUnit v3) skips the test at run time on Linux.

```csharp
[Fact]
[SupportedOSPlatform("windows")]
public void Protect_ThenUnprotect_ReturnsTheOriginalBytes()
{
    Assert.SkipUnless(OperatingSystem.IsWindows(), "DPAPI exists only on Windows.");
    var protector = new DpapiTokenProtector();

    protector.Unprotect(protector.Protect([1, 2, 3])).ShouldBe(new byte[] { 1, 2, 3 });
}
```

Run these locally on Windows before handover and say so in the pull request: CI on Linux reports them as skipped.
Keep such classes small, because their lines count as uncovered in the CI coverage report.

## 5. Client and composition tests

- **Time:** inject `TimeProvider` and use `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`, added to
  `Directory.Packages.props` when first needed). Advance it to fire countdowns, expiry and polling delays; never wait
  for real time.
- **HTTP:** test API clients against a stub `HttpMessageHandler` that returns the API's real problem-details bodies
  (`CompanionPairing.Pending`, `Companion.Revoked`) and records the requests; assert the method, path, body and the
  `Authorization` header. No real network.
- **Token store:** a fake `ITokenProtector`, a temporary folder per test, and checks that a failed write leaves the
  previous file intact and that a revoked or expired 401 deletes it.
- **Composition root:** build the service collection with test configuration and resolve every root view model, so
  a missing registration fails a test rather than the player's first start.
- **Polling:** assert the interval (the API's value, never under five seconds, plus five after a 429), that it stops
  on expiry, success and disposal, and that a network failure keeps polling.

## 6. Rendered frames for the mockup comparison

`raidmanager-conventions` §10 requires comparing every UI state with its mockup board. For the companion, render each
state to PNG with a throwaway test, never committed, that switches the test application to Skia:

```csharp
AppBuilder.Configure<App>()
    .UseSkia()
    .UseHarfBuzz()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
```

- Add `Avalonia.Skia` and `Avalonia.HarfBuzz` to the test project for that run only; Avalonia 12 throws "No text
  shaping system configured" when `UseSkia()` comes without `UseHarfBuzz()`.
- Show the window at the board's size, then `window.CaptureRenderedFrame()!.Save(path, new PngBitmapEncoderOptions())`
  into the scratchpad (the `Save(string, int?)` overload is obsolete in Avalonia 12 and fails the build).
- Crop the board out of the mockup SVG as `raidmanager-conventions` §10 describes, render it to PNG, and compare
  layout, colors, type, button shape and case.
- On Linux, Skia needs `libfontconfig1` (missing from the .NET SDK container image), and frames only match the board
  when the companion embeds Open Sans (`avalonia-desktop` §6).
- Remove the throwaway test and the packages before committing.

## 7. Coverage

- Both test projects load their `src/` project, so both count in the gate (ADR-0015). The compiled `.axaml` markup
  counts as coverable lines of its view, so a view no test shows lowers the changed-lines figure.
- `Program.Main` can't run under a test: keep it to one line. Windows-only classes count as uncovered on Linux CI
  (section 4).
- Never add `[ExcludeFromCodeCoverage]` or widen `coverage.runsettings` to pass the gate.

## 8. Review checklist

- [ ] Client tests on xUnit v2 with Reqnroll for business rules; host tests on xUnit v3 with `Avalonia.Headless.XUnit`.
- [ ] One `TestAppBuilder` using the real `App` and headless drawing; no Skia in committed tests.
- [ ] Every mockup state has a view test; commands are proven through input, not `RaiseEvent`.
- [ ] Windows-only tests carry `[SupportedOSPlatform("windows")]` and `Assert.SkipUnless`, and were run on Windows.
- [ ] Time, HTTP and the token protector are faked; no real network, clock or user profile.
- [ ] Each state was compared with its board through a rendered frame, and the throwaway test is gone.
- [ ] The coverage gate passes without new exclusions.

## Sources

- Avalonia 12 documentation: headless testing with xUnit, the headless platform (input, flushing, visual regression),
  breaking changes (xUnit v3, configurable text shaper).
- `dotnet-unit-tests`, `gherkin-scenarios`, `raidmanager-conventions` §8 and §10, ADR-0015 and ADR-0032; the
  prototype of #521.
