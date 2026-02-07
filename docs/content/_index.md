---
title: Http11Probe
layout: hextra-home
---

{{< hextra/hero-badge link="https://github.com/MDA2AV/Http11Probe" >}}
  <span>GitHub</span>
  {{< icon name="arrow-circle-right" attributes="height=14" >}}
{{< /hextra/hero-badge >}}

<div class="hx-mt-6 hx-mb-6">
{{< hextra/hero-headline >}}
  HTTP/1.1 Server Compliance Probe
{{< /hextra/hero-headline >}}
</div>

<div class="hx-mb-12">
{{< hextra/hero-subtitle >}}
  A standalone testing tool that validates HTTP/1.1 servers against RFC 9110/9112 requirements, smuggling vectors, and malformed input handling.
{{< /hextra/hero-subtitle >}}
</div>

<div class="hx-mb-6">
{{< hextra/hero-button text="View Probe Results" link="probe-results" >}}
</div>

## Features

{{< cards >}}
  {{< card link="compliance" title="Compliance Testing" subtitle="RFC 9110/9112 protocol requirements — bare LF, obs-fold, missing Host, invalid versions, and more." icon="check-circle" >}}
  {{< card link="smuggling" title="Smuggling Detection" subtitle="CL/TE ambiguity, duplicate Content-Length, leading zeros, pipeline probes, and obfuscation vectors." icon="shield-exclamation" >}}
  {{< card link="malformed-input" title="Robustness Testing" subtitle="Binary garbage, oversized URLs/headers, control characters, integer overflow, and incomplete requests." icon="lightning-bolt" >}}
{{< /cards >}}
