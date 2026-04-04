---
title: WebSockets
layout: wide
toc: false
---

## WebSocket / Upgrade

These tests check how servers handle the HTTP `Upgrade` mechanism, primarily used for the WebSocket handshake. RFC 9110 Section 7.8 and RFC 6455 define strict requirements for when a server may respond with `101 Switching Protocols`.

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
<div id="table-websockets"><p><em>Loading...</em></p></div>

<script src="/probe/data.js"></script>
<script src="/probe/render.js"></script>
<script>
(function () {
  if (!window.PROBE_DATA) {
    document.getElementById('table-websockets').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow manually on <code>main</code> to generate results.</em></p>';
    return;
  }
  var GROUPS = [
    { key: 'websockets', label: 'WebSocket / Upgrade', testIds: [
      'WS-UPGRADE-POST','WS-UPGRADE-MISSING-CONN','WS-UPGRADE-UNKNOWN',
      'WS-UPGRADE-INVALID-VER','WS-UPGRADE-HTTP10'
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
    ProbeRender.renderSubTables('table-websockets', 'WebSockets', ctx, GROUPS);
  }
  rerender();
  var catData = ProbeRender.filterByCategory(window.PROBE_DATA, ['WebSockets']);
  ProbeRender.renderLanguageFilter('lang-filter', window.PROBE_DATA, function (d) { langData = d; rerender(); });
  ProbeRender.renderMethodFilter('method-filter', catData, function (m) { methodFilter = m; rerender(); });
  ProbeRender.renderRfcLevelFilter('rfc-level-filter', catData, function (l) { rfcLevelFilter = l; rerender(); });
})();
</script>
