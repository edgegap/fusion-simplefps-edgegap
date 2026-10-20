#!/usr/bin/env bash
# Sets up the project and builds with the Unity CLI. The editor must be closed on the project.
# Usage: scripts/build.sh setup|server|client [path/to/Unity]   (or set UNITY)
#   setup   apply the Edgegap changes to Deathmatch and Startup
#   server  check, set up and build the Linux IL2CPP server -> Builds/EdgegapServer/ServerBuild
#   client  set up and build the Windows IL2CPP client      -> Builds/Client/SimpleFPS.exe
# Run from the Simple FPS project root, with the kit copied in.
set -euo pipefail
cd "$(dirname "$0")/.."

what="${1:-}"
case "$what" in
    setup)  method=SetUpFromCommandLine ;;
    server) method=BuildServerFromCommandLine ;;
    client) method=BuildClientFromCommandLine ;;
    *) echo "usage: scripts/build.sh setup|server|client [path/to/Unity]" >&2; exit 2 ;;
esac

version="$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt | tr -d '\r')"
unity="${2:-${UNITY:-}}"
if [[ -z "$unity" ]]; then
    for candidate in \
        "/c/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe" \
        "/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity" \
        "$HOME/Unity/Hub/Editor/$version/Editor/Unity"; do
        [[ -x "$candidate" ]] && unity="$candidate" && break
    done
fi
[[ -n "$unity" ]] || { echo "Unity $version not found; pass its path or set UNITY" >&2; exit 1; }

mkdir -p Builds
log="Builds/$what.log"
echo "Unity $version: $what (log in $log)"
run() {
    status=0
    "$unity" -batchmode -nographics -quit -projectPath "$PWD" \
        -executeMethod "Edgegap.SimpleFPS.Editor.SimpleFpsEdgegapSetup.$method" \
        -logFile "$log" || status=$?
}
run
# Unity 6000.3 on Windows sometimes fails to find the Linux sysroot even with the SDK package
# installed; the same build then succeeds on the next run.
if [[ "$status" != 0 && "$what" == server ]] && grep -q 'No Linux sysroot found' "$log"; then
    echo "Linux sysroot not found (intermittent); building again"
    run
fi
grep -a '\[Edgegap Simple FPS\]' "$log" | sort -u || true
grep -a 'error CS' "$log" | sort -u || true
exit "$status"
