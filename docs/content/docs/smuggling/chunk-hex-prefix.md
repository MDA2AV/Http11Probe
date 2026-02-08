---
title: "CHUNK-HEX-PREFIX"
description: "CHUNK-HEX-PREFIX test documentation"
weight: 25
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-HEX-PREFIX` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

Chunk size `0x5` — with C-style hex prefix.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
0x5\r\n
hello\r\n
0\r\n
\r\n
```

The chunk size uses a `0x` hex prefix.


## What the RFC says

> "The chunk-size field is a string of hex digits indicating the size of the chunk-data in octets." — RFC 9112 §7.1

The ABNF grammar is strict:

> `chunk-size = 1*HEXDIG`

The grammar allows only hexadecimal digits (`0`-`9`, `A`-`F`, `a`-`f`). The `0x` prefix is not part of the `HEXDIG` production (RFC 5234 §B.1), so `0x5` is invalid: the parser encounters `x` where it expects either another HEXDIG, a chunk extension semicolon, or CRLF.

> "A recipient MUST be able to parse and decode the chunked transfer coding." — RFC 9112 §7.1

## Why it matters

`0x5` is valid hex notation in C, Python, and many other languages, but invalid in HTTP chunked encoding. If a server uses a general-purpose hex parser that accepts the `0x` prefix, it reads chunk size 5 -- but a strict parser sees `0` as chunk size zero (the last-chunk), ending the message prematurely. This disagreement on message boundaries enables desynchronization.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1)

```
chunked-body = *chunk last-chunk trailer-section CRLF

chunk        = chunk-size [ chunk-ext ] CRLF
               chunk-data CRLF
chunk-size   = 1*HEXDIG
last-chunk   = 1*("0") [ chunk-ext ] CRLF

HEXDIG       = DIGIT / "A" / "B" / "C" / "D" / "E" / "F"
               ; case-insensitive per RFC 5234
```

### RFC Evidence

**RFC 9112 §7.1** defines the chunk-size production:

> "chunk-size = 1*HEXDIG"

The `HEXDIG` rule is defined in RFC 5234 §B.1 and allows only the characters `0`-`9`, `A`-`F`, and `a`-`f`. No prefix notation is permitted.

**RFC 9112 §7.1** also mandates overflow protection:

> "A recipient MUST anticipate potentially large hexadecimal numerals and prevent parsing errors due to integer conversion overflows or precision loss due to integer conversion."

This requirement reinforces that the chunk-size is a raw hexadecimal numeral -- not a programming language literal with prefix notation.

**RFC 9112 §7.1** defines the last-chunk:

> "last-chunk = 1*('0') [ chunk-ext ] CRLF"

This is important because a parser that stops at the first non-HEXDIG character in `0x5` would read `0` as the chunk-size, matching the `last-chunk` production and prematurely ending the chunked body.

### Step-by-Step ABNF Violation

1. The parser begins reading `chunk-size` = `1*HEXDIG`.
2. It reads `0` -- valid HEXDIG. Current chunk-size value: 0.
3. It reads `x` -- **not a HEXDIG**. The character `x` is not in `0-9`, `A-F`, or `a-f`.
4. At this point, the parser has two options depending on implementation:
   - **Strict parser:** Expects either another HEXDIG, a `;` (chunk-ext), or CRLF after the chunk-size. `x` matches none of these. Parse failure -- reject with 400.
   - **Lenient parser (stops at non-HEXDIG):** Accepts chunk-size `0`, which matches `last-chunk`. The chunked body ends immediately. The remaining bytes `x5\r\nhello\r\n0\r\n\r\n` are left in the connection buffer.
   - **Lenient parser (accepts 0x prefix):** Reads `0x5` as hexadecimal 5, consuming 5 bytes of chunk data.
5. In all cases, the `0x` prefix violates the ABNF because `x` is not HEXDIG and the grammar has no provision for prefix notation.

### Real-World Smuggling Scenario

**Attack vector:** An attacker sends `0x5\r\nhello\r\n0\r\n\r\n`. A parser using a general-purpose hex conversion function (like C's `strtol()` with base 16, or Python's `int("0x5", 16)`) accepts the `0x` prefix and reads chunk-size 5. A strict parser reads chunk-size `0` (stopping at `x`), treats it as the last-chunk, and considers the chunked body complete. The remaining bytes `x5\r\nhello\r\n0\r\n\r\n` are left in the TCP buffer and prepended to the next HTTP request on that connection -- this is request smuggling.

This is particularly dangerous because the `0x` prefix is the universal hex notation in C, Python, JavaScript, Java, and most other languages. Library functions like `strtol()`, `parseInt()`, and `int()` all accept it by default, making it a likely implementation error when a developer uses a standard library integer parser instead of a purpose-built HEXDIG-only parser.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
