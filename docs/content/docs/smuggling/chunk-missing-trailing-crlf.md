---
title: "CHUNK-MISSING-TRAILING-CRLF"
description: "CHUNK-MISSING-TRAILING-CRLF test documentation"
weight: 29
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-MISSING-TRAILING-CRLF` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

Chunk data without the required trailing CRLF after data.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello0\r\n
\r\n
```

The chunk data `hello` is not followed by `\r\n` — the `0` terminator runs directly into the chunk data, reading as `hello0`.


## What the RFC says

> "The chunk-size field is a string of hex digits indicating the size of the chunk-data in octets." — RFC 9112 §7.1

> "A recipient MUST be able to parse and decode the chunked transfer coding." — RFC 9112 §7.1

The ABNF grammar specifies:

> `chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF`

The grammar requires two CRLFs per chunk: one after the chunk-size line and one after the chunk-data. The trailing CRLF after chunk-data is mandatory. When it is missing, the bytes of the next chunk (here `0\r\n`) are consumed as chunk data, corrupting the framing.

## Why it matters

Without the trailing CRLF, the parser reads past the declared chunk data into the next chunk-size line. A strict parser detects the missing CRLF and rejects the message, while a lenient parser may silently absorb the extra bytes as data. This disagreement on chunk boundaries between a front-end and back-end enables request smuggling.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1)

```
chunked-body = *chunk last-chunk trailer-section CRLF

chunk        = chunk-size [ chunk-ext ] CRLF
               chunk-data CRLF
chunk-size   = 1*HEXDIG
chunk-data   = 1*OCTET ; a sequence of chunk-size octets
last-chunk   = 1*("0") [ chunk-ext ] CRLF
```

### RFC Evidence

**RFC 9112 §7.1** specifies the `chunk` production with two mandatory CRLFs:

> "chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF"

The second CRLF (after `chunk-data`) is the trailing terminator. Without it, the parser cannot determine where chunk-data ends and the next chunk-size begins.

**RFC 9112 §7.1** defines chunk-data:

> "chunk-data = 1*OCTET ; a sequence of chunk-size octets"

The parser reads exactly `chunk-size` octets, then expects CRLF. This is a length-delimited protocol -- the CRLF after chunk-data serves as a framing verification marker.

**RFC 9112 §7** establishes the decoding obligation:

> "A recipient MUST be able to parse and decode the chunked transfer coding."

If the chunked framing is violated (missing CRLF), the recipient cannot successfully decode the transfer coding.

### Step-by-Step ABNF Violation

1. The parser reads `chunk-size` = `5` (valid HEXDIG), then CRLF (valid).
2. The parser reads exactly 5 bytes of `chunk-data`: `h`, `e`, `l`, `l`, `o`.
3. The parser now expects CRLF (0x0D 0x0A) as the trailing chunk-data terminator.
4. The next bytes are `0\r\n` -- the intended last-chunk. But `0` (0x30) is not `\r` (0x0D).
5. The parser expected `\r` but got `0`. The trailing CRLF is missing. The `chunk` production fails.
6. From the wire perspective, the bytes are: `5\r\nhello0\r\n\r\n`. The `0` that was intended as the last-chunk is consumed into the data stream because it appears where the CRLF should be.

### Real-World Smuggling Scenario

**Attack vector:** An attacker sends a chunk declaring size 5, immediately followed by `hello0\r\n\r\n` (no CRLF between `hello` and `0`). A strict parser reads 5 bytes (`hello`), expects CRLF, finds `0` instead, and rejects the message. A lenient parser might:

- Read the `0` as the 6th byte, expecting CRLF at offset 6 -- it finds `\r\n`, accepts it, and then reads `\r\n` as the next chunk-size line (empty/invalid).
- Or read 5 bytes (`hello`), skip the CRLF check, see `0` as the next chunk-size (last-chunk), and process the message as valid.

In a proxy chain, the strict front-end rejects the request, but if the raw bytes somehow reach the back-end (e.g., via a non-parsing proxy), the lenient back-end processes a different message boundary than intended. The bytes after what the back-end considers the message end become the start of the next request.

This missing-CRLF confusion is a fundamental chunked framing violation. The Apache HTTP Server had CVE-2015-3183, where improper chunk parsing allowed request smuggling via malformed chunk boundaries. Similarly, research on chunked encoding edge cases has consistently demonstrated that CRLF verification failures between proxies and back-ends remain a practical smuggling vector.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
