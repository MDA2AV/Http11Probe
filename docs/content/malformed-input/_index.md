---
title: Malformed Input
layout: wide
toc: false
---

## Malformed Input Handling

These tests send pathological, oversized, or completely invalid payloads to verify the server handles them gracefully &mdash; rejecting with an appropriate error status rather than crashing, hanging, or consuming unbounded resources.

A well-implemented server should respond with `400 Bad Request`, `414 URI Too Long`, or `431 Request Header Fields Too Large` depending on the violation, or simply close the connection.

<style>h1.hx\:mt-2{display:none}.probe-hint{background:#ddf4ff;border:1px solid #54aeff;border-radius:6px;padding:10px 14px;font-size:13px;color:#0969da;font-weight:500}html.dark .probe-hint{background:#1c2333;border-color:#1f6feb;color:#58a6ff}</style>
<div style="display:grid;grid-template-columns:repeat(3,1fr);gap:10px;margin-bottom:16px;">
<div class="probe-hint"><strong style="font-size:14px;">Server Name</strong><br>Click to view Dockerfile and source code</div>
<div class="probe-hint"><strong style="font-size:14px;">Table Row</strong><br>Click to expand all results for that server</div>
<div class="probe-hint"><strong style="font-size:14px;">Result Cell</strong><br>Click to see the full HTTP request and response</div>
</div>

<div id="lang-filter"></div>
<div id="method-filter"></div>
<div id="rfc-level-filter"></div>
<div id="table-malformed"><p><em>Loading...</em></p></div>

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function () {
  if (!window.PROBE_DATA) {
    document.getElementById('table-malformed').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow manually on <code>main</code> to generate results.</em></p>';
    return;
  }
  var GROUPS = [
    { key: 'oversized', label: 'Oversized & Invalid Bytes', testIds: [
      'MAL-LONG-URL','MAL-LONG-HEADER-NAME','MAL-LONG-HEADER-VALUE',
      'MAL-LONG-METHOD','MAL-MANY-HEADERS','MAL-CHUNK-EXT-64K',
      'MAL-NUL-IN-URL','MAL-NUL-IN-HEADER-VALUE','MAL-CONTROL-CHARS-HEADER',
      'MAL-NON-ASCII-HEADER-NAME','MAL-NON-ASCII-URL','MAL-BINARY-GARBAGE',
      'MAL-POST-CL-HUGE-NO-BODY','MAL-RANGE-OVERLAPPING'
    ]},
    { key: 'parsing-edge', label: 'Parsing & Edge Cases', testIds: [
      'MAL-CL-OVERFLOW','MAL-CL-EMPTY','MAL-CHUNK-SIZE-OVERFLOW',
      'MAL-CL-TAB-BEFORE-VALUE',
      'MAL-INCOMPLETE-REQUEST','MAL-EMPTY-REQUEST',
      'MAL-WHITESPACE-ONLY-LINE','MAL-H2-PREFACE',
      'MAL-URL-BACKSLASH','MAL-URL-OVERLONG-UTF8',
      'MAL-URL-PERCENT-NULL','MAL-URL-PERCENT-CRLF'
    ]}
  ];
  var langData = window.PROBE_DATA;
  var methodFilter = null;
  var rfcLevelFilter = null;

  function rerender() {
    var data = langData;
    if (methodFilter) data = ProbeRender.filterByMethod(data, methodFilter);
    if (rfcLevelFilter) data = ProbeRender.filterByRfcLevel(data, rfcLevelFilter);
    var ctx = ProbeRender.buildLookups(data.servers);
    ProbeRender.renderSubTables('table-malformed', 'MalformedInput', ctx, GROUPS);
  }
  rerender();
  var catData = ProbeRender.filterByCategory(window.PROBE_DATA, ['MalformedInput']);
  ProbeRender.renderLanguageFilter('lang-filter', window.PROBE_DATA, function (d) { langData = d; rerender(); });
  ProbeRender.renderMethodFilter('method-filter', catData, function (m) { methodFilter = m; rerender(); });
  ProbeRender.renderRfcLevelFilter('rfc-level-filter', catData, function (l) { rfcLevelFilter = l; rerender(); });
})();
</script>
