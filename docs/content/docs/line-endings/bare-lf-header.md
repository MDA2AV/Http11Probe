---
title: "BARE-LF-HEADER"
description: "BARE-LF-HEADER test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `RFC9112-2.2-BARE-LF-HEADER` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | MAY |
| **Expected** | `400` or close (pass), `2xx` (warn) |

## What it sends

A valid `GET` request where one of the header lines is terminated with `\n` (bare LF) instead of `\r\n`.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\n
X-Test: value\r\n
\r\n
```


## What the RFC says

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR." — RFC 9112 Section 2.2

The same rule applies to header field lines as to the request-line. Bare LF is a sender violation, but recipients are permitted to tolerate it. Strict rejection (`400` or connection close) is the safer posture because it eliminates parser disagreements between hops.

## Why it matters

If headers are delimited differently by different parsers in a request chain, an attacker can inject headers that only one parser sees. This is the foundation of header injection and smuggling attacks.

## Deep Analysis

### ABNF grammar for line endings

The HTTP message grammar from RFC 9112 Section 2.1 requires CRLF after every field-line:

```
HTTP-message = start-line CRLF
               *( field-line CRLF )
               CRLF
               [ message-body ]
```

Each header is a `field-line` followed by `CRLF`. The core ABNF definitions from RFC 5234 Appendix B.1:

```
CRLF = CR LF        ; Internet standard newline
CR   = %x0D          ; carriage return
LF   = %x0A          ; linefeed
```

The `field-line` production itself is:

```
field-line = field-name ":" OWS field-value OWS
```

The terminator is not part of `field-line` --- it comes from the outer `HTTP-message` rule, which demands `CRLF`. A bare `%x0A` after a header does not match the `CRLF` production.

### RFC evidence

**Quote 1 --- The canonical line terminator and bare LF allowance:**

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR." --- RFC 9112 Section 2.2

The phrase "start-line **and fields**" is critical: the same MAY-accept rule applies equally to header field lines. A server has discretion to accept or reject bare LF in headers.

**Quote 2 --- Security of octet-level parsing:**

> "A recipient MUST parse an HTTP message as a sequence of octets in an encoding that is a superset of US-ASCII. Parsing an HTTP message as a stream of Unicode characters, without regard for the specific encoding, creates security vulnerabilities due to the varying ways that string processing libraries handle invalid multibyte character sequences that contain the octet LF (%x0A)." --- RFC 9112 Section 2.2

The RFC specifically warns about how different libraries handle the `%x0A` octet. When one parser in a chain treats bare LF as a header terminator and another does not, the two parsers see different header boundaries --- the foundation of header injection attacks.

**Quote 3 --- Smuggling from inconsistent parsing:**

> "Lenient parsing can result in request smuggling security vulnerabilities if there are multiple recipients of the message and each has its own unique interpretation of robustness." --- RFC 9112 Section 3

This directly supports why bare LF in headers is a security concern: if the front-end accepts `Host: localhost:8080\n` as a complete header but the back-end does not recognize the bare LF, the back-end may concatenate the next header into the Host value, or vice versa. This disagreement is exploitable.

### Chain of reasoning

1. **The payload:** The test sends a valid request-line terminated with CRLF, but the `Host` header is terminated with bare LF (`%x0A`) instead of CRLF (`%x0D %x0A`): `Host: localhost:8080\n`.
2. **The ABNF violation:** The `HTTP-message` grammar requires `field-line CRLF`. Bare LF does not match `CRLF = CR LF`.
3. **The MAY exception:** RFC 9112 Section 2.2 permits recipients to recognize bare LF as a line terminator for "the start-line and fields." This is a MAY --- full discretion.
4. **The header injection vector:** Header boundaries are security-critical. If a proxy and origin server disagree on where `Host: localhost:8080` ends, an attacker can inject headers visible to only one parser. For example, a bare-LF-tolerant proxy might see two headers where a strict origin sees one malformed header.
5. **Conclusion:** Rejecting with `400` or closing the connection is the safer posture. Accepting is a valid MAY but introduces risk in multi-hop deployments.

### Scored / Unscored justification

This test is **scored (Pass/Warn)**:

- **Pass** for `400` or connection close --- strict rejection prevents header boundary disagreements between hops.
- **Warn** for `2xx` --- the server accepted bare LF in a header. This is permitted by the RFC (MAY) but introduces a smuggling vector in multi-hop architectures.

The strict posture is rewarded because header-level bare LF is particularly sensitive --- header boundaries directly control how Content-Length, Transfer-Encoding, and Host are parsed, all of which are smuggling-critical fields.

## Sources

- [RFC 9112 Section 2.2 — Message Parsing](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
