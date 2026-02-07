---
title: Glossary
layout: wide
toc: true
---

## What is an RFC?

An **RFC** (Request for Comments) is a formal document published by the [Internet Engineering Task Force (IETF)](https://www.ietf.org/) that defines the standards and protocols that power the internet. Despite the informal-sounding name, RFCs are the authoritative specifications that all implementations must follow for interoperability.

HTTP/1.1 is defined by two key RFCs:

- **[RFC 9110](https://www.rfc-editor.org/rfc/rfc9110)** &mdash; *HTTP Semantics*. Defines the meaning of HTTP methods, status codes, headers, and content negotiation. This is the "what" of HTTP.
- **[RFC 9112](https://www.rfc-editor.org/rfc/rfc9112)** &mdash; *HTTP/1.1 Message Syntax and Routing*. Defines the wire format: how requests and responses are framed as bytes on a TCP connection. This is the "how" of HTTP/1.1.

RFCs use specific keywords defined in [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119):

| Keyword | Meaning |
|---------|---------|
| **MUST** | Absolute requirement. Violating this means the implementation is non-compliant. |
| **MUST NOT** | Absolute prohibition. |
| **SHOULD** | Recommended but there may be valid reasons to deviate in particular circumstances. |
| **MAY** | Optional behavior. |

When a test in Http11Probe references `RFC 9112 &sect;5.1`, it means the test validates a specific requirement from section 5.1 of that document.

## What is HTTP Request Smuggling?

HTTP request smuggling is an attack that exploits disagreements between HTTP processors about where one request ends and the next begins in a TCP byte stream.

### How it works

In a typical web architecture, requests flow through multiple layers:

```
Client  -->  CDN / Load Balancer  -->  Application Server
             (front-end)               (back-end)
```

Both the front-end and back-end must parse the HTTP stream to determine request boundaries. HTTP/1.1 provides two mechanisms for this:

1. **Content-Length** &mdash; an explicit byte count of the body
2. **Transfer-Encoding: chunked** &mdash; the body is sent in length-prefixed chunks

When a request contains **both** headers, or contains them in ambiguous forms, different servers may disagree on the body length. This disagreement lets an attacker hide a second request inside the body of the first.

### CL.TE Smuggling Example

The attacker sends a request where the front-end uses `Content-Length` and the back-end uses `Transfer-Encoding`:

```http
POST / HTTP/1.1
Host: example.com
Content-Length: 13
Transfer-Encoding: chunked

0\r\n
\r\n
SMUGGLED
```

**Front-end** (reads Content-Length: 13): sees 13 bytes of body (`0\r\n\r\nSMUGGLED`), forwards the whole thing as one request.

**Back-end** (reads Transfer-Encoding: chunked): reads chunk size `0`, which signals end-of-body. The remaining bytes (`SMUGGLED`) are left in the TCP buffer and interpreted as the **start of the next request**.

The attacker has now injected a request that bypasses the front-end entirely.

### TE.CL Smuggling Example

The reverse: the front-end uses `Transfer-Encoding` and the back-end uses `Content-Length`:

```http
POST / HTTP/1.1
Host: example.com
Content-Length: 3
Transfer-Encoding: chunked

8\r\n
SMUGGLED\r\n
0\r\n
\r\n
```

**Front-end** (chunked): reads chunk of size 8 (`SMUGGLED`), then chunk of size 0 (end). Forwards everything.

**Back-end** (Content-Length: 3): reads only 3 bytes of body (`8\r\n`). The rest (`SMUGGLED\r\n0\r\n\r\n`) becomes the next request.

### Why servers must reject ambiguous requests

The only safe behavior is to **reject** any request with ambiguous framing:

- Both `Content-Length` and `Transfer-Encoding` present &rarr; **400**
- Duplicate `Content-Length` with different values &rarr; **400**
- Obfuscated `Transfer-Encoding` (e.g. `xchunked`, `chunked `) &rarr; **400**

A server that "guesses" which header to use is a smuggling vector waiting to happen.

### Real-world impact

HTTP smuggling has been used to:
- **Bypass authentication** &mdash; smuggle requests that skip WAF/auth layers
- **Poison web caches** &mdash; inject responses that get cached and served to other users
- **Hijack sessions** &mdash; prepend attacker-controlled headers to other users' requests
- **Exfiltrate data** &mdash; redirect internal responses to attacker-controlled endpoints

## Test Definitions

Every test ID links back here from the result tables. Click a test name in any results table to jump to its definition.

<div id="glossary-tests"><p><em>Loading...</em></p></div>

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function () {
  if (!window.PROBE_DATA) {
    document.getElementById('glossary-tests').innerHTML = '<p><em>No probe data available yet.</em></p>';
    return;
  }
  var ctx = ProbeRender.buildLookups(window.PROBE_DATA.servers);
  var names = ctx.names, lookup = ctx.lookup, testIds = ctx.testIds;

  var categories = [
    { key: 'Compliance', title: 'Compliance Tests' },
    { key: 'Smuggling', title: 'Smuggling Tests' },
    { key: 'MalformedInput', title: 'Malformed Input Tests' }
  ];

  var html = '';
  categories.forEach(function (cat) {
    var tests = testIds.filter(function (tid) {
      return lookup[names[0]][tid] && lookup[names[0]][tid].category === cat.key;
    });
    if (tests.length === 0) return;

    var scored = tests.filter(function (tid) { return lookup[names[0]][tid].scored !== false; });
    var unscored = tests.filter(function (tid) { return lookup[names[0]][tid].scored === false; });

    html += '<h3>' + cat.title + '</h3>';
    html += '<div style="margin-bottom:1.5rem;">';

    function row(tid) {
      var r = lookup[names[0]][tid];
      if (!r) return '';
      var rfc = r.rfc ? ' <span style="color:#656d76;font-size:11px;">(' + r.rfc + ')</span>' : '';
      return '<div id="test-' + tid + '" style="display:flex;gap:8px;align-items:baseline;padding:6px 0;border-bottom:1px solid #f0f0f0;">'
        + '<div style="min-width:260px;"><code style="font-size:12px;">' + tid + '</code>' + rfc + '</div>'
        + '<div style="min-width:60px;text-align:center;">' + ProbeRender.pill(ProbeRender.EXPECT_BG, r.expected) + '</div>'
        + '<div style="flex:1;font-size:13px;">' + r.reason + '</div>'
        + '</div>';
    }

    scored.forEach(function (tid) { html += row(tid); });
    if (unscored.length > 0) {
      html += '<div style="padding:8px 0;font-weight:700;font-size:12px;color:#656d76;border-bottom:1px solid #d0d7de;margin-top:4px;">Not scored (RFC-compliant behavior)</div>';
      unscored.forEach(function (tid) { html += row(tid); });
    }
    html += '</div>';
  });

  document.getElementById('glossary-tests').innerHTML = html;
})();
</script>
