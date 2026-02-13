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

<style>h1.hx\:mt-2{display:none}.probe-hint{background:#ddf4ff;border:1px solid #54aeff;border-radius:6px;padding:10px 14px;font-size:13px;color:#0969da;font-weight:500}html.dark .probe-hint{background:#1c2333;border-color:#1f6feb;color:#58a6ff}</style>
<div style="display:grid;grid-template-columns:repeat(3,1fr);gap:10px;margin-bottom:16px;">
<div class="probe-hint"><strong style="font-size:14px;">Server Name</strong><br>Click to view Dockerfile and source code</div>
<div class="probe-hint"><strong style="font-size:14px;">Table Row</strong><br>Click to expand all results for that server</div>
<div class="probe-hint"><strong style="font-size:14px;">Result Cell</strong><br>Click to see the full HTTP request and response</div>
</div>

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
