---
title: "CHUNKED-HEX-UPPERCASE"
description: "CHUNKED-HEX-UPPERCASE test documentation"
weight: 12
---

| | |
|---|---|
| **Test ID** | `COMP-CHUNKED-HEX-UPPERCASE` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` |

## What it sends

A valid chunked POST where the chunk size is expressed using an uppercase hexadecimal digit: `A` (which equals 10 in decimal), followed by exactly 10 bytes of data.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
A\r\n
helloworld\r\n
0\r\n
\r\n
```

The chunk size `A` is uppercase hex for 10. The chunk data `helloworld` is exactly 10 bytes.

## What the RFC says

> "A recipient MUST be able to parse and decode the chunked transfer coding." — RFC 9112 Section 7.1

The chunked grammar defines `chunk-size = 1*HEXDIG`. `HEXDIG` is defined in RFC 5234 (ABNF) as `DIGIT / "A" / "B" / "C" / "D" / "E" / "F"`, and ABNF string matching is case-insensitive by definition. Both `a` and `A` represent the decimal value 10. A compliant chunked parser must accept hex digits in any case.

> "Recipients MUST anticipate potentially large hexadecimal numerals and prevent parsing errors due to integer conversion overflows or precision loss due to integer representation." — RFC 9112 Section 7.1

## Why it matters

While most chunk sizes in practice are small decimal numbers (like `5` or `1a`), the grammar allows any combination of uppercase and lowercase hex digits. A parser that only handles lowercase hex, or only decimal digits, will fail on legitimate chunked bodies. This is a basic interoperability requirement for any HTTP/1.1 implementation.

## Deep Analysis

### Relevant ABNF grammar

From RFC 9112 Section 7.1:

```
chunk-size     = 1*HEXDIG
```

From RFC 5234 Appendix B.1 (Core ABNF):

```
HEXDIG         = DIGIT / "A" / "B" / "C" / "D" / "E" / "F"
DIGIT          = %x30-39  ; 0-9
```

Note: RFC 5234 Section 2.3 states that ABNF strings are case-insensitive. The HEXDIG definition listing uppercase `"A"` through `"F"` implicitly includes `"a"` through `"f"`.

### Direct RFC quotes

> "A recipient MUST be able to parse and decode the chunked transfer coding." -- RFC 9112 Section 7.1

> "Recipients MUST anticipate potentially large hexadecimal numerals and prevent parsing errors due to integer conversion overflows or precision loss due to integer representation." -- RFC 9112 Section 7.1

> "HEXDIG = DIGIT / "A" / "B" / "C" / "D" / "E" / "F"" -- RFC 5234 Appendix B.1

### Chain of reasoning

1. The test sends chunk-size `A\r\n` followed by exactly 10 bytes of data (`helloworld`).
2. The `chunk-size` ABNF production is `1*HEXDIG`, requiring one or more hexadecimal digits.
3. `HEXDIG` is defined in RFC 5234 as `DIGIT / "A" / "B" / "C" / "D" / "E" / "F"`. Per RFC 5234 Section 2.3, ABNF string comparison is case-insensitive, so both `A` and `a` are valid HEXDIG values.
4. `A` in hexadecimal equals 10 in decimal. The test provides exactly 10 bytes of chunk-data, satisfying the `chunk-data = 1*OCTET` production with the correct length.
5. The `0\r\n\r\n` terminator satisfies `last-chunk` and the trailing CRLF.
6. The entire message is a valid `chunked-body`. The MUST requirement to "parse and decode" chunked encoding necessarily includes correctly interpreting hex digits of any case.

### Scored / Unscored justification

**Scored.** The MUST requirement ("A recipient MUST be able to parse and decode the chunked transfer coding") encompasses correct hex parsing. Since `chunk-size = 1*HEXDIG` and HEXDIG is case-insensitive by ABNF rules, rejecting uppercase hex is a failure to parse valid chunked encoding. There is no SHOULD or MAY ambiguity -- the grammar is unambiguous and the requirement is MUST-level.

### Edge cases

- Some implementations use `strtol()` or equivalent with base 16, which naturally handles both cases. These pass without issue.
- Implementations that use a hand-rolled hex parser with only `0-9` and `a-f` ranges (missing `A-F`) will fail this test. This is a common bug in minimal HTTP parsers.
- Mixed-case chunk sizes like `1a`, `1A`, `1b3F` are all equally valid per HEXDIG case-insensitivity. This test uses pure uppercase to catch the most common parser limitation.
- The RFC also warns about large hex numerals causing integer overflow. While this test uses a small value (`A` = 10), the parser must be robust against both case variation and large values.

## Sources

- [RFC 9112 §7.1 -- Chunked Transfer Coding](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
- [RFC 5234 -- ABNF (HEXDIG definition)](https://www.rfc-editor.org/rfc/rfc5234#appendix-B.1)
