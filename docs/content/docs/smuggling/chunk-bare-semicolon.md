---
title: "CHUNK-BARE-SEMICOLON"
description: "CHUNK-BARE-SEMICOLON test documentation"
weight: 18
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-BARE-SEMICOLON` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

Chunk size `5;` with a semicolon but no extension name.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5;\r\n
hello\r\n
0\r\n
\r\n
```

The chunk size line `5;` has a semicolon but no extension name after it.


## What the RFC says

> chunk-ext = *( BWS ";" BWS chunk-ext-name [ BWS "=" BWS chunk-ext-val ] )
>
> chunk-ext-name = token
>
> — RFC 9112 §7.1.1

The grammar requires a `chunk-ext-name` (which is a `token`, i.e., one or more `tchar` characters) after each semicolon. A bare semicolon with no extension name does not match the production and is therefore invalid.

## Why it matters

A bare semicolon can cause parser confusion about chunk boundaries. A lenient parser might skip the empty extension and parse the chunk normally, while a strict parser rejects the line. If these two parsers sit in sequence (front-end / back-end), they disagree on whether the message is valid, enabling request smuggling.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1 and §7.1.1)

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

**RFC 9112 §7.1.1** defines the chunk extension grammar:

> "chunk-ext = *( BWS ';' BWS chunk-ext-name [ BWS '=' BWS chunk-ext-val ] )"

This means after the semicolon delimiter and optional whitespace, a `chunk-ext-name` is **mandatory**. The `chunk-ext-name` is defined as `token`, which is `1*tchar` -- it requires at least one character.

**RFC 9112 §7.1.1** also states:

> "A recipient MUST ignore unrecognized chunk extensions."

This applies to well-formed but unknown extensions, not to syntactically invalid ones like a bare semicolon with no name.

**RFC 9112 §7.1.1** further notes:

> "A server ought to limit the total length of chunk extensions received in a request to an amount reasonable for the services provided, in the same way that it applies length limitations and timeouts for other parts of a message, and generate an appropriate 4xx (Client Error) response if that amount is exceeded."

### Step-by-Step ABNF Violation

1. The parser reads `chunk-size` and gets `5` (valid HEXDIG).
2. The parser encounters `;` -- this starts a `chunk-ext` production.
3. Inside `chunk-ext`, after the `;` and optional BWS, the parser expects `chunk-ext-name`, which is `token` = `1*tchar`.
4. The next character is `\r` (0x0D, start of CRLF). The character `\r` is not a `tchar` -- it is a control character.
5. A `token` requires **at least one** `tchar`. Zero `tchar` characters means the `chunk-ext-name` production fails.
6. Since the `chunk-ext` production cannot be satisfied, the entire `chunk` production fails. The message is syntactically invalid.

### Real-World Smuggling Scenario

A bare semicolon creates ambiguity in how parsers determine chunk boundaries:

**Attack vector:** A front-end proxy encounters `5;\r\n` and strips the empty extension, forwarding it as `5\r\n` followed by 5 bytes of chunk data. A back-end parser sees the raw `5;\r\n` and either (a) rejects it, causing the connection to desynchronize, or (b) interprets the semicolon differently -- some parsers treat the semicolon as the start of an extension and scan forward for the name, potentially consuming the CRLF and chunk data bytes as part of the extension name.

This type of chunk extension parsing ambiguity was documented in the PortSwigger research on HTTP request smuggling, where malformed chunk extensions caused front-end/back-end disagreements on message framing. CVE-2023-44487 and related HTTP/2-to-HTTP/1.1 downgrade issues demonstrated that chunk extension handling inconsistencies are a practical attack surface.

## Sources

- [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1)
