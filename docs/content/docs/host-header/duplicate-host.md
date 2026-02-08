---
title: "DUPLICATE-HOST"
description: "DUPLICATE-HOST test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `RFC9110-5.4-DUPLICATE-HOST` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | MUST |
| **Expected** | `400` |

## What it sends

A request with two `Host` headers containing different values:

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Host: other.example.com\r\n
\r\n
```

Two `Host` headers with different values.


## What the RFC says

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

Same MUST-400 as the missing Host case. The server must send an actual 400 response.

## Why it matters

Duplicate Host headers with different values are a classic host header injection attack. If the application uses the first Host and the CDN uses the second (or vice versa), the attacker can:
- Poison caches with content for the wrong domain
- Bypass host-based access controls
- Trigger SSRF via internal hostnames

## Deep Analysis

### Relevant ABNF Grammar

```
Host = uri-host [ ":" port ]
```

The Host header is defined as a singleton field -- it is not a list-based header and does not use the `#` (comma-separated list) syntax. Multiple Host header field lines are therefore structurally invalid, regardless of whether the values match.

### RFC Evidence

**RFC 9112 Section 3.2** explicitly covers the duplicate case:

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

**RFC 9112 Section 3.2** mandates the client side:

> "A client MUST send a Host header field in all HTTP/1.1 request messages." -- RFC 9112 Section 3.2

**RFC 9110 Section 7.2** reinforces the requirement:

> "A client MUST send the Host header field in an HTTP/1.1 request message, unless the request target is a URI whose origin is undefined." -- RFC 9110 Section 7.2

### Chain of Reasoning

1. The test sends two Host headers with different values: `localhost:8080` and `other.example.com`.
2. The phrase "more than one Host header field line" in RFC 9112 Section 3.2 unambiguously covers this case.
3. The RFC mandates 400 with no alternative disposition -- not "reject," not "close," but specifically "respond with a 400."
4. Different values in duplicate Host headers are a textbook host header injection attack. If the server picks the first value and a proxy picks the second (or vice versa), the attacker controls routing for one of the two components.
5. Even if both values were identical, the RFC still requires 400 (see DUPLICATE-HOST-SAME). The prohibition is on the structural count of Host lines, not on the semantic content.

### Scoring Justification

**Scored (MUST).** The RFC mandates exactly 400 for duplicate Host headers. No alternative response is permitted. Connection close without sending 400 is non-compliant. The `AllowConnectionClose` flag is not set because the RFC explicitly requires the 400 status code.

## Sources

- [RFC 9112 Section 3.2 -- Request Target](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
- [RFC 9110 Section 7.2 -- Host and :authority](https://www.rfc-editor.org/rfc/rfc9110#section-7.2)
