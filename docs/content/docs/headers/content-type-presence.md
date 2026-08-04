---
title: "Content Type Presence — HTTP/1.1 Compliance"
description: "A standard GET request. The test validates that the server includes a Content-Type header when the response contains a body. Tested against RFC 9110 §8.3."
weight: 12
---

| | |
|---|---|
| **Test ID** | `COMP-CONTENT-TYPE` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 §8.3](https://www.rfc-editor.org/rfc/rfc9110#section-8.3) |
| **Requirement** | SHOULD |
| **Expected** | `2xx` with `Content-Type` header |

## What it sends

A standard GET request. The test validates that the server includes a `Content-Type` header when the response contains a body.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "A sender that generates a message containing content SHOULD generate a Content-Type header field in the message unless the intended media type of the enclosed representation is unknown to the sender." -- RFC 9110 Section 8.3

And:

> "If a Content-Type header field is not present, the recipient MAY either assume a media type of 'application/octet-stream' or examine the data to determine its type." -- RFC 9110 Section 8.3

## Why it matters

Without Content-Type, clients must guess the media type through content sniffing, which is a well-known security risk. Browsers performing MIME sniffing may interpret a response as HTML when it was intended as plain text, enabling XSS attacks. Including Content-Type is a baseline security practice.

## Sources

- [RFC 9110 §8.3 -- Content-Type](https://www.rfc-editor.org/rfc/rfc9110#section-8.3)
