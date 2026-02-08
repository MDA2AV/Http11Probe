---
title: "CHUNK-EXT-LF"
description: "CHUNK-EXT-LF test documentation"
weight: 25
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-EXT-LF` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) |
| **Requirement** | MAY accept bare LF |
| **Expected** | `400` or `2xx` |

## What it sends

A chunked request where the chunk extension area contains a bare `LF` instead of `CRLF`: `5;\nhello`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5;\n
hello\r\n
0\r\n
\r\n
```

The chunk size line `5;` is terminated with bare LF (`\n`) instead of CRLF.


## What the RFC says

The chunk line grammar requires CRLF as the terminator:

> chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF
>
> — RFC 9112 §7.1

However, RFC 9112 §2.2 provides a MAY-level allowance:

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR."
>
> — RFC 9112 §2.2

This means a server MAY accept bare LF -- both strict rejection and lenient acceptance are RFC-compliant.

## Why this test is unscored

The MAY-level allowance for bare LF in RFC 9112 Section 2.2 means both strict rejection and lenient acceptance are RFC-compliant behaviors. Neither response can be considered wrong, so the test cannot be scored.

**Pass:** Server rejects with `400` (strict CRLF enforcement, safest behavior).
**Warn:** Server accepts and responds `2xx` (RFC-valid per Section 2.2 MAY-level bare LF acceptance).

## Why it matters

This is the **TERM.EXT** vector from chunked encoding research. If a parser accepts bare LF in chunk extensions, it may parse chunk boundaries differently from a strict parser, enabling desynchronization and smuggling.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1 and §2.2)

```
chunked-body = *chunk last-chunk trailer-section CRLF

chunk        = chunk-size [ chunk-ext ] CRLF
               chunk-data CRLF
chunk-size   = 1*HEXDIG

chunk-ext      = *( BWS ";" BWS chunk-ext-name
                    [ BWS "=" BWS chunk-ext-val ] )
chunk-ext-name = token
chunk-ext-val  = token / quoted-string
```

### RFC Evidence

**RFC 9112 §7.1** defines the chunk production with CRLF terminators:

> "chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF"

The `CRLF` production is defined in RFC 5234 §B.1 as the two-byte sequence `\r\n` (0x0D 0x0A). A bare LF (0x0A alone) does not match `CRLF`.

**RFC 9112 §2.2** provides a MAY-level robustness allowance:

> "Although the line terminator for the start-line and fields is the sequence CRLF, a recipient MAY recognize a single LF as a line terminator and ignore any preceding CR."

This MAY language means recipients are permitted but not required to accept bare LF. Both strict rejection and lenient acceptance are RFC-compliant.

**RFC 9112 §2.2** draws an explicit asymmetry between bare LF and bare CR:

> "A sender MUST NOT generate a bare CR (a CR character not immediately followed by LF) within any protocol elements other than the content. A recipient of such a bare CR MUST consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message."

Bare LF gets a MAY-accept allowance; bare CR gets a MUST-reject requirement. This test targets the bare LF case.

### Step-by-Step ABNF Analysis

1. The parser reads `chunk-size` = `5` (valid HEXDIG).
2. The parser encounters `;` -- this starts a `chunk-ext` production.
3. After the semicolon, the parser expects `chunk-ext-name` (a `token`). But the next byte is `\n` (0x0A).
4. `\n` is not a `tchar`, so it cannot begin a `token`. The `chunk-ext-name` production fails.
5. However, per §2.2 MAY language, a recipient MAY recognize bare LF as a line terminator. If the parser does so, it effectively treats `5;\n` as `5;\r\n` -- a chunk-size line with an empty/absent extension terminated by a line ending.
6. Under strict parsing: the bare LF is not CRLF, and the extension name after `;` is missing. The parse fails at two levels.
7. Under lenient parsing: the bare LF acts as a line terminator, the bare semicolon may be ignored, and the parser reads 5 bytes of chunk data.

### Real-World Smuggling Scenario

This is the **TERM.EXT** vector from chunked encoding smuggling research:

**Attack vector:** An attacker sends `5;\nhello\r\n0\r\n\r\n`. A lenient front-end proxy that accepts bare LF as a line terminator parses this as: chunk-size 5, skip empty extension, read `hello` as chunk data, then read the `0\r\n\r\n` terminator. It forwards the processed request to the back-end. A strict back-end that requires CRLF sees the bare LF differently -- it may interpret `;\nhello\r\n` as a malformed extension containing raw bytes, fail to find the CRLF terminator at the expected position, and reject or misparse the message.

This exact technique was demonstrated in research by James Kettle on HTTP/2 downgrade smuggling, where HTTP/2 front-ends converted requests to HTTP/1.1 with bare LF terminators that back-end servers interpreted differently. The bare-LF-in-extension variant specifically exploits the fact that the §2.2 MAY language creates implementation-defined behavior, guaranteeing disagreement between strict and lenient parsers.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
- [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1)
- [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
