#!/usr/bin/env bash
# Runs the server image locally with the variables Edgegap would inject, and follows its log.
# Usage: scripts/run-local.sh [photon-region]
# Join with session f68e011bfb01.pr.edgegap.net and the region printed in the
# "[edgegap-simplefps] server started" line. Local clients show CT Relayed (Docker Desktop NAT); on Edgegap it's Direct.
set -euo pipefail

docker rm -f simplefps-local >/dev/null 2>&1 || true
args=(-d --name simplefps-local -p 7777:7777/udp
      -e ARBITRIUM_REQUEST_ID=f68e011bfb01
      -e ARBITRIUM_PORT_GAMEPORT_INTERNAL=7777)
[[ -z "${1:-}" ]] || args+=(-e "PHOTON_REGION=$1")
docker run "${args[@]}" simplefps-edgegap:local >/dev/null
echo "Started container simplefps-local. Ctrl+C stops following the log; docker rm -f simplefps-local stops the server."
docker logs -f simplefps-local 2>&1 | grep --line-buffered -aE '^\[edgegap-simplefps\]|^\[probe\]|Exception'
