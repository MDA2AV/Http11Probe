---
title: "405 Allow — HTTP/1.1 Compliance"
description: "A DELETE request to the root path, which most servers do not support. This is intended to trigger a 405 response. Tested against RFC 9110 §15.5.6."
weight: 17
---

| | |
|---|---|
| **Test ID** | `COMP-405-ALLOW` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 §15.5.6](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.6) |
| **Requirement** | MUST |
| **Expected** | `405` with `Allow` header |

## What it sends

A DELETE request to the root path, which most servers do not support. This is intended to trigger a 405 response.

```http
DELETE / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "The origin server MUST generate an Allow header field in a 405 response containing a list of the target resource's currently supported methods." -- RFC 9110 Section 15.5.6

And:

> "An origin server MUST generate an Allow header field in a 405 (Method Not Allowed) response and MAY do so in any other response." -- RFC 9110 Section 10.2.1

## Why it matters

The Allow header in a 405 response tells clients which methods are actually supported. Without it, clients have no way to discover valid methods for the resource, forcing them to guess. Automated tools and API clients depend on this header for correct operation. If the server returns a status other than 405 (e.g., it accepts DELETE or returns 501), the test reports a warning since the Allow requirement cannot be verified.

## Sources

- [RFC 9110 §15.5.6 -- 405 Method Not Allowed](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.6)
- [RFC 9110 §10.2.1 -- Allow](https://www.rfc-editor.org/rfc/rfc9110#section-10.2.1)
