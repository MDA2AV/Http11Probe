---
title: Http11Probe
layout: hextra-home
---

{{< hextra/hero-badge link="https://github.com/MDA2AV/Http11Probe" >}}
  <span>Open Source</span>
  {{< icon name="arrow-circle-right" attributes="height=14" >}}
{{< /hextra/hero-badge >}}

<div class="hx-mt-6 hx-mb-6">
{{< hextra/hero-headline >}}
  HTTP/1.1 Compliance&nbsp;Platform
{{< /hextra/hero-headline >}}
</div>

<div class="hx-mb-12">
{{< hextra/hero-subtitle >}}
  An open testing platform that probes HTTP/1.1 servers against RFC 9110/9112 requirements, smuggling vectors, and malformed input handling. Add your framework, get compliance results automatically.
{{< /hextra/hero-subtitle >}}
</div>

<div style="height:60px"></div>

## What It Does

Http11Probe sends a suite of crafted HTTP requests to each server and checks whether the response matches the exact expected behavior from the RFCs. Every server is tested identically, producing a side-by-side compliance comparison.

<div style="height:20px"></div>

{{< cards >}}
  {{< card link="compliance" title="Compliance" subtitle="RFC 9110/9112 protocol requirements — line endings, request-line format, header syntax, Host validation, Content-Length parsing." icon="check-circle" >}}
  {{< card link="smuggling" title="Smuggling" subtitle="CL/TE ambiguity, duplicate Content-Length, obfuscated Transfer-Encoding, pipeline injection vectors." icon="shield-exclamation" >}}
  {{< card link="malformed-input" title="Robustness" subtitle="Binary garbage, oversized fields, too many headers, control characters, integer overflow, incomplete requests." icon="lightning-bolt" >}}
{{< /cards >}}

<div style="height:60px"></div>

## Add Your Framework

Http11Probe is designed so anyone can contribute their HTTP server and get compliance results without touching the test infrastructure.

<div style="height:24px"></div>

**1. Write a minimal server** — Create a directory under `src/Servers/YourServer/` with a simple HTTP server that returns `200 OK` on `GET /`. Any language, any framework.

<div style="height:16px"></div>

**2. Add a Dockerfile** — Build and run your server. It will use `network_mode: host` so it binds directly to the host network.

<div style="height:16px"></div>

**3. Add to docker-compose.yml** — Add a service entry with two labels. That's the only configuration needed:

<div style="height:12px"></div>

```yaml
yourserver:
  build:
    context: .
    dockerfile: src/Servers/YourServer/Dockerfile
  network_mode: host
  labels:
    probe.port: "9020"
    probe.name: "Your Server"
```

<div style="height:24px"></div>

The CI pipeline auto-discovers servers from `docker-compose.yml` labels. No workflow edits, no test changes, no config files. Open a PR and the probe runs automatically.

<div style="height:60px"></div>

## Currently Tested

Servers across C#, Python, JavaScript, Java, Rust, and C — from application frameworks to reverse proxies.

<div style="height:20px"></div>

{{< cards >}}
  {{< card link="probe-results" title="Leaderboard" subtitle="See which frameworks pass the most tests, ranked from best to worst compliance." icon="chart-bar" >}}
  {{< card link="glossary" title="Glossary" subtitle="What RFCs are, how smuggling works, and what every test ID means." icon="book-open" >}}
{{< /cards >}}
