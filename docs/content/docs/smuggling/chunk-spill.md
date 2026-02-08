---
title: "CHUNK-SPILL"
description: "CHUNK-SPILL test documentation"
weight: 26
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-SPILL` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

A chunked request that declares chunk size `5` but sends 7 bytes of data (`hello!!`), followed by the terminator.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello!!\r\n
0\r\n
\r\n
```

The chunk size declares 5 bytes but the data is `hello!!` (7 bytes).


## What the RFC says

> "The chunk-size field is a string of hex digits indicating the size of the chunk-data in octets." — RFC 9112 §7.1

> "A recipient MUST be able to parse and decode the chunked transfer coding." — RFC 9112 §7.1

The ABNF grammar specifies:

> `chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF`
>
> `chunk-data = 1*OCTET ; a sequence of chunk-size octets`

The grammar specifies that `chunk-data` is exactly `chunk-size` octets, followed by CRLF. A conforming parser reads exactly that many bytes, then expects `\r\n`. When more data is sent than declared (7 bytes vs. 5), the excess bytes (`!!`) land where the CRLF terminator should be, violating the grammar.

## Why it matters

An oversized chunk is a framing violation. If a lenient parser reads past the declared size, it desynchronizes from a strict parser that reads exactly `chunk-size` bytes. This discrepancy is exploitable for smuggling the excess bytes as part of the next request.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1)

```
chunked-body = *chunk last-chunk trailer-section CRLF

chunk        = chunk-size [ chunk-ext ] CRLF
               chunk-data CRLF
chunk-size   = 1*HEXDIG
chunk-data   = 1*OCTET ; a sequence of chunk-size octets
```

### RFC Evidence

**RFC 9112 §7.1** defines `chunk-data`:

> "chunk-data = 1*OCTET ; a sequence of chunk-size octets"

The comment "a sequence of chunk-size octets" is normative context: the parser MUST read exactly `chunk-size` octets, no more and no fewer. After those octets, a CRLF must follow.

**RFC 9112 §7.1** defines the full chunk production:

> "chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF"

After reading `chunk-size` octets of data, the parser expects CRLF. If the bytes at position `chunk-size + 1` and `chunk-size + 2` (relative to the start of chunk-data) are not `\r\n`, the framing is violated.

**RFC 9112 §7** establishes the parsing obligation:

> "A recipient MUST be able to parse and decode the chunked transfer coding."

Successful decoding requires that the declared chunk-size matches the actual data length. An oversized chunk makes decoding impossible without either truncating or absorbing extra bytes.

### Step-by-Step ABNF Violation

1. The parser reads `chunk-size` = `5` (valid HEXDIG, value 5), then CRLF (valid).
2. The parser reads exactly 5 bytes of `chunk-data`: `h`, `e`, `l`, `l`, `o`.
3. The parser now expects CRLF (0x0D 0x0A) at bytes 6-7 of the data segment.
4. Byte 6 is `!` (0x21) and byte 7 is `!` (0x21). These are not `\r\n`.
5. The trailing CRLF check fails. The `chunk` production is violated.
6. The actual data sent is `hello!!` (7 bytes), but only 5 were declared. The 2 excess bytes (`!!`) occupy the position where CRLF should be.

### Real-World Smuggling Scenario

**Attack vector:** An attacker sends a chunk declaring size 5 but containing 7 bytes (`hello!!`). The byte layout is:

```
5\r\n           -- chunk-size line
hello!!\r\n     -- 7 bytes + CRLF (but chunk-size said 5)
0\r\n\r\n       -- last-chunk + trailer terminator
```

A strict parser reads 5 bytes (`hello`), then expects CRLF at position 6. It finds `!!` instead and rejects the message. A lenient parser might:

- Read until it finds CRLF, effectively accepting `hello!!` (7 bytes) as chunk data despite the size mismatch. It then continues parsing normally.
- Read 5 bytes, find `!!` instead of CRLF, and try error recovery by scanning forward for CRLF.

In a proxy chain, the lenient front-end accepts the 7-byte chunk, but if it re-encodes the body for the back-end (e.g., recalculating chunk-size), the back-end sees a different message. Alternatively, if the front-end passes through raw bytes, the back-end reads 5 bytes and then encounters `!!` where it expects a new chunk-size line -- potentially interpreting `!!` as the start of a new chunk, corrupting the request stream.

Chunk size/data length mismatches are a core technique in HTTP smuggling. CVE-2015-3183 (Apache httpd) exploited improper chunk size validation to desynchronize front-end and back-end parsers. Research on chunk-spill attacks has demonstrated that oversized chunks are a practical vector for smuggling arbitrary data past proxy validation.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
