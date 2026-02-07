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

<div class="hx-mb-6" style="display:flex;gap:12px;justify-content:center;flex-wrap:wrap;">
{{< hextra/hero-button text="View Results" link="probe-results" >}}
{{< hextra/hero-button text="Add Your Framework" link="https://github.com/MDA2AV/Http11Probe#adding-a-server" style="secondary" >}}
</div>

<div class="hx-mt-16"></div>

## What It Does

Http11Probe sends **41 crafted HTTP requests** to each server and checks whether the response matches the exact expected behavior from the RFCs. Every server is tested identically, producing a side-by-side compliance comparison.

{{< cards >}}
  {{< card link="compliance" title="Compliance" subtitle="RFC 9110/9112 protocol requirements — line endings, request-line format, header syntax, Host validation, Content-Length parsing." icon="check-circle" >}}
  {{< card link="smuggling" title="Smuggling" subtitle="CL/TE ambiguity, duplicate Content-Length, obfuscated Transfer-Encoding, pipeline injection vectors." icon="shield-exclamation" >}}
  {{< card link="malformed-input" title="Robustness" subtitle="Binary garbage, 100 KB fields, 10,000 headers, control characters, integer overflow, incomplete requests." icon="lightning-bolt" >}}
{{< /cards >}}

<div class="hx-mt-16"></div>

## Add Your Framework

Http11Probe is designed so anyone can add their HTTP server and get compliance results without touching the test infrastructure. Three steps:

{{< steps >}}

### Write a minimal server

Create a directory under `src/Servers/YourServer/` with a simple HTTP server that returns `200 OK` on `GET /`. Any language, any framework.

### Add a Dockerfile

Add a `Dockerfile` that builds and runs your server. Use `network_mode: host` so it binds directly to the host network.

### Add to docker-compose.yml

Add a service entry with two labels — that's the only configuration needed:

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

{{< /steps >}}

The CI pipeline auto-discovers servers from `docker-compose.yml` labels. No workflow edits, no test changes, no config files. Open a PR and the probe runs automatically.

<div class="hx-mt-16"></div>

## Currently Tested

Glyph11, ASP.NET Kestrel, Flask, Express, Spring Boot, Quarkus, Nancy, Jetty, Nginx, Apache, Caddy, and Pingora — across C#, Python, JavaScript, Java, and Rust.

{{< cards >}}
  {{< card link="probe-results" title="Leaderboard" subtitle="See which frameworks pass the most tests, ranked from best to worst compliance." icon="chart-bar" >}}
  {{< card link="glossary" title="Glossary" subtitle="What RFCs are, how smuggling works, and what every test ID means." icon="book-open" >}}
{{< /cards >}}
