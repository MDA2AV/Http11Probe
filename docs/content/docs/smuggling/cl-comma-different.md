---
title: "CL-COMMA-DIFFERENT"
description: "CL-COMMA-DIFFERENT test documentation"
weight: 15
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-COMMA-DIFFERENT` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Content-Length: 5, 10` — comma-separated CL with different values.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5, 10\r\n
\r\n
hello
```


## What the RFC says

RFC 9110 §8.6 defines the Content-Length grammar:

> "Content-Length = 1*DIGIT"

A sender MUST NOT forward invalid Content-Length. RFC 9112 §6.3 specifies how invalid values must be handled:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error, unless the field value can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same."

The value `5, 10` can be parsed as a comma-separated list, but the two values differ. The exception does not apply, so this is an unrecoverable error:

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection."

## Why it matters

Comma-separated CL values are equivalent to multiple CL headers. Different values create ambiguity about body length — if one parser picks 5 and another picks 10, they disagree on the body boundary, enabling request smuggling.

## Deep Analysis

### ABNF Violation

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The `1*DIGIT` production requires one or more ASCII digits (`0`-`9`) with no other characters. The value `5, 10` contains a comma and a space, neither of which is a DIGIT. Therefore `5, 10` does not match `1*DIGIT` and is invalid on its face.

### RFC Evidence Chain

**Step 1 -- The value is invalid per the grammar.**

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above" -- RFC 9110 §8.6

**Step 2 -- The comma-separated list exception is evaluated.**

RFC 9112 §6.3 provides a narrow exception for comma-separated Content-Length values:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error, unless the field value can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same (in which case, the message is processed with that single value used as the Content-Length field value)."

Parsing `5, 10` as a comma-separated list yields two members: `5` and `10`. Both are individually valid `1*DIGIT` values, **but they are not the same**. The exception requires "all values in the list are the same", which fails here. Therefore the exception does not apply and this remains an unrecoverable error.

**Step 3 -- The server must reject with 400.**

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection." -- RFC 9112 §6.3

### Scored / Unscored Justification

This test is **scored** (MUST reject). The comma-separated list exception explicitly requires all values to be identical. Since `5` and `10` differ, no exception applies, and the RFC mandates a 400 response. A `2xx` response is a compliance failure.

### Real-World Smuggling Scenario

In a reverse-proxy chain, the front-end may parse the comma-separated list and select the first value (`5`), reading only 5 bytes of body. The back-end may select the last value (`10`), expecting 10 bytes. The front-end forwards only 5 bytes of body, but the back-end waits for 5 more -- consuming the beginning of the next legitimate request as body data. Alternatively, if the front-end picks `10` and the back-end picks `5`, the extra 5 bytes spill forward and are interpreted as a new request. Either way, the attacker controls the boundary between requests.

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
