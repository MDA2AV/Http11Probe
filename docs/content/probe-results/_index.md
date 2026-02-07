---
title: Probe Results
layout: wide
toc: false
---

HTTP/1.1 compliance comparison across frameworks. Each test sends a specific malformed or ambiguous request and checks the server's response against the **exact** expected status code. Updated on each manual probe run on `main`.

## Summary

<div id="probe-summary"><p><em>Loading probe data...</em></p></div>

{{< callout type="info" >}}
These results are from CI runs (`ubuntu-latest`). Click on the **Compliance**, **Smuggling**, or **Malformed Input** tabs above for detailed results per category.
{{< /callout >}}

## Compliance

RFC 9110/9112 protocol requirements. These tests verify the parser rejects malformed framing per the HTTP specification.

<div id="table-compliance"></div>

## Smuggling

HTTP request smuggling vectors. These tests verify the parser rejects ambiguous requests that could be interpreted differently by intermediaries.

<div id="table-smuggling"></div>

## Malformed Input

Robustness tests for garbage, oversized, and invalid payloads. These tests verify the server handles pathological input without crashing.

<div id="table-malformed"></div>

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function () {
  if (!window.PROBE_DATA) {
    document.getElementById('probe-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow manually on <code>main</code> to generate results.</em></p>';
    return;
  }
  var data = window.PROBE_DATA;
  ProbeRender.renderSummary('probe-summary', data);
  var ctx = ProbeRender.buildLookups(data.servers);
  ProbeRender.renderTable('table-compliance', 'Compliance', ctx);
  ProbeRender.renderTable('table-smuggling', 'Smuggling', ctx);
  ProbeRender.renderTable('table-malformed', 'MalformedInput', ctx);
})();
</script>
