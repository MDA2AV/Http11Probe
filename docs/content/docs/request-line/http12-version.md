---
title: "HTTP12-VERSION"
description: "HTTP12-VERSION test documentation"
weight: 19
---

| | |
|---|---|
| **Test ID** | `COMP-HTTP12-VERSION` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) |
| **Requirement** | MAY (unscored) |
| **Expected** | `200` or `505` = Warn |

## What it sends

A request using HTTP version 1.2, which does not exist but has a higher minor version than 1.1.

```http
GET / HTTP/1.2\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "A recipient that receives a message with a major version number that it implements and a minor version number higher than what it implements SHOULD process the message as if it were in the highest minor version within that major version to which the recipient is conformant." — RFC 9110 Section 2.5

> "The 505 (HTTP Version Not Supported) status code indicates that the server does not support, or refuses to support, the major version of HTTP that was used in the request message." — RFC 9110 Section 15.6.6

A server implementing HTTP/1.1 that receives `HTTP/1.2` should treat it as HTTP/1.1 and process normally. The server may also respond with `505 HTTP Version Not Supported` if it chooses not to handle unrecognized minor versions.

**Warn:** Server responds `200` (correctly processes as HTTP/1.1) or `505` (refuses the minor version). Both are acceptable behaviors.

## Why this test is unscored

The RFC uses SHOULD (not MUST) for processing higher minor versions, and `505` is an explicitly permitted alternative. Since both `200` and `505` are valid responses, and even `400` (while strict) does not represent a security risk, this test records behavior without scoring it.

## Why it matters

Forward compatibility is a core design principle of HTTP versioning. Minor version increments within the same major version should not break communication. A server that rejects `HTTP/1.2` with a `400` instead of processing it as `HTTP/1.1` or returning `505` has an overly strict version parser that may break when clients or proxies use future HTTP/1.x versions.

## Deep Analysis

### Relevant ABNF Grammar

```
HTTP-version = HTTP-name "/" DIGIT "." DIGIT
HTTP-name    = %s"HTTP"
```

The `HTTP-version` production accepts any single digit for both major and minor version numbers. `HTTP/1.2` is syntactically valid -- it matches `HTTP-name "/" DIGIT "." DIGIT` where major=1 and minor=2. The question is not syntax but semantics: how should a server handle a recognized major version with an unrecognized minor version?

### RFC Evidence

**RFC 9110 Section 2.5** provides the forward-compatibility guidance:

> "A recipient that receives a message with a major version number that it implements and a minor version number higher than what it implements SHOULD process the message as if it were in the highest minor version within that major version to which the recipient is conformant." -- RFC 9110 Section 2.5

**RFC 9110 Section 2.5** also permits version refusal:

> "A server can send a 505 (HTTP Version Not Supported) response if it wishes, for any reason, to refuse service of the client's major protocol version." -- RFC 9110 Section 2.5

**RFC 9110 Section 15.6.6** defines the 505 status code:

> "The 505 (HTTP Version Not Supported) status code indicates that the server does not support, or refuses to support, the major version of HTTP that was used in the request message." -- RFC 9110 Section 15.6.6

### Chain of Reasoning

1. `HTTP/1.2` is syntactically valid per the `HTTP-version` ABNF. The major version (1) matches what the server implements; only the minor version (2) is unrecognized.
2. The RFC uses SHOULD for the forward-compatibility behavior: the server SHOULD treat `HTTP/1.2` as `HTTP/1.1` and process the message normally. This is the ideal behavior for forward compatibility.
3. The server MAY alternatively respond with `505`, which is explicitly permitted by Section 2.5. While `505` is technically for refusing "the client's major protocol version," servers are given latitude ("for any reason").
4. A server that returns `400` is being overly strict -- the version string is syntactically valid and the major version matches. This indicates a brittle version parser.
5. Since the RFC uses SHOULD (not MUST) and provides `505` as a legitimate alternative, there is no single required behavior.

### Scoring Justification

**Unscored (MAY).** The RFC provides two explicitly acceptable paths: process as HTTP/1.1 (SHOULD) or respond with 505 (MAY). Neither path is mandated with MUST. Even a `400` response, while indicating an overly strict parser, does not represent a security vulnerability. The test records behavior as Warn for informational purposes without penalizing any outcome.

### Edge Cases

- **HTTP/1.0:** A server implementing HTTP/1.1 that receives `HTTP/1.0` should process it under HTTP/1.0 semantics. This is a downgrade, not an upgrade, and is well-defined behavior.
- **HTTP/2.0 in HTTP/1.1 syntax:** `GET / HTTP/2.0\r\n` -- major version 2 is a different protocol. The server should respond with `505` since it does not implement HTTP/2 over this wire format (HTTP/2 uses a different framing mechanism).
- **HTTP/1.9:** Same situation as HTTP/1.2. The server should treat it as HTTP/1.1. The single-digit minor version means the highest possible is HTTP/1.9.
- **Intermediary version forwarding:** RFC 9112 Section 2.3 states intermediaries "MUST send their own HTTP-version in forwarded messages." A proxy should not forward `HTTP/1.2` -- it should downgrade to `HTTP/1.1`.

## Sources

- [RFC 9112 §2.3 -- HTTP Version](https://www.rfc-editor.org/rfc/rfc9112#section-2.3)
- [RFC 9110 Section 2.5 -- Protocol Version](https://www.rfc-editor.org/rfc/rfc9110#section-2.5)
- [RFC 9110 Section 15.6.6 -- 505 HTTP Version Not Supported](https://www.rfc-editor.org/rfc/rfc9110#section-15.6.6)
