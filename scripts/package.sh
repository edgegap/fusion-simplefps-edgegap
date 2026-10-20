#!/usr/bin/env bash
# Builds the server image for linux/amd64, and optionally tags and pushes it.
# Usage: scripts/package.sh                                  -> simplefps-edgegap:local
#        scripts/package.sh registry.edgegap.com/<project>/simplefps <tag>   (docker login first)
set -euo pipefail
cd "$(dirname "$0")/.."

grep -qx 'result=Succeeded' Builds/EdgegapServer/edgegap-build.txt 2>/dev/null \
    && grep -qx 'subtarget=Server' Builds/EdgegapServer/edgegap-build.txt \
    || { echo "No successful server build: run scripts/build.sh server (see Builds/server.log)" >&2; exit 1; }

docker build --platform linux/amd64 -f edgegap/Dockerfile -t simplefps-edgegap:local .

if [[ $# -ge 1 ]]; then
    [[ $# -eq 2 && -n "$2" && "$2" != latest ]] || { echo "Pass a new tag (not 'latest') after the image" >&2; exit 2; }
    docker tag simplefps-edgegap:local "$1:$2"
    docker push "$1:$2"
fi
