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
  {{< card link="docs/rfc-requirement-dashboard" title="RFC Requirement Dashboard" subtitle="Every test classified by RFC 2119 requirement level (MUST/SHOULD/MAY)." icon="document-search" >}}
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
  {{< card link="caching" title="Caching" subtitle="Conditional request support — ETag, Last-Modified, If-None-Match precedence, weak comparison, edge cases." icon="beaker" >}}
  {{< card link="cookies" title="Cookies" subtitle="Cookie header parsing resilience — oversized values, NUL bytes, control characters, malformed pairs, multiple headers." icon="cake" >}}
{{< /cards >}}

<div style="height:60px"></div>

<h2 style="font-size:2rem;font-weight:800;">Contribute to the Project</h2>

<div style="height:16px"></div>

Http11Probe is open source and built for contributions. Add your HTTP server to the leaderboard, or write new test cases to expand coverage.

Every new framework added makes the comparison more useful for the entire community, and every new test strengthens the compliance bar for all servers on the platform. If you've found an edge case that isn't covered, or you maintain a framework that isn't listed yet, your contribution directly improves HTTP security and interoperability for everyone.

<div style="height:20px"></div>

{{< cards >}}
  {{< card link="add-a-framework" title="Add a Framework" subtitle="Three steps to add your framework — Dockerfile, probe.json, and open a PR." icon="plus-circle" >}}
  {{< card link="add-a-test" title="Add a Test" subtitle="How to define a new test case, write its documentation, and wire it into the platform." icon="beaker" >}}
  {{< card link="add-with-ai-agent" title="Add with AI Agent" subtitle="Use an AI coding agent to add a test or framework using the machine-readable AGENTS.md guide." icon="chip" >}}
{{< /cards >}}

