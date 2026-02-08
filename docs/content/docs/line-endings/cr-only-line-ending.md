---
title: "CR-ONLY-LINE-ENDING"
description: "CR-ONLY-LINE-ENDING test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `RFC9112-3-CR-ONLY-LINE-ENDING` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | MUST |
| **Expected** | `400` |

## What it sends

A request where lines are terminated with `\r` (bare CR) instead of `\r\n` (CRLF).

```http
GET / HTTP/1.1\rHost: localhost:8080\r\n
\r\n
```

The request-line is terminated with bare `\r` (CR only) instead of `\r\n` (CRLF). The `Host:` header starts immediately after the CR.


## What the RFC says

> "A sender MUST NOT generate a bare CR (a CR character not immediately followed by LF) within any protocol elements other than the content. A recipient of such a bare CR MUST consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message." — RFC 9112 Section 2.2

This is a MUST with two alternatives: consider the element invalid (reject with `400`), or replace each bare CR with SP before processing. Unlike bare LF, which is MAY-accept, bare CR has a mandatory handling requirement — the server cannot silently treat it as a line terminator.

## Why it matters

Bare CR that is silently ignored creates a discrepancy between what different parsers see. If one parser treats CR as a line ending and another ignores it, the resulting disagreement can be exploited for smuggling.

## Deep Analysis

### ABNF grammar for line endings

The HTTP message grammar from RFC 9112 Section 2.1 requires CRLF throughout:

```
HTTP-message = start-line CRLF
               *( field-line CRLF )
               CRLF
               [ message-body ]
```

From RFC 5234 Appendix B.1, the core ABNF definitions:

```
CRLF = CR LF        ; Internet standard newline
CR   = %x0D          ; carriage return
LF   = %x0A          ; linefeed
```

A bare CR (`%x0D` not immediately followed by `%x0A`) does not match any valid ABNF production for line termination. Unlike bare LF, which is addressed by a MAY-accept clause, bare CR has its own **mandatory** handling rule.

### RFC evidence

**Quote 1 --- The sender prohibition:**

> "A sender MUST NOT generate a bare CR (a CR character not immediately followed by LF) within any protocol elements other than the content." --- RFC 9112 Section 2.2

This establishes that a bare CR in the request-line or headers is a protocol violation at the sender level. The "MUST NOT" makes the sender non-conforming.

**Quote 2 --- The mandatory recipient handling:**

> "A recipient of such a bare CR MUST consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message." --- RFC 9112 Section 2.2

This is the critical sentence. The keyword is MUST (RFC 2119), and it provides exactly two permitted behaviors: (a) consider the element invalid, or (b) replace each bare CR with SP (`%x20`). There is no third option --- silently treating bare CR as a line terminator is explicitly forbidden.

**Quote 3 --- The bare LF contrast (showing bare CR is treated more strictly):**

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR." --- RFC 9112 Section 2.2

This quote is relevant by contrast. Bare LF gets MAY-accept treatment, but bare CR gets MUST-reject-or-replace treatment. The RFC deliberately treats them differently: bare LF is a known legacy pattern with a tolerance path, while bare CR has no legitimate use as a line terminator and must be handled strictly.

### Chain of reasoning

1. **The payload:** The test sends `GET / HTTP/1.1\rHost: localhost:8080\r\n\r\n`. The request-line is terminated with bare CR (`%x0D`) instead of CRLF (`%x0D %x0A`). The `Host:` header starts immediately after the bare CR.
2. **The ABNF violation:** `%x0D` alone does not match `CRLF = CR LF`. The message is syntactically non-conforming.
3. **The MUST requirement:** RFC 9112 Section 2.2 mandates that the recipient MUST either (a) consider the element invalid, or (b) replace the bare CR with SP. There is no MAY-accept path.
4. **Option (a) --- invalid:** The server considers the request-line invalid and responds with `400 Bad Request`. This is the expected outcome.
5. **Option (b) --- replace with SP:** The server replaces the bare CR with a space, producing `GET / HTTP/1.1 Host: localhost:8080` as a single (malformed) line. This would likely result in a `400` anyway because the resulting request-line has extra tokens after the HTTP-version.
6. **What must NOT happen:** The server must not silently treat bare CR as a line terminator. If it did, it would see `GET / HTTP/1.1` as the request-line and `Host: localhost:8080` as a header --- appearing to work normally. This would violate the MUST in RFC 9112 Section 2.2.
7. **The smuggling angle:** If one parser treats bare CR as a line ending and another replaces it with SP, they will completely disagree on the message structure. The first sees two lines; the second sees one. This disagreement is directly exploitable.

### Scored / Unscored justification

This test is **scored (Pass/Fail)** at the MUST level:

- **Pass** for `400` --- the server correctly considers the bare-CR element invalid, satisfying option (a) of the MUST requirement.
- **Fail** for `2xx` --- a `2xx` response means the server silently treated bare CR as a line terminator, violating the MUST in RFC 9112 Section 2.2. Neither of the two permitted behaviors (reject as invalid, replace with SP) would produce a successful response to this payload.
- **No Warn tier** --- MUST requirements are binary. The server either complies or it does not.
- **AllowConnectionClose is false** --- connection close is not an acceptable alternative because this is a MUST-level requirement where only `400` demonstrates correct handling. A silent close without a `400` does not prove the server identified the bare CR correctly.

## Sources

- [RFC 9112 Section 2.2 — Message Parsing](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
