---
title: "CHUNK-LF-TERM"
description: "CHUNK-LF-TERM test documentation"
weight: 27
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-LF-TERM` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MAY accept bare LF |
| **Expected** | `400` or `2xx` |

## What it sends

A chunked request where the chunk data terminator is a bare `LF` (`\n`) instead of `CRLF` (`\r\n`): `5\r\nhello\n0\r\n\r\n`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\n
0\r\n
\r\n
```

The chunk data `hello` is terminated with bare LF (`\n`) instead of CRLF (`\r\n`).


## What the RFC says

The chunk grammar requires CRLF after chunk data:

> chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF
>
> — RFC 9112 §7.1

However, RFC 9112 §2.2 provides a MAY-level allowance:

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR."
>
> — RFC 9112 §2.2

This means a server MAY accept bare LF as a chunk data terminator -- both strict rejection and lenient acceptance are RFC-compliant.

## Why this test is unscored

The MAY-level allowance for bare LF in RFC 9112 Section 2.2 means both strict rejection and lenient acceptance are RFC-compliant behaviors. Neither response can be considered wrong, so the test cannot be scored.

**Pass:** Server rejects with `400` (strict CRLF enforcement, safest behavior).
**Warn:** Server accepts and responds `2xx` (RFC-valid per Section 2.2 MAY-level bare LF acceptance).

## Why it matters

If one parser accepts bare LF as a chunk data terminator and another requires strict CRLF, they disagree on where the chunk data ends. The byte that the strict parser considers part of chunk data is treated as the next chunk-size line by the lenient parser — a classic desynchronization vector.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1)

```
chunked-body = *chunk last-chunk trailer-section CRLF

chunk        = chunk-size [ chunk-ext ] CRLF
               chunk-data CRLF
chunk-size   = 1*HEXDIG
chunk-data   = 1*OCTET ; a sequence of chunk-size octets

CRLF         = CR LF    ; \r\n (0x0D 0x0A)
```

### RFC Evidence

**RFC 9112 §7.1** specifies CRLF in two positions per chunk:

> "chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF"

The second `CRLF` -- after `chunk-data` -- is the one violated by this test. A bare LF (0x0A) does not satisfy the `CRLF` production (which requires the two-byte sequence 0x0D 0x0A).

**RFC 9112 §2.2** provides the MAY-level robustness allowance:

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR."

This explicitly grants recipients the option to accept bare LF. Both strict rejection and lenient acceptance are valid.

**RFC 9112 §2.2** contrasts this with the bare CR rule:

> "A sender MUST NOT generate a bare CR (a CR character not immediately followed by LF) within any protocol elements other than the content. A recipient of such a bare CR MUST consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message."

The asymmetry is clear: bare LF gets MAY-accept treatment, while bare CR gets MUST-reject treatment.

### Step-by-Step ABNF Analysis

1. The parser reads `chunk-size` = `5` (valid), then CRLF (valid). It now expects `chunk-data`.
2. The parser reads exactly 5 bytes: `h`, `e`, `l`, `l`, `o`. This satisfies `chunk-data`.
3. The parser now expects `CRLF` (0x0D 0x0A) to terminate the chunk data.
4. The next byte is `\n` (0x0A) -- a bare LF without a preceding CR.
5. **Strict interpretation:** `\n` alone does not match the `CRLF` production. The parse fails.
6. **Lenient interpretation (per §2.2 MAY):** The parser recognizes bare LF as a line terminator. It treats `hello\n` as equivalent to `hello\r\n` and proceeds to the next chunk.
7. Under lenient parsing, the next bytes `0\r\n\r\n` form a valid last-chunk and trailer terminator. The message is complete.

### Real-World Smuggling Scenario

**Attack vector:** An attacker sends `5\r\nhello\n0\r\n\r\n`. A lenient parser treats the bare LF after `hello` as the chunk-data terminator and processes the message normally (5 bytes of body: `hello`). A strict parser does not recognize bare LF as CRLF. After reading 5 bytes (`hello`), it expects `\r\n` but gets `\n0`. The `\n` is not `\r`, so the parser considers the chunk framing broken.

**Byte-level desync:** The strict parser may interpret the stream differently -- for example, treating the `\n` as part of the next data segment, or closing the connection. If the strict parser is a back-end and the lenient parser is a front-end proxy, the front-end considers the message complete and moves on, while the back-end sees leftover bytes. On a keep-alive connection, those leftover bytes become the start of the "next" request -- enabling smuggling.

This bare-LF chunk data terminator vector is part of the "TERM" class of chunked smuggling techniques. It was demonstrated in practice in HAProxy + various back-end combinations, where HAProxy's lenient LF acceptance conflicted with stricter back-end parsers. CVE-2023-25725 (HAProxy) involved related chunked parsing discrepancies in the context of HTTP request smuggling.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
- [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
