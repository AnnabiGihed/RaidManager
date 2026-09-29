# Warmane Raid Manager

Warmane Raid Manager is a Discord-authenticated raid-planning platform for Warmane realms.
Its differentiator is that raid leaders build rosters from synchronized World of Warcraft character data instead of
manually maintained GearScore, talent, gear and lockout fields.

## Status

Foundation and initial Domain layer are scaffolded. Application, persistence, Discord OAuth, Warmane integration,
addon sync, raid planning UI and Discord bot behavior will be implemented as vertical slices on top of this foundation.

## Product direction

- Discord is the only user authentication provider.
- Warmane is the initial game platform, with WotLK 3.3.5a as the first supported client.
- Warmane Armory supplies public character data where reliable.
- The WoW addon supplies data the Armory cannot reliably model, especially raid lockouts and multiple gear/loadout sets.
- GearScore, gear, talents and stats are loadout-specific. Raid lockouts are character-specific.
- Raid signups offer one or more verified character loadouts; a roster selects at most one option per user.

## Architecture

The repository follows Clean Architecture with feature-first DDD/CQRS slices. Dependencies point inward:

```text
Containers -> Application -> Domain
     |             ^
     v             |
Infrastructure ----+
```

The solution uses .NET 10, C# 14, Blazor, ASP.NET Core, SQL Server, .NET Aspire and Pivot.Framework. Pivot's Keycloak
packages are intentionally excluded because authentication is Discord OAuth only.

See the [architecture documentation](docs/architecture/overview.md) and
[ADR-0001](docs/adr/0001-foundation-and-authentication.md).

## Local prerequisites

- .NET 10 SDK
- Docker Desktop or another Docker-compatible runtime
- GitHub Packages credentials able to read `AnnabiGihed/Pivot.Framework`
- Discord application credentials (required once the authentication slice is implemented)

Set these environment variables before restore:

```bash
export PIVOT_PACKAGES_USER="your-github-user"
export PIVOT_PACKAGES_TOKEN="your-github-package-token"
```

Then restore and build:

```bash
dotnet restore RaidManager.sln
dotnet build RaidManager.sln --no-restore
```

> The initial generated skeleton deliberately uses floating package ranges where a stable version could not be verified
> in this execution environment. Pin all entries in `Directory.Packages.props` before the first production deployment.

## Ownership

Owner: Gihed Annabi
