# Http11Probe

HTTP/1.1 server compliance and security tester. Sends malformed, ambiguous, and oversized requests over raw TCP sockets and validates responses against RFC 9110/9112 requirements.

**Website:** [mda2av.github.io/Http11Probe](https://MDA2AV.github.io/Http11Probe/) — full documentation, test glossary with RFC citations, and live probe results across all tested servers.

## 116 Tests across 3 Categories

| Category | Tests | What it covers |
|----------|------:|----------------|
| **Compliance** | 47 | RFC 9110/9112 protocol requirements — bare LF, obs-fold, missing Host, invalid versions, chunked encoding, upgrade handling, etc. |
| **Smuggling** | 50 | CL/TE ambiguity, duplicate Content-Length, pipeline desync, TE obfuscation, chunk extension abuse, bare LF in chunked framing |
| **Malformed Input** | 19 | Binary garbage, oversized URLs/headers/methods, NUL bytes, control characters, integer overflow, HTTP/2 preface |

Each test is scored against RFC normative language (MUST/SHOULD/MAY) and classified as **Pass**, **Fail**, or **Warn** (when the RFC permits both strict and lenient behavior).

## 31 Server Targets

Tested across 8 languages:

| Language | Servers |
|----------|---------|
| C# | Kestrel, EmbedIO, GenHTTP, Glyph11, NetCoreServer, ServiceStack, SimpleW, Sisk, Watson |
| Rust | Actix, Hyper, Ntex, Pingora |
| Go | Caddy, FastHTTP, Gin, Traefik |
| Java | Jetty, Quarkus, Spring Boot |
| JavaScript | Bun, Express, Node |
| C | Apache, H2O, HAProxy, Nginx |
| C++ | Drogon, Envoy, Lithium |
| Python | Flask |

## Usage

```
dotnet run --project src/Http11Probe.Cli -- --host localhost --port 8080
```

### Options

| Flag | Description | Default |
|------|-------------|---------|
| `--host` | Target host | `localhost` |
| `--port` | Target port | `8080` |
| `--category` | Filter by category (`Compliance`, `Smuggling`, `MalformedInput`) | all |
| `--timeout` | Connect/read timeout in seconds | `5` |
| `--output` | Write JSON report to file | — |

### Example

```
dotnet run --project src/Http11Probe.Cli -- --host localhost --port 8080 --output results.json
```

Results stream to the console as each test completes, with a summary at the end:

```
Score: 97/97  19 warnings  (116 tests, 35.5s)
```

## Building

Requires .NET 10 SDK.

```
dotnet build Http11Probe.slnx
```

## CI

The [Probe workflow](.github/workflows/probe.yml) runs on PRs and `workflow_dispatch`. It builds each server's Docker image, probes it, and posts a comparison table as a PR comment.

## Results

See the [live comparison](https://MDA2AV.github.io/Http11Probe/probe-results/) across all servers, or browse the [test glossary](https://MDA2AV.github.io/Http11Probe/docs/) for per-test RFC references and explanations.
