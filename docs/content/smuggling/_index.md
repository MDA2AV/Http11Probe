---
title: Smuggling
layout: wide
toc: false
---

## HTTP Request Smuggling

HTTP request smuggling exploits disagreements between front-end and back-end servers about where one request ends and the next begins. When two servers in a chain parse the same byte stream differently, an attacker can "smuggle" a hidden request past the front-end.

These tests send requests with ambiguous framing &mdash; conflicting `Content-Length` and `Transfer-Encoding` headers, duplicated values, obfuscated encoding names &mdash; and verify the server rejects them outright rather than guessing.

**What's tested:**
- **CL+TE both present** &mdash; the classic smuggling setup: which header wins? (RFC 9112 &sect;6.1)
- **Duplicate Content-Length** &mdash; two `Content-Length` headers with different values (RFC 9110 &sect;8.6)
- **Leading zeros in CL** &mdash; `Content-Length: 007` can be parsed as octal or decimal
- **Negative CL** &mdash; `Content-Length: -1` wraps to a large value in some parsers
- **TE obfuscation** &mdash; `xchunked`, `chunked ` (trailing space), `Chunked` (case), `chunked, chunked` (duplicate)
- **Space before colon** &mdash; `Transfer-Encoding : chunked` with space tricks some parsers
- **CL.TE / TE.CL pipelines** &mdash; full smuggling payloads that inject a second request

{{< callout type="warning" >}}
Some tests are **unscored** (marked with `*`). These send payloads where the RFC permits multiple valid interpretations &mdash; for example, OWS trimming or case-insensitive TE matching. A `2xx` response is RFC-compliant but shown as a warning since stricter rejection is preferred.
{{< /callout >}}

<div id="table-smuggling"><p><em>Loading...</em></p></div>

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function () {
  if (!window.PROBE_DATA) {
    document.getElementById('table-smuggling').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow manually on <code>main</code> to generate results.</em></p>';
    return;
  }
  var ctx = ProbeRender.buildLookups(window.PROBE_DATA.servers);
  ProbeRender.renderTable('table-smuggling', 'Smuggling', ctx);
})();
</script>
