# ADR-0027: Run the test environment on one OVH VPS with Docker Compose

- Status: Proposed
- Date: 2026-10-03
- Deciders: Gihed Annabi
- Amended: 2026-10-03, by #387, while still Proposed (owner decision on #374)

The server turned out to be in use already: it hosts the `pivotsoftwares.com` website and Delivery Atlas behind one
shared Caddy. The amendment keeps RaidManager beside them: it joins the shared proxy instead of running its own,
the server isn't reinstalled, the memory budget counts what already runs, and provisioning only adds.

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
  version) and a Docker Compose environment, so `aspire publish` writes `docker-compose.yaml` and an `.env` template
  from the same app model that runs locally. No hand-written Compose file can drift from the AppHost.
- **Images are built in GitHub Actions, never on the server.** Building needs the Pivot.Framework feed credentials
  and more memory than the server can spare. The workflow (#376) builds the images, copies them to the server over
  SSH (`docker save` piped to `docker load`), copies the Compose file, and runs `docker compose up -d`. The server
  pulls from no registry, so it holds no registry credentials; how the workflow authenticates over SSH is #103's
  decision.

### The shared Caddy in front, with Let's Encrypt certificates

- **The server's shared Caddy stays the only container with published ports**, 80 and 443. It obtains and renews
  Let's Encrypt certificates by itself and forwards each hostname to its container. RaidManager doesn't run a proxy
  of its own, and the AppHost adds none.
- **RaidManager adds two sites to the shared Caddyfile**, kept in the repository as
  `deploy/test-server/raidmanager.Caddyfile`. They forward to `raidmanager-web:8080` and `raidmanager-api:8080`. Until
  those containers exist, `handle_errors` answers 503 with a short message, so both hostnames get their certificates
  before the first deployment, and a deployment never edits the shared file.
- **The website and API containers join the external network `web`** under the aliases `raidmanager-web` and
  `raidmanager-api`. SQL Server and the bot stay on the RaidManager Compose network.
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
| Ubuntu and Docker | 0.5 GB |
| Shared Caddy, website and Delivery Atlas (measured: 46 MB) | 0.1 GB |
| SQL Server Express | 2.0 GB at most |
| API, website and bot | 0.75 GB together |
| Aspire dashboard | 0.15 GB |
| Total | 3.5 GB of the 3.7 GiB (about 3.9 GB) the system sees |

A 2 GB swap file absorbs peaks. If the measured use stays above 3.5 GB, the dashboard is the first thing to remove.
The database stays in a Docker volume on the local disk. The images take about 3 GB, so the 40 GB disk keeps the
current and the previous images, and the workflow prunes older ones.

### The server

- Ubuntu 24.04 with Docker Engine and the Compose plugin, already installed, and unattended security upgrades.
- A `deploy` user that owns `/opt/apps/raidmanager`, beside the other applications, and belongs to the `docker` group.
  SSH accepts keys only, and `root` can't sign in over SSH.
- `ufw` allows 22, 80 and 443 for RaidManager. Docker bypasses `ufw` for published ports, which is why only Caddy
  publishes any. RaidManager doesn't need the 8080 rule; provisioning reports it and leaves it to the owner.
- OVH's daily automated backup covers the server and the database volume. Test data can be lost without harm.

## Changes the applications need

These belong to #375 (settings) and #376 (pipeline), not to this decision:

- **Shared network:** the Compose file puts the website and the API on the external network `web` with the aliases
  above, and publishes no port.
- **Forwarded headers:** the website and the API call `UseForwardedHeaders` for the proxy's scheme and host, or Discord
  sign-in builds an `http` redirect.
- **Data protection keys:** the website keeps its keys in a Docker volume. Otherwise, each deployment signs everyone
  out and breaks protected community links (`CommunityLinkProtector`).
- **Environment name:** the containers run as `Staging`, which keeps Development-only pages such as `/scalar` off.
- **Secrets:** the Discord client secret, the bot token, the website key and the SQL password reach the containers as
  #103 decides. Until then, the `.env` file on the server holds no value.

## Provisioning steps for #374

The server is never reinstalled: that would erase the applications already on it. `deploy/test-server/provision.sh`
does steps 3 to 5; it only adds what is missing, so it can run again, and it never resets the firewall or stops
another application. The how-to [Provision the test server](../how-to/provision-the-test-server.md) has the commands.

1. **Owner:** at the DNS provider of `pivotsoftwares.com`, add A records for `raidmanager-test` and
   `api.raidmanager-test` pointing at the server's IPv4 address, and AAAA records if the server has IPv6.
2. **Owner:** copy `deploy/test-server` to the server and run `sudo bash provision.sh` from the `ubuntu` account.
3. The script turns off SSH password and `root` sign-in, after checking that `ubuntu` signs in with a key; allows
   22, 80 and 443 in `ufw`; and turns on unattended security upgrades.
4. It creates the 2 GB swap file and the `deploy` user in the `docker` group, with `/opt/apps/raidmanager`.
5. Once both hostnames resolve to the server, it appends the two sites to `/opt/apps/proxy/Caddyfile`, validates it
   and reloads Caddy; if Caddy rejects it, the previous file is put back.
6. Check from outside: SSH works with a key only, no port other than 22, 80, 443 and the owner's 8080 is open, and
   both hostnames answer 503 over HTTPS with a valid certificate.

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

- Memory is tight: SQL Server takes half the server, and a heavy test load can swap. Moving the database to a managed
  service or a larger VPS is the way out if that happens.
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
