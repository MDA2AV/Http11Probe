---
title: "CL-NEGATIVE-ZERO"
description: "CL-NEGATIVE-ZERO test documentation"
weight: 47
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-NEGATIVE-ZERO` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

Content-Length with a negative zero value: `Content-Length: -0`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: -0\r\n
\r\n
```


## What the RFC says

RFC 9110 §8.6 defines the Content-Length grammar:

> "Content-Length = 1*DIGIT"

The `1*DIGIT` grammar means only one or more ASCII digits (0-9) are permitted. The minus sign (`-`) is not a digit, so `-0` is invalid regardless of the fact that -0 equals 0 mathematically. RFC 9110 §8.6 further requires:

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above"

RFC 9112 §6.3 mandates rejection:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error."

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection."

## Why it matters

Some parsers apply numeric conversion first and check validity second. If a parser converts `-0` to the integer `0` and accepts it, it silently consumes an invalid format. A stricter front-end might reject the request or see no body at all, while a lenient back-end accepts it — creating framing disagreement. The `-` character is especially dangerous because it could allow negative body lengths through similar parser shortcuts.

## Deep Analysis

### ABNF Violation

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The `1*DIGIT` production requires one or more ASCII digits (`0`-`9`) exclusively. The value `-0` begins with a minus sign (`-`, 0x2D), which is not a DIGIT. Therefore `-0` fails the `1*DIGIT` grammar at the very first character. The fact that `-0` equals `0` mathematically is irrelevant -- the ABNF is a syntactic rule, not a semantic one.

### RFC Evidence Chain

**Step 1 -- The value is invalid per the grammar.**

The minus sign fails the DIGIT check regardless of what follows it. `-0` is syntactically identical to `-1` or `-999` from the grammar's perspective: all begin with a non-DIGIT character.

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above" -- RFC 9110 §8.6

**Step 2 -- The comma-separated list exception does not apply.**

RFC 9112 §6.3 provides an exception only when the value "can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same." The value `-0` has no commas. As a single-element list, `-0` must be valid `1*DIGIT` -- and it is not. The exception does not apply.

**Step 3 -- The server must reject with 400.**

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error." -- RFC 9112 §6.3

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection." -- RFC 9112 §6.3

### Scored / Unscored Justification

This test is **scored** (MUST reject). The minus sign is not a DIGIT, making `-0` unambiguously invalid. No mathematical equivalence to `0` changes the syntactic analysis. The RFC mandates 400 and connection close. A `2xx` response is a compliance failure.

### Real-World Smuggling Scenario

The danger of `-0` is parser shortcutting. Some parsers apply numeric conversion first (`atoi("-0")` returns `0`) and then check if the result is non-negative. Since `0` passes the non-negative check, the parser accepts the value without ever validating the syntax. This creates a differential: a strict front-end rejects the request (or treats it as having no body), while a lenient back-end accepts `Content-Length: 0` and reads no body. If the front-end rejects but the connection is reused (a misconfiguration), the back-end may process subsequent bytes on the connection as a new request. More importantly, accepting `-0` signals that the parser tolerates the `-` character, meaning `-1` or other negative values may also slip through to trigger integer underflow attacks.

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
