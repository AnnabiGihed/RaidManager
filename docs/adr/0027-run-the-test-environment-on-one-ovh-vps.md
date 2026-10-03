# ADR-0027: Run the test environment on one OVH VPS with Docker Compose

- Status: Proposed
- Date: 2026-10-03
- Deciders: Gihed Annabi
- Amended: 2026-10-03, by #387, while still Proposed (owner decisions on #374, #369 and #387); database and memory
  amended by [ADR-0029](0029-store-data-in-postgresql.md) (#410); application settings amended by #388; Compose
  generation detailed by #444; the dev deployment detailed by #389

The server turned out to be in use already: it hosts the `pivotsoftwares.com` website and Delivery Atlas behind one
shared Caddy. The amendment keeps RaidManager beside them: it joins the shared proxy instead of running its own,
the server isn't reinstalled, the memory budget counts what already runs, and provisioning only adds. A second
amendment the same day adds the sites of the dev and production environments the owner decided on #369, so the shared
Caddyfile is edited once. ADR-0029 then replaces SQL Server with PostgreSQL, one instance per environment, and its
memory budget covers the three environments; the rest of this record still describes the test environment.

## Context

Release v0.3 deploys RaidManager to a test environment on an OVH VPS-1 (owner decision on #367). Spike #373 asks how
the Aspire application runs there before the server is provisioned (#374), configured (#375) and deployed to
automatically (#376).

The application is four resources in `RaidManager.AppHost`: the website, the API, the Discord bot and the database
(SQL Server, then PostgreSQL by ADR-0029). The
desktop companion uploads to the API from players' machines (ADR-0002), so the API is public as well as the website.

The server, as ordered by the owner (#380, 2026-10-03):

| Property | Value |
| --- | --- |
| Offer | OVH VPS-1 2027, datacenter in Gravelines, France |
| Processor | 2 vCores |
| Memory | 4 GB |
| Storage | 40 GB NVMe SSD, local |
| Image | Ubuntu 24.04 |
| Backup | OVH Automated Backup Standard |

What already runs on it, as read on 2026-10-03 (#374):

| Property | Value |
| --- | --- |
| Memory seen by the system | 3.7 GiB, no swap |
| Disk | 38 GB, 34 GB free |
| Docker | Engine 29.8.2, Compose 5.6.0 |
| Applications | the `pivotsoftwares.com` website (`pivot-website`) and Delivery Atlas (`delivery-atlas`), each in its own folder under `/opt/apps` |
| Proxy | one `caddy:2` container (`caddy`), the only one publishing ports 80 and 443, with its Caddyfile at `/opt/apps/proxy/Caddyfile` |
| Network | the applications reach the proxy through the external Docker network `web` and publish no port |
| Administrator account | `ubuntu`, with sudo and Docker access |
| Firewall | `ufw` active, allowing 22, 80, 443 and 8080 |

## Decision

### Containers from the AppHost, through Docker Compose

- **Aspire generates the Compose file.** The AppHost adds `Aspire.Hosting.Docker` (13.5.4, the pinned Aspire
  version) and a Docker Compose environment, so publishing writes `docker-compose.yaml` and an `.env` template from
  the same app model that runs locally. No hand-written Compose file can drift from the AppHost, and no override file
  is needed (#444). Publishing runs the AppHost itself:

  ```bash
  dotnet run --project src/Containers/Aspire/Hosting/RaidManager.AppHost -- --operation publish --step publish --output-path <folder>
  ```

- **One file serves the three environments.** Each runs it as its own Compose project, named
  `raidmanager-dev`, `raidmanager-test` or `raidmanager-prod` with `docker compose -p`, so its volumes and network are
  its own, and the workflow fills its `.env`:

  | `.env` key | Value | From |
  | --- | --- | --- |
  | `DEPLOY_ENVIRONMENT` | `dev`, `test` or `prod`, in the container names Caddy forwards to | the workflow |
  | `ASPNETCORE_ENVIRONMENT` | `Dev`, `Test` or `Production` (#388) | the workflow |
  | `API_PORT`, `WEB_PORT` | `8080`, the port Caddy's sites use | the workflow |
  | `API_IMAGE`, `WEB_IMAGE`, `DISCORD_BOT_IMAGE` | the images the run built | the workflow |
  | `POSTGRES_MEMORY_LIMIT`, `POSTGRES_SHARED_BUFFERS` | `192M` and `64MB` for dev and test, `320M` and `128MB` for production (ADR-0029) | the workflow |
  | `DISCORD_CLIENT_ID`, `DISCORD_CLIENT_SECRET`, `DISCORD_BOT_TOKEN`, `WEBSITE_SERVICE_KEY`, `POSTGRES_PASSWORD` | the environment's values | its GitHub environment ([ADR-0028](0028-keep-the-test-secrets-in-a-github-environment.md)) |

  The published template holds no value; a test checks it, so no local secret can reach it.
- **Volumes with fixed names.** The database's volume is `postgres-data` when published, because the generated name
  hashes the build folder, and a new hash on another build machine would start an empty database. The website's
  `data-protection` volume is mounted on `/home/app`: the images run as the non-root `app` user, and Docker gives a
  new volume the owner of its folder only when the image already has that folder (tested on the .NET 10 image).
- **No dashboard and no restart by hand:** the Compose environment has no Aspire dashboard (ADR-0029), and every
  service restarts unless stopped.

### Deploying an environment

The `deploy-dev` workflow deploys `main` to dev; the `review` workflow starts it after each merge, and it can be
started by hand from the Actions tab. Its steps, with the server's part in `deploy/server/deploy-environment.sh`:

1. Build the three images on the runner (`dotnet publish -t:PublishContainer`), tagged with the commit, and generate
   the Compose file.
2. Over SSH, with the environment's deploy key in an `ssh-agent`: load the images (`docker save` piped to
   `docker load`), copy the Compose file, and write `.env` from the environment's secrets, as `*.next` files.
3. On the server: keep the current files as `*.previous`, switch to the new ones, start PostgreSQL, and run the new
   API image once with `--Database:MigrateAndExit=true`, so the migrations apply before any new version serves.
4. Start the new version and wait until the API answers 200 and the website 200 or 302 on the environment's network.
5. From the runner, through Caddy: the website answers 200, sign-in redirects to Discord with the environment's HTTPS
   address, and the API answers 404 for an unknown companion route (Caddy alone would answer 503).
6. If step 3, 4 or 5 fails, the previous files come back and the previous version runs again; a first deployment
   that fails is stopped. A migration that already ran isn't undone: that is #434's.
7. On success, the RaidManager images that no environment's current or previous version uses are removed.

- **Images are built in GitHub Actions, never on the server.** Building needs the Pivot.Framework feed credentials
  and more memory than the server can spare. The workflow (#376) builds the images, copies them to the server over
  SSH (`docker save` piped to `docker load`), copies the Compose file, and runs `docker compose up -d`. The server
  pulls from no registry, so it holds no registry credentials; how the workflow authenticates over SSH is #103's
  decision.

### The shared Caddy in front, with Let's Encrypt certificates

- **The server's shared Caddy stays the only container with published ports**, 80 and 443. It obtains and renews
  Let's Encrypt certificates by itself and forwards each hostname to its container. RaidManager doesn't run a proxy
  of its own, and the AppHost adds none.
- **RaidManager adds six sites to the shared Caddyfile**, one website and one API for each environment, kept in the
  repository as `deploy/server/raidmanager.Caddyfile`. Each forwards to its container on port 8080. Until that
  container exists, `handle_errors` answers 503 with a short message, so every hostname gets its certificate before
  its first deployment, and a deployment never edits the shared file.
- **The public API hostnames serve the desktop companion only** (owner decision on #369). Each `api.` site forwards
  the paths under `/companion/` and answers 404 to everything else, including `/internal/...` and `/openapi`, so the
  routes only the website uses are never reachable from the internet. The website calls its API inside Docker, at
  `http://raidmanager-<env>-api:8080`, never through the public hostname. The API still checks its own
  authentication behind the proxy.
- **Each environment's website and API containers join the external network `web`** under the aliases below. The
  database and the bots stay on each environment's own Compose network.
- **Hostnames (owner decisions on #380 and #369):**

  | Environment | Hostname | Container alias |
  | --- | --- | --- |
  | Dev | `raidmanager-dev.pivotsoftwares.com` | `raidmanager-dev-web` |
  | Dev | `api.raidmanager-dev.pivotsoftwares.com` | `raidmanager-dev-api` |
  | Test | `raidmanager-test.pivotsoftwares.com` | `raidmanager-test-web` |
  | Test | `api.raidmanager-test.pivotsoftwares.com` | `raidmanager-test-api` |
  | Production | `raidmanager.pivotsoftwares.com` | `raidmanager-prod-web` |
  | Production | `api.raidmanager.pivotsoftwares.com` | `raidmanager-prod-api` |

- **The applications listen on plain HTTP inside the Compose network** and trust the proxy's forwarded headers, so
  they see the original scheme and host. Discord sign-in redirects to
  `https://raidmanager-test.pivotsoftwares.com/signin-discord`.
- **The databases and the bots publish no port.** The Aspire dashboard doesn't run on the server (ADR-0029).

### The database

[ADR-0029](0029-store-data-in-postgresql.md) replaces the SQL Server Express instance first planned here (2 GB of
memory) with PostgreSQL, one instance per environment, and gives the memory budget for the three environments:
about 2.8 GB of the 3.7 GiB the system sees, with the 2 GB swap file for peaks. Each database stays in a Docker volume
on the local disk. Each environment keeps its current and previous images, and the workflow prunes older ones.

### The server

- Ubuntu 24.04 with Docker Engine and the Compose plugin, already installed, and unattended security upgrades.
- A `deploy` user that owns `/opt/apps/raidmanager`, beside the other applications, and belongs to the `docker` group.
  `root` can't sign in over SSH, and SSH accepts keys only once the owner signs in with a key (#387).
- `ufw` allows 22, 80 and 443 for RaidManager. Docker bypasses `ufw` for published ports, which is why only Caddy
  publishes any. RaidManager doesn't need the 8080 rule; provisioning reports it and leaves it to the owner.
- OVH's daily automated backup covers the server and the database volumes. A logical backup of production's database
  comes with the production work (ADR-0029).

## Changes the applications need

The applications support these settings (#388); the deploy workflow (#389) sets them in each environment's Compose
file:

- **Shared network:** the Compose file puts the website and the API on the external network `web` with the aliases
  above, and publishes no port.
- **Internal API address:** the website reaches its API through Aspire service discovery (`https+http://api`), which
  the generated Compose file resolves to the environment's own `api` service, never the public hostname, which
  forwards only `/companion/`.
- **Forwarded headers:** the website and the API run with `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, so they trust
  Caddy's `X-Forwarded-Proto`; Caddy keeps the original host. Without it, Discord sign-in builds an `http` redirect.
  Trusting any sender is safe here only because the containers publish no port: Caddy is the only way in.
- **Data protection keys:** the website keeps its keys in the directory set by `DataProtection__KeysPath`, on a
  volume. Otherwise, each deployment signs everyone out and breaks protected community links
  (`CommunityLinkProtector`).
- **Environment names:** the containers run as `Dev`, `Test` or `Production` (owner decision on #388). None counts as
  Development, so Development-only pages such as `/scalar` and the automatic migrations stay off; the first
  deployment applies migrations as a step of its own (#389, then #434).
- **Secrets:** the Discord client secret, the bot token, the website key and the database password reach the
  containers from each environment's GitHub environment, as [ADR-0028](0028-keep-the-test-secrets-in-a-github-environment.md)
  decides.

## Provisioning steps for #374

The server is never reinstalled: that would erase the applications already on it. `deploy/server/provision.sh`
does steps 3 to 5; it only adds what is missing, so it can run again, and it never resets the firewall or stops
another application. The how-to [Provision the server](../how-to/provision-the-server.md) has the commands.

1. **Owner:** at the DNS provider of `pivotsoftwares.com`, add an A record for each of the six hostnames above,
   pointing at the server's IPv4 address, and AAAA records if the server has IPv6.
2. **Owner:** copy `deploy/server` to the server and run `sudo bash provision.sh` from the `ubuntu` account.
3. The script turns off `root` sign-in over SSH, allows 22, 80 and 443 in `ufw`, and turns on unattended security
   upgrades. Password sign-in stays on until the owner runs it again with `--keys-only`, which it accepts only after
   `ubuntu` has signed in with an SSH key.
4. It creates the 2 GB swap file and the `deploy` user in the `docker` group, with `/opt/apps/raidmanager`.
5. Once all six hostnames resolve to the server, it appends the six sites to `/opt/apps/proxy/Caddyfile`, validates it
   and reloads Caddy; if Caddy rejects it, the previous file is put back.
6. Check from outside: no port other than 22, 80, 443 and the owner's 8080 is open, every hostname answers 503 over
   HTTPS with a valid certificate, and, after `--keys-only`, password sign-in is refused.

Steps 1 and 2 need the owner's DNS and server access, and the agent never enters credentials or signs in to the
server (owner decision on #374).

## Consequences

**Positive**

- The test environment runs the same app model as the local run, so a new resource in the AppHost reaches the
  server without a second definition.
- One small server, one Compose file and automatic certificates keep the setup within what one person can maintain.
- Sharing the proxy adds no container and no port, and the other applications keep running untouched.
- No registry or feed credential is stored on the server.

**Negative**

- Memory stays tight with three environments, even with PostgreSQL: about 2.8 GB of 3.9 GB is planned, and a heavy
  load can swap. A larger VPS, or moving production off the shared server (owner decision on #369: later), is the way
  out if that happens.
- One server has no redundancy; a failed deployment or an OVH incident takes the environment down until it's fixed.
- RaidManager shares the server with the `pivotsoftwares.com` website and Delivery Atlas: a memory peak in one can
  slow the others, and a broken edit of the shared Caddyfile would affect all of them, which is why provisioning
  validates it and restores the previous file on failure.
- Copying images over SSH sends every image in full each time, which is slower than pulling only the changed layers
  from a registry.

## Open questions

- Whether production later runs on the same pattern, on a larger server, is for a later release to decide.
- Docker Compose support in Aspire is recent; if `aspire publish` can't express the Caddy container or the volume for
  data protection keys, #376 adds a small Compose override file and records why. The same holds for the external
  `web` network and the aliases.
