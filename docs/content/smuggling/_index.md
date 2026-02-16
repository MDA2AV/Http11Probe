---
title: Smuggling
layout: wide
toc: false
---

## HTTP Request Smuggling

HTTP request smuggling exploits disagreements between front-end and back-end servers about where one request ends and the next begins. When two servers in a chain parse the same byte stream differently, an attacker can "smuggle" a hidden request past the front-end.

These tests send requests with ambiguous framing &mdash; conflicting `Content-Length` and `Transfer-Encoding` headers, duplicated values, obfuscated encoding names &mdash; and verify the server rejects them outright rather than guessing.

{{< callout type="info" >}}
Multi-step desync checks are under [Sequence Tests &rarr; Smuggling](../sequence-tests/smuggling/).
{{< /callout >}}

{{< callout type="warning" >}}
Some tests are **unscored** (marked with `*`). These send payloads where the RFC permits multiple valid interpretations &mdash; for example, OWS trimming or case-insensitive TE matching. A `2xx` response is RFC-compliant but shown as a warning since stricter rejection is preferred.
{{< /callout >}}

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
<div id="table-smuggling"><p><em>Loading...</em></p></div>

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function () {
  if (!window.PROBE_DATA) {
    document.getElementById('table-smuggling').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow manually on <code>main</code> to generate results.</em></p>';
    return;
  }
	  var SEQUENCE_IDS = [
	    'SMUG-CLTE-CONN-CLOSE','SMUG-TECL-CONN-CLOSE',
	    'SMUG-CLTE-DESYNC','SMUG-CLTE-SMUGGLED-GET','SMUG-TECL-DESYNC','SMUG-PIPELINE-SAFE',
	    'SMUG-CLTE-SMUGGLED-GET-CL-PLUS','SMUG-CLTE-SMUGGLED-GET-CL-NON-NUMERIC','SMUG-CLTE-SMUGGLED-GET-TE-OBS-FOLD',
	    'SMUG-CLTE-SMUGGLED-HEAD','SMUG-CLTE-SMUGGLED-GET-TE-TRAILING-SPACE','SMUG-CLTE-SMUGGLED-GET-TE-LEADING-COMMA',
	    'SMUG-CLTE-SMUGGLED-GET-TE-CASE-MISMATCH','SMUG-TECL-SMUGGLED-GET','SMUG-TE-DUPLICATE-HEADERS-SMUGGLED-GET',
	    'SMUG-DUPLICATE-CL-SMUGGLED-GET','SMUG-GET-CL-PREFIX-DESYNC',
	    'SMUG-CL0-BODY-POISON','SMUG-GET-CL-BODY-DESYNC','SMUG-OPTIONS-CL-BODY-DESYNC',
	    'SMUG-EXPECT-100-CL-DESYNC','SMUG-OPTIONS-TE-OBS-FOLD','SMUG-CHUNK-INVALID-SIZE-DESYNC'
	  ];
  var GROUPS = [
    { key: 'framing', label: 'Framing Conflicts', testIds: [
      'SMUG-CL-TE-BOTH','SMUG-CLTE-PIPELINE','SMUG-TECL-PIPELINE','SMUG-TE-HTTP10',
      'SMUG-DUPLICATE-CL','SMUG-CL-LEADING-ZEROS','SMUG-CL-NEGATIVE',
      'SMUG-CL-COMMA-DIFFERENT','SMUG-CL-OCTAL','SMUG-CL-HEX-PREFIX',
      'SMUG-CL-INTERNAL-SPACE','SMUG-CL-COMMA-SAME',
      'SMUG-CL-TRAILING-SPACE','SMUG-CL-EXTRA-LEADING-SP',
      'SMUG-CL-UNDERSCORE','SMUG-CL-NEGATIVE-ZERO','SMUG-CL-DOUBLE-ZERO',
      'SMUG-CL-LEADING-ZEROS-OCTAL',
      'SMUG-TE-XCHUNKED','SMUG-TE-TRAILING-SPACE','SMUG-TE-SP-BEFORE-COLON',
      'SMUG-TE-EMPTY-VALUE','SMUG-TE-LEADING-COMMA','SMUG-TE-DUPLICATE-HEADERS',
      'SMUG-TE-NOT-FINAL-CHUNKED','SMUG-TE-IDENTITY',
      'SMUG-TE-DOUBLE-CHUNKED','SMUG-TE-CASE-MISMATCH',
      'SMUG-TE-OBS-FOLD','SMUG-TE-TRAILING-COMMA','SMUG-TE-TAB-BEFORE-VALUE',
      'SMUG-TE-VTAB','SMUG-TE-FORMFEED','SMUG-TE-NULL',
      'SMUG-TRANSFER_ENCODING','SMUG-CHUNKED-WITH-PARAMS'
    ]},
    { key: 'chunk', label: 'Chunk Encoding', testIds: [
      'SMUG-CHUNK-BARE-SEMICOLON','SMUG-CHUNK-HEX-PREFIX','SMUG-CHUNK-UNDERSCORE',
      'SMUG-CHUNK-LEADING-SP','SMUG-CHUNK-MISSING-TRAILING-CRLF',
      'SMUG-CHUNK-EXT-LF','SMUG-CHUNK-SPILL','SMUG-CHUNK-LF-TERM',
	      'SMUG-CHUNK-EXT-CTRL','SMUG-CHUNK-EXT-CR','SMUG-CHUNK-LF-TRAILER',
	      'SMUG-CHUNK-NEGATIVE','SMUG-CHUNK-BARE-CR-TERM'
	    ]},
	    { key: 'headers-trailers', label: 'Headers, Trailers & Methods', testIds: [
	      'SMUG-BARE-CR-HEADER-VALUE',
	      'SMUG-TRAILER-CL','SMUG-TRAILER-TE','SMUG-TRAILER-HOST',
	      'SMUG-TRAILER-AUTH','SMUG-TRAILER-CONTENT-TYPE',
      'SMUG-EXPECT-100-CL','SMUG-HEAD-CL-BODY','SMUG-OPTIONS-CL-BODY',
      'SMUG-ABSOLUTE-URI-HOST-MISMATCH','SMUG-MULTIPLE-HOST-COMMA'
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
    ctx.testIds = ctx.testIds.filter(function (tid) { return SEQUENCE_IDS.indexOf(tid) === -1; });
    ProbeRender.renderSubTables('table-smuggling', 'Smuggling', ctx, GROUPS);
  }
  rerender();
  var catData = ProbeRender.filterByCategory(window.PROBE_DATA, ['Smuggling']);
  ProbeRender.renderLanguageFilter('lang-filter', window.PROBE_DATA, function (d) { langData = d; rerender(); });
  ProbeRender.renderMethodFilter('method-filter', catData, function (m) { methodFilter = m; rerender(); });
  ProbeRender.renderRfcLevelFilter('rfc-level-filter', catData, function (l) { rfcLevelFilter = l; rerender(); });
})();
</script>
