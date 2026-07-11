# Http11Probe

Http11Probe is an HTTP/1.1 compliance and security tester. It sends malformed, ambiguous, and oversized requests over raw TCP sockets and checks each response against the normative requirements of RFC 9110 and RFC 9112 — the edge cases (bare LF, obs-fold, CL/TE request smuggling, chunk-framing abuse, oversized headers, NUL bytes) that separate a strict parser from a lenient one.

The same suite of 215 tests runs against 41 reference servers spanning 12 languages — from Nginx, Apache, and Envoy to Kestrel, Gin, Actix, and the built-in servers of Node, Bun, and Deno. Each result is scored against the RFC's MUST/SHOULD/MAY language as **Pass**, **Fail**, or **Warn** (Warn where the spec permits both strict and lenient behaviour).

Full documentation, the per-test glossary with RFC citations, and the live results matrix across every server live at **[http-probe.com](https://www.http-probe.com/)**.

## Usage

The probe is **target-agnostic** — it tests whatever HTTP/1.1 server is already listening on `--host:--port`. There's no flag to select a framework; start the server first (or use [`probe-local.sh`](#probing-the-bundled-servers-locally) to spin one up for you), then point the probe at it.

```
dotnet run --project src/Http11Probe.Cli -- --host localhost --port 8080
```

### Options

| Flag | Description | Default |
|------|-------------|---------|
| `--host` | Target hostname or IP address | `localhost` |
| `--port` | Target port number | `8080` |
| `--category` | Run only tests in this category (`Compliance`, `Smuggling`, `MalformedInput`, `Normalization`, `Cookies`, `Capabilities`) | all |
| `--test` | Run only specific test IDs, case-insensitive (repeatable) | all |
| `--timeout` | Connect and read timeout in seconds per test | `5` |
| `--output` | Write JSON results to file | — |
| `--verbose`, `-v` | Print the raw server response for each test | off |

### Examples

```
dotnet run --project src/Http11Probe.Cli -- --host localhost --port 8080 --output results.json
```

Run specific tests:

```
dotnet run --project src/Http11Probe.Cli -- --test SMUG-CL-TE-BOTH --test SMUG-DUPLICATE-CL
```

Results stream to the console as each test completes, with a summary at the end:

```
Score: 97/97  19 warnings  (146 tests, 35.5s)
```

## Probing the bundled servers locally

The probe only sends requests — it does not start servers. To probe one of the bundled servers under `src/Servers/`, use `scripts/probe-local.sh`. It mirrors the CI pipeline: builds the server's Docker image, runs it on `--network host`, waits for it to come up, probes it, then tears it down.

```
# Probe a single server — pass the directory name under src/Servers/, e.g. ActixServer
scripts/probe-local.sh --server ActixServer

# Probe every bundled server
scripts/probe-local.sh --all
```

The `--server` value is the **directory name** (`ActixServer`), not the display name (`Actix`). If your Docker daemon requires root, add `--docker-sudo` so you don't have to run the whole script with `sudo`:

```
scripts/probe-local.sh --server ActixServer --docker-sudo
```

### Script options

| Flag | Description |
|------|-------------|
| `--server <Dir>` | Probe a single server by its directory name under `src/Servers/` (e.g. `NginxServer`) |
| `--all` | Probe every server under `src/Servers/*/probe.json` |
| `--port <Port>` | Target port (default: `8080`) |
| `--skip-build` | Skip `dotnet build` (assumes a Release build already exists) |
| `--verbose` | Pass `--verbose` to the CLI |
| `--docker-sudo` | Run Docker commands via `sudo` (lets you run the script without `sudo`) |
| `-h`, `--help` | Show help |

It writes `probe-<ServerDir>.json` (one per server), plus `probe-data.js` and `docs/static/probe/data.js` for local rendering. Requires `jq`, `docker`, `curl`, `python3`, and the .NET 10 SDK.

### Probing a server manually

`probe-local.sh` is just a convenience wrapper. To do the same by hand — for example to keep a server up across several probe runs — build and run the container, then point the probe at it. Run these from the repo root, since the Docker build context is the repo root:

```
docker build -t probe-actix -f src/Servers/ActixServer/Dockerfile .
docker run -d --name probe-target --network host probe-actix
dotnet run --project src/Http11Probe.Cli -- --host localhost --port 8080
docker rm -f probe-target
```

You can also point the probe at any HTTP/1.1 server you already have running:

```
dotnet run --project src/Http11Probe.Cli -- --host localhost --port 9000
```

## Building

Requires .NET 10 SDK.

```
dotnet build Http11Probe.slnx
```

## CI

The [Probe workflow](.github/workflows/probe.yml) runs on PRs and `workflow_dispatch`. It builds each server's Docker image, probes it, and posts a comparison table as a PR comment.
