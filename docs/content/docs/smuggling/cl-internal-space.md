---
title: "CL-INTERNAL-SPACE"
description: "CL-INTERNAL-SPACE test documentation"
weight: 27
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-INTERNAL-SPACE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Content-Length: 1 0` — space inside the number.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 1 0\r\n
\r\n
hello12345
```

The Content-Length value `1 0` has a space between the digits.


## What the RFC says

RFC 9110 §8.6 defines the Content-Length grammar:

> "Content-Length = 1*DIGIT"

The `1*DIGIT` grammar permits only a contiguous sequence of ASCII digits 0-9. A space character is not a digit, so `1 0` does not match the grammar and is invalid. RFC 9110 §8.6 further requires:

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above"

RFC 9112 §6.3 mandates rejection:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection."

## Why it matters

A server that strips the internal space and interprets `1 0` as `10` reads 10 bytes of body. A server that reads only up to the first non-digit reads 1 byte. This disagreement on body boundaries is a smuggling vector.

## Deep Analysis

### ABNF Violation

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The `1*DIGIT` production requires a contiguous sequence of one or more ASCII digits (`0`-`9`) with no intervening characters. The value `1 0` contains a space character (0x20) between the two digits. Space is not a DIGIT, so `1 0` does not match `1*DIGIT` and is invalid.

Note that the space here is not leading or trailing OWS -- it is embedded *within* the field value, between two digits. RFC 9112 §5's OWS stripping only applies to whitespace before the first non-whitespace octet and after the last non-whitespace octet, not to whitespace in the middle.

### RFC Evidence Chain

**Step 1 -- The value is invalid per the grammar.**

The space at position 2 breaks the contiguous digit sequence. `1*DIGIT` requires an unbroken run of digits; `1 0` is two separate digit groups separated by a non-DIGIT character.

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above" -- RFC 9110 §8.6

**Step 2 -- The comma-separated list exception does not apply.**

RFC 9112 §6.3 provides an exception for comma-separated lists where "all values in the list are valid, and all values in the list are the same." The value `1 0` contains no commas. As a single-element list, `1 0` itself must be valid `1*DIGIT` -- and it is not. The exception does not apply.

**Step 3 -- The server must reject with 400.**

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error." -- RFC 9112 §6.3

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection." -- RFC 9112 §6.3

### Scored / Unscored Justification

This test is **scored** (MUST reject). The space within the digit sequence makes the value unambiguously invalid. No exception applies. The RFC mandates 400 and connection close. A `2xx` response is a compliance failure.

### Real-World Smuggling Scenario

A server that strips internal whitespace would interpret `1 0` as `10` and read 10 bytes of body. A server that parses only up to the first non-DIGIT character would read `1` byte. This 9-byte disagreement is a smuggling vector: the first server consumes 10 bytes as body, while the second server consumes 1 byte as body and treats the remaining 9 bytes as the beginning of the next HTTP request. An attacker can embed a crafted request in those 9 bytes.

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
