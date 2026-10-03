# Provision the server

The RaidManager dev, test and production environments run on the OVH server that already hosts the
`pivotsoftwares.com` website and Delivery Atlas, behind their shared Caddy
([ADR-0027](../adr/0027-run-the-test-environment-on-one-ovh-vps.md)). Provisioning prepares that server for
RaidManager once, without touching the other applications. Running it again only adds what is missing.

The steps need DNS and server access, so the owner performs them. Never reinstall the server: that erases the other
applications.

## Before you start

- You sign in to the server as `ubuntu`, for example with PuTTY. A password is fine for now: the script keeps password
  sign-in on unless you ask otherwise (step 4).
- Docker, the shared `caddy` container, its Caddyfile at `/opt/apps/proxy/Caddyfile` and the Docker network `web`
  exist. If the server is ever rebuilt, set those up first, as the Delivery Atlas deployment notes describe.

## 1. Point the hostnames at the server

At the DNS provider of `pivotsoftwares.com`, add six A records with the server's IPv4 address:

| Host | Serves |
| --- | --- |
| `raidmanager-dev` | the dev website |
| `api.raidmanager-dev` | the dev API |
| `raidmanager-test` | the test website |
| `api.raidmanager-test` | the test API |
| `raidmanager` | the production website |
| `api.raidmanager` | the production API |

Add AAAA records as well only if the server's IPv6 works. Check one from your machine:

```bash
nslookup api.raidmanager-dev.pivotsoftwares.com
```

## 2. Run the provisioning script

Only the `deploy/server` folder goes to the server, not the repository. From the repository folder on your machine,
copy it with the `scp` command built into Windows; it asks for the `ubuntu` password:

```bash
scp -r deploy/server ubuntu@<server address>:raidmanager-provisioning
```

Then, signed in to the server, run it:

```bash
cd ~/raidmanager-provisioning && sudo bash provision.sh
```

The script prints each step:

1. **SSH:** `root` can't sign in over SSH (`/etc/ssh/sshd_config.d/10-raidmanager-no-root.conf`). Your own password
   sign-in doesn't change.
2. **Firewall:** OpenSSH, 80 and 443 allowed in `ufw`. Any other rule, such as 8080, is listed and left alone.
3. **Updates:** unattended security upgrades on.
4. **Swap:** a 2 GB `/swapfile`, kept across restarts.
5. **Deploy user:** `deploy`, in the `docker` group, owning `/opt/apps/raidmanager`. Its key, used by the deploy
   workflow, comes later with #386: `sudo bash provision.sh deploy_key.pub`.
6. **Proxy:** once all six hostnames resolve to the server, the RaidManager sites from `raidmanager.Caddyfile` are
   appended to the shared Caddyfile, checked with `caddy validate`, and loaded with `caddy reload`. A copy of the
   previous file is kept next to it, and put back if Caddy rejects the change. If a hostname doesn't resolve yet, the
   script says which; run it again later.

Paste the output on #374. It holds no secret.

## 3. Check from outside

From your machine, before RaidManager is deployed:

```bash
curl -sI https://raidmanager-dev.pivotsoftwares.com
```

Every website hostname answers `503` with a valid certificate and the message "not deployed yet". Each `api.`
hostname answers `404`, except under `/companion/`, where it answers `503` until the API is deployed: only the
desktop companion's routes are public. A port scan shows only 22, 80, 443 and 8080 open. Record the results on #374.

## 4. Later: sign in with a key only

Password sign-in is the main way attackers try to get into a server. Once you sign in with an SSH key, turn it off:

1. In **PuTTYgen**, choose **EdDSA** (Ed25519), select **Generate**, set a passphrase, and save the private key
   (`.ppk`) somewhere only you can read.
2. Copy the text under `Public key for pasting into OpenSSH authorized_keys file`.
3. Signed in to the server as `ubuntu`, add it on a new line of `~/.ssh/authorized_keys`:

   ```bash
   mkdir -p ~/.ssh && chmod 700 ~/.ssh && nano ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys
   ```

4. In PuTTY, under `Connection > SSH > Auth > Credentials`, choose the `.ppk` file, then save the session.
5. Open a new PuTTY session. It asks for the key's passphrase, not the account password: key sign-in works.
6. In that session, run the script again with `--keys-only`:

   ```bash
   cd ~/raidmanager-provisioning && sudo bash provision.sh --keys-only
   ```

The script refuses `--keys-only` unless `ubuntu` has signed in with a key in the last 30 days, so it can't lock you
out. Keep a session open until a fresh key sign-in works. From then on, a password sign-in is refused.
