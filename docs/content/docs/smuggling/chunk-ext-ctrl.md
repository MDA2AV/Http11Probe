---
title: "CHUNK-EXT-CTRL"
description: "CHUNK-EXT-CTRL test documentation"
weight: 28
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-EXT-CTRL` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

A chunked request with a NUL byte (`0x00`) embedded in the chunk extension: `5;\x00ext`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5;\x00ext\r\n
hello\r\n
0\r\n
\r\n
```

The chunk extension contains a NUL byte (`\x00`) before `ext`.


## What the RFC says

> chunk-ext = *( BWS ";" BWS chunk-ext-name [ BWS "=" BWS chunk-ext-val ] )
>
> chunk-ext-name = token
>
> chunk-ext-val = token / quoted-string
>
> — RFC 9112 §7.1.1

A `token` is defined as `1*tchar`, where `tchar` only includes visible ASCII characters and a limited set of symbols (RFC 9110 §5.6.2). NUL (`0x00`) and other control characters (except HTAB in specific contexts) are not valid `tchar` characters and therefore cannot appear in a chunk extension name or unquoted value.

## Why it matters

NUL bytes in chunk extensions can cause parsers to truncate or misinterpret the extension, leading to disagreements about chunk boundaries. C-based string functions often treat NUL as a string terminator, creating divergent behavior between parsers.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1 and §7.1.1, RFC 9110 §5.6.2)

```
chunk        = chunk-size [ chunk-ext ] CRLF
               chunk-data CRLF
chunk-size   = 1*HEXDIG

chunk-ext      = *( BWS ";" BWS chunk-ext-name
                    [ BWS "=" BWS chunk-ext-val ] )
chunk-ext-name = token
chunk-ext-val  = token / quoted-string

token          = 1*tchar
tchar          = "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+"
               / "-" / "." / "^" / "_" / "`" / "|" / "~"
               / DIGIT / ALPHA
```

### RFC Evidence

**RFC 9110 §5.6.2** defines `token` as `1*tchar`, where `tchar` is an exhaustive list of permitted characters:

> "tchar = '!' / '#' / '$' / '%' / '&' / ''' / '*' / '+' / '-' / '.' / '^' / '_' / '`' / '|' / '~' / DIGIT / ALPHA"

NUL (0x00) is not in this list. No control characters other than HTAB are valid in HTTP token positions.

**RFC 9112 §7.1.1** specifies:

> "chunk-ext-name = token"

And provides context:

> "A recipient MUST ignore unrecognized chunk extensions."

This "ignore" directive applies to syntactically valid extensions with unrecognized names, not to extensions containing illegal characters.

**RFC 9112 §7.1.1** also states:

> "A server ought to limit the total length of chunk extensions received in a request to an amount reasonable for the services provided, in the same way that it applies length limitations and timeouts for other parts of a message, and generate an appropriate 4xx (Client Error) response if that amount is exceeded."

### Step-by-Step ABNF Violation

1. The parser reads `chunk-size` = `5` (valid HEXDIG).
2. The parser encounters `;` -- this starts a `chunk-ext` production.
3. After `;` and optional BWS, the parser expects `chunk-ext-name` = `token` = `1*tchar`.
4. The next byte is `\x00` (NUL). NUL is not a `tchar` -- it is not in the exhaustive list of permitted characters.
5. Since the first character fails to match `tchar`, the `token` production requires at least one `tchar`, so `chunk-ext-name` fails.
6. The `chunk-ext` production cannot be satisfied, and the entire `chunk` production is invalid.

### Real-World Smuggling Scenario

NUL bytes are particularly dangerous because of how C-based parsers handle them:

**Attack vector:** An attacker sends `5;\x00ext\r\n`. A C-based parser using `strlen()` or similar string functions may treat the NUL byte as a string terminator, seeing only `5;` (effectively a bare semicolon). It might then ignore the truncated extension and parse the chunk normally. Meanwhile, a parser that processes the raw byte stream sees the NUL as an invalid character and rejects the message. This disagreement enables desynchronization.

**Memory safety implications:** NUL bytes in unexpected positions have historically caused buffer over-reads and information disclosure in HTTP servers. CVE-2019-5482 (curl) involved NUL byte handling in TFTP URLs, and similar NUL injection issues have been found in HTTP parsers where control characters cause truncation or bypass validation logic.

The broader class of control-character injection in chunk extensions was identified as a smuggling vector in chunked encoding research, where parsers disagree on whether to reject, truncate, or pass through control characters in extension fields.

## Sources

- [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1)
- [RFC 9110 §5.6.2](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.2)
