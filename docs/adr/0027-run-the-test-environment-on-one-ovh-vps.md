# ADR-0027: Run the test environment on one OVH VPS with Docker Compose

- Status: Proposed
- Date: 2026-10-03
- Deciders: Gihed Annabi

## Context

Release v0.3 deploys RaidManager to a test environment on an OVH VPS-1 (owner decision on #367). Spike #373 asks how
the Aspire application runs there before the server is provisioned (#374), configured (#375) and deployed to
automatically (#376).

The application is four resources in `RaidManager.AppHost`: the website, the API, the Discord bot and SQL Server. The
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

## Decision

### Containers from the AppHost, through Docker Compose

- **Aspire generates the Compose file.** The AppHost adds `Aspire.Hosting.Docker` (13.5.4, the pinned Aspire
  version) and a Docker Compose environment, so `aspire publish` writes `docker-compose.yaml` and an `.env` template
  from the same app model that runs locally. No hand-written Compose file can drift from the AppHost.
- **Images are built in GitHub Actions, never on the server.** Building needs the Pivot.Framework feed credentials
  and more memory than the server can spare. The workflow (#376) builds the images, copies them to the server over
  SSH (`docker save` piped to `docker load`), copies the Compose file, and runs `docker compose up -d`. The server
  pulls from no registry, so it holds no registry credentials; how the workflow authenticates over SSH is #103's
  decision.

### Caddy in front, with Let's Encrypt certificates

- **Caddy is the only container with published ports**, 80 and 443. It obtains and renews Let's Encrypt
  certificates by itself and forwards each hostname to its container. The AppHost adds it in publish mode only, with
  a `Caddyfile` mounted read-only, so it is in the same Compose file.
- **Hostnames (owner decision on #380):**

  | Hostname | Container |
  | --- | --- |
  | `raidmanager-test.pivotsoftwares.com` | website |
  | `api.raidmanager-test.pivotsoftwares.com` | API |

- **The applications listen on plain HTTP inside the Compose network** and trust the proxy's forwarded headers, so
  they see the original scheme and host. Discord sign-in redirects to
  `https://raidmanager-test.pivotsoftwares.com/signin-discord`.
- **SQL Server, the bot and the Aspire dashboard publish no port.** The dashboard binds to `127.0.0.1` on the server
  and is reached through an SSH tunnel.

### SQL Server Express beside the applications

SQL Server fits if its memory is capped. It runs the same `mssql/server:2022` image as the local run, with
`MSSQL_PID=Express`, which the license allows in production too, and `MSSQL_MEMORY_LIMIT_MB=2048`, since SQL
Server on Linux needs 2 GB. Express caps its buffer pool at about 1.4 GB, well below the limit. The planned memory:

| Process | Planned memory |
| --- | --- |
| Ubuntu, Docker and Caddy | 0.5 GB |
| SQL Server Express | 2.0 GB at most |
| API, website and bot | 0.75 GB together |
| Aspire dashboard | 0.15 GB |
| Total | 3.4 GB of 4 GB |

A 2 GB swap file absorbs peaks. If the measured use stays above 3.5 GB, the dashboard is the first thing to remove.
The database stays in a Docker volume on the local disk. The images take about 3 GB, so the 40 GB disk keeps the
current and the previous images, and the workflow prunes older ones.

### The server

- Ubuntu 24.04 with Docker Engine and the Compose plugin from the Docker repository, and unattended security upgrades.
- A `deploy` user that owns `/opt/raidmanager` and belongs to the `docker` group. SSH accepts keys only, and `root`
  can't sign in over SSH.
- `ufw` allows 22, 80 and 443 only. Docker bypasses `ufw` for published ports, which is why only Caddy publishes any.
- OVH's daily automated backup covers the server and the database volume. Test data can be lost without harm.

## Changes the applications need

These belong to #375 (settings) and #376 (pipeline), not to this decision:

- **Forwarded headers:** the website and the API call `UseForwardedHeaders` for the proxy's scheme and host, or Discord
  sign-in builds an `http` redirect.
- **Data protection keys:** the website keeps its keys in a Docker volume. Otherwise, each deployment signs everyone
  out and breaks protected community links (`CommunityLinkProtector`).
- **Environment name:** the containers run as `Staging`, which keeps Development-only pages such as `/scalar` off.
- **Secrets:** the Discord client secret, the bot token, the website key and the SQL password reach the containers as
  #103 decides. Until then, the `.env` file on the server holds no value.

## Provisioning steps for #374

1. **Owner:** in the OVH control panel, install the server with the Ubuntu 24.04 image and add your SSH public key.
2. **Owner:** at the DNS provider of `pivotsoftwares.com`, add A records for `raidmanager-test` and
   `api.raidmanager-test` pointing at the server's IPv4 address, and AAAA records if the server has IPv6.
3. Create the `deploy` user, install its key, then turn off SSH password and `root` sign-in.
4. Turn on `ufw` for 22, 80 and 443, and turn on unattended security upgrades.
5. Install Docker Engine and the Compose plugin, add `deploy` to the `docker` group, and create the 2 GB swap file.
6. Create `/opt/raidmanager` for the Compose file, the `Caddyfile` and the `.env` file, owned by `deploy`.
7. Check from outside: SSH works with a key only, ports other than 22, 80 and 443 are closed, and both hostnames
   resolve to the server.

Steps 1 and 2 need the owner's OVH and DNS access, and the agent never enters credentials. Steps 3 to 7 run over SSH,
by the owner or by the agent once the owner gives it a session.

## Consequences

**Positive**

- The test environment runs the same app model as the local run, so a new resource in the AppHost reaches the
  server without a second definition.
- One small server, one Compose file and automatic certificates keep the setup within what one person can maintain.
- No registry or feed credential is stored on the server.

**Negative**

- Memory is tight: SQL Server takes half the server, and a heavy test load can swap. Moving the database to a managed
  service or a larger VPS is the way out if that happens.
- One server has no redundancy; a failed deployment or an OVH incident takes the environment down until it's fixed.
- Copying images over SSH sends every image in full each time, which is slower than pulling only the changed layers
  from a registry.

## Open questions

- Whether production later runs on the same pattern, on a larger server, is for a later release to decide.
- Docker Compose support in Aspire is recent; if `aspire publish` can't express the Caddy container or the volume for
  data protection keys, #376 adds a small Compose override file and records why.
