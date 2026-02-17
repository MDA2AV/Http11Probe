# Http11Probe

HTTP/1.1 server compliance and security tester. Sends malformed, ambiguous, and oversized requests over raw TCP sockets and validates responses against RFC 9110/9112 requirements.

**Website:** [mda2av.github.io/Http11Probe](https://MDA2AV.github.io/Http11Probe/) — full documentation, test glossary with RFC citations, and live probe results across all tested servers.

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

## Building

Requires .NET 10 SDK.

```
dotnet build Http11Probe.slnx
```

## CI

The [Probe workflow](.github/workflows/probe.yml) runs on PRs and `workflow_dispatch`. It builds each server's Docker image, probes it, and posts a comparison table as a PR comment.

## Results

See the [live comparison](https://MDA2AV.github.io/Http11Probe/probe-results/) across all servers, or browse the [test glossary](https://MDA2AV.github.io/Http11Probe/docs/) for per-test RFC references and explanations.
