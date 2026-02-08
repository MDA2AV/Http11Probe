---
title: "METHOD-CONNECT"
description: "METHOD-CONNECT test documentation"
weight: 12
---

| | |
|---|---|
| **Test ID** | `COMP-METHOD-CONNECT` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 Section 9.3.6](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.6) |
| **Requirement** | origin server SHOULD reject |
| **Expected** | `400`, `405`, `501`, or close |

## What it sends

`CONNECT example.com:443 HTTP/1.1` — a CONNECT request sent directly to an origin server (not a proxy).

```http
CONNECT example.com:443 HTTP/1.1\r\n
Host: example.com:443\r\n
\r\n
```


## What the RFC says

> "The CONNECT method requests that the recipient establish a tunnel to the destination origin server identified by the request target and, if successful, thereafter restrict its behavior to blind forwarding of data, in both directions, until the tunnel is closed." -- RFC 9110 §9.3.6

> "CONNECT is intended for use in requests to a proxy." -- RFC 9110 §9.3.6

> "An origin server MAY accept a CONNECT request, but most origin servers do not implement CONNECT." -- RFC 9110 §9.3.6

Origin servers are not proxies. They have no reason to accept CONNECT and establish a TCP tunnel.

## Why it matters

If an origin server accepts CONNECT, it effectively becomes an open proxy. This can be exploited for port scanning internal networks, bypassing firewalls, or pivoting attacks through the server.

## Deep Analysis

### Relevant ABNF Grammar

```
request-line   = method SP request-target SP HTTP-version
method         = token
authority-form = uri-host ":" port
```

CONNECT is one of eight standardized HTTP methods. Unlike all other methods, CONNECT uses `authority-form` for its request-target rather than `origin-form`. The method itself is a `token` and is case-sensitive.

### RFC Evidence

**RFC 9110 Section 9.3.6** defines the purpose of CONNECT:

> "The CONNECT method requests that the recipient establish a tunnel to the destination origin server identified by the request target and, if successful, thereafter restrict its behavior to blind forwarding of data, in both directions, until the tunnel is closed." -- RFC 9110 Section 9.3.6

**RFC 9110 Section 9.3.6** states the intended usage context:

> "CONNECT is intended for use in requests to a proxy." -- RFC 9110 Section 9.3.6

**RFC 9110 Section 9.3.6** acknowledges origin server handling:

> "An origin server MAY accept a CONNECT request, but most origin servers do not implement CONNECT." -- RFC 9110 Section 9.3.6

### Chain of Reasoning

1. CONNECT is designed for proxy-to-proxy or client-to-proxy communication. Its purpose is to establish a TCP tunnel through an intermediary to a destination server.
2. The RFC explicitly states CONNECT "is intended for use in requests to a proxy." Sending CONNECT to an origin server is outside the designed use case.
3. An origin server MAY accept CONNECT, but this is a permissive allowance, not an expectation. The RFC acknowledges "most origin servers do not implement CONNECT."
4. An origin server that accepts CONNECT effectively becomes a proxy, enabling tunnel creation to arbitrary hosts. This is a significant security concern: the server could be used for port scanning, firewall bypass, or SSRF.
5. The safest behavior is to reject CONNECT with `405 Method Not Allowed` (method exists but is not applicable), `501 Not Implemented` (method not recognized), or `400 Bad Request`.

### Scoring Justification

**Scored (SHOULD).** The RFC uses "intended for use in requests to a proxy" and "MAY accept," making it clear that origin servers are not expected to support CONNECT. While there is no MUST to reject, accepting CONNECT on an origin server has severe security implications -- it turns the server into an open proxy. The test expects rejection (`400`, `405`, `501`, or close) and treats acceptance as a failure due to the security risk.

### Edge Cases

- **CONNECT to self:** `CONNECT localhost:8080 HTTP/1.1` sent to the server on port 8080. If accepted, the server would establish a tunnel to itself, potentially enabling request smuggling through the tunnel.
- **CONNECT to internal networks:** `CONNECT 10.0.0.1:6379 HTTP/1.1` -- if the origin server accepts this, it becomes a gateway to internal Redis instances or other services behind the firewall.
- **CONNECT with body:** RFC 9110 Section 9.3.6 states that a CONNECT request has no defined body semantics. A server that reads body data from a CONNECT request may misparse the tunnel data.
- **2xx response:** A `200` response to CONNECT means the server has agreed to establish a tunnel. After sending `200`, the server is expected to blindly forward bytes. This is catastrophic on an origin server.

## Sources

- [RFC 9110 Section 9.3.6](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.6)
