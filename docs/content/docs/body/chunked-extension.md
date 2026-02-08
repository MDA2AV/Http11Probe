---
title: "CHUNKED-EXTENSION"
description: "CHUNKED-EXTENSION test documentation"
weight: 10
---

| | |
|---|---|
| **Test ID** | `COMP-CHUNKED-EXTENSION` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) |
| **Requirement** | MUST ignore unrecognized extensions |
| **Expected** | `2xx` or `400` |

## What it sends

A chunked POST where the chunk size line includes a valid extension: `5;ext=value`.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Transfer-Encoding: chunked\r\n
\r\n
5;ext=value\r\n
hello\r\n
0\r\n
\r\n
```

## What the RFC says

> "The chunked coding allows each chunk to include zero or more chunk extensions, immediately following the chunk-size, for the sake of supplying per-chunk metadata (such as a signature or hash), mid-message control information, or randomization of message body size." — RFC 9112 Section 7.1.1

> "A recipient MUST ignore unrecognized chunk extensions." — RFC 9112 Section 7.1.1

> "A server ought to limit the total length of chunk extensions received in a request to an amount reasonable for the services provided, in the same way that it applies length limitations and timeouts for other parts of a message, and generate an appropriate 4xx (Client Error) response if that amount is exceeded." — RFC 9112 Section 7.1.1

Chunk extensions are part of the chunked encoding grammar. A compliant parser must ignore unrecognized extensions and process the chunk data normally.

## Why it matters

While chunk extensions are rarely used in practice, they are syntactically valid. A server that rejects them has an overly strict chunk parser that may break with legitimate clients or proxies that add extensions for metadata (e.g., checksums, signatures).

## Deep Analysis

### Relevant ABNF grammar

From RFC 9112 Section 7.1 and 7.1.1:

```
chunk          = chunk-size [ chunk-ext ] CRLF
                 chunk-data CRLF
chunk-size     = 1*HEXDIG

chunk-ext      = *( BWS ";" BWS chunk-ext-name
                    [ BWS "=" BWS chunk-ext-val ] )

chunk-ext-name = token
chunk-ext-val  = token / quoted-string
```

### Direct RFC quotes

> "The chunked coding allows each chunk to include zero or more chunk extensions, immediately following the chunk-size, for the sake of supplying per-chunk metadata (such as a signature or hash), mid-message control information, or randomization of message body size." -- RFC 9112 Section 7.1.1

> "A recipient MUST ignore unrecognized chunk extensions." -- RFC 9112 Section 7.1.1

> "A server ought to limit the total length of chunk extensions received in a request to an amount reasonable for the services provided, in the same way that it applies length limitations and timeouts for other parts of a message, and generate an appropriate 4xx (Client Error) response if that amount is exceeded." -- RFC 9112 Section 7.1.1

### Chain of reasoning

1. The test sends chunk-size line `5;ext=value\r\n`. Parsing this against the ABNF: `chunk-size` matches `5`, then `chunk-ext` matches `;ext=value` where `ext` is the `chunk-ext-name` (a token) and `value` is the `chunk-ext-val` (also a token).
2. The `chunk` production explicitly includes `[ chunk-ext ]` -- chunk extensions are an optional but grammatically valid part of every chunk.
3. RFC 9112 Section 7.1.1 states recipients "MUST ignore unrecognized chunk extensions". The word "ignore" means the server must parse past them and process the chunk-data normally.
4. However, the RFC also says servers "ought to limit the total length of chunk extensions" and may generate a 4xx response if limits are exceeded. This introduces a legitimate reason for a `400` response.
5. The extension in this test (`ext=value`) is short (9 bytes), so a length-limit rejection would be unreasonable. But the RFC permits it in principle.

### Scored / Unscored justification

**Unscored.** The MUST keyword applies to *ignoring unrecognized* extensions, which implies the server should parse and skip them. However, the RFC also explicitly permits servers to reject requests with excessive chunk extensions via a 4xx response. Because the boundary between "acceptable" and "excessive" is left to the server's discretion, there is room for a compliant server to reject even short extensions. The test uses SHOULD accept (`2xx` = Pass, `400` = Warn) to acknowledge that `2xx` is the preferred behavior while `400` is not a clear violation.

### Edge cases

- Some servers strip chunk extensions before passing data to the application layer -- this is correct behavior per "MUST ignore unrecognized chunk extensions."
- A few servers fail to parse the semicolon delimiter and treat `5;ext=value` as an invalid chunk-size, returning `400`. This is a parser bug, not a policy decision.
- Chunk extensions with quoted-string values (e.g., `5;ext="hello world"`) are also valid per the ABNF but may trigger additional parser failures in implementations that only handle token values.
- The BWS (bad whitespace) allowance means `5 ; ext = value` is also technically valid, though rarely seen in practice.

## Sources

- [RFC 9112 Section 7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1)
