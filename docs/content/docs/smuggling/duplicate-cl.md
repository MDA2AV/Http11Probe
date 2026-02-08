---
title: "DUPLICATE-CL"
description: "DUPLICATE-CL test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `SMUG-DUPLICATE-CL` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

Two `Content-Length` headers with different values.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
Content-Length: 10\r\n
\r\n
hello
```


## What the RFC says

RFC 9110 §5.2 establishes that multiple header fields with the same name can be combined:

> "A recipient MAY combine multiple header fields with the same field name into one field with a comma-separated list, in the order in which the header fields were received"

This means `Content-Length: 5` and `Content-Length: 10` as separate headers is equivalent to `Content-Length: 5, 10`. RFC 9112 §6.3 mandates rejection when the values differ:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error, unless the field value can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same."

Since `5` and `10` differ, the exception does not apply. This is an unrecoverable error:

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection."

## Why it matters

If parser A uses the first CL (5 bytes) and parser B uses the second (10 bytes), they disagree on body length. The extra 5 bytes that parser B expects can contain an attacker-crafted request -- classic smuggling.

## Deep Analysis

### ABNF Analysis

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

Each individual `Content-Length` header has a valid value: `5` and `10` both match `1*DIGIT`. The violation is not in the grammar of either individual value, but in the presence of two `Content-Length` headers with differing values.

### RFC Evidence Chain

**Step 1 -- Multiple headers with the same name are combinable.**

RFC 9110 §5.2 establishes that multiple header fields with the same name can be combined into a comma-separated list. Therefore `Content-Length: 5` and `Content-Length: 10` as separate headers is semantically equivalent to `Content-Length: 5, 10`.

**Step 2 -- The combined value is invalid.**

The combined value `5, 10` does not match `1*DIGIT`. RFC 9112 §6.3 evaluates the comma-separated list exception:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error, unless the field value can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same."

Parsing `5, 10` yields `5` and `10`. Both are individually valid, but they differ. The exception requires "all values in the list are the same" -- which fails. This is an unrecoverable error.

**Step 3 -- The server must reject with 400.**

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection." -- RFC 9112 §6.3

Additionally, RFC 9110 §8.6 prohibits forwarding:

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above" -- RFC 9110 §8.6

### Scored / Unscored Justification

This test is **scored** (MUST reject). Two Content-Length headers with different values produce an invalid combined value where the comma-separated list exception explicitly fails (values are not the same). The RFC mandates 400 and connection close with no discretion. A `2xx` response is a compliance failure.

### Real-World Smuggling Scenario

Duplicate Content-Length headers are the most straightforward smuggling vector. Many HTTP implementations select either the first or last header when duplicates exist -- a behavior that is implementation-specific and undocumented. If the front-end proxy uses the first `Content-Length: 5` and forwards 5 bytes of body, but the back-end uses the last `Content-Length: 10` and expects 10 bytes, the back-end waits for 5 more bytes. It consumes the first 5 bytes of the next legitimate request as body data for the current request, then interprets the remainder of that next request as a new (truncated or malformed) request. Conversely, if the front-end uses 10 and the back-end uses 5, the extra 5 bytes spill forward. This is why the RFC is absolute: differing Content-Length values MUST cause rejection.

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9110 §5.2](https://www.rfc-editor.org/rfc/rfc9110#section-5.2)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
