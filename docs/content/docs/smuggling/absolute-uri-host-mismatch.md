---
title: "ABSOLUTE-URI-HOST-MISMATCH"
description: "ABSOLUTE-URI-HOST-MISMATCH test documentation"
weight: 57
---

| | |
|---|---|
| **Test ID** | `SMUG-ABSOLUTE-URI-HOST-MISMATCH` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §3.2.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.2) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

A GET request using absolute-form URI with a host that differs from the Host header.

```http
GET http://other.example.com/ HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

The request-target uses absolute-form with `other.example.com` while the Host header says `localhost:8080`.


## What the RFC says

> "When an origin server receives a request with an absolute-form of request-target, the origin server MUST ignore the received Host header field (if any) and instead use the host information of the request-target." — RFC 9112 §3.2.2

> "A server MUST accept the absolute-form in requests even though most HTTP/1.1 clients will only send the absolute-form to a proxy." — RFC 9112 §3.2.2

When a server receives absolute-form, the URI host takes priority over the Host header. However, not all servers support absolute-form, and some may ignore the URI and use the Host header regardless.

## Why this test is unscored

The RFC requires the origin server to use the URI host from the absolute-form request-target, but not all servers support absolute-form requests. A server that rejects the request with `400` is being strict but safe, while a server that accepts and processes the request is handling the absolute-form correctly per the RFC. Both are defensible behaviors.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (handles the mismatch in some way).

## Why it matters

If a proxy routes requests based on the Host header (`localhost:8080`) but the origin server resolves the target based on the URI host (`other.example.com`), routing confusion occurs. An attacker can use this mismatch to access virtual hosts that should be restricted, bypass access controls, or poison caches for a different domain. This is especially dangerous in reverse proxy configurations where the proxy and origin have different URI-vs-Host precedence rules.

## Deep Analysis

### Relevant ABNF

From RFC 9112 Section 3.2.2:

```
absolute-form = absolute-URI
```

The request-target in absolute-form contains the full URI including scheme and authority (host), which may differ from the `Host` header value.

### RFC Evidence

> "When an origin server receives a request with an absolute-form of request-target, the origin server MUST ignore the received Host header field (if any) and instead use the host information of the request-target." -- RFC 9112 Section 3.2.2

> "A client MUST send a Host header field in an HTTP/1.1 request even if the request-target is in absolute-form." -- RFC 9112 Section 3.2.2

> "When a proxy receives a request with an absolute-form of request-target, the proxy MUST ignore the received Host header field (if any) and instead replace it with the host information of the request-target." -- RFC 9112 Section 3.2.2

> "A server MUST accept the absolute-form in requests even though most HTTP/1.1 clients will only send the absolute-form to a proxy." -- RFC 9112 Section 3.2.2

### Chain of Reasoning

1. **The specification establishes dual authority sources.** When a request uses absolute-form, the URI authority (`http://other.example.com/`) and the `Host` header (`localhost:8080`) both carry host information. RFC 9112 Section 3.2.2 resolves this by mandating that the URI authority takes precedence.

2. **Not all implementations follow the precedence rule.** Many origin servers never see absolute-form requests in practice (clients typically send them only to proxies). As a result, some servers ignore the absolute-form URI entirely and route based on the `Host` header. This creates an inconsistency between what the RFC requires and what actually happens.

3. **The mismatch enables routing confusion.** In a reverse proxy deployment, the proxy might resolve the URI host (`other.example.com`) for routing while the origin server uses the `Host` header (`localhost:8080`), or vice versa. This disagreement about which host is authoritative is the foundation of host-header attacks: cache poisoning (the proxy caches a response under the wrong host), SSRF (the origin processes a request intended for an internal service), and virtual host bypass (an attacker reaches a restricted vhost).

4. **Attack scenario.** An attacker sends `GET http://internal.corp/ HTTP/1.1` with `Host: public.example.com`. If the proxy uses the Host header for routing (to `public.example.com`) but the origin server uses the URI authority, the attacker's request reaches `internal.corp` on the origin while the proxy believes it went to the public site. Alternatively, if the proxy follows the RFC and routes to `internal.corp` while the origin ignores the absolute-form and uses the Host header, the response may be cached under the wrong host key.

### Scored / Unscored Justification

This test is **unscored** (`Scored = false`). The RFC says the server MUST ignore the Host header and use the URI authority -- but it does not say the server MUST reject mismatches. A server that correctly prioritizes the URI authority over the Host header and responds `2xx` is technically compliant. A server that rejects the mismatch with `400` is being defensively strict. Since both behaviors are defensible under the RFC, neither can be penalized. The test flags `2xx` as a warning to surface the behavior for human review, since the routing confusion risk depends on the deployment topology, not on the server alone.

## Sources

- [RFC 9112 §3.2.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.2)
- [RFC 9110 §7.2](https://www.rfc-editor.org/rfc/rfc9110#section-7.2)
