---
title: Compliance
layout: wide
toc: false
---

## RFC 9110/9112 Compliance

These tests validate that HTTP/1.1 servers correctly implement the protocol requirements defined in [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110) (HTTP Semantics) and [RFC 9112](https://www.rfc-editor.org/rfc/rfc9112) (HTTP/1.1 Message Syntax and Routing).

Each test sends a request that violates a specific **MUST** or **MUST NOT** requirement from the RFCs. A compliant server should reject these with a `400 Bad Request` (or close the connection). Accepting the request silently means the server is non-compliant and potentially vulnerable to downstream attacks.

**What's tested:**
- **Line endings** &mdash; bare `LF` without `CR`, `CR` without `LF` (RFC 9112 &sect;2.2)
- **Request-line format** &mdash; multiple spaces, missing target, fragments in URI (RFC 9112 &sect;3)
- **HTTP version** &mdash; invalid version strings, HTTP/0.9 requests (RFC 9112 &sect;2.3)
- **Header syntax** &mdash; obs-fold, space before colon, empty names, invalid characters, missing colon (RFC 9112 &sect;5, RFC 9110 &sect;5.6.2)
- **Host header** &mdash; missing or duplicate Host with conflicting values (RFC 9112 &sect;7.1, RFC 9110 &sect;5.4)
- **Content-Length** &mdash; non-numeric, plus sign, overflow (RFC 9112 &sect;6.1)

<div id="lang-filter"></div>
<div id="table-compliance"><p><em>Loading...</em></p></div>

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function () {
  if (!window.PROBE_DATA) {
    document.getElementById('table-compliance').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow manually on <code>main</code> to generate results.</em></p>';
    return;
  }
  function render(data) {
    var ctx = ProbeRender.buildLookups(data.servers);
    ProbeRender.renderTable('table-compliance', 'Compliance', ctx);
  }
  render(window.PROBE_DATA);
  ProbeRender.renderLanguageFilter('lang-filter', window.PROBE_DATA, render);
})();
</script>
