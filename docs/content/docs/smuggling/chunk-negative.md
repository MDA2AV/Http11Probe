---
title: "CHUNK-NEGATIVE"
description: "CHUNK-NEGATIVE test documentation"
weight: 31
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-NEGATIVE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

A chunked request with a negative chunk size: `-1\r\nhello\r\n0\r\n\r\n`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
-1\r\n
hello\r\n
0\r\n
\r\n
```

The chunk size `-1` is negative.


## What the RFC says

> chunk-size = 1*HEXDIG
>
> — RFC 9112 §7.1

The `HEXDIG` production (RFC 5234 §B.1) allows only the characters `0`-`9`, `A`-`F`, and `a`-`f`. A minus sign (`-`) is not a HEXDIG, so `-1` does not match the `chunk-size` rule. Additionally, the RFC requires implementations to guard against overflow:

> "...recipients MUST anticipate potentially large hexadecimal numerals and prevent parsing errors due to integer conversion overflows or precision loss due to integer representation."
>
> — RFC 9112 §7.1

## Why it matters

A parser that interprets `-1` as a signed integer may wrap it to a very large unsigned value, causing it to read far beyond the actual data. This can lead to buffer over-reads, denial of service, or desynchronization with a stricter parser that rejects the negative value.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1)

```
chunked-body = *chunk last-chunk trailer-section CRLF

chunk        = chunk-size [ chunk-ext ] CRLF
               chunk-data CRLF
chunk-size   = 1*HEXDIG
chunk-data   = 1*OCTET ; a sequence of chunk-size octets
last-chunk   = 1*("0") [ chunk-ext ] CRLF

HEXDIG       = DIGIT / "A" / "B" / "C" / "D" / "E" / "F"
DIGIT        = %x30-39   ; 0-9
```

### RFC Evidence

**RFC 9112 §7.1** defines chunk-size:

> "chunk-size = 1*HEXDIG"

The `HEXDIG` production (RFC 5234 §B.1) permits only the characters `0`-`9`, `A`-`F`, and `a`-`f`. The minus sign (`-`, 0x2D) is not a HEXDIG.

**RFC 9112 §7.1** mandates overflow protection:

> "A recipient MUST anticipate potentially large hexadecimal numerals and prevent parsing errors due to integer conversion overflows or precision loss due to integer conversion."

This requirement specifically addresses integer parsing safety. A negative sign introduces signed integer semantics that the ABNF never permits -- the chunk-size is an unsigned hexadecimal numeral by definition.

**RFC 9112 §7** establishes the decoding obligation:

> "A recipient MUST be able to parse and decode the chunked transfer coding."

If the chunk-size cannot be parsed according to the ABNF, the chunked coding cannot be decoded, and the message must be rejected.

### Step-by-Step ABNF Violation

1. The parser begins reading `chunk-size` = `1*HEXDIG`.
2. The first byte is `-` (0x2D, minus sign).
3. `-` is not a HEXDIG. The `1*HEXDIG` production requires at least one HEXDIG as the very first character.
4. The production fails immediately at the first character. No valid chunk-size can be read.
5. Since the `chunk` production fails, the entire `chunked-body` is invalid.

### Real-World Smuggling Scenario

**Attack vector:** An attacker sends `-1\r\nhello\r\n0\r\n\r\n`. A parser using a signed integer conversion function (like C's `strtol()` or `atoi()`) may interpret `-1` as the signed integer -1. When cast to an unsigned type (e.g., `size_t` on a 64-bit system), -1 becomes `0xFFFFFFFFFFFFFFFF` (18,446,744,073,709,551,615 bytes). The parser then attempts to read that many bytes of chunk data, causing:

- **Buffer over-read:** The parser reads far beyond the actual data, potentially exposing memory contents from adjacent buffers (similar to Heartbleed-style information disclosure).
- **Denial of service:** The parser hangs waiting for approximately 18 exabytes of data that will never arrive.
- **Desynchronization:** If the parser reads some fixed buffer amount and then fails, leftover bytes corrupt the connection state.

A strict parser rejects `-1` immediately because `-` is not HEXDIG. If a strict front-end rejects but a lenient back-end somehow receives the raw bytes (e.g., via a non-parsing L4 proxy), the back-end may exhibit undefined behavior.

Integer signedness issues in HTTP chunk size parsing have been a recurring vulnerability class. CVE-2015-3183 (Apache httpd) involved chunk parsing errors that enabled smuggling. The signed/unsigned integer confusion in chunk sizes is a well-documented anti-pattern in HTTP security research, and the explicit `1*HEXDIG` grammar (excluding `-`) is intentionally designed to prevent it.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
