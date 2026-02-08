---
title: "CL-UNDERSCORE"
description: "CL-UNDERSCORE test documentation"
weight: 46
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-UNDERSCORE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

Content-Length with an underscore digit separator: `Content-Length: 1_0` with 10 bytes of body.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 1_0\r\n
\r\n
helloworld
```


## What the RFC says

RFC 9110 §8.6 defines the Content-Length grammar:

> "Content-Length = 1*DIGIT"

The underscore character is not a digit. The `1*DIGIT` grammar only permits ASCII digits 0-9, so `1_0` is not a valid Content-Length value. RFC 9110 §8.6 further requires:

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above"

RFC 9112 §6.3 mandates rejection:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection."

## Why it matters

Several programming languages (Python, Rust, Java, Ruby, Kotlin) accept underscores as numeric separators in source code (e.g., `1_000_000`). If a server's parser uses a language-level integer-parsing function that accepts underscores, it would read `1_0` as `10`. A stricter front-end proxy would reject the request or misparse the value, creating a parser differential that enables request smuggling.

## Deep Analysis

### ABNF Violation

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The `1*DIGIT` production requires one or more ASCII digits (`0`-`9`) exclusively. The value `1_0` contains the underscore character (`_`, 0x5F), which is not a DIGIT (0x30-0x39). Therefore `1_0` fails the `1*DIGIT` grammar and is unambiguously invalid. The underscore breaks the contiguous digit sequence at position 2.

### RFC Evidence Chain

**Step 1 -- The value is invalid per the grammar.**

The underscore is not in the ASCII digit range. Even though `1` and `0` are valid digits, the intervening `_` makes the complete value non-conformant with `1*DIGIT`.

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above" -- RFC 9110 §8.6

**Step 2 -- The comma-separated list exception does not apply.**

RFC 9112 §6.3 provides an exception only when the value "can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same." The value `1_0` contains no commas. As a single-element list, `1_0` must be valid `1*DIGIT` -- and it is not. The exception does not apply.

**Step 3 -- The server must reject with 400.**

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error." -- RFC 9112 §6.3

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection." -- RFC 9112 §6.3

### Scored / Unscored Justification

This test is **scored** (MUST reject). The underscore is not a DIGIT, making the value unambiguously invalid. No exception applies. The RFC mandates 400 and connection close. A `2xx` response is a compliance failure.

### Real-World Smuggling Scenario

Many modern programming languages accept underscores as numeric separators in integer literals: Python (`1_000`), Rust (`1_000`), Java (`1_000`), Ruby (`1_000`), Kotlin (`1_000`), Swift (`1_000`), and C# 7.0+ (`1_000`). If a server parses Content-Length by passing the raw string to a language-level integer parser (e.g., Python's `int("1_0")` returns `10`), it would read 10 bytes of body. A front-end that correctly rejects the value sees no body at all, and the attacker's 10 body bytes spill forward as the next request. Alternatively, a parser that stops at the underscore reads only 1 byte, creating a 9-byte smuggling window vs. the 10-byte back-end interpretation. The underscore is specifically dangerous because it is invisible in many code review contexts and widely supported across language ecosystems.

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
