# RaidManager

RaidManager is a Discord-authenticated raid-planning platform for Warmane players, built around synchronized
World of Warcraft (WoW) 3.3.5a character data.

## Status

**Experimental.** Owned by Gihed Annabi ([project issues](https://github.com/AnnabiGihed/RaidManager/issues)).
The solution builds and the initial domain model has tests, but the
website, Discord bot, addon synchronization, and raid workflows are not yet usable product features.

## Build locally

You need the .NET 10 software development kit (SDK), and Docker for the Aspire host's local PostgreSQL and for the
persistence and API integration tests, which start a PostgreSQL container.

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

The tests cover the Domain, Application, Infrastructure, view model and website projects, the AppHost's application
model, and, against a PostgreSQL container, the Entity Framework Core persistence and the API. The end-to-end test
project is still a placeholder, and the Discord bot has no tests yet.

To measure test coverage the way CI does, run the tests with the coverage settings and read the summary:

```bash
dotnet test RaidManager.sln --no-build --settings coverage.runsettings --results-directory TestResults
python scripts/coverage_gate.py --reports TestResults --base origin/main
```

Pull requests must cover at least 80% of the source lines they change, and total coverage must stay at or above
60% ([ADR-0015](docs/adr/0015-gate-pull-requests-on-test-coverage.md)). Each pull request shows the result in a
coverage comment.

Run the product through the Aspire AppHost. It generates the shared website key that the API requires
(`Website:ServiceKey`, at least 32 characters) and passes it to the API and the website.

- **Visual Studio:** set `RaidManager.AppHost` as the startup project (right-click it, then **Set as Startup
  Project**), choose the `https` profile, and start it. Starting another project on its own fails for lack of the
  secrets the AppHost provides.
- **Command line:** `dotnet run --project src/Containers/Aspire/Hosting/RaidManager.AppHost`.

Docker Desktop must be running. The browser opens the Aspire dashboard at `https://localhost:17190`; the console
window only shows logs. Wait until `postgres`, `api`, and `web` show **Running**. The first run downloads the
PostgreSQL image, and the API then creates the database schema. The website is at
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

### Give the API the Discord bot token

The API asks Discord for each member's roles with the bot's token
([ADR-0022](docs/adr/0022-check-discord-roles-at-action-time.md)):

1. Set up the bot as [Set up the Discord application and bot](docs/how-to/set-up-discord.md) describes, which also
   checks the setup against Discord.
2. From the repository root, store its token:

   ```bash
   dotnet user-secrets set "Parameters:discord-bot-token" "<bot-token>" --project src/Containers/Aspire/Hosting/RaidManager.AppHost
   ```

The AppHost passes it to the API, which refuses to start without it.

### Deploy to the environments

RaidManager has three environments on one shared OVH server
([ADR-0027](docs/adr/0027-run-the-test-environment-on-one-ovh-vps.md)), none deployed yet:

| Environment | Website | Deployed |
| --- | --- | --- |
| Dev | `https://raidmanager-dev.pivotsoftwares.com` | on every merge to `main` |
| Test | `https://raidmanager-test.pivotsoftwares.com` | on every release |
| Production | `https://raidmanager.pivotsoftwares.com` | when a release is tested and confirmed |

Each API is at the same address with `api.` in front, and serves only the desktop companion's routes. The server is
shared with other applications; [Provision the server](docs/how-to/provision-the-server.md) prepares it without
touching them.

Their secrets never live in the repository or in user secrets
([ADR-0028](docs/adr/0028-keep-the-test-secrets-in-a-github-environment.md)):

- They are environment secrets of the GitHub environments `dev`, `test` and `production`, with the same names in
  each. Only `main` can deploy to `dev`, and only release tags (`v*`) to `test` and `production`. The deploy workflow
  writes them to the server at each deployment.
- Each environment has its own Discord application, separate from the one you use locally, and its own deploy key.
- Set or change a value from your own machine; `gh` asks for it at the prompt:

  ```bash
  gh secret set DISCORD_CLIENT_SECRET --env test
  ```

  Then deploy that environment again, so the server picks up the new value. ADR-0028 lists every secret and how to
  rotate it.

To see what an environment runs, publish the AppHost's Compose file and its `.env` template, which holds no value:

```bash
dotnet run --project src/Containers/Aspire/Hosting/RaidManager.AppHost -- --operation publish --step publish --output-path artifacts/compose
```

### Change the database schema

The database is PostgreSQL ([ADR-0029](docs/adr/0029-store-data-in-postgresql.md)). Data from the earlier SQL Server
volume isn't carried over; remove that volume in Docker Desktop once you no longer need it.

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
