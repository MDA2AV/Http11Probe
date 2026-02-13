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

<div style="height:20px"></div>

{{< cards >}}
  {{< card link="probe-results" title="Leaderboard" subtitle="See which frameworks pass the most tests, ranked from best to worst compliance." icon="chart-bar" >}}
{{< /cards >}}

<div style="height:60px"></div>

<h2 style="font-size:2rem;font-weight:800;">What It Does</h2>

<div style="height:16px"></div>

Http11Probe sends a suite of crafted HTTP requests to each server and checks whether the response matches the exact expected behavior from the RFCs. Every server is tested identically, producing a side-by-side compliance comparison.

<div style="height:20px"></div>

{{< cards >}}
  {{< card link="compliance" title="Compliance" subtitle="RFC 9110/9112 protocol requirements — line endings, request-line format, header syntax, Host validation, Content-Length parsing." icon="check-circle" >}}
  {{< card link="smuggling" title="Smuggling" subtitle="CL/TE ambiguity, duplicate Content-Length, obfuscated Transfer-Encoding, pipeline injection vectors." icon="shield-exclamation" >}}
  {{< card link="malformed-input" title="Robustness" subtitle="Binary garbage, oversized fields, too many headers, control characters, integer overflow, incomplete requests." icon="lightning-bolt" >}}
  {{< card link="normalization" title="Normalization" subtitle="Header normalization behavior — underscore-to-hyphen, space before colon, tab in name, case folding on Transfer-Encoding." icon="adjustments" >}}
{{< /cards >}}

<div style="height:60px"></div>

<h2 style="font-size:2rem;font-weight:800;">Add Your Framework</h2>

<div style="height:16px"></div>

Http11Probe is designed so anyone can contribute their HTTP server and get compliance results without touching the test infrastructure. Just add a Dockerfile, a one-line `probe.json`, and open a PR.

<div style="height:20px"></div>

{{< cards >}}
  {{< card link="add-a-framework" title="Get Started" subtitle="Three steps to add your framework — Dockerfile, probe.json, and open a PR." icon="plus-circle" >}}
{{< /cards >}}

