---
title: Malformed Input
layout: wide
toc: false
---

## Malformed Input Handling

These tests send pathological, oversized, or completely invalid payloads to verify the server handles them gracefully &mdash; rejecting with an appropriate error status rather than crashing, hanging, or consuming unbounded resources.

A well-implemented server should respond with `400 Bad Request`, `414 URI Too Long`, or `431 Request Header Fields Too Large` depending on the violation, or simply close the connection.

**What's tested:**
- **Binary garbage** &mdash; random bytes that aren't valid HTTP at all
- **Oversized fields** &mdash; 100 KB URLs, header names, header values, and method names
- **Too many headers** &mdash; 10,000 headers in a single request
- **Invalid bytes** &mdash; NUL bytes in URL, control characters in header values, non-ASCII in header names and URLs
- **Integer overflow** &mdash; `Content-Length` value exceeding 64-bit integer range
- **Incomplete/empty requests** &mdash; partial HTTP or zero bytes sent
- **Whitespace-only request** &mdash; just spaces/tabs with no method or URI

<div id="table-malformed"><p><em>Loading...</em></p></div>

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function () {
  if (!window.PROBE_DATA) {
    document.getElementById('table-malformed').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow manually on <code>main</code> to generate results.</em></p>';
    return;
  }
  var ctx = ProbeRender.buildLookups(window.PROBE_DATA.servers);
  ProbeRender.renderTable('table-malformed', 'MalformedInput', ctx);
})();
</script>
