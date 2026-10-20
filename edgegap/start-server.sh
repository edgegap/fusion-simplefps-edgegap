#!/usr/bin/env bash
# Starts the Simple FPS dedicated server in an Edgegap deployment.
# EdgegapFusionBootstrap (Deathmatch scene) starts it as a Fusion server on 7777/UDP and
# names the session <ARBITRIUM_REQUEST_ID>.pr.edgegap.net.
set -uo pipefail

log() { printf '[edgegap-simplefps] %s\n' "$*"; }

# Never print the environment: ARBITRIUM_DELETE_TOKEN would end up in the logs.
log "deployment=${ARBITRIUM_REQUEST_ID:-local} public=${ARBITRIUM_PUBLIC_IP:-?}:${ARBITRIUM_PORT_GAMEPORT_EXTERNAL:-?} internal=7777/udp"

/srv/simplefps/ServerBuild -batchmode -nographics -logFile - "$@" &
child=$!
stopping=false
trap 'stopping=true; kill -TERM "$child" 2>/dev/null' TERM INT

while :; do
    wait "$child"; status=$?
    kill -0 "$child" 2>/dev/null || break
done
log "server exited with code $status"

# When the server ends on its own, stop the deployment so it doesn't keep running.
if [[ "$stopping" == false && -n "${ARBITRIUM_DELETE_URL:-}" && -n "${ARBITRIUM_DELETE_TOKEN:-}" ]]; then
    curl -fsS -X DELETE -H "Authorization: $ARBITRIUM_DELETE_TOKEN" "$ARBITRIUM_DELETE_URL" >/dev/null \
        && log "asked Edgegap to stop this deployment" || log "self-stop request failed"
fi
exit "$status"
