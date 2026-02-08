---
title: "CHUNK-LF-TRAILER"
description: "CHUNK-LF-TRAILER test documentation"
weight: 29
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-LF-TRAILER` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MAY accept bare LF |
| **Expected** | `400` or `2xx` |

## What it sends

A chunked request where the final trailer section terminator uses bare `LF` instead of `CRLF`: `0\r\n\n` instead of `0\r\n\r\n`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
0\r\n
\n
```

The final trailer terminator uses bare LF (`\n`) instead of CRLF (`\r\n`).


## What the RFC says

The chunked body grammar requires CRLF to terminate the trailer section:

> chunked-body = *chunk last-chunk trailer-section CRLF
>
> — RFC 9112 §7.1

However, RFC 9112 §2.2 provides a MAY-level allowance:

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR."
>
> — RFC 9112 §2.2

This means a server MAY accept bare LF as the trailer terminator -- both strict rejection and lenient acceptance are RFC-compliant.

## Why this test is unscored

The MAY-level allowance for bare LF in RFC 9112 Section 2.2 means both strict rejection and lenient acceptance are RFC-compliant behaviors. Neither response can be considered wrong, so the test cannot be scored.

**Pass:** Server rejects with `400` (strict CRLF enforcement, safest behavior).
**Warn:** Server accepts and responds `2xx` (RFC-valid per Section 2.2 MAY-level bare LF acceptance).

## Why it matters

If a front-end parser accepts bare LF as the end of the chunked body but a back-end requires strict CRLF, the back-end may continue waiting for data or interpret subsequent bytes differently. This desync between message boundary detection is a smuggling vector.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1)

```
chunked-body    = *chunk last-chunk trailer-section CRLF

last-chunk      = 1*("0") [ chunk-ext ] CRLF
trailer-section = *( field-line CRLF )
```

The final `CRLF` in `chunked-body` terminates the entire chunked message. After the `last-chunk` and any trailer fields, this CRLF signals the end of the chunked body.

### RFC Evidence

**RFC 9112 §7.1** defines the chunked-body terminator:

> "chunked-body = *chunk last-chunk trailer-section CRLF"

The final `CRLF` is mandatory. It follows the `trailer-section` (which may be empty, producing zero trailer field lines). The `CRLF` production requires the two-byte sequence 0x0D 0x0A.

**RFC 9112 §2.2** provides the MAY-level bare LF allowance:

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR."

This MAY language applies to all line terminators in the protocol, including the final chunked-body CRLF.

**RFC 9112 §2.2** contrasts this with bare CR handling:

> "A sender MUST NOT generate a bare CR (a CR character not immediately followed by LF) within any protocol elements other than the content. A recipient of such a bare CR MUST consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message."

### Step-by-Step ABNF Analysis

1. The parser reads the first chunk: `5\r\nhello\r\n` -- valid chunk with size 5 and data `hello`.
2. The parser reads `0\r\n` -- this matches `last-chunk` = `1*("0") [ chunk-ext ] CRLF`.
3. The parser now expects the `trailer-section` followed by `CRLF`. Since there are no trailer fields, the `trailer-section` is empty (zero repetitions).
4. The parser expects the final `CRLF` (0x0D 0x0A) to complete the `chunked-body`.
5. The next byte is `\n` (0x0A) -- a bare LF.
6. **Strict interpretation:** Bare LF does not satisfy the `CRLF` production. The chunked body is not properly terminated.
7. **Lenient interpretation (per §2.2 MAY):** The parser accepts bare LF as a line terminator. The chunked body is considered complete.

### Real-World Smuggling Scenario

The final trailer CRLF is the boundary between the current HTTP message and the next one on a keep-alive connection. Disagreement on whether the message has ended is directly exploitable:

**Attack vector:** An attacker sends `5\r\nhello\r\n0\r\n\n` followed immediately by a smuggled request (e.g., `GET /admin HTTP/1.1\r\n...`). A lenient front-end accepts the bare LF as the end of the chunked body and considers the first message complete. It then sees the `GET /admin` as a new, separate request. A strict back-end does not accept bare LF as the final CRLF -- it continues waiting for `\r\n`. The back-end reads the `GET /admin` bytes as continuation of the first message (possibly as trailer data or an error). This disagreement means the front-end routes the `GET /admin` independently, while the back-end never sees it as a standalone request.

Alternatively, the reverse scenario: if the back-end is lenient and the front-end is strict, the front-end may buffer additional data while the back-end considers the message done and begins reading the next request from the remaining bytes.

This trailer-termination confusion was part of the attack surface explored in HTTP desynchronization research. CVE-2023-25725 (HAProxy) involved chunked message boundary detection issues where front-end and back-end disagreed on when the chunked body ended.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
- [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
