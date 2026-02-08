---
title: "METHOD-CONNECT-NO-PORT"
description: "METHOD-CONNECT-NO-PORT test documentation"
weight: 13
---

| | |
|---|---|
| **Test ID** | `COMP-METHOD-CONNECT-NO-PORT` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2.3](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.3) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`CONNECT example.com HTTP/1.1` — a CONNECT request with authority-form that is missing the required port.

```http
CONNECT example.com HTTP/1.1\r\n
Host: example.com\r\n
\r\n
```

The CONNECT target uses the hostname without a port number.


## What the RFC says

> "The 'authority-form' of request-target is only used for CONNECT requests. It consists of only the uri-host and port number of the tunnel destination, separated by a colon (':')." -- RFC 9112 §3.2.3

> "authority-form = uri-host ':' port" -- RFC 9112 §3.2.3

> "CONNECT uses a special form of request target, unique to this method, consisting of only the host and port number of the tunnel destination, separated by a colon. There is no default port; a client MUST send the port number even if the CONNECT request is based on a URI reference that contains an authority component with an elided port." -- RFC 9110 §9.3.6

The authority-form grammar requires both host and port separated by a colon. Omitting the port entirely makes the request-target invalid.

## Why it matters

A server or proxy that accepts CONNECT without a port must guess the target port, which can lead to unexpected connections. This is a parsing ambiguity that could be exploited for SSRF or port confusion attacks.

## Deep Analysis

### Relevant ABNF Grammar

```
request-line    = method SP request-target SP HTTP-version
request-target  = origin-form / absolute-form / authority-form / asterisk-form
authority-form  = uri-host ":" port
port            = *DIGIT
```

The `authority-form` production requires three components: a `uri-host`, a literal colon (`":"`), and a `port`. In `CONNECT example.com HTTP/1.1`, the request-target is `example.com` -- there is no colon and no port. This does not match the `authority-form` grammar, nor does it match any other valid `request-target` form for the CONNECT method.

### RFC Evidence

**RFC 9112 Section 3.2.3** defines the authority-form grammar and its restriction:

> "The 'authority-form' of request-target is only used for CONNECT requests (Section 9.3.6 of [HTTP])." -- RFC 9112 Section 3.2.3

**RFC 9112 Section 3.2.3** mandates what the client must send:

> "When making a CONNECT request to establish a tunnel through one or more proxies, a client MUST send only the host and port of the tunnel destination as the request-target." -- RFC 9112 Section 3.2.3

**RFC 9110 Section 9.3.6** explicitly prohibits port elision:

> "CONNECT uses a special form of request target, unique to this method, consisting of only the host and port number of the tunnel destination, separated by a colon. There is no default port; a client MUST send the port number even if the CONNECT request is based on a URI reference that contains an authority component with an elided port." -- RFC 9110 Section 9.3.6

### Chain of Reasoning

1. The `authority-form` ABNF requires `uri-host ":" port`. A request-target of `example.com` (no colon, no port) does not match this production.
2. RFC 9110 Section 9.3.6 contains an exceptionally explicit MUST: "There is no default port; a client MUST send the port number." This leaves no room for interpretation.
3. The RFC specifically addresses the case where a URI reference has an elided port (e.g., `http://example.com` where port 80 is implied). Even then, the client MUST send the port explicitly in the CONNECT target.
4. `CONNECT example.com HTTP/1.1` does not match any valid request-target form. It is not authority-form (no colon), not origin-form (no leading `/`), not absolute-form (no scheme), and not asterisk-form (not `*`).
5. A server that accepts this must infer a default port, creating a security-critical ambiguity in tunnel destination.

### Scoring Justification

**Scored (MUST).** This is one of the most explicit requirements in the HTTP specification. RFC 9110 Section 9.3.6 states "There is no default port; a client MUST send the port number." The authority-form grammar also requires the port component. A server that accepts a CONNECT without a port is violating both the grammar and an explicit MUST, and is guessing the tunnel destination, which has direct SSRF implications.

### Edge Cases

- **Implied port from scheme:** Even if the client intends port 443 for HTTPS, the RFC requires explicit `CONNECT example.com:443 HTTP/1.1`. Port inference is not permitted.
- **IPv6 without port:** `CONNECT [::1] HTTP/1.1` -- the IPv6 address is in brackets but there is no colon-port suffix. This is equally invalid because the port is missing.
- **Host header as fallback:** A server might attempt to extract the port from the Host header (`Host: example.com:443`). This is non-compliant because the request-target itself must contain the port.
- **Ambiguity with other forms:** Without a colon, `example.com` could be confused with a partial URI or hostname. Only the presence of `host:port` format unambiguously identifies this as authority-form.

## Sources

- [RFC 9112 Section 3.2.3](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.3)
