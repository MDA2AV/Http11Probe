---
title: "CL-NEGATIVE"
description: "CL-NEGATIVE test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-NEGATIVE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

Negative Content-Length: `Content-Length: -1`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: -1\r\n
\r\n
```


## What the RFC says

RFC 9110 §8.6 defines the Content-Length grammar:

> "Content-Length = 1*DIGIT"

The `1*DIGIT` grammar permits only ASCII digits 0-9. The minus sign (`-`) is not a digit, so `-1` does not match the grammar and is invalid. RFC 9110 §8.6 further requires:

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above"

RFC 9112 §6.3 mandates rejection:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection."

## Why it matters

A negative Content-Length could cause integer underflow in parsers that convert the value to a signed integer before validation. If a server interprets `-1` as a very large unsigned value (e.g., 4294967295 on 32-bit), it could read far beyond the intended body — a severe security vulnerability.

## Deep Analysis

### ABNF Violation

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The `1*DIGIT` production requires one or more ASCII digits (`0`-`9`) exclusively. The value `-1` begins with a minus sign (`-`, 0x2D), which is not a DIGIT (0x30-0x39). Therefore `-1` fails the `1*DIGIT` grammar at the very first character and is unambiguously invalid.

### RFC Evidence Chain

**Step 1 -- The value is invalid per the grammar.**

The minus sign is not in the DIGIT range. The ABNF `1*DIGIT` requires the first character to be a digit; `-` immediately disqualifies the value.

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above" -- RFC 9110 §8.6

**Step 2 -- The comma-separated list exception does not apply.**

RFC 9112 §6.3 provides an exception only when the value "can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same." The value `-1` has no commas. As a single-element list, `-1` must be valid `1*DIGIT` -- and it is not. The exception does not apply.

**Step 3 -- The server must reject with 400.**

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error." -- RFC 9112 §6.3

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection." -- RFC 9112 §6.3

### Scored / Unscored Justification

This test is **scored** (MUST reject). The minus sign is not a DIGIT, so the value is invalid with no exception. The RFC chain from grammar violation to unrecoverable error to mandatory 400 is unambiguous. A `2xx` response is a compliance failure.

### Real-World Smuggling Scenario

A negative Content-Length is one of the most dangerous malformed values because of how programming languages handle signed-to-unsigned integer conversion. If a server parses `-1` as a signed 32-bit integer and then casts it to an unsigned type, it becomes `4294967295` (2^32 - 1). The server would attempt to read ~4 GB of body data from the connection, consuming not just the current request's body but potentially hundreds of subsequent requests from other clients on a shared connection. Even on 64-bit systems, `-1` as unsigned is `18446744073709551615`. Beyond smuggling, this is a denial-of-service vector: the server hangs waiting for billions of bytes that will never arrive, tying up the connection indefinitely.

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
