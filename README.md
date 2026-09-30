# RaidManager

RaidManager is a Discord-authenticated raid-planning platform for Warmane players, built around synchronized
World of Warcraft (WoW) 3.3.5a character data.

## Status

**Experimental.** Owned by Gihed Annabi ([project issues](https://github.com/AnnabiGihed/RaidManager/issues)).
The solution builds and the initial domain model has tests, but the
website, Discord bot, addon synchronization, and raid workflows are not yet usable product features.

## Build locally

You need the .NET 10 software development kit (SDK), and Docker for the Aspire host's local SQL Server and for the
persistence and API integration tests, which start a SQL Server container.

### Give your machine access to the Pivot.Framework packages

The `Pivot.Framework.*` packages come from the GitHub Packages feed of `AnnabiGihed/Pivot.Framework`.
`nuget.config` reads the feed credentials from two environment variables, so no credential is ever stored in the
repository:

| Variable | Value |
| --- | --- |
| `PIVOT_PACKAGES_USER` | Your GitHub user name. |
| `PIVOT_PACKAGES_TOKEN` | A GitHub personal access token that can read packages. |

1. On GitHub, open **Settings**, **Developer settings**, **Personal access tokens**, **Tokens (classic)**, then
   **Generate new token (classic)**. Select only the `read:packages` scope and set an expiration date.
   Use a classic token: the GitHub Packages NuGet registry does not accept fine-grained tokens.
2. Store both values as environment variables for your user account.

   On Windows, in PowerShell:

   ```powershell
   [Environment]::SetEnvironmentVariable("PIVOT_PACKAGES_USER", "<your-github-user>", "User")
   [Environment]::SetEnvironmentVariable("PIVOT_PACKAGES_TOKEN", "<your-token>", "User")
   ```

   On macOS or Linux, add them to your shell profile, such as `~/.zshrc` or `~/.bashrc`:

   ```bash
   export PIVOT_PACKAGES_USER="<your-github-user>"
   export PIVOT_PACKAGES_TOKEN="<your-token>"
   ```

3. Restart open terminals, your IDE, and any other tool that builds the solution; running programs do not see new
   environment variables.
4. Check the setup with `dotnet restore RaidManager.sln`. A missing or expired token shows as
   "Your request could not be authenticated by the GitHub Packages service" or as `NU1301` errors.

Continuous integration reads the same two names from the repository secrets, so nothing changes there.
Renew the token before it expires, and update the variable on each machine that builds the solution.

### Build, test, and run

```bash
dotnet restore RaidManager.sln
dotnet build RaidManager.sln --no-restore
dotnet test RaidManager.sln --no-build
```

The executable tests cover the Domain and Application projects and, against a SQL Server container, the
Entity Framework Core persistence and the API. The other test projects are placeholders.

Run the product through the Aspire AppHost. It generates the shared website key that the API requires
(`Website:ServiceKey`, at least 32 characters) and passes it to the API and the website.

- **Visual Studio:** set `RaidManager.AppHost` as the startup project (right-click it, then **Set as Startup
  Project**), choose the `https` profile, and start it. Starting another project on its own fails for lack of the
  secrets the AppHost provides.
- **Command line:** `dotnet run --project src/Containers/Aspire/Hosting/RaidManager.AppHost`.

Docker Desktop must be running. The browser opens the Aspire dashboard at `https://localhost:17190`; the console
window only shows logs. Wait until `sql`, `api`, and `web` show **Running**. The first run downloads the SQL Server
image, which takes a few minutes, and the API then creates the database schema. The website is at
`https://localhost:55365`, and the interactive API reference is at `https://localhost:55366/scalar` (linked as
"API reference" in the dashboard).

### Sign in with Discord locally

The website signs players in with the Discord application's OAuth2 credentials. Store them in the Aspire AppHost's
user secrets, which live in your user profile, never in the repository:

1. In the [Discord Developer Portal](https://discord.com/developers/applications), open the application, then
   **OAuth2**, and add the redirect `https://localhost:55365/signin-discord`.
2. Copy the **Client ID**, and generate a **Client Secret**.
3. From the repository root, store both:

   ```bash
   dotnet user-secrets set "Parameters:discord-client-id" "<client-id>" --project src/Containers/Aspire/Hosting/RaidManager.AppHost
   dotnet user-secrets set "Parameters:discord-client-secret" "<client-secret>" --project src/Containers/Aspire/Hosting/RaidManager.AppHost
   ```

The AppHost passes them to the website, which refuses to start without them. The Aspire dashboard also asks for any
parameter that has no value yet.

### Change the database schema

The persistence schema is managed with Entity Framework Core migrations, using the repository's local `dotnet-ef`
tool. After changing a mapping, add a migration:

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add <Name> --project src/Infrastructure/RaidManager.Persistence.EntityFrameworkCore --output-dir Migrations
```

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
- [Published documentation](https://annabigihed.github.io/RaidManager/)
- [GitHub Wiki](https://github.com/AnnabiGihed/RaidManager/wiki), generated from `docs/` after every merge
- [Product scope and workflows](./docs/explanation/product.md)
- [Version 1 visual guide](./docs/explanation/visual-guide.md)
- [Architecture and implementation status](./docs/explanation/architecture.md)
- [Architecture decisions](./docs/adr/README.md)
- [Contributing](./CONTRIBUTING.md)
- [Security reporting](./SECURITY.md)
- [Changelog](./CHANGELOG.md)

## License

Proprietary. See [LICENSE](./LICENSE).
