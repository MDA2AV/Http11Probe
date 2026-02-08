---
title: "CL-PLUS-SIGN"
description: "CL-PLUS-SIGN test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `RFC9112-6.1-CL-PLUS-SIGN` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6), [RFC 9112 Section 6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A request with a plus sign in the Content-Length value: `Content-Length: +5`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: +5\r\n
\r\n
hello
```


## What the RFC says

> "Content-Length = 1*DIGIT" -- RFC 9110 Section 8.6

The `+` character is not in the DIGIT set (`%x30-39`), so `+5` does not match `1*DIGIT`.

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error." -- RFC 9112 Section 6.3

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection." -- RFC 9112 Section 6.3

## Why it matters

Many programming languages’ integer parsers accept leading `+` signs (e.g., `parseInt("+42")` returns `42` in JavaScript). A server that blindly passes Content-Length through such a parser may accept this value while another server in the chain rejects it — creating a framing disagreement.

## Deep Analysis

### Relevant ABNF Grammar

```
Content-Length = 1*DIGIT
DIGIT          = %x30-39 ; 0-9
```

The `+` character (0x2B) is not in the DIGIT range (`%x30-39`). The ABNF is unambiguous: Content-Length must begin with a digit, not a sign character.

### RFC Evidence

**RFC 9110 Section 8.6** defines the grammar:

> "Content-Length = 1*DIGIT" -- RFC 9110 Section 8.6

**RFC 9112 Section 6.3** classifies invalid Content-Length as an unrecoverable error:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error, unless the field value can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same." -- RFC 9112 Section 6.3

**RFC 9112 Section 6.3** mandates the server response:

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection." -- RFC 9112 Section 6.3

### Chain of Reasoning

1. The test sends `Content-Length: +5`. The `+` character (ASCII 0x2B) is not a DIGIT (`%x30-39`).
2. The value `+5` does not match `1*DIGIT` because the first character is not a digit. This makes the Content-Length invalid.
3. The subtle danger is that many programming language standard libraries accept leading `+` in integer parsing: JavaScript's `parseInt("+5")` returns `5`, Python's `int("+5")` returns `5`, and C#'s `int.Parse("+5")` returns `5`.
4. If the server's parser accepts `+5` as `5` but a downstream proxy's parser rejects it (or vice versa), the two disagree on the message body length. This framing disagreement is the fundamental precondition for request smuggling.
5. The RFC deliberately chose `1*DIGIT` rather than a more permissive integer syntax precisely to prevent this class of parsing divergence.

### Scoring Justification

**Scored (MUST).** The `+` character violates the `1*DIGIT` grammar, making this an invalid Content-Length. RFC 9112 Section 6.3 mandates 400 followed by connection close. Both 400 and connection close are acceptable test outcomes. A server that parses `+5` as `5` and processes the request normally is non-compliant and vulnerable to smuggling via framing disagreement.

## Sources

- [RFC 9110 Section 8.6 -- Content-Length](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 Section 6.3 -- Message Body Length](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
