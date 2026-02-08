---
title: "POST-CL-ZERO"
description: "POST-CL-ZERO test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `COMP-POST-CL-ZERO` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` or close |

## What it sends

A POST with `Content-Length: 0` and no body bytes after the headers.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Content-Length: 0\r\n
\r\n
```

## What the RFC says

> "When a message does not have a Transfer-Encoding header field, a Content-Length header field can provide the anticipated size, as a decimal number of octets, for potential content." — RFC 9112 Section 6.2

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." — RFC 9112 Section 6.3

A Content-Length of zero is explicitly valid — its decimal value (0) defines the body length as zero octets. The server must not block waiting for body bytes that will never arrive.

## Why it matters

Zero-length POSTs are common in APIs (e.g., triggering an action with no payload). A server that hangs waiting for a body on CL:0 will cause client timeouts and connection leaks.

## Deep Analysis

### Relevant ABNF grammar

From RFC 9110 Section 8.6 (Content-Length):

```
Content-Length = 1*DIGIT
```

The value `0` is a valid `1*DIGIT` production (one digit, "0"). It defines a message body length of zero octets.

From RFC 9112 Section 6:

```
message-body = *OCTET
```

A `*OCTET` production with zero repetitions (empty string) is valid. A zero-length message body is a valid instance of `*OCTET`.

### Direct RFC quotes

> "When a message does not have a Transfer-Encoding header field, a Content-Length header field can provide the anticipated size, as a decimal number of octets, for potential content." -- RFC 9112 Section 6.2

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." -- RFC 9112 Section 6.3

> "For messages that do include content, the Content-Length field value provides the framing information necessary for determining where the data (and message) ends." -- RFC 9112 Section 6.2

### Chain of reasoning

1. The test sends `Content-Length: 0` with no body bytes after the `\r\n\r\n` header terminator.
2. The Content-Length value `0` is a valid `1*DIGIT` production -- the digit "0" satisfies the one-or-more requirement.
3. Per RFC 9112 Section 6.3 rule 6, the server must read exactly 0 bytes of body data. This means the message ends immediately after the header section.
4. The server must not block waiting for body data. The Content-Length has explicitly declared that zero octets follow.
5. POST with a zero-length body is semantically valid. RFC 9110 Section 9.3.3 does not require POST to have a non-empty body.
6. The request is syntactically complete and well-formed. The server must process it and respond.

### Scored / Unscored justification

**Scored.** RFC 9112 Section 6.3 establishes that Content-Length "defines the expected message body length in octets." A value of 0 unambiguously means zero bytes. The server must process this as a complete request with an empty body. There is no ambiguity or discretionary latitude. The `AllowConnectionClose` flag is set because a server may legitimately close the connection after processing a POST (e.g., if it does not support keep-alive for POST), but it must still process the request.

### Edge cases

- A server that hangs waiting for body data after seeing `Content-Length: 0` has a framing bug -- it is ignoring the declared body length.
- Some servers treat `Content-Length: 0` differently from "no Content-Length" on POST. Both should result in a zero-length body (per Section 6.3 rules 6 and 7), but the presence of the header is more explicit.
- `Content-Length: 0` is commonly used in API calls that trigger actions without payload (e.g., `POST /api/restart` with no body). Servers must handle this pattern.
- A server that returns `411 Length Required` when Content-Length is already present and valid has a logic error in its header checking.

## Sources

- [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2)
