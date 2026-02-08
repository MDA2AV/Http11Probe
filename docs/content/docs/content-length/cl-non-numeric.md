---
title: "CL-NON-NUMERIC"
description: "CL-NON-NUMERIC test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `RFC9112-6.1-CL-NON-NUMERIC` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6), [RFC 9112 Section 6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A request with a non-numeric `Content-Length` value, e.g., `Content-Length: abc`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: abc\r\n
\r\n
```


## What the RFC says

> "Content-Length = 1*DIGIT" -- RFC 9110 Section 8.6

A value containing non-digit characters (`abc`) does not match the `1*DIGIT` grammar.

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error, unless the field value can be successfully parsed as a comma-separated list (Section 5.6.1 of [HTTP]), all values in the list are valid, and all values in the list are the same." -- RFC 9112 Section 6.3

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection." -- RFC 9112 Section 6.3

"Unrecoverable error" means the server must reject -- either with a 400 response or by closing the connection. It cannot attempt to parse the body.

## Why it matters

Content-Length is the primary framing mechanism for HTTP messages without Transfer-Encoding. If a server accepts a non-numeric Content-Length value, it has no reliable way to determine where the message body ends. This framing ambiguity is the foundation of HTTP request smuggling: if the server and a downstream proxy disagree on the body length, an attacker can inject a second request into the body of the first.

## Deep Analysis

### Relevant ABNF Grammar

```
Content-Length = 1*DIGIT
DIGIT          = %x30-39 ; 0-9
```

The Content-Length grammar is one of the simplest in the HTTP specification: one or more ASCII digit characters. No sign characters (`+`, `-`), no whitespace, no alphabetic characters, no hexadecimal notation -- strictly decimal digits.

### RFC Evidence

**RFC 9110 Section 8.6** defines the grammar:

> "Content-Length = 1*DIGIT" -- RFC 9110 Section 8.6

**RFC 9112 Section 6.3** mandates how invalid Content-Length must be handled:

> "If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error, unless the field value can be successfully parsed as a comma-separated list, all values in the list are valid, and all values in the list are the same." -- RFC 9112 Section 6.3

**RFC 9112 Section 6.3** specifies the exact server response for unrecoverable errors:

> "If the unrecoverable error is in a request message, the server MUST respond with a 400 (Bad Request) status code and then close the connection." -- RFC 9112 Section 6.3

### Chain of Reasoning

1. The test sends `Content-Length: abc`. The characters `a`, `b`, `c` are not in the DIGIT range (`%x30-39`).
2. The value `abc` cannot match `1*DIGIT`, making the Content-Length header field invalid.
3. Without Transfer-Encoding, Content-Length is the sole mechanism for determining message body length. An invalid Content-Length means the message framing is indeterminate.
4. RFC 9112 Section 6.3 explicitly designates this as an "unrecoverable error" -- the strongest error classification in the HTTP framing specification.
5. For requests, the server MUST respond with 400 and then close the connection. The RFC mandates both actions: the 400 response and the subsequent connection closure.

### Scoring Justification

**Scored (MUST).** RFC 9112 Section 6.3 mandates 400 followed by connection close for invalid Content-Length in requests. This is one of the few requirements where the RFC mandates both a specific status code and a connection behavior. Both 400 and connection close are acceptable test outcomes because the "and then close the connection" phrasing means some servers may close the connection before fully transmitting the 400 response.

## Sources

- [RFC 9110 Section 8.6 -- Content-Length](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 Section 6.3 -- Message Body Length](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
