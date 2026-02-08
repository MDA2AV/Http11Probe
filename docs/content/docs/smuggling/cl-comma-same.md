---
title: "CL-COMMA-SAME"
description: "CL-COMMA-SAME test documentation"
weight: 31
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-COMMA-SAME` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

`Content-Length: 5, 5` — comma-separated CL with identical values.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5, 5\r\n
\r\n
hello
```

The Content-Length value `5, 5` has two identical comma-separated values.


## What the RFC says

RFC 9110 §8.6 defines the Content-Length grammar:

> "Content-Length = 1*DIGIT"

The value `5, 5` does not match `1*DIGIT`, so it is technically invalid. However, RFC 9110 §8.6 provides an explicit exception:

> "a recipient of a Content-Length header field value consisting of the same decimal value repeated as a comma-separated list (e.g, "Content-Length: 42, 42") MAY either reject the message as invalid or replace that invalid field value with a single instance of the decimal value"

RFC 9112 §6.3 reinforces this:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error, unless the field value can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same (in which case, the message is processed with that single value used as the Content-Length field value)."

## Why this test is unscored

The RFC explicitly allows recipients to either reject or accept identical comma-separated CL values. Both `400` (strict rejection) and `2xx` (collapsing identical values) are RFC-compliant behaviors.

## Why it matters

While accepting identical comma-separated values is valid, it indicates the server's CL parser tolerates non-`1*DIGIT` input. This leniency could mask bugs in how the server handles other malformed Content-Length values.

## Deep Analysis

### ABNF Violation

RFC 9110 §8.6 defines the Content-Length grammar as:

> `Content-Length = 1*DIGIT`

The `1*DIGIT` production requires one or more ASCII digits (`0`-`9`) with no other characters. The value `5, 5` contains a comma and a space, so it does not match `1*DIGIT`. Strictly speaking, this value is grammatically invalid.

### RFC Evidence Chain

**Step 1 -- The value is invalid per the grammar.**

> "a sender MUST NOT forward a message with a Content-Length header field value that does not match the ABNF above" -- RFC 9110 §8.6

**Step 2 -- The comma-separated list exception is evaluated.**

RFC 9112 §6.3 provides a narrow exception:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error, unless the field value can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same (in which case, the message is processed with that single value used as the Content-Length field value)."

Parsing `5, 5` as a comma-separated list yields two members: `5` and `5`. Both are valid `1*DIGIT` values, and both are the same. **The exception applies.** The recipient may process the message using `5` as the Content-Length.

**Step 3 -- The recipient has explicit discretion.**

RFC 9110 §8.6 reinforces this with an explicit MAY:

> "a recipient of a Content-Length header field value consisting of the same decimal value repeated as a comma-separated list (e.g, "Content-Length: 42, 42") MAY either reject the message as invalid or replace that invalid field value with a single instance of the decimal value"

Both rejection (400) and acceptance (collapsing to `5` and responding 2xx) are RFC-compliant.

### Scored / Unscored Justification

This test is **unscored**. The RFC explicitly grants recipients a MAY choice: reject or collapse. Neither behavior violates the specification. A 400 demonstrates stricter parsing (safer), while a 2xx demonstrates the permitted collapse behavior (also valid). Scoring either as wrong would contradict the RFC's own allowance.

### Real-World Smuggling Scenario

While identical comma-separated values do not directly create a body-length disagreement, accepting them reveals that the server's Content-Length parser tolerates non-`1*DIGIT` input. This leniency is a signal: if the parser strips commas and collapses values, it may also be lenient with other malformed Content-Length patterns (e.g., `5, 10` where values differ). Attackers probe with safe payloads like `5, 5` to fingerprint parser behavior before escalating to exploitable payloads.

## Sources

- [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
