# RaidManager

RaidManager is a Discord-authenticated raid-planning platform for Warmane players, built around synchronized
World of Warcraft (WoW) 3.3.5a character data.

## Status

**Experimental.** Owned by Gihed Annabi. The solution builds and the initial domain model has tests, but the
website, Discord bot, addon synchronization, and raid workflows are not yet usable product features.

## Build locally

You need the .NET 10 software development kit (SDK) and GitHub Packages read access for
`AnnabiGihed/Pivot.Framework`.
Set `PIVOT_PACKAGES_USER` and `PIVOT_PACKAGES_TOKEN` in your environment before restore.
Docker is needed when running the Aspire host with its local SQL Server resource.

```bash
dotnet restore RaidManager.sln
dotnet build RaidManager.sln --no-restore
dotnet test RaidManager.sln --no-build
```

Most test projects are placeholders; the current executable tests cover the Domain project.

## Product direction

The target product combines a raid-planning website, a Discord bot, a WoW addon, and a desktop companion.
Players sign in through Discord and approve characters discovered across their WoW accounts.
The addon captures character loadouts and raid lockouts; the companion uploads saved data when the game writes it
to disk. Raid leaders schedule raids and select from players' offered characters and specializations.
Eligibility depends on whether each character's matching lockout has expired by the scheduled raid start.
The first usable version also includes recurring raid templates, one signup shared by the website and bot,
officer compositions and buff coverage, boss assignments, reminders, gear checks, attendance history,
and roster export to the addon.
Missing or stale lockout data requires a fresh sync or a reasoned officer exception; an active lockout cannot be
overridden.

The raid list, signup, roster, character, raid-save, and preset workflows use
[raiding.site](https://raiding.site/) as a product reference. The [product scope](./docs/explanation/product.md)
records the observed reference screens and the planned improvements; it does not claim feature parity today.
All workflows in that scope document belong to version 1, not a later feature phase.

## Documentation

- [Documentation index](./docs/index.md)
- [Product scope and workflows](./docs/explanation/product.md)
- [Architecture and implementation status](./docs/explanation/architecture.md)
- [Architecture decisions](./docs/adr/README.md)
- [Contributing](./CONTRIBUTING.md)
- [Changelog](./CHANGELOG.md)

## License

Proprietary. See [LICENSE](./LICENSE).
