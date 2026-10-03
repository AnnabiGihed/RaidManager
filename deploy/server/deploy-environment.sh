#!/usr/bin/env bash
# Deploys one RaidManager environment on the server, as the deploy workflow's last step (ADR-0027, #389).
# Author: Gihed Annabi
#
# Usage: bash deploy-environment.sh <dev|test|prod> <deploy|rollback>
# Before "deploy", the workflow has loaded the images and written docker-compose.yaml.next and .env.next in the
# environment's folder. A failed step puts the previous version back, so the environment keeps serving.
set -euo pipefail

readonly SLUG="${1:-}"
readonly MODE="${2:-}"
readonly APP_ROOT="${RAIDMANAGER_ROOT:-/opt/apps/raidmanager}"
readonly DIR="$APP_ROOT/$SLUG"
readonly PROJECT="raidmanager-$SLUG"
readonly PROBE_IMAGE="curlimages/curl:8.11.1"
readonly READY_ATTEMPTS=30
readonly MIGRATION_ATTEMPTS=10

fail() { printf 'ERROR: %s\n' "$1" >&2; exit 1; }

compose() {
    docker compose -p "$PROJECT" --project-directory "$DIR" -f "$DIR/docker-compose.yaml" --env-file "$DIR/.env" "$@"
}

# Prints the HTTP status a service answers with on the environment's own network.
status_of() {
    docker run --rm --network "${PROJECT}_aspire" "$PROBE_IMAGE" -s -o /dev/null -m 5 -w '%{http_code}' "$1" || true
}

wait_until_ready() {
    local attempt api web
    for attempt in $(seq 1 "$READY_ATTEMPTS"); do
        api="$(status_of http://api:8080/)"
        web="$(status_of http://web:8080/)"
        if [[ $api == 200 && ($web == 200 || $web == 302) ]]; then
            echo "The API answers $api and the website $web"
            return 0
        fi
        echo "Waiting for the new version ($attempt/$READY_ATTEMPTS): API $api, website $web"
        sleep 2
    done
    return 1
}

migrate() {
    local attempt
    for attempt in $(seq 1 "$MIGRATION_ATTEMPTS"); do
        # The new API image applies the migrations and exits; the running version keeps serving meanwhile.
        if compose run --rm --no-deps api --Database:MigrateAndExit=true; then
            return 0
        fi
        echo "The migration step failed ($attempt/$MIGRATION_ATTEMPTS); the database may still be starting, or the run failed above"
        sleep 3
    done
    return 1
}

restore_previous() {
    if [[ -f $DIR/.env.previous && -f $DIR/docker-compose.yaml.previous ]]; then
        mv -f "$DIR/.env.previous" "$DIR/.env"
        mv -f "$DIR/docker-compose.yaml.previous" "$DIR/docker-compose.yaml"
        compose up -d --remove-orphans
        echo "The previous version is back"
    else
        compose down
        echo "The first deployment failed and is stopped; there was no previous version to keep"
    fi
}

# Removes the RaidManager images that no environment's current or previous version uses, so the disk doesn't fill.
remove_unused_images() {
    local referenced image
    local -a files
    # Only the files that exist: a first deployment has no .env.previous, and grep would fail on it.
    shopt -s nullglob
    files=("$APP_ROOT"/*/.env "$APP_ROOT"/*/.env.previous)
    shopt -u nullglob
    referenced="$(grep -h -E '^[A-Z_]+_IMAGE=' "${files[@]}" | cut -d= -f2- | sort -u || true)"
    while read -r image; do
        if ! grep -qxF "$image" <<<"$referenced"; then
            docker rmi "$image" >/dev/null && echo "Removed the unused image $image"
        fi
    done < <(docker images --format '{{.Repository}}:{{.Tag}}' | grep -E '^raidmanager-(api|web|discord-bot):' || true)
}

deploy() {
    [[ -f $DIR/.env.next && -f $DIR/docker-compose.yaml.next ]] || fail "the workflow hasn't copied the new files to $DIR"
    if [[ -f $DIR/.env ]]; then
        cp -p "$DIR/.env" "$DIR/.env.previous"
        cp -p "$DIR/docker-compose.yaml" "$DIR/docker-compose.yaml.previous"
    else
        rm -f "$DIR/.env.previous" "$DIR/docker-compose.yaml.previous"
    fi
    mv -f "$DIR/.env.next" "$DIR/.env"
    mv -f "$DIR/docker-compose.yaml.next" "$DIR/docker-compose.yaml"

    compose up -d postgres
    if ! migrate; then
        echo "The migrations failed" >&2
        restore_previous
        exit 1
    fi
    compose up -d --remove-orphans
    if ! wait_until_ready; then
        echo "The new version didn't answer" >&2
        compose logs --tail 50 api web >&2 || true
        restore_previous
        exit 1
    fi
    remove_unused_images
    echo "Deployed $PROJECT"
}

[[ $SLUG =~ ^(dev|test|prod)$ ]] || fail "unknown environment '$SLUG'; use dev, test or prod"
case $MODE in
    deploy) deploy ;;
    rollback) restore_previous ;;
    *) fail "unknown mode '$MODE'; use deploy or rollback" ;;
esac
