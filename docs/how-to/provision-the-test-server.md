# Provision the test server

The RaidManager test environment runs on the OVH server that already hosts the `pivotsoftwares.com` website and
Delivery Atlas, behind their shared Caddy ([ADR-0027](../adr/0027-run-the-test-environment-on-one-ovh-vps.md)).
Provisioning prepares that server for RaidManager without touching the other applications. Run it once; running it
again only adds what is missing.

The steps need DNS and server access, so the owner performs them. Never reinstall the server: that erases the other
applications.

## Before you start

- You sign in to the server as `ubuntu` with an SSH key. The script refuses to turn off password sign-in otherwise.
- Docker, the shared `caddy` container, its Caddyfile at `/opt/apps/proxy/Caddyfile` and the Docker network `web`
  exist. If the server is ever rebuilt, set those up first, as the Delivery Atlas deployment notes describe.

## 1. Point the hostnames at the server

At the DNS provider of `pivotsoftwares.com`, add two A records with the server's IPv4 address:

| Name | Serves |
| --- | --- |
| `raidmanager-test` | the website |
| `api.raidmanager-test` | the API |

Add AAAA records as well only if the server's IPv6 works. Check them from your machine:

```bash
nslookup raidmanager-test.pivotsoftwares.com
```

## 2. Run the provisioning script

From the repository root on your machine, copy the folder to the server, then run the script there:

```bash
scp -r deploy/test-server ubuntu@<server address>:~/raidmanager-provisioning
```

```bash
cd ~/raidmanager-provisioning && sudo bash provision.sh
```

The script prints each step:

1. **SSH:** password and keyboard-interactive sign-in off, `root` sign-in off, through
   `/etc/ssh/sshd_config.d/10-raidmanager.conf`. Your open session stays connected.
2. **Firewall:** OpenSSH, 80 and 443 allowed in `ufw`. Any other rule, such as 8080, is listed and left alone.
3. **Updates:** unattended security upgrades on.
4. **Swap:** a 2 GB `/swapfile`, kept across restarts.
5. **Deploy user:** `deploy`, in the `docker` group, owning `/opt/apps/raidmanager`. To install the deploy workflow's
   public key now, pass its file: `sudo bash provision.sh deploy_key.pub`; otherwise #386 installs it later.
6. **Proxy:** once both hostnames resolve to the server, the two RaidManager sites from `raidmanager.Caddyfile` are
   appended to the shared Caddyfile, checked with `caddy validate`, and loaded with `caddy reload`. A copy of the
   previous file is kept next to it, and put back if Caddy rejects the change. If DNS isn't ready, the script says so;
   run it again later.

Paste the output on #374. It holds no secret.

## 3. Check from outside

From your machine, before RaidManager is deployed:

```bash
curl -sI https://raidmanager-test.pivotsoftwares.com
```

Both hostnames answer `503` with a valid certificate and the message "not deployed yet". Also check that:

- `ssh -o PubkeyAuthentication=no ubuntu@<server address>` is refused;
- a port scan shows only 22, 80, 443 and 8080 open.

Record the results on #374.
