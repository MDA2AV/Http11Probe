---
title: "POST-NO-CL-NO-TE"
description: "POST-NO-CL-NO-TE test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `COMP-POST-NO-CL-NO-TE` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3) |
| **Requirement** | MUST treat as zero-length |
| **Expected** | `2xx` or close |

## What it sends

A POST with neither Content-Length nor Transfer-Encoding headers — no body framing at all.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
\r\n
```

## What the RFC says

> "If this is a request message and none of the above are true, then the message body length is zero (no message body is present)." — RFC 9112 Section 6.3

When a request has no framing headers, the server must assume the body is empty and process the request immediately.

## Why it matters

Some servers hang waiting for a body when they see POST without Content-Length, causing connection timeouts. The RFC is clear: no framing headers means zero-length body.

## Sources

- [RFC 9112 Section 6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
