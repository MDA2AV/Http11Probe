---
title: "BARE-LF-REQUEST-LINE"
description: "BARE-LF-REQUEST-LINE test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `RFC9112-2.2-BARE-LF-REQUEST-LINE` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | MAY |
| **Expected** | `400` or close |

## What it sends

A `GET / HTTP/1.1` request where the request-line is terminated with `\n` (bare LF) instead of `\r\n` (CRLF).

```http
GET / HTTP/1.1\n
Host: localhost:8080\r\n
\r\n
```


## What the RFC says

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR." — RFC 9112 Section 2.2

The sender MUST NOT generate bare LF, but the recipient is explicitly given permission to accept it. This is a MAY — not a MUST. Strict rejection (`400` or connection close) is the safer posture because it eliminates parser disagreements between hops.

## Why it matters

Bare LF acceptance is a common source of parser disagreements. If a front-end proxy accepts bare LF as a line terminator but a back-end server does not (or vice versa), the two may disagree on request boundaries — a prerequisite for request smuggling.

Strict rejection is the safer choice, which is why Http11Probe scores it as a pass when the server rejects.

## Deep Analysis

### ABNF grammar for line endings

The formal grammar mandates CRLF as the line terminator throughout the HTTP message structure. From RFC 9112 Section 2.1:

```
HTTP-message = start-line CRLF
               *( field-line CRLF )
               CRLF
               [ message-body ]
```

And from RFC 5234 Appendix B.1, the core ABNF definition:

```
CRLF = CR LF        ; Internet standard newline
CR   = %x0D          ; carriage return
LF   = %x0A          ; linefeed
```

The request-line itself is defined in RFC 9112 Section 3:

```
request-line = method SP request-target SP HTTP-version
```

The `request-line` does not include a line terminator in its own production rule --- the termination comes from the `HTTP-message` grammar, which places `CRLF` after `start-line`. This means the **only** formally valid terminator for the request-line is the two-octet sequence `%x0D %x0A`.

### RFC evidence

**Quote 1 --- The canonical line terminator:**

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR." --- RFC 9112 Section 2.2

This sentence establishes two things: (a) CRLF is the normative line terminator, and (b) recognizing bare LF is **permitted** but not required. The keyword is MAY (RFC 2119): the recipient has full discretion to accept or reject.

**Quote 2 --- Parsing as octets:**

> "A recipient MUST parse an HTTP message as a sequence of octets in an encoding that is a superset of US-ASCII. Parsing an HTTP message as a stream of Unicode characters, without regard for the specific encoding, creates security vulnerabilities due to the varying ways that string processing libraries handle invalid multibyte character sequences that contain the octet LF (%x0A)." --- RFC 9112 Section 2.2

This is directly relevant because bare LF (`%x0A`) is called out by name as a security-sensitive octet. The RFC warns that how parsers handle the LF octet is a source of vulnerabilities.

**Quote 3 --- Request smuggling from lenient parsing:**

> "Lenient parsing can result in request smuggling security vulnerabilities if there are multiple recipients of the message and each has its own unique interpretation of robustness." --- RFC 9112 Section 3

This explains the security motivation for rejecting bare LF: when a front-end proxy accepts bare LF and a back-end server does not (or vice versa), they disagree on where the request-line ends, creating a smuggling vector.

### Chain of reasoning

1. **The payload:** The test sends `GET / HTTP/1.1\nHost: localhost:8080\r\n\r\n`. The request-line is terminated with a single `%x0A` (LF) instead of the required `%x0D %x0A` (CRLF).
2. **The ABNF violation:** The `HTTP-message` grammar requires `start-line CRLF`. A bare LF does not match the `CRLF` production (`CR LF`), so the message is syntactically non-conforming.
3. **The MAY exception:** RFC 9112 Section 2.2 says a recipient MAY recognize a single LF as a line terminator. This gives the server discretion: it can accept or reject.
4. **The security argument:** RFC 9112 Section 11.2 warns that lenient parsing leads to smuggling when intermediaries parse differently. A strict server that rejects bare LF eliminates this class of disagreement entirely.
5. **Conclusion:** A server that rejects with `400` or closes the connection is taking the safer posture. A server that accepts is exercising a valid MAY but introduces potential parser-differential risk.

### Scored / Unscored justification

This test is **scored (Pass/Fail)**. Although the RFC requirement level is MAY, Http11Probe enforces strict rejection:

- **Pass** for `400` or connection close --- strict rejection eliminates parser-differential attacks.
- **Fail** for `2xx` --- the server accepted bare LF as a line terminator, which introduces a smuggling vector in multi-hop architectures.

The scoring reflects Http11Probe's security-first philosophy: when the RFC gives discretion (MAY), the tool rewards the stricter posture because bare LF acceptance is a well-known source of parser disagreements that enable request smuggling.

## Sources

- [RFC 9112 Section 2.2 — Message Parsing](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
- [RFC 9110 Section 16.3 — Intermediary Encapsulation Attacks](https://www.rfc-editor.org/rfc/rfc9110#section-16.3)
