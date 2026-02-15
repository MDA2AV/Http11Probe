---
title: "RANGE-POST"
description: "RANGE-POST test documentation"
weight: 13
---

| | |
|---|---|
| **Test ID** | `COMP-RANGE-POST` |
| **Category** | Compliance |
| **Scored** | Yes |
| **RFC** | [RFC 9110 §14.2](https://www.rfc-editor.org/rfc/rfc9110#section-14.2) |
| **RFC Level** | MUST |
| **Expected** | `2xx` (Range ignored) |

## What it sends

A POST request with a `Range` header. The Range mechanism only applies to GET requests.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
Range: bytes=0-10\r\n
\r\n
hello
```

## What the RFC says

> "A server MUST ignore a Range header field received with a request method that is unrecognized or for which range handling is not defined." — RFC 9110 §14.2

Range handling is only defined for GET (RFC 9110 §14.2). For all other methods, the server must ignore the Range header and process the request normally.

## Why it matters

If a server incorrectly applies Range semantics to a POST request (returning `206 Partial Content`), it could truncate the request body or cause unexpected behavior. The server should process the full POST body and return a normal `2xx` response.

## Verdicts

- **Pass** — Server returns `2xx` (correctly ignored Range for POST)
- **Fail** — Server returns `206` (incorrectly applied Range to POST) or any non-2xx response

## Sources

- [RFC 9110 §14.2](https://www.rfc-editor.org/rfc/rfc9110#section-14.2)
