---
title: Normalization
layout: wide
toc: false
---

## Header Normalization

Header normalization tests check what happens when a server *accepts* a malformed header rather than rejecting it. The `/echo` endpoint reflects received headers back in the response body, letting Http11Probe see whether the server:

- **Normalized** the header name to its standard form (smuggling risk &mdash; a proxy chain member may interpret it differently)
- **Preserved** the original malformed name (mild proxy-chain risk)
- **Dropped** the header entirely (safe)

{{< callout type="warning" >}}
Some tests are **unscored** (marked with `*`). These cover behaviors like case normalization that are RFC-compliant and common across servers.
{{< /callout >}}

{{< callout type="info" >}}
Click a **server name** to view its Dockerfile and source code. Click a **row** to expand all results for that server. Click a **result cell** to see the full HTTP request and response.
{{< /callout >}}

<div id="lang-filter"></div>
<div id="table-normalization"><p><em>Loading...</em></p></div>

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function () {
  if (!window.PROBE_DATA) {
    document.getElementById('table-normalization').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow manually on <code>main</code> to generate results.</em></p>';
    return;
  }
  function render(data) {
    var ctx = ProbeRender.buildLookups(data.servers);
    ProbeRender.renderTable('table-normalization', 'Normalization', ctx);
  }
  render(window.PROBE_DATA);
  ProbeRender.renderLanguageFilter('lang-filter', window.PROBE_DATA, render);
})();
</script>
