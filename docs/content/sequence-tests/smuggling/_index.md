---
title: Smuggling
layout: wide
toc: false
---

## HTTP Request Smuggling (Sequence Tests)

These tests send multiple requests on the same TCP connection to detect desynchronization and request queue poisoning. A safe server should reject the first ambiguous request (usually `400`) or close the connection so the follow-up request cannot be interpreted out-of-sync.

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
<div id="table-sequence-smuggling"><p><em>Loading...</em></p></div>

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function () {
  if (!window.PROBE_DATA) {
    document.getElementById('table-sequence-smuggling').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow manually on <code>main</code> to generate results.</em></p>';
    return;
  }
  var GROUPS = [
	    { key: 'conn-close', label: 'Connection Close Requirements', testIds: [
	      'SMUG-CLTE-CONN-CLOSE','SMUG-TECL-CONN-CLOSE'
	    ]},
    { key: 'baselines', label: 'Baseline Desync Detection', testIds: [
      'SMUG-CLTE-DESYNC','SMUG-TECL-DESYNC','SMUG-PIPELINE-SAFE'
	    ]},
	    { key: 'confirm', label: 'Embedded Request Execution Signals', testIds: [
	      'SMUG-CLTE-SMUGGLED-GET','SMUG-CLTE-SMUGGLED-HEAD',
	      'SMUG-TECL-SMUGGLED-GET','SMUG-TE-DUPLICATE-HEADERS-SMUGGLED-GET','SMUG-DUPLICATE-CL-SMUGGLED-GET'
	    ]},
    { key: 'obf-te', label: 'Obfuscated Transfer-Encoding Variants', testIds: [
      'SMUG-CLTE-SMUGGLED-GET-TE-TRAILING-SPACE','SMUG-CLTE-SMUGGLED-GET-TE-LEADING-COMMA','SMUG-CLTE-SMUGGLED-GET-TE-CASE-MISMATCH'
    ]},
    { key: 'malformed', label: 'Malformed CL/TE Smuggling Variants', testIds: [
      'SMUG-CLTE-SMUGGLED-GET-CL-PLUS','SMUG-CLTE-SMUGGLED-GET-CL-NON-NUMERIC','SMUG-CLTE-SMUGGLED-GET-TE-OBS-FOLD'
    ]},
	    { key: 'cl-body', label: 'Ignored Body / Unread-Body Desync', testIds: [
	      'SMUG-GET-CL-PREFIX-DESYNC'
	    ]},
    { key: 'vectors', label: 'Real-World Desync Vectors', testIds: [
      'SMUG-CL0-BODY-POISON','SMUG-GET-CL-BODY-DESYNC','SMUG-OPTIONS-CL-BODY-DESYNC',
      'SMUG-EXPECT-100-CL-DESYNC','SMUG-OPTIONS-TE-OBS-FOLD','SMUG-CHUNK-INVALID-SIZE-DESYNC'
    ]}
  ];

  // Restrict the page to sequence test IDs only (avoid auto-adding an "Other" group with simple tests).
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
    ProbeRender.renderSubTables('table-sequence-smuggling', 'Smuggling', ctx, GROUPS);
  }
  rerender();
  var catData = ProbeRender.filterByCategory(window.PROBE_DATA, ['Smuggling']);
  ProbeRender.renderLanguageFilter('lang-filter', window.PROBE_DATA, function (d) { langData = d; rerender(); });
  ProbeRender.renderMethodFilter('method-filter', catData, function (m) { methodFilter = m; rerender(); });
  ProbeRender.renderRfcLevelFilter('rfc-level-filter', catData, function (l) { rfcLevelFilter = l; rerender(); });
})();
</script>
