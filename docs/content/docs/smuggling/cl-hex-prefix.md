---
title: "CL-HEX-PREFIX"
description: "CL-HEX-PREFIX test documentation"
weight: 26
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-HEX-PREFIX` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Content-Length: 0x5` — CL with hex prefix.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 0x5\r\n
\r\n
hello
```


## What the RFC says

RFC 9110 §8.6 defines the Content-Length grammar:

> "Content-Length = 1*DIGIT"

The `1*DIGIT` grammar permits only ASCII digits 0-9. The value `0x5` contains `x`, which is not a digit, making this an invalid Content-Length. RFC 9110 §8.6 further requires:

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above"

RFC 9112 §6.3 mandates rejection:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection."

## Why it matters

If a server parses `0x5` as hexadecimal 5, it reads a different body length than a server that rejects it or truncates at the first non-digit. This parser disagreement is a smuggling vector.

## Deep Analysis

### ABNF Violation

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The `1*DIGIT` production requires one or more ASCII digits (`0`-`9`) exclusively. The value `0x5` contains the character `x`, which is not a DIGIT. Therefore `0x5` fails the `1*DIGIT` grammar and is unambiguously invalid.

### RFC Evidence Chain

**Step 1 -- The value is invalid per the grammar.**

The character `x` (0x78) is not in the range `0`-`9` (0x30-0x39). The value `0x5` cannot be produced by `1*DIGIT`, regardless of how many digits surround the `x`.

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above" -- RFC 9110 §8.6

**Step 2 -- The comma-separated list exception does not apply.**

RFC 9112 §6.3 provides an exception only when the value "can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same." The value `0x5` contains no commas, so it is a single-element list. That single element, `0x5`, is not valid `1*DIGIT`. The exception does not apply.

**Step 3 -- The server must reject with 400.**

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error." -- RFC 9112 §6.3

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection." -- RFC 9112 §6.3

### Scored / Unscored Justification

This test is **scored** (MUST reject). The value `0x5` is unambiguously invalid: `x` is not a DIGIT. The RFC chain from grammar violation through unrecoverable error to mandatory 400 is airtight. A `2xx` response is a compliance failure.

### Real-World Smuggling Scenario

C and C-derived languages (C++, Java, JavaScript) recognize `0x` as a hexadecimal prefix. If a server's parser calls a language-level integer function (e.g., `strtol` with base 0, or JavaScript's `parseInt`), `0x5` would be interpreted as hexadecimal 5 (decimal 5). A front-end that rejects or truncates at the `x` sees 0 bytes of body, while the back-end that parses hex sees 5 bytes. The 5 bytes the front-end considers the start of the next request are consumed as body by the back-end, desynchronizing the connection.

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
