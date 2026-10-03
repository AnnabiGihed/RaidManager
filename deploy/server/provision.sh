#!/usr/bin/env bash
# Prepares the shared OVH server for the RaidManager dev, test and production environments (ADR-0027).
# Author: Gihed Annabi
#
# Run it from your admin account: sudo bash provision.sh [--keys-only] [deploy-public-key-file]
#   --keys-only  also turns off SSH password sign-in. Use it only after you have signed in with your SSH key.
# It only adds what is missing, so it is safe to run again. It never reinstalls anything, resets the firewall, or
# stops the applications already on the server.
set -euo pipefail

readonly APP_DIR=/opt/apps/raidmanager
readonly DEPLOY_USER=deploy
readonly PROXY_CONTAINER=caddy
readonly PROXY_CADDYFILE=/opt/apps/proxy/Caddyfile
readonly PROXY_CONFIG_IN_CONTAINER=/etc/caddy/Caddyfile
readonly SHARED_NETWORK=web
readonly SWAP_FILE=/swapfile
readonly SWAP_SIZE=2G
readonly SSHD_NO_ROOT=/etc/ssh/sshd_config.d/10-raidmanager-no-root.conf
readonly SSHD_KEYS_ONLY=/etc/ssh/sshd_config.d/11-raidmanager-keys-only.conf
readonly SITE_MARKER="# RaidManager environments (ADR-0027)"
readonly HOSTNAMES=(
    raidmanager-dev.pivotsoftwares.com api.raidmanager-dev.pivotsoftwares.com
    raidmanager-test.pivotsoftwares.com api.raidmanager-test.pivotsoftwares.com
    raidmanager.pivotsoftwares.com api.raidmanager.pivotsoftwares.com
)
readonly OPEN_PORTS=(OpenSSH 80/tcp 443/tcp)

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_DIR
readonly SITES_FILE="$SCRIPT_DIR/raidmanager.Caddyfile"

KEYS_ONLY=false
DEPLOY_KEY_FILE=""
ADMIN=""

step() { printf '\n== %s\n' "$1"; }
fail() { printf 'ERROR: %s\n' "$1" >&2; exit 1; }

parse_arguments() {
    local argument
    for argument in "$@"; do
        case $argument in
            --keys-only) KEYS_ONLY=true ;;
            -*) fail "unknown option $argument; usage: sudo bash provision.sh [--keys-only] [deploy-public-key-file]" ;;
            *) DEPLOY_KEY_FILE=$argument ;;
        esac
    done
}

check_preconditions() {
    step "Checking the server"
    [[ $EUID -eq 0 ]] || fail "run it with sudo from your admin account"
    # shellcheck source=/dev/null
    . /etc/os-release
    [[ $ID == ubuntu ]] || fail "expected Ubuntu, found $ID"

    ADMIN="${SUDO_USER:-}"
    [[ -n $ADMIN && $ADMIN != root ]] || fail "run it with sudo from your admin account, not as root"

    command -v docker >/dev/null || fail "Docker is missing; install Docker Engine and the Compose plugin first"
    docker compose version >/dev/null || fail "the Docker Compose plugin is missing"
    docker network inspect "$SHARED_NETWORK" >/dev/null || fail "the shared Docker network '$SHARED_NETWORK' is missing"
    docker inspect "$PROXY_CONTAINER" >/dev/null || fail "the shared proxy container '$PROXY_CONTAINER' isn't running"
    [[ -f $PROXY_CADDYFILE ]] || fail "the shared proxy's Caddyfile isn't at $PROXY_CADDYFILE"
    [[ -f $SITES_FILE ]] || fail "copy raidmanager.Caddyfile next to this script"
    echo "Ubuntu $VERSION_ID, admin account $ADMIN, Docker and the shared proxy found"
}

signed_in_with_key() {
    # A key in authorized_keys isn't enough (another tool may have put it there): require a real key sign-in.
    journalctl --quiet --since "-30 days" _COMM=sshd 2>/dev/null | grep -q "Accepted publickey for $ADMIN from" ||
        grep -qs "Accepted publickey for $ADMIN from" /var/log/auth.log
}

apply_sshd_setting() {
    local file="$1" content="$2"
    printf '%s\n' "$content" >"$file"
    # Ubuntu 24.04 starts SSH through a socket, so the directory sshd -t needs may not exist yet.
    install -d -m 755 /run/sshd
    sshd -t || { rm -f "$file"; fail "sshd rejected $file; it is removed and nothing changed"; }
}

harden_ssh() {
    step "SSH: no root sign-in"
    apply_sshd_setting "$SSHD_NO_ROOT" "# RaidManager (ADR-0027): no root sign-in over SSH.
PermitRootLogin no"
    if [[ $KEYS_ONLY == true ]]; then
        signed_in_with_key ||
            fail "$ADMIN has never signed in with an SSH key in the last 30 days; set up your key and sign in with it first"
        apply_sshd_setting "$SSHD_KEYS_ONLY" "# RaidManager (ADR-0027): keys only.
PasswordAuthentication no
KbdInteractiveAuthentication no"
    elif [[ ! -f $SSHD_KEYS_ONLY ]]; then
        echo "Password sign-in stays on. Once you sign in with your SSH key, run this script again with --keys-only."
    fi
    systemctl try-reload-or-restart ssh
    sshd -T | grep -Ei '^(passwordauthentication|kbdinteractiveauthentication|permitrootlogin) '
}

configure_firewall() {
    step "Firewall: 22, 80 and 443 allowed"
    local port
    for port in "${OPEN_PORTS[@]}"; do
        ufw allow "$port" >/dev/null
    done
    if ! ufw status | grep -q '^Status: active'; then
        ufw --force enable
    fi
    local extra
    extra="$(ufw status | grep 'ALLOW' | grep -Ev '^(22|80|443)/tcp' || true)"
    if [[ -n $extra ]]; then
        echo "Other rules are open; they aren't RaidManager's, so they're left as they are:"
        echo "$extra"
    fi
}

enable_security_upgrades() {
    step "Unattended security upgrades"
    DEBIAN_FRONTEND=noninteractive apt-get install -y -qq unattended-upgrades >/dev/null
    cat >/etc/apt/apt.conf.d/20auto-upgrades <<'CONF'
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Unattended-Upgrade "1";
CONF
    systemctl enable --now unattended-upgrades >/dev/null
    systemctl is-active unattended-upgrades
}

add_swap() {
    step "Swap file of $SWAP_SIZE"
    if swapon --show=NAME --noheadings | grep -qx "$SWAP_FILE"; then
        echo "Already on"
    else
        [[ -f $SWAP_FILE ]] || fallocate -l "$SWAP_SIZE" "$SWAP_FILE"
        chmod 600 "$SWAP_FILE"
        mkswap "$SWAP_FILE" >/dev/null
        swapon "$SWAP_FILE"
    fi
    grep -q "^$SWAP_FILE " /etc/fstab || echo "$SWAP_FILE none swap sw 0 0" >>/etc/fstab
    swapon --show
}

create_deploy_user() {
    step "The $DEPLOY_USER user and $APP_DIR"
    id "$DEPLOY_USER" >/dev/null 2>&1 || useradd --create-home --shell /bin/bash "$DEPLOY_USER"
    usermod -aG docker "$DEPLOY_USER"
    install -d -o "$DEPLOY_USER" -g "$DEPLOY_USER" -m 700 "/home/$DEPLOY_USER/.ssh"
    install -d -o "$DEPLOY_USER" -g "$DEPLOY_USER" -m 750 "$APP_DIR"

    local key_file="$DEPLOY_KEY_FILE"
    if [[ -n $key_file ]]; then
        [[ -f $key_file ]] || fail "public key file $key_file not found"
        local keys="/home/$DEPLOY_USER/.ssh/authorized_keys"
        local key
        key="$(head -n 1 "$key_file")"
        [[ $key == ssh-ed25519\ * ]] || fail "$key_file isn't an Ed25519 public key"
        touch "$keys"
        grep -qF "$key" "$keys" || echo "restrict $key" >>"$keys"
        chown "$DEPLOY_USER:$DEPLOY_USER" "$keys"
        chmod 600 "$keys"
        echo "Deploy key installed with 'restrict' (ADR-0028)"
    else
        echo "No deploy key given; #386 installs it later"
    fi
    id "$DEPLOY_USER"
    ls -ld "$APP_DIR"
}

points_here() {
    local name="$1" address
    for address in $(getent ahostsv4 "$name" | awk '{print $1}' | sort -u); do
        hostname -I | tr ' ' '\n' | grep -qx "$address" && return 0
    done
    return 1
}

add_proxy_sites() {
    step "The RaidManager sites in the shared proxy"
    if grep -qF "$SITE_MARKER" "$PROXY_CADDYFILE"; then
        echo "Already in $PROXY_CADDYFILE"
        return
    fi
    local name
    for name in "${HOSTNAMES[@]}"; do
        if ! points_here "$name"; then
            echo "$name doesn't resolve to this server yet; add its DNS A record, then run this script again."
            return
        fi
    done

    local backup
    backup="$PROXY_CADDYFILE.$(date +%Y%m%d%H%M%S).bak"
    cp -p "$PROXY_CADDYFILE" "$backup"
    # Append in place: the proxy mounts this one file, and replacing it would hide the change from the container.
    printf '\n' >>"$PROXY_CADDYFILE"
    cat "$SITES_FILE" >>"$PROXY_CADDYFILE"
    if ! docker exec "$PROXY_CONTAINER" caddy validate --config "$PROXY_CONFIG_IN_CONTAINER" --adapter caddyfile; then
        cat "$backup" >"$PROXY_CADDYFILE"
        fail "Caddy rejected the new sites; the Caddyfile is restored from $backup"
    fi
    docker exec "$PROXY_CONTAINER" caddy reload --config "$PROXY_CONFIG_IN_CONTAINER" --adapter caddyfile
    echo "Sites added and the proxy reloaded; the previous Caddyfile is $backup"
}

main() {
    parse_arguments "$@"
    check_preconditions
    harden_ssh
    configure_firewall
    enable_security_upgrades
    add_swap
    create_deploy_user
    add_proxy_sites
    step "Done"
}

main "$@"
