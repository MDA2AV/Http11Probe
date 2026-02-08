---
title: "INVALID-VERSION"
description: "INVALID-VERSION test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `RFC9112-2.3-INVALID-VERSION` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) |
| **Requirement** | No MUST |
| **Expected** | `400`, `505`, or close |

## What it sends

A request with an unrecognizable HTTP version string, e.g., `GET / HTTP/9.9`.

```http
GET / HTTP/9.9\r\n
Host: localhost:8080\r\n
\r\n
```


## What the RFC says

The HTTP-version grammar is strict and case-sensitive:

> "HTTP-version = HTTP-name '/' DIGIT '.' DIGIT" -- RFC 9112 §2.3

> "HTTP-version is case-sensitive." -- RFC 9112 §2.3

`HTTP/9.9` matches the grammar syntactically (single digit, dot, single digit), but it uses a major version that the server does not implement. The server can refuse it:

> "A server can send a 505 (HTTP Version Not Supported) response if it wishes, for any reason, to refuse service of the client's major protocol version." — RFC 9110 Section 2.5

> "The 505 (HTTP Version Not Supported) status code indicates that the server does not support, or refuses to support, the major version of HTTP that was used in the request message." -- RFC 9110 §15.6.6

There is no MUST-level requirement for a specific response to invalid versions.

## Why it matters

An unrecognized major version like `HTTP/9.9` means the server cannot determine the client's protocol capabilities. Accepting such a request could lead to applying incorrect framing rules, connection semantics, or feature assumptions. Rejecting with `400` or `505` is the safest response.

## Deep Analysis

### Relevant ABNF Grammar

```
request-line = method SP request-target SP HTTP-version CRLF
HTTP-version = HTTP-name "/" DIGIT "." DIGIT
HTTP-name    = %s"HTTP"
```

The `HTTP-version` production is syntactically permissive: any `HTTP/X.Y` where X and Y are single digits is grammatically valid. `HTTP/9.9` satisfies the ABNF. The issue is purely semantic -- major version 9 does not exist and the server cannot determine what protocol rules to apply.

### RFC Evidence

**RFC 9112 Section 2.3** defines the version grammar:

> "HTTP-version = HTTP-name '/' DIGIT '.' DIGIT" -- RFC 9112 Section 2.3

**RFC 9112 Section 2.3** states version handling is case-sensitive:

> "HTTP-version is case-sensitive." -- RFC 9112 Section 2.3

**RFC 9110 Section 15.6.6** defines the 505 status code:

> "The 505 (HTTP Version Not Supported) status code indicates that the server does not support, or refuses to support, the major version of HTTP that was used in the request message." -- RFC 9110 Section 15.6.6

**RFC 9110 Section 2.5** permits version refusal:

> "A server can send a 505 (HTTP Version Not Supported) response if it wishes, for any reason, to refuse service of the client's major protocol version." -- RFC 9110 Section 2.5

### Chain of Reasoning

1. `HTTP/9.9` is syntactically valid per the `HTTP-version` ABNF -- it is `HTTP-name "/" DIGIT "." DIGIT` with major=9 and minor=9.
2. Unlike `HTTP/1.2` (same major, higher minor), `HTTP/9.9` uses a major version the server does not implement. The forward-compatibility SHOULD from Section 2.5 only applies when the major version matches.
3. The server has no knowledge of HTTP/9.x semantics -- it cannot safely assume any framing, header, or connection rules. Processing the message as HTTP/1.1 would be a guess.
4. `505` is the purpose-built response for this situation. `400` is also reasonable since the server considers the version unsupported. Connection close is acceptable as a last resort.
5. There is no MUST-level requirement for any specific response. The RFC provides `505` as a SHOULD and connection close as an implicit option.

### Scoring Justification

**Scored (no MUST) -- Pass/Warn.** There is no mandatory behavior defined for unrecognized major versions. The test accepts `400`, `505`, or connection close as passing outcomes. A server that returns `200` would be concerning -- it would mean the server is blindly processing a request from a completely unknown protocol version, which could lead to incorrect framing or security assumptions.

### Edge Cases

- **HTTP/0.9 as version string:** `GET / HTTP/0.9\r\n` -- syntactically valid per the ABNF (unlike the HTTP/0.9 protocol which has no version string). The server should reject with `505` since HTTP/0.x is not implemented.
- **HTTP/3.0 over TCP:** `GET / HTTP/3.0\r\n` -- HTTP/3 is a real protocol but runs over QUIC, not TCP. Receiving it over a TCP connection is invalid. `505` is appropriate.
- **Malformed versions:** `HTTP/1.` (missing minor digit) or `HTTP/11.1` (multi-digit major) do not match the ABNF at all and are invalid request-lines, not just unsupported versions.

## Sources

- [RFC 9112 Section 2.3 -- HTTP Version](https://www.rfc-editor.org/rfc/rfc9112#section-2.3)
- [RFC 9110 Section 15.6.6 -- 505 HTTP Version Not Supported](https://www.rfc-editor.org/rfc/rfc9110#section-15.6.6)
