---
title: Malformed Input
layout: wide
toc: false
---

## Malformed Input Handling

These tests send pathological, oversized, or completely invalid payloads to verify the server handles them gracefully &mdash; rejecting with an appropriate error status rather than crashing, hanging, or consuming unbounded resources.

A well-implemented server should respond with `400 Bad Request`, `414 URI Too Long`, or `431 Request Header Fields Too Large` depending on the violation, or simply close the connection.

{{< callout type="info" >}}
Click a **server name** to view its Dockerfile and source code. Click a **row** to expand all results for that server. Click a **result cell** to see the full HTTP request and response.
{{< /callout >}}

<div id="lang-filter"></div>
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
  function render(data) {
    var ctx = ProbeRender.buildLookups(data.servers);
    ProbeRender.renderSubTables('table-malformed', 'MalformedInput', ctx, GROUPS);
  }
  render(window.PROBE_DATA);
  ProbeRender.renderLanguageFilter('lang-filter', window.PROBE_DATA, render);
})();
</script>
