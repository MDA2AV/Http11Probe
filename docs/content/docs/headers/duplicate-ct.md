---
title: "DUPLICATE-CT"
description: "DUPLICATE-CT test documentation"
weight: 20
---

| | |
|---|---|
| **Test ID** | `COMP-DUPLICATE-CT` |
| **Category** | Compliance |
| **Scored** | Yes |
| **RFC** | [RFC 9110 §5.3](https://www.rfc-editor.org/rfc/rfc9110#section-5.3) |
| **RFC Level** | SHOULD |
| **Expected** | `400` preferred, `2xx` acceptable |

## What it sends

A POST request with two `Content-Type` headers that have conflicting values.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
Content-Type: text/plain\r\n
Content-Type: text/html\r\n
\r\n
hello
```

## What the RFC says

> "A sender MUST NOT generate multiple header fields with the same field name in a message unless either the entire field value for that header field is defined as a comma-separated list or the header field is a well-known exception." — RFC 9110 §5.3

> "A recipient MAY combine multiple header fields with the same field name into one 'field-name: field-value' pair... by appending each subsequent field value to the combined field value in order, separated by a comma." — RFC 9110 §5.3

`Content-Type` is not a list-based header — it has a single value. Duplicate `Content-Type` headers with different values create ambiguity about which value the server uses.

## Why it matters

When a proxy and origin server disagree on which `Content-Type` to use, it can lead to content-type confusion attacks. An attacker could craft a request that a proxy interprets as `text/plain` while the origin processes as `text/html`, enabling XSS or other injection attacks.

## Verdicts

- **Pass** — Server rejects with `400` or closes the connection
- **Warn** — Server accepts with `2xx` (silently picks one value)
- **Fail** — Server returns an unexpected error status

## Sources

- [RFC 9110 §5.3](https://www.rfc-editor.org/rfc/rfc9110#section-5.3)
