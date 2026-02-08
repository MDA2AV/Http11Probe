---
title: "POST-CL-BODY"
description: "POST-CL-BODY test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `COMP-POST-CL-BODY` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` |

## What it sends

A valid POST with `Content-Length: 5` and exactly 5 bytes of body (`hello`).

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Content-Length: 5\r\n
\r\n
hello
```

## What the RFC says

> "When a message does not have a Transfer-Encoding header field, a Content-Length header field can provide the anticipated size, as a decimal number of octets, for potential content. For messages that do include content, the Content-Length field value provides the framing information necessary for determining where the data (and message) ends." — RFC 9112 Section 6.2

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." — RFC 9112 Section 6.3

The server must read exactly 5 bytes from the connection after the header section ends, then process the request normally.

## Why it matters

This is the most basic body consumption test. If a server cannot read a fixed-length POST body, it cannot handle form submissions, API calls, or file uploads — the foundation of any interactive web application.

## Deep Analysis

### Relevant ABNF grammar

From RFC 9112 Section 6:

```
message-body = *OCTET
```

From RFC 9110 Section 8.6 (Content-Length):

```
Content-Length = 1*DIGIT
```

### Direct RFC quotes

> "When a message does not have a Transfer-Encoding header field, a Content-Length header field can provide the anticipated size, as a decimal number of octets, for potential content. For messages that do include content, the Content-Length field value provides the framing information necessary for determining where the data (and message) ends." -- RFC 9112 Section 6.2

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." -- RFC 9112 Section 6.3

> "A sender MUST NOT send a Content-Length header field in any message that contains a Transfer-Encoding header field." -- RFC 9112 Section 6.2

### Chain of reasoning

1. The test sends a POST request with `Content-Length: 5` and no `Transfer-Encoding` header.
2. Per RFC 9112 Section 6.3 rule 6, when a valid Content-Length is present without Transfer-Encoding, "its decimal value defines the expected message body length in octets." The server must read exactly 5 bytes.
3. The test sends exactly 5 bytes (`hello`) after the header section. This satisfies the declared Content-Length precisely.
4. POST is a method that inherently expects content (RFC 9110 Section 9.3.3). There is no ambiguity about whether a body is appropriate.
5. The Content-Length value `5` is a valid `1*DIGIT` production, the body length matches the declared value, and there is no Transfer-Encoding conflict. The request is fully well-formed.
6. The server has no grounds to reject this request. Accepting a valid Content-Length-framed POST body is the most fundamental requirement for an HTTP/1.1 server.

### Scored / Unscored justification

**Scored.** RFC 9112 Section 6.3 establishes that Content-Length "defines the expected message body length" as a definitive framing mechanism. While the word MUST does not appear in the specific framing sentence, the entire message body length determination algorithm in Section 6.3 is normative, and a server that cannot read a Content-Length-framed body cannot function as an HTTP/1.1 server. This is the baseline body consumption test -- if this fails, the server is fundamentally broken.

### Edge cases

- Some servers impose upper limits on Content-Length and reject bodies exceeding a configured maximum with `413 Content Too Large`. This test uses only 5 bytes, well below any reasonable limit.
- A server that reads fewer than 5 bytes (e.g., reads 0 bytes and ignores the body) may appear to "work" but will desynchronize the connection on keep-alive because the unread body bytes will be interpreted as the next request.
- Servers that only accept GET (static file servers) may return `405 Method Not Allowed` for POST. This would be a legitimate response but indicates the server does not support POST at all, which is a separate concern from body parsing.
- The absence of a `Content-Type` header is deliberate -- the server must still read the body based on Content-Length framing regardless of whether the content type is specified.

## Sources

- [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2)
