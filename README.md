# Http11Probe

HTTP/1.1 server compliance and security tester. Sends malformed, ambiguous, and oversized requests over raw TCP sockets and validates responses against RFC 9110/9112 requirements.

**Website:** [http-probe.com](https://www.http-probe.com/) — full documentation, test glossary with RFC citations, and live probe results across all tested servers.

## 215 Tests across 6 Categories

| Category | Tests | What it covers |
|----------|------:|----------------|
| **Compliance** | 76 | RFC 9110/9112 protocol requirements — bare LF, obs-fold, missing Host, invalid versions, chunked encoding, connection semantics, upgrade handling, etc. |
| **Smuggling** | 87 | CL/TE ambiguity, duplicate Content-Length, pipeline desync, TE obfuscation, chunk extension abuse, bare LF in chunked framing, URI/Host mismatch |
| **Malformed Input** | 26 | Binary garbage, oversized URLs/headers/methods, NUL bytes, control characters, integer overflow, overlong UTF-8, encoded CRLF injection |
| **Normalization** | 5 | Header name casing, whitespace trimming, and other normalization behaviors |
| **Cookies** | 12 | Cookie parsing, Set-Cookie handling, and RFC 6265bis compliance |
| **Capabilities** | 9 | Server capability detection — keep-alive, pipelining, chunked responses (unscored) |

Each test is scored against RFC normative language (MUST/SHOULD/MAY) and classified as **Pass**, **Fail**, or **Warn** (when the RFC permits both strict and lenient behavior).

## 36 Server Targets

Tested across 11 languages:

| Language | Servers |
|----------|---------|
| C# | Kestrel, EmbedIO, FastEndpoints, GenHTTP, Glyph11, NetCoreServer, ServiceStack, SimpleW, Sisk |
| C | Apache, H2O, HAProxy, Lighttpd, Nginx |
| Rust | Actix, Hyper, Ntex, Pingora |
| Go | Caddy, FastHTTP, Gin, Traefik |
| Java | Jetty, Quarkus, Spring Boot, Tomcat |
| Python | Flask, Gunicorn, Uvicorn |
| JavaScript | Bun, Express, Node |
| C++ | Envoy |
| TypeScript | Deno |
| Ruby | Puma |
| PHP | PHP built-in |

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

It writes `probe-<ServerDir>.json` (one per server), plus `probe-data.js` and `docs/static/probe/data.js` for local Hugo rendering. Requires `jq`, `docker`, `curl`, `python3`, and the .NET 10 SDK.

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

## Results

See the [live comparison](https://www.http-probe.com/probe-results/) across all servers, or browse the [test glossary](https://www.http-probe.com/docs/) for per-test RFC references and explanations.
