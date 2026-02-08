---
title: "BARE-CR-HEADER-VALUE"
description: "BARE-CR-HEADER-VALUE test documentation"
weight: 19
---

| | |
|---|---|
| **Test ID** | `SMUG-BARE-CR-HEADER-VALUE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | MUST reject or replace with SP |
| **Expected** | `400` or close |

## What it sends

Header value containing a bare CR (0x0D not followed by LF).

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
X-Test: val\rue\r\n
\r\n
hello
```

The `X-Test` header value contains a bare CR (`\r` / `0x0D`) between `val` and `ue`.


## What the RFC says

> "A sender MUST NOT generate a bare CR (a CR character not immediately followed by LF) within any protocol elements other than the content." — RFC 9112 §2.2

> "A recipient of such a bare CR MUST consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message." — RFC 9112 §2.2

## Why it matters

Bare CR in header values can cause parsers to disagree on header boundaries. A parser that treats bare CR as a line terminator may see the bytes after the CR as a new header line, while a parser that only recognizes CRLF sees them as part of the original value. If a front-end and back-end disagree on where headers begin and end, an attacker can inject headers visible to one parser but not the other -- enabling request smuggling.

## Deep Analysis

### Relevant ABNF

From RFC 9112 Section 2.2, HTTP/1.1 messages use CRLF as the standard line terminator:

```
HTTP-message = start-line CRLF
               *( field-line CRLF )
               CRLF
               [ message-body ]
```

A bare CR (0x0D not followed by 0x0A) is not a valid protocol element outside of message content.

### RFC Evidence

> "A sender MUST NOT generate a bare CR (a CR character not immediately followed by LF) within any protocol elements other than the content." -- RFC 9112 Section 2.2

> "A recipient of such a bare CR MUST consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message." -- RFC 9112 Section 2.2

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR." -- RFC 9112 Section 2.2

### Chain of Reasoning

1. **The RFC draws a hard line against bare CR.** The MUST NOT / MUST language in Section 2.2 is unambiguous: bare CR in protocol elements (headers, request-line, etc.) is prohibited by senders and must be treated as invalid or replaced with SP by recipients. There is no third option -- the recipient cannot silently pass it through unchanged.

2. **Parser disagreement is the core danger.** Consider the header `X-Test: val\rue`. A parser that treats bare CR as a line terminator sees `X-Test: val` followed by a new header line starting with `ue`. A parser that treats only CRLF as a line terminator sees the entire value as `val\rue`. A parser that replaces bare CR with SP sees `val ue`. These three interpretations produce three different header sets from the same bytes on the wire.

3. **The split enables header injection.** If an attacker places `\rEvil-Header: payload` inside a header value, a CR-as-terminator parser will see a new header `Evil-Header: payload` that is invisible to parsers using CRLF. This is particularly dangerous when the front-end proxy forwards the bare CR unchanged while the back-end splits on it, allowing attacker-controlled headers to reach the back-end.

4. **Attack scenario.** An attacker sends `X-Forwarded-For: 127.0.0.1\rTransfer-Encoding: chunked`. The proxy sees one header (`X-Forwarded-For`) with a strange value. The back-end, splitting on bare CR, sees two headers -- including `Transfer-Encoding: chunked` -- enabling a CL.TE smuggling attack that the proxy never detected.

### Scored / Unscored Justification

This test is **scored**. The RFC uses double-MUST language: senders MUST NOT generate bare CR, and recipients MUST either reject the element as invalid or replace bare CR with SP. There is no MAY or SHOULD qualifier -- the requirement is absolute. A server that silently passes through bare CR unchanged violates a MUST-level requirement, making it appropriate to score this test and fail servers that do not reject or sanitize.

## Sources

- [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
