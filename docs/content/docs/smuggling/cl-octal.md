---
title: "CL-OCTAL"
description: "CL-OCTAL test documentation"
weight: 20
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-OCTAL` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Content-Length: 0o5` — CL with octal prefix.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 0o5\r\n
\r\n
hello
```


## What the RFC says

RFC 9110 §8.6 defines the Content-Length grammar:

> "Content-Length = 1*DIGIT"

The `1*DIGIT` grammar permits only ASCII digits 0-9. The value `0o5` contains `o`, which is not a digit, making this an invalid Content-Length. RFC 9110 §8.6 further requires:

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above"

RFC 9112 §6.3 mandates rejection:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection."

## Why it matters

Some programming languages (Python, Rust, Ruby) parse `0o5` as an octal literal for the value 5. If a server uses a language-level parser that accepts this notation, it would read a body of 5 bytes. A stricter front-end would reject the request, creating a parser differential that enables smuggling.

## Deep Analysis

### ABNF Violation

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The `1*DIGIT` production requires one or more ASCII digits (`0`-`9`) exclusively. The value `0o5` contains the character `o` (0x6F), which is not a DIGIT (0x30-0x39). Therefore `0o5` fails the `1*DIGIT` grammar and is unambiguously invalid.

### RFC Evidence Chain

**Step 1 -- The value is invalid per the grammar.**

The character `o` at position 2 breaks the digit-only requirement. Even though `0` and `5` are digits, the intervening `o` makes the complete value non-conformant.

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above" -- RFC 9110 §8.6

**Step 2 -- The comma-separated list exception does not apply.**

RFC 9112 §6.3 provides an exception only when the value "can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same." The value `0o5` contains no commas. As a single-element list, `0o5` must be valid `1*DIGIT` -- and it is not. The exception does not apply.

**Step 3 -- The server must reject with 400.**

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error." -- RFC 9112 §6.3

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection." -- RFC 9112 §6.3

### Scored / Unscored Justification

This test is **scored** (MUST reject). The `o` character is not a DIGIT, making the value unambiguously invalid. No exception applies. The RFC mandates 400 and connection close. A `2xx` response is a compliance failure.

### Real-World Smuggling Scenario

The `0o` prefix is the standard octal literal notation in Python 3 (`0o5` = 5), Rust (`0o5` = 5), Ruby (`0o5` = 5), and ECMAScript 2015+ (`0o5` = 5). If a server's Content-Length parser delegates to a language-level integer parser that accepts `0o` notation, `0o5` would be interpreted as the integer 5, and the server would read 5 bytes of body. A front-end that correctly rejects this value (or a parser that stops at the `o` and reads 0 bytes) would disagree on the body boundary. The attacker's 5 body bytes would spill forward as the start of the next request. This is particularly dangerous with values like `0o12` (octal 10) vs. a truncation-to-`0` parser, creating a 10-byte smuggling window.

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
