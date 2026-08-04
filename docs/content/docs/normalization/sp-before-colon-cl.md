---
title: "Space Before Colon CL — Header Normalization"
description: "A POST request to /echo with a valid Content-Length: 11 for body framing, plus a malformed Content-Length : 5 header with a space before the colon. Tested against RFC 9112 §5."
weight: 2
---

| | |
|---|---|
| **Test ID** | `NORM-SP-BEFORE-COLON-CL` |
| **Category** | Normalization |
| **RFC** | [RFC 9112 §5](https://www.rfc-editor.org/rfc/rfc9112#section-5) |
| **Requirement** | MUST reject |
| **Expected** | Reject/drop (pass), normalize (fail), preserve (warn) |

## What it sends

A POST request to `/echo` with a valid `Content-Length: 11` for body framing, plus a malformed `Content-Length : 5` header with a space before the colon.

```http
POST /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 11\r\n
Content-Length : 5\r\n
\r\n
hello world
```

## What the RFC says

RFC 9112 Section 5 states:

> "No whitespace is allowed between the field name and colon. [...] A server MUST reject, with a response status code of 400 (Bad Request), any received request message that contains whitespace between a header field name and colon."

## Pass / Fail / Warn

**Pass:** Server rejects the request (`400`) or drops the malformed header.
**Fail:** Server strips the whitespace and normalizes to `Content-Length: 5` — the echo shows a Content-Length header with value `5`, overriding the valid value of `11`.
**Warn:** Server preserves the header with the trailing space in the name.

## Why it matters

If a server normalizes `Content-Length : 5` by stripping the whitespace, the request now has two Content-Length values (11 and 5). This creates a framing disagreement that can enable request smuggling. The RFC explicitly mandates rejection with 400 for this reason.
