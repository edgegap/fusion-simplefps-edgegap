#!/usr/bin/env bash
# Test-only: starts headless clients that join a server through the Party menu, waits, and prints
# what each one reports. Needs a client built with Assets/EdgegapSimpleFPSTest in the project.
# Usage: scripts/test-clients.sh <deployment fqdn> <photon region> [clients=2] [seconds=60]
set -euo pipefail
cd "$(dirname "$0")/.."

fqdn="${1:?deployment FQDN}" region="${2:?Photon region}" count="${3:-2}" wait="${4:-60}"
exe="Builds/Client/SimpleFPS.exe"
[[ -f "$exe" ]] || { echo "No client build: run scripts/build.sh client first" >&2; exit 1; }
mkdir -p Builds/test-logs

pids=()
for n in $(seq 1 "$count"); do
    "$exe" -batchmode -nographics -edgegapJoin "$fqdn" -edgegapRegion "$region" \
        -logFile "Builds/test-logs/client$n.log" >/dev/null 2>&1 &
    pids+=($!)
    sleep 5
done
sleep "$wait"
kill "${pids[@]}" 2>/dev/null || true
taskkill //IM SimpleFPS.exe //F >/dev/null 2>&1 || true

fail=0
for n in $(seq 1 "$count"); do
    log="Builds/test-logs/client$n.log"
    last="$(grep -a '^\[probe\] mode=Client' "$log" | tail -n 1)"
    exceptions="$(grep -ac 'Exception: ' "$log" || true)"
    echo "client $n: ${last:-no probe line} | exceptions=$exceptions"
    [[ "$last" == *"players=$count"* && "$exceptions" == 0 ]] || fail=1
done
exit "$fail"
