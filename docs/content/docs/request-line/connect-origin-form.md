---
title: "CONNECT-ORIGIN-FORM"
description: "CONNECT-ORIGIN-FORM test documentation"
weight: 18
---

| | |
|---|---|
| **Test ID** | `COMP-CONNECT-ORIGIN-FORM` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §3.2.3](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.3) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A CONNECT request that uses origin-form (`/path`) instead of the required authority-form (`host:port`).

```http
CONNECT /path HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "The 'authority-form' of request-target is only used for CONNECT requests. It consists of only the uri-host and port number of the tunnel destination, separated by a colon (':')." -- RFC 9112 §3.2.3

> "When making a CONNECT request to establish a tunnel through one or more proxies, a client MUST send only the host and port of the tunnel destination as the request-target." -- RFC 9112 §3.2.3

> "CONNECT uses a special form of request target, unique to this method, consisting of only the host and port number of the tunnel destination, separated by a colon." -- RFC 9110 §9.3.6

The CONNECT method establishes a tunnel to the destination identified by the request-target. The target must be in authority-form (`host:port`), not origin-form (`/path`). A CONNECT request with `/path` is syntactically invalid because the server cannot determine the tunnel endpoint.

## Why it matters

If a server accepts a CONNECT request with origin-form, it may attempt to establish a tunnel to an unintended destination, or worse, process it as a regular proxied request. This confusion between request forms can be exploited to bypass access controls, reach internal services, or cause Server-Side Request Forgery (SSRF) through a misconfigured proxy.

## Deep Analysis

### Relevant ABNF Grammar

```
request-line    = method SP request-target SP HTTP-version
request-target  = origin-form / absolute-form / authority-form / asterisk-form
authority-form  = uri-host ":" port
origin-form     = absolute-path [ "?" query ]
```

CONNECT exclusively requires `authority-form`. The `origin-form` (`/path`) is reserved for non-CONNECT, non-OPTIONS requests to origin servers. Using origin-form with CONNECT is a category error -- the wrong request-target production for the method.

### RFC Evidence

**RFC 9112 Section 3.2.3** restricts CONNECT to authority-form:

> "The 'authority-form' of request-target is only used for CONNECT requests (Section 9.3.6 of [HTTP])." -- RFC 9112 Section 3.2.3

**RFC 9112 Section 3.2.3** mandates what clients must send:

> "When making a CONNECT request to establish a tunnel through one or more proxies, a client MUST send only the host and port of the tunnel destination as the request-target." -- RFC 9112 Section 3.2.3

**RFC 9110 Section 9.3.6** reinforces the target format from the semantics side:

> "CONNECT uses a special form of request target, unique to this method, consisting of only the host and port number of the tunnel destination, separated by a colon." -- RFC 9110 Section 9.3.6

### Chain of Reasoning

1. The RFC defines four request-target forms and maps each to specific methods: origin-form for general requests, absolute-form for proxy requests, authority-form for CONNECT, and asterisk-form for server-wide OPTIONS.
2. CONNECT is the **only** method that uses authority-form, and authority-form is the **only** valid form for CONNECT. This is a bidirectional restriction.
3. `CONNECT /path HTTP/1.1` uses origin-form instead of authority-form. The `/path` string cannot be parsed as `uri-host ":" port` -- it lacks both a host component and a port.
4. A server cannot determine a tunnel destination from `/path`. There is no host, no port, and no colon separator.
5. If the server interprets `/path` as a resource path rather than a tunnel target, it may confuse CONNECT with a regular request, leading to SSRF or access control bypass.

### Scoring Justification

**Scored (MUST).** The RFC uses explicit MUST language: clients "MUST send only the host and port of the tunnel destination." An origin-form target like `/path` fundamentally violates this requirement because it does not contain a host or port. A server that accepts this is processing an invalid request-target form for the method, creating a parsing ambiguity with serious security implications.

### Edge Cases

- **CONNECT /host:port HTTP/1.1:** Even if the path happens to look like `host:port`, it starts with `/`, making it origin-form rather than authority-form. The leading slash disqualifies it.
- **CONNECT http://host:port/ HTTP/1.1:** This would be absolute-form, not authority-form. While it contains host and port information, the RFC explicitly requires authority-form for CONNECT.
- **CONNECT host:port/path HTTP/1.1:** The trailing `/path` makes this invalid authority-form since `authority-form = uri-host ":" port` has no path component.

## Sources

- [RFC 9112 §3.2.3 -- CONNECT](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.3)
- [RFC 9110 Section 9.3.6 -- CONNECT](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.6)
