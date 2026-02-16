#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  scripts/probe-local.sh --all
  scripts/probe-local.sh --server <ServerDir>

Options:
  --all                 Run against all servers under src/Servers/*/probe.json
  --server <ServerDir>  Run against a single server directory (e.g. NginxServer)
  --port <Port>         Target port (default: 8080)
  --skip-build          Skip 'dotnet build' (assumes Release build already exists)
  --verbose             Pass --verbose to the CLI (still writes JSON output)
  --docker-sudo         Run docker commands via sudo (lets you run the script without sudo)
  -h, --help            Show help

This mirrors the GitHub Actions workflow in .github/workflows/probe.yml and produces:
  - probe-<ServerDir>.json (one per server)
  - probe-data.js (window.PROBE_DATA = ...)
  - docs/static/probe/data.js (copied from probe-data.js for local Hugo rendering)

Environment:
  DOTNET=/path/to/dotnet   Override which dotnet binary to use (useful if PATH points to an older SDK).
EOF
}

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
}

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REPO_USER=""
if [[ "$(id -u)" -eq 0 && -n "${SUDO_USER:-}" && "${SUDO_USER:-}" != "root" ]]; then
  # If invoked via sudo, run dotnet + file writes as the original user to avoid picking up
  # a different (often older) dotnet install, and to keep generated files user-owned.
  REPO_USER="$SUDO_USER"
fi

run_as_repo_user() {
  if [[ -n "$REPO_USER" ]]; then
    sudo -u "$REPO_USER" -H "$@"
  else
    "$@"
  fi
}

MODE=""
SINGLE_SERVER=""
PROBE_PORT=8080
SKIP_BUILD=0
VERBOSE=0
DOCKER_SUDO=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --all)
      MODE="all"
      shift
      ;;
    --server)
      MODE="single"
      SINGLE_SERVER="${2:-}"
      shift 2
      ;;
    --port)
      PROBE_PORT="${2:-}"
      shift 2
      ;;
    --skip-build)
      SKIP_BUILD=1
      shift
      ;;
    --verbose)
      VERBOSE=1
      shift
      ;;
    --docker-sudo)
      DOCKER_SUDO=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 2
      ;;
  esac
done

if [[ -z "$MODE" ]]; then
  echo "Must specify either --all or --server <ServerDir>." >&2
  usage
  exit 2
fi

require_cmd jq
require_cmd docker
require_cmd base64
require_cmd curl
require_cmd python3

docker_cmd() {
  if [[ "$(id -u)" -eq 0 ]]; then
    docker "$@"
  elif [[ "$DOCKER_SUDO" -eq 1 ]]; then
    sudo docker "$@"
  else
    docker "$@"
  fi
}

DOTNET_BIN=""
if [[ -n "${DOTNET:-}" ]]; then
  DOTNET_BIN="$DOTNET"
elif [[ -n "$REPO_USER" ]]; then
  require_cmd sudo
  DOTNET_BIN="$(sudo -u "$REPO_USER" -H bash -lc 'source ~/.profile >/dev/null 2>&1 || true; source ~/.bashrc >/dev/null 2>&1 || true; command -v dotnet' 2>/dev/null || true)"
fi
if [[ -z "$DOTNET_BIN" ]]; then
  DOTNET_BIN="$(command -v dotnet 2>/dev/null || true)"
fi
if [[ -z "$DOTNET_BIN" ]]; then
  echo "Missing required command: dotnet" >&2
  exit 1
fi
if [[ ! -x "$DOTNET_BIN" ]]; then
  echo "DOTNET='$DOTNET_BIN' is not executable." >&2
  exit 1
fi

dotnet_cmd() {
  if [[ -n "$REPO_USER" ]]; then
    sudo -u "$REPO_USER" -H "$DOTNET_BIN" "$@"
  else
    "$DOTNET_BIN" "$@"
  fi
}

DOTNET_VERSION="$(dotnet_cmd --version 2>/dev/null || true)"
if [[ -z "$DOTNET_VERSION" ]]; then
  echo "dotnet is installed but not usable." >&2
  exit 1
fi
DOTNET_MAJOR="${DOTNET_VERSION%%.*}"
if [[ "$DOTNET_MAJOR" -lt 10 ]]; then
  echo "dotnet $DOTNET_VERSION detected; Http11Probe expects dotnet 10.x." >&2
  if [[ -n "$REPO_USER" ]]; then
    echo "Hint: this script is running dotnet as '$REPO_USER' (from sudo). Install/activate dotnet 10 for that user." >&2
  else
    echo "Hint: run 'dotnet --version' and ensure it reports 10.x." >&2
  fi
  exit 1
fi

if [[ "$DOCKER_SUDO" -eq 1 && "$(id -u)" -ne 0 ]]; then
  require_cmd sudo
fi

if ! docker_cmd info >/dev/null 2>&1; then
  echo "Docker is installed but not usable (cannot connect to the Docker daemon)." >&2
  echo "Fix: ensure you can run 'docker ps' (or re-run with --docker-sudo), and that the daemon is running." >&2
  exit 1
fi

cleanup() {
  docker_cmd rm -f probe-target >/dev/null 2>&1 || true
}
trap cleanup EXIT

if [[ "$SKIP_BUILD" -eq 0 ]]; then
  dotnet_cmd build src/Http11Probe.Cli/Http11Probe.Cli.csproj -c Release
fi

SERVERS='[]'

if [[ "$MODE" == "single" ]]; then
  if [[ -z "$SINGLE_SERVER" ]]; then
    echo "--server requires a value (e.g. NginxServer)." >&2
    exit 2
  fi
  if [[ ! -f "src/Servers/$SINGLE_SERVER/probe.json" ]]; then
    echo "Server '$SINGLE_SERVER' not found (expected src/Servers/$SINGLE_SERVER/probe.json)." >&2
    echo "Available servers:" >&2
    ls src/Servers >&2
    exit 1
  fi
  name="$(jq -r .name "src/Servers/$SINGLE_SERVER/probe.json")"
  lang="$(jq -r '.language // ""' "src/Servers/$SINGLE_SERVER/probe.json")"
  SERVERS="$(jq -c -n --arg d "$SINGLE_SERVER" --arg n "$name" --arg l "$lang" '[{"dir":$d,"name":$n,"language":$l}]')"
else
  for f in src/Servers/*/probe.json; do
    dir="$(basename "$(dirname "$f")")"
    name="$(jq -r .name "$f")"
    lang="$(jq -r '.language // ""' "$f")"
    SERVERS="$(echo "$SERVERS" | jq -c --arg d "$dir" --arg n "$name" --arg l "$lang" '. + [{"dir": $d, "name": $n, "language": $l}]')"
  done
fi

echo "Servers:"
echo "$SERVERS" | jq -r '.[].name' | sed 's/^/  - /'
echo

for row in $(echo "$SERVERS" | jq -r '.[] | @base64'); do
  dir="$(echo "$row" | base64 -d | jq -r '.dir')"
  name="$(echo "$row" | base64 -d | jq -r '.name')"
  tag="$(echo "probe-$dir" | tr '[:upper:]' '[:lower:]')"

  echo "== $name =="

  docker_cmd build -t "$tag" -f "src/Servers/$dir/Dockerfile" .

  cleanup
  docker_cmd run -d --name probe-target --network host "$tag" >/dev/null

  # Wait up to 30s for server readiness.
  ready=0
  for _ in $(seq 1 30); do
    if curl -sf "http://localhost:${PROBE_PORT}/" >/dev/null 2>&1; then
      ready=1
      break
    fi
    sleep 1
  done
  if [[ "$ready" -ne 1 ]]; then
    echo "  WARN: server did not respond on http://localhost:${PROBE_PORT}/ after 30s" >&2
  fi

  cli_args=(--host localhost --port "$PROBE_PORT" --output "probe-${dir}.json")
  if [[ "$VERBOSE" -eq 1 ]]; then
    cli_args+=(--verbose)
  fi

  dotnet_cmd run --no-build -c Release --project src/Http11Probe.Cli -- "${cli_args[@]}" || true

  cleanup
  echo
done

if [[ -n "$REPO_USER" ]]; then
  sudo -u "$REPO_USER" -H env PROBE_SERVERS="$SERVERS" python3 <<'PYEOF'
import json, sys, os, subprocess, pathlib

def evaluate(raw):
    results = []
    for r in raw['results']:
        status = r.get('statusCode')
        conn   = r.get('connectionState', '')
        got = str(status) if status is not None else conn
        expected = r.get('expected', '?')
        verdict = r['verdict']
        scored = r.get('scored', True)
        reason = r['description']
        if verdict == 'Fail':
            reason = f"Expected {expected}, got {got} — {reason}"

        results.append({
            'id': r['id'], 'description': r['description'],
            'category': r['category'], 'rfc': r.get('rfcReference'),
            'verdict': verdict, 'statusCode': status,
            'expected': expected, 'got': got,
            'connectionState': conn, 'reason': reason,
            'scored': scored,
            'rfcLevel': r.get('rfcLevel', 'Must'),
            'durationMs': r.get('durationMs', 0),
            'rawRequest': r.get('rawRequest'),
            'rawResponse': r.get('rawResponse'),
            'behavioralNote': r.get('behavioralNote'),
            'doubleFlush': r.get('doubleFlush'),
        })

    scored_results = [r for r in results if r['scored']]
    scored_pass = sum(1 for r in scored_results if r['verdict'] == 'Pass')
    scored_fail = sum(1 for r in scored_results if r['verdict'] == 'Fail')
    scored_warn = sum(1 for r in scored_results if r['verdict'] == 'Warn')
    unscored = sum(1 for r in results if not r['scored'])
    return {
        'summary': {'total': len(results), 'scored': len(scored_results), 'passed': scored_pass, 'failed': scored_fail, 'warnings': scored_warn, 'unscored': unscored},
        'results': results,
    }

servers_config = json.loads(os.environ['PROBE_SERVERS'])
SERVERS = [(s['name'], f"probe-{s['dir']}.json", s.get('language', '')) for s in servers_config]

commit_id   = subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip()
commit_msg  = subprocess.check_output(['git', 'log', '-1', '--format=%s']).decode().strip()
commit_time = subprocess.check_output(['git', 'log', '-1', '--format=%cI']).decode().strip()

server_data = []
for name, path, language in SERVERS:
    p = pathlib.Path(path)
    if not p.exists():
        print(f'::warning::{name}: result file {path} not found, skipping')
        continue
    with open(path) as f:
        raw = json.load(f)
    ev = evaluate(raw)
    ev['name'] = name
    ev['language'] = language
    server_data.append(ev)
    s = ev['summary']
    print(f"{name}: {s['passed']}/{s['scored']} passed, {s['failed']} failed, {s['warnings']} warnings")

if not server_data:
    print('::warning::No probe results found — nothing to report')
    sys.exit(0)

output = {
    'commit': {'id': commit_id, 'message': commit_msg, 'timestamp': commit_time},
    'servers': server_data,
}
with open('probe-data.js', 'w') as f:
    f.write('window.PROBE_DATA = ' + json.dumps(output) + ';')
PYEOF
else
  env PROBE_SERVERS="$SERVERS" python3 <<'PYEOF'
import json, sys, os, subprocess, pathlib

def evaluate(raw):
    results = []
    for r in raw['results']:
        status = r.get('statusCode')
        conn   = r.get('connectionState', '')
        got = str(status) if status is not None else conn
        expected = r.get('expected', '?')
        verdict = r['verdict']
        scored = r.get('scored', True)
        reason = r['description']
        if verdict == 'Fail':
            reason = f"Expected {expected}, got {got} — {reason}"

        results.append({
            'id': r['id'], 'description': r['description'],
            'category': r['category'], 'rfc': r.get('rfcReference'),
            'verdict': verdict, 'statusCode': status,
            'expected': expected, 'got': got,
            'connectionState': conn, 'reason': reason,
            'scored': scored,
            'rfcLevel': r.get('rfcLevel', 'Must'),
            'durationMs': r.get('durationMs', 0),
            'rawRequest': r.get('rawRequest'),
            'rawResponse': r.get('rawResponse'),
            'behavioralNote': r.get('behavioralNote'),
            'doubleFlush': r.get('doubleFlush'),
        })

    scored_results = [r for r in results if r['scored']]
    scored_pass = sum(1 for r in scored_results if r['verdict'] == 'Pass')
    scored_fail = sum(1 for r in scored_results if r['verdict'] == 'Fail')
    scored_warn = sum(1 for r in scored_results if r['verdict'] == 'Warn')
    unscored = sum(1 for r in results if not r['scored'])
    return {
        'summary': {'total': len(results), 'scored': len(scored_results), 'passed': scored_pass, 'failed': scored_fail, 'warnings': scored_warn, 'unscored': unscored},
        'results': results,
    }

servers_config = json.loads(os.environ['PROBE_SERVERS'])
SERVERS = [(s['name'], f"probe-{s['dir']}.json", s.get('language', '')) for s in servers_config]

commit_id   = subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip()
commit_msg  = subprocess.check_output(['git', 'log', '-1', '--format=%s']).decode().strip()
commit_time = subprocess.check_output(['git', 'log', '-1', '--format=%cI']).decode().strip()

server_data = []
for name, path, language in SERVERS:
    p = pathlib.Path(path)
    if not p.exists():
        print(f'::warning::{name}: result file {path} not found, skipping')
        continue
    with open(path) as f:
        raw = json.load(f)
    ev = evaluate(raw)
    ev['name'] = name
    ev['language'] = language
    server_data.append(ev)
    s = ev['summary']
    print(f"{name}: {s['passed']}/{s['scored']} passed, {s['failed']} failed, {s['warnings']} warnings")

if not server_data:
    print('::warning::No probe results found — nothing to report')
    sys.exit(0)

output = {
    'commit': {'id': commit_id, 'message': commit_msg, 'timestamp': commit_time},
    'servers': server_data,
}
with open('probe-data.js', 'w') as f:
    f.write('window.PROBE_DATA = ' + json.dumps(output) + ';')
PYEOF
fi

run_as_repo_user mkdir -p docs/static/probe
run_as_repo_user cp probe-data.js docs/static/probe/data.js

echo "Wrote:"
echo "  - probe-data.js"
echo "  - docs/static/probe/data.js"
