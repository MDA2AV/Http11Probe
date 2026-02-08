---
title: "CHUNK-LEADING-SP"
description: "CHUNK-LEADING-SP test documentation"
weight: 28
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-LEADING-SP` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

Chunk size ` 5` — with leading space.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
 5\r\n
hello\r\n
0\r\n
\r\n
```

The chunk size ` 5` has a leading space.


## What the RFC says

> "The chunk-size field is a string of hex digits indicating the size of the chunk-data in octets." — RFC 9112 §7.1

> "A recipient MUST be able to parse and decode the chunked transfer coding." — RFC 9112 §7.1

The ABNF grammar is:

> `chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF`
>
> `chunk-size = 1*HEXDIG`

The chunk line begins directly with `chunk-size`, which is `1*HEXDIG`. There is no optional whitespace (OWS or BWS) before the chunk size in the grammar. A leading space character (`0x20`) is not a HEXDIG and does not match any production at that position.

## Why it matters

Leading whitespace in chunk sizes can cause parser disagreements. A lenient parser might strip the space and parse `5` as the chunk size, while a strict parser fails on the leading space. If a front-end tolerates the space and a back-end does not, the back-end sees different message boundaries -- enabling smuggling.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1)

```
chunked-body = *chunk last-chunk trailer-section CRLF

chunk        = chunk-size [ chunk-ext ] CRLF
               chunk-data CRLF
chunk-size   = 1*HEXDIG
last-chunk   = 1*("0") [ chunk-ext ] CRLF
```

### RFC Evidence

**RFC 9112 §7.1** defines the chunk production:

> "chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF"

The `chunk` production begins directly with `chunk-size`. There is no optional whitespace (OWS, BWS, or SP) before `chunk-size` in the grammar.

**RFC 9112 §7.1** defines chunk-size:

> "chunk-size = 1*HEXDIG"

The `HEXDIG` production allows only `0`-`9`, `A`-`F`, `a`-`f`. Space (0x20) is not a HEXDIG.

**RFC 9112 §7.1.1** uses BWS (bad whitespace) only **within** chunk extensions, not before the chunk-size:

> "chunk-ext = *( BWS ';' BWS chunk-ext-name [ BWS '=' BWS chunk-ext-val ] )"

This is a deliberate design: BWS is permitted around the semicolons and equals signs inside extensions, but the start of each chunk line has no whitespace allowance.

### Step-by-Step ABNF Violation

1. After the preceding chunk's trailing CRLF (or the headers' CRLF for the first chunk), the parser expects the start of a new `chunk` production.
2. A `chunk` begins with `chunk-size` = `1*HEXDIG`.
3. The first byte is SP (0x20, space). SP is not a HEXDIG.
4. The `1*HEXDIG` production requires at least one HEXDIG as the first character. The space character fails this requirement immediately.
5. No ABNF production at this position in the grammar permits whitespace. The parse fails.
6. A conforming parser must reject the message because the chunked body cannot be decoded.

### Real-World Smuggling Scenario

**Attack vector:** An attacker sends ` 5\r\nhello\r\n0\r\n\r\n` (note the leading space before `5`). A lenient parser that strips leading whitespace (a common behavior in general-purpose line parsers) reads chunk-size 5 and processes the chunk normally. A strict parser fails on the leading space because it does not match `HEXDIG`.

This is exploitable in proxy chains: if the front-end strips the space and forwards the request with chunked body intact, but the back-end applies different whitespace handling (or rejects the message), the two parsers disagree on message boundaries.

Leading whitespace tolerance in chunk-size parsing was identified as a real-world issue in HTTP server implementations. For example, Node.js's llhttp parser had multiple CVEs related to lenient whitespace handling in HTTP parsing (CVE-2022-32213, CVE-2022-32214), where extra whitespace in framing-critical positions caused parser disagreements.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
