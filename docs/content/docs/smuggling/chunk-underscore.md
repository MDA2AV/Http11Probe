---
title: "CHUNK-UNDERSCORE"
description: "CHUNK-UNDERSCORE test documentation"
weight: 21
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-UNDERSCORE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

Chunk size `1_0` — with underscore separator.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
1_0\r\n
hello world!!!!!\r\n
0\r\n
\r\n
```

The chunk size `1_0` uses an underscore separator (like numeric literals in some languages).


## What the RFC says

> "The chunk-size field is a string of hex digits indicating the size of the chunk-data in octets." — RFC 9112 §7.1

The ABNF grammar is strict:

> `chunk-size = 1*HEXDIG`

The `HEXDIG` production (RFC 5234 §B.1) allows only the characters `0`-`9`, `A`-`F`, and `a`-`f`. An underscore (`_`) is not a HEXDIG, so `1_0` does not match the `chunk-size` rule.

> "A recipient MUST be able to parse and decode the chunked transfer coding." — RFC 9112 §7.1

## Why it matters

Languages like Python, Rust, Java, and C# accept `_` as a visual separator in numeric literals (e.g., `1_000_000`). If a server uses such a parser internally and interprets `1_0` as hex 16 (decimal), it reads 16 bytes of chunk data instead of 1. The strict parser sees chunk size `1` (stopping at the underscore), causing a framing disagreement that enables smuggling.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1)

```
chunked-body = *chunk last-chunk trailer-section CRLF

chunk        = chunk-size [ chunk-ext ] CRLF
               chunk-data CRLF
chunk-size   = 1*HEXDIG

HEXDIG       = DIGIT / "A" / "B" / "C" / "D" / "E" / "F"
DIGIT        = %x30-39   ; 0-9
```

### RFC Evidence

**RFC 9112 §7.1** defines chunk-size:

> "chunk-size = 1*HEXDIG"

The `HEXDIG` production (RFC 5234 §B.1) is an exhaustive list: `0`-`9`, `A`-`F`, `a`-`f`. The underscore character (`_`, 0x5F) is not in this list.

**RFC 9112 §7.1** mandates overflow protection, which implies strict numeric parsing:

> "A recipient MUST anticipate potentially large hexadecimal numerals and prevent parsing errors due to integer conversion overflows or precision loss due to integer conversion."

This requirement reinforces that chunk-size parsing must be implemented with careful numeric handling -- not delegated to general-purpose integer parsers that may accept non-HEXDIG characters.

**RFC 9112 §7.1** defines the chunk production:

> "chunk = chunk-size [ chunk-ext ] CRLF chunk-data CRLF"

After `chunk-size`, only a `chunk-ext` (starting with `;`) or `CRLF` is valid. An underscore matches neither.

### Step-by-Step ABNF Violation

1. The parser begins reading `chunk-size` = `1*HEXDIG`.
2. It reads `1` -- valid HEXDIG. Current chunk-size value: 0x1 = 1.
3. It reads `_` (0x5F) -- **not a HEXDIG**. The underscore is not in `0-9`, `A-F`, `a-f`.
4. The parser should stop reading `chunk-size` here. But what happens next depends on implementation:
   - **Strict parser:** After reading chunk-size `1`, it expects `;` or CRLF. `_` is neither. Parse failure -- reject with 400.
   - **Parser that stops at non-HEXDIG:** Reads chunk-size `1`, stops at `_`, then expects `;` or CRLF. `_` is neither. Parse failure.
   - **Language-aware parser (accepting `_` as digit separator):** Reads `1_0` as 0x10 = 16 decimal. It then reads 16 bytes of chunk data.
5. The intended chunk data is `hello world!!!!!` (16 bytes if interpreted as hex 0x10), but only 1 byte (`h`) if interpreted as hex 0x1. This creates a massive framing disagreement.

### Real-World Smuggling Scenario

**Attack vector:** An attacker sends `1_0\r\nhello world!!!!!\r\n0\r\n\r\n`. The key ambiguity:

- A strict parser reads chunk-size `1` (stopping at `_`), expects CRLF, finds `_` instead, and rejects.
- A parser that treats `_` as a digit separator reads chunk-size 0x10 = 16, reads 16 bytes (`hello world!!!!!`), and processes the message normally.
- A parser that reads `1` and then tolerates `_0` as a chunk-ext or ignores it reads only 1 byte of chunk data (`h`), then expects CRLF where `e` is -- causing further cascading misparse.

The underscore-as-digit-separator pattern is particularly insidious because it is valid syntax in Python (`0x1_0`), Rust (`0x1_0`), Java (`0x1_0`), C# (`0x1_0`), Ruby (`0x1_0`), and modern JavaScript (`0x1_0`). Any server that delegates chunk-size parsing to a standard library integer parser in these languages may silently accept `1_0` as 16. This was identified as a risk in HTTP parser security audits, where the recommendation is to use a purpose-built HEXDIG-only parser rather than general-purpose `parseInt()`-style functions.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
