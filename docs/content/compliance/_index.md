---
title: Compliance
layout: wide
toc: false
---

## RFC 9110/9112 Compliance

These tests validate that HTTP/1.1 servers correctly implement the protocol requirements defined in [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110) (HTTP Semantics) and [RFC 9112](https://www.rfc-editor.org/rfc/rfc9112) (HTTP/1.1 Message Syntax and Routing).

Each test sends a request that violates a specific **MUST** or **MUST NOT** requirement from the RFCs. A compliant server should reject these with a `400 Bad Request` (or close the connection). Accepting the request silently means the server is non-compliant and potentially vulnerable to downstream attacks.

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
  var GROUPS = [
    { key: 'request-parsing', label: 'Request Parsing', testIds: [
      'RFC9112-2.2-BARE-LF-REQUEST-LINE','RFC9112-2.2-BARE-LF-HEADER',
      'RFC9112-3-CR-ONLY-LINE-ENDING','COMP-LEADING-CRLF','COMP-WHITESPACE-BEFORE-HEADERS',
      'RFC9112-3-MULTI-SP-REQUEST-LINE','RFC9112-3-MISSING-TARGET',
      'RFC9112-3.2-FRAGMENT-IN-TARGET','RFC9112-2.3-INVALID-VERSION',
      'RFC9112-2.3-HTTP09-REQUEST','COMP-ASTERISK-WITH-GET','COMP-OPTIONS-STAR',
      'COMP-ABSOLUTE-FORM',
      'COMP-METHOD-CASE','COMP-REQUEST-LINE-TAB',
      'COMP-VERSION-MISSING-MINOR','COMP-VERSION-LEADING-ZEROS',
      'COMP-VERSION-WHITESPACE','COMP-HTTP12-VERSION',
      'RFC9112-5.1-OBS-FOLD','RFC9110-5.6.2-SP-BEFORE-COLON',
      'RFC9112-5-EMPTY-HEADER-NAME','RFC9112-5-INVALID-HEADER-NAME',
      'RFC9112-5-HEADER-NO-COLON',
      'RFC9112-7.1-MISSING-HOST','RFC9110-5.4-DUPLICATE-HOST',
      'COMP-DUPLICATE-HOST-SAME','COMP-HOST-WITH-USERINFO','COMP-HOST-WITH-PATH',
      'COMP-HOST-EMPTY-VALUE',
      'RFC9112-6.1-CL-NON-NUMERIC','RFC9112-6.1-CL-PLUS-SIGN'
    ]},
    { key: 'body', label: 'Body Handling', testIds: [
      'COMP-POST-CL-BODY','COMP-POST-CL-ZERO','COMP-POST-NO-CL-NO-TE',
      'COMP-POST-CL-UNDERSEND','COMP-CHUNKED-BODY','COMP-CHUNKED-MULTI',
      'COMP-CHUNKED-EMPTY','COMP-CHUNKED-NO-FINAL',
      'COMP-GET-WITH-CL-BODY','COMP-CHUNKED-EXTENSION',
      'COMP-CHUNKED-TRAILER-VALID','COMP-CHUNKED-HEX-UPPERCASE'
    ]},
    { key: 'methods-upgrade', label: 'Methods & Upgrade', testIds: [
      'COMP-METHOD-CONNECT',
      'COMP-UNKNOWN-TE-501','COMP-EXPECT-UNKNOWN','COMP-METHOD-TRACE',
      'COMP-TRACE-WITH-BODY',
      'COMP-UPGRADE-POST','COMP-UPGRADE-MISSING-CONN',
      'COMP-UPGRADE-UNKNOWN','COMP-UPGRADE-INVALID-VER',
      'COMP-CONNECTION-CLOSE','COMP-HTTP10-DEFAULT-CLOSE','COMP-HTTP10-NO-HOST'
    ]}
  ];
  function render(data) {
    var ctx = ProbeRender.buildLookups(data.servers);
    ProbeRender.renderSubTables('table-compliance', 'Compliance', ctx, GROUPS);
  }
  render(window.PROBE_DATA);
  ProbeRender.renderLanguageFilter('lang-filter', window.PROBE_DATA, render);
})();
</script>
