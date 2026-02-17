---
title: Cookies
layout: wide
toc: false
---

## Cookie Handling

These tests check how servers and frameworks handle adversarial `Cookie` headers. Cookie parsing is done at the framework level, not by application code, so malformed cookies can crash parsers or produce mangled values before your code ever runs. All cookie tests are **unscored** since cookies are governed by RFC 6265, not RFC 9110/9112.

<style>h1.hx\:mt-2{display:none}.probe-hint{background:#ddf4ff;border:1px solid #54aeff;border-radius:6px;padding:10px 14px;font-size:13px;color:#0969da;font-weight:500}html.dark .probe-hint{background:#1c2333;border-color:#1f6feb;color:#58a6ff}</style>
<div style="display:grid;grid-template-columns:repeat(3,1fr);gap:10px;margin-bottom:16px;">
<div class="probe-hint"><strong style="font-size:14px;">Server Name</strong><br>Click to view Dockerfile and source code</div>
<div class="probe-hint"><strong style="font-size:14px;">Table Row</strong><br>Click to expand all results for that server</div>
<div class="probe-hint"><strong style="font-size:14px;">Result Cell</strong><br>Click to see the full HTTP request and response</div>
</div>

<div class="probe-filters">
<div id="lang-filter"></div>
<div id="method-filter"></div>
<div id="rfc-level-filter"></div>
</div>
<div id="table-cookies"><p><em>Loading...</em></p></div>

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function () {
  if (!window.PROBE_DATA) {
    document.getElementById('table-cookies').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow manually on <code>main</code> to generate results.</em></p>';
    return;
  }
  var GROUPS = [
    { key: 'echo', label: 'Echo-Based (Survivability)', testIds: [
      'COOK-ECHO','COOK-OVERSIZED','COOK-EMPTY','COOK-NUL',
      'COOK-CONTROL-CHARS','COOK-MANY-PAIRS','COOK-MALFORMED','COOK-MULTI-HEADER'
    ]},
    { key: 'parsed', label: 'Parsed Cookies (Framework Parser)', testIds: [
      'COOK-PARSED-BASIC','COOK-PARSED-MULTI','COOK-PARSED-EMPTY-VAL','COOK-PARSED-SPECIAL'
    ]}
  ];

  var ALL_IDS = [];
  GROUPS.forEach(function (g) { g.testIds.forEach(function (tid) { ALL_IDS.push(tid); }); });

  var langData = window.PROBE_DATA;
  var methodFilter = null;
  var rfcLevelFilter = null;

  function rerender() {
    var data = langData;
    if (methodFilter) data = ProbeRender.filterByMethod(data, methodFilter);
    if (rfcLevelFilter) data = ProbeRender.filterByRfcLevel(data, rfcLevelFilter);
    var ctx = ProbeRender.buildLookups(data.servers);
    ctx.testIds = ALL_IDS;
    ProbeRender.renderSubTables('table-cookies', 'Cookies', ctx, GROUPS);
  }
  rerender();
  var catData = ProbeRender.filterByCategory(window.PROBE_DATA, ['Cookies']);
  ProbeRender.renderLanguageFilter('lang-filter', window.PROBE_DATA, function (d) { langData = d; rerender(); });
  ProbeRender.renderMethodFilter('method-filter', catData, function (m) { methodFilter = m; rerender(); });
  ProbeRender.renderRfcLevelFilter('rfc-level-filter', catData, function (l) { rfcLevelFilter = l; rerender(); });
})();
</script>
