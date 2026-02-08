---
title: "HOST-EMPTY-VALUE"
description: "HOST-EMPTY-VALUE test documentation"
weight: 6
---

| | |
|---|---|
| **Test ID** | `COMP-HOST-EMPTY-VALUE` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A request with a `Host` header present but with an empty value.

```http
GET / HTTP/1.1\r\n
Host: \r\n
\r\n
```

The `Host` header line exists, but its value is empty (nothing between the colon and CRLF).

## What the RFC says

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

> "If the authority component is missing or undefined for the target URI, then a client MUST send a Host header field with an empty field value." -- RFC 9112 Section 3.2

An empty Host value is only valid when the authority component of the target URI is missing or undefined. For an origin-form request like `GET / HTTP/1.1`, the authority must come from the Host header, so an empty value leaves the server unable to determine which virtual host is being addressed.

## Why it matters

A Host header with an empty value is functionally equivalent to having no Host header at all. If a server accepts this, it may fall back to a default virtual host, potentially serving content from an unintended application. In multi-tenant environments, this can lead to information disclosure or incorrect routing.

## Deep Analysis

### Relevant ABNF Grammar

```
Host     = uri-host [ ":" port ]
uri-host = <host, see [URI], Section 3.2.2>
```

The Host grammar allows `uri-host` which can be an IP-literal, IPv4address, or reg-name. An empty string is a degenerate case: it does not match any of these productions (reg-name can match empty, but the RFC prohibits empty host identifiers).

### RFC Evidence

**RFC 9112 Section 3.2** provides the validity rule:

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

**RFC 9112 Section 3.2** describes when an empty Host is legitimate:

> "If the authority component is missing or undefined for the target URI, then a client MUST send a Host header field with an empty field value." -- RFC 9112 Section 3.2

**RFC 9110 Section 4.2.1** prohibits empty host identifiers:

> "A sender MUST NOT generate an 'http' URI with an empty host identifier." -- RFC 9110 Section 4.2.1

### Chain of Reasoning

1. The test sends `Host: ` (empty value) with an origin-form request-target (`GET / HTTP/1.1`).
2. An empty Host value is only valid when the authority component of the target URI is "missing or undefined." For an origin-form request, the target URI is reconstructed using the Host header as the authority. An empty Host means no authority can be determined.
3. Since the request uses origin-form, the authority is neither "missing" nor "undefined" -- it is expected to be provided by the Host header. An empty value fails to provide the required information.
4. RFC 9110 Section 4.2.1 reinforces that http URIs must not have empty host identifiers. A request with an empty Host to an http-scheme server would produce an invalid effective request URI.
5. The server cannot determine which virtual host is being addressed, which is functionally equivalent to a missing Host header.

### Scoring Justification

**Scored (MUST).** An empty Host value on an origin-form request results in an invalid effective request URI, making it an "invalid field value" under RFC 9112 Section 3.2. The MUST-400 requirement applies. Both 400 and connection close are accepted because the empty Host creates ambiguity about whether the request has a valid target at all.

## Sources

- [RFC 9112 Section 3.2 -- Request Target](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
- [RFC 9110 Section 7.2 -- Host and :authority](https://www.rfc-editor.org/rfc/rfc9110#section-7.2)
