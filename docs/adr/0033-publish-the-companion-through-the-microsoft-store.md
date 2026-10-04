# ADR-0033: Publish the companion through the Microsoft Store from GitHub Actions

- Status: Proposed
- Date: 2026-10-04
- Deciders: Gihed Annabi

## Context

[ADR-0032](0032-build-the-windows-companion-in-avalonia.md) builds the companion as a self-contained, unsigned `win-x64`
executable and leaves distribution to a later decision. Testing it on dev, the owner found that Chrome flags its
download as suspicious and Windows SmartScreen stops it as an unrecognized app (spike #532). Signing it ourselves isn't
a good option for a single developer in the EU: Microsoft's Artifact Signing serves individuals only in the United
States and Canada, and an organization-validated certificate is paid and still warns until reputation builds.

The owner chose the Microsoft Store, and decided that publishing is automatic, from the pipelines, and that the pipeline
building the package runs only when the companion changes (owner decisions on #532).

Spike #532 found, with the owner's checks on Windows 11:

- **The package works.** The companion packed as an MSIX full-trust desktop app starts, shows its window and tray icon,
  opens the browser, pairs, keeps its DPAPI token across restarts, and shows its first-close notification. It reads and
  writes the real `%LOCALAPPDATA%\RaidManager` folder, so the Store version and the downloaded one share the pairing.
- **A package must be signed.** Windows refuses an unsigned package that contains an application (`0x80073D2B`). The
  Store signs what it distributes.
- **Publishing can be automated.** The Microsoft Store Developer CLI (`msstore`) publishes package updates from GitHub
  Actions with a Microsoft Entra app registration. It updates only a product that is already published, so the first
  submission is made once by hand.
- **Build it on Windows.** `makeappx` comes with the Windows SDK on `windows-latest`. Building on Linux would need the
  MSIX SDK compiled from source.

## Decision

### Distribution

Players install the companion from the Microsoft Store. The Store signs the package, so there is no certificate of our
own and no warning from Chrome or SmartScreen, and Windows updates the companion by itself. The listing starts with a
**private audience** (the owner and testers) and becomes public with the release that ships the companion to players.

### The package

- **Template:** `src/Containers/UI/Hosting/RaidManager.Companion/Package/` holds the MSIX manifest template and its
  logos (44, 150 and 50 pixels, and the Store's other sizes, generated from the companion's mark). The manifest declares
  a full-trust desktop app (`Windows.FullTrustApplication`, `runFullTrust`) for Windows 10 2004 and later, x64.
- **Identity:** the package name, publisher and publisher display name come from the owner's reservation of the app's
  name in Partner Center. They are kept as repository variables, not secrets, and filled into the template at build
  time.
- **Version:** each package is versioned `<major>.<minor>.<run number>.0`: the release's major and minor, then the
  workflow's run number, so every upload is higher than the last. The Store requires the last part to be 0.
- **Environment:** the package carries the environment of the Store channel, set by a repository variable: `Dev` while
  the audience is private, `Production` once the listing is public (ADR-0032's `CompanionEnvironment`).

### The pipeline

A new workflow, `companion-store.yml`, runs on `windows-latest`, and only when the companion changes:

```yaml
on:
  pull_request:
    paths:
      - 'src/Containers/UI/Core/RaidManager.Companion.Client/**'
      - 'src/Containers/UI/Hosting/RaidManager.Companion/**'
      - '.github/workflows/companion-store.yml'
  push:
    branches: [main]
    paths:
      - 'src/Containers/UI/Core/RaidManager.Companion.Client/**'
      - 'src/Containers/UI/Hosting/RaidManager.Companion/**'
      - '.github/workflows/companion-store.yml'
```

- **On a pull request:** it publishes the companion, packs the MSIX with `makeappx`, and uploads it as an artifact. It
  doesn't publish anything.
- **On a merge to `main`:** it packs the same way, then publishes the package with `msstore publish` through the
  `microsoft/microsoft-store-apppublisher` action. The Store certifies the update, usually within days, and players get
  it automatically.
- **Credentials:** the four credentials (Entra tenant id, client id, client secret, seller id) are secrets of a `store`
  GitHub environment, as [ADR-0028](0028-keep-the-test-secrets-in-a-github-environment.md) does for the test server. The
  product id is a variable.
- **The current `companion` job:** it moves out of `ci.yml` into this workflow, behind the same path filter. The
  unsigned executable stays available to developers as a pull request artifact.

### What the owner sets up once

1. A free Partner Center account as an individual developer.
2. A Microsoft Entra tenant associated with the account, and an app registration with the Manager role in Partner
   Center.
3. The reservation of the app's name.
4. The store listing: description, screenshots, age rating and a privacy policy URL, which a networked app needs.
5. The first submission, by hand, to a private audience.
6. The `store` environment's secrets and the repository variables.

## Consequences

**Positive**

- Players install and update the companion without warnings, certificates or downloads from GitHub.
- Every merge that changes the companion reaches the Store without anyone uploading anything, and other changes start no
  Windows runner.
- Testers get the same Store build players will get, through the private audience.

**Negative**

- A Windows runner for the package, slower than Linux, on companion changes only.
- Each update waits for Store certification, usually up to a few days. An urgent fix isn't instant.
- The first submission, the listing and the privacy policy are manual, once.
- The path filter leaves out the shared build files (`Directory.Build.props`, `Directory.Packages.props`). A package
  update, such as a new Avalonia version, reaches the Store only with the next companion change.

## Alternatives considered

- **An organization-validated code-signing certificate:** downloads stay on GitHub, but it costs every year, needs a
  hardware or cloud key, and SmartScreen still warns until reputation builds.
- **Artifact Signing:** not open to individuals outside the United States and Canada.
- **Building the MSIX on Linux:** possible with the MSIX SDK compiled from source, but that tool would have to be built
  and maintained in CI; `makeappx` is already on Windows runners.
- **Publishing on release tags only:** fewer certifications, but the owner asked for publishing by the pipelines on
  every companion change; the private audience keeps unfinished work away from players.
- **Unsigned packages for testers:** Windows refuses them for an application (`0x80073D2B`).
