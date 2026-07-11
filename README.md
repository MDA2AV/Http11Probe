# Http11Probe

Http11Probe is a compliance and security tester for HTTP/1.1 servers. It throws malformed, ambiguous, and oversized requests at a server over raw TCP sockets and checks how it responds against what RFC 9110 and RFC 9112 actually require. These are the awkward cases like bare LF line endings, obsolete line folding, CL/TE request smuggling, chunk-framing tricks, oversized headers, and NUL bytes, where a strict parser and a lenient one start to disagree.

The same 215 tests run against 41 reference servers written in 12 languages, from Nginx, Apache, and Envoy to Kestrel, Gin, Actix, and the built-in servers in Node, Bun, and Deno. Every result is scored against the MUST/SHOULD/MAY wording in the spec and marked **Pass**, **Fail**, or **Warn**. A Warn just means the RFC allows both the strict and the lenient behavior, so neither one is wrong.

You'll find the full documentation, a per-test glossary with RFC citations, and the live results matrix for every server at [http-probe.com](https://www.http-probe.com/).

## Usage

The probe is target-agnostic. It tests whatever HTTP/1.1 server is already listening on `--host:--port`, and there's no flag to pick a framework. Start the server first (or let [`probe-local.sh`](#probing-the-bundled-servers-locally) spin one up for you), then point the probe at it.

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
| `--output` | Write JSON results to file | none |
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

The probe only sends requests, it doesn't start servers. To probe one of the bundled servers under `src/Servers/`, use `scripts/probe-local.sh`. It does the same thing the CI pipeline does: build the server's Docker image, run it on `--network host`, wait for it to come up, probe it, then tear it down.

```
# Probe one server by its directory name under src/Servers/, e.g. ActixServer
scripts/probe-local.sh --server ActixServer

# Probe every bundled server
scripts/probe-local.sh --all
```

The `--server` value is the directory name (`ActixServer`), not the display name (`Actix`). If your Docker daemon needs root, add `--docker-sudo` so you don't have to run the whole script with `sudo`:

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

It writes `probe-<ServerDir>.json` (one per server), plus `probe-data.js` and `docs/static/probe/data.js` for local rendering. You'll need `jq`, `docker`, `curl`, `python3`, and the .NET 10 SDK.

### Probing a server manually

`probe-local.sh` is just a convenience wrapper. If you'd rather do it by hand, say to keep a server up across several probe runs, build and run the container yourself, then point the probe at it. Run these from the repo root, since that's the Docker build context:

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

Requires the .NET 10 SDK.

```
dotnet build Http11Probe.slnx
```

## CI

The [Probe workflow](.github/workflows/probe.yml) runs on pull requests and `workflow_dispatch`. It builds each server's Docker image, probes it, and posts a comparison table as a PR comment.
