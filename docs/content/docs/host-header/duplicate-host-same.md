---
title: "DUPLICATE-HOST-SAME"
description: "DUPLICATE-HOST-SAME test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `COMP-DUPLICATE-HOST-SAME` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | MUST respond with 400 |
| **Expected** | `400` |

## What it sends

A request with two identical Host headers.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Host: localhost:8080\r\n
\r\n
```

Two `Host` headers with identical values.


## What the RFC says

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

The phrase "more than one Host header field line" makes no exception for identical values.

## Why it matters

The RFC mandates 400 for *any* duplicate Host headers, regardless of whether the values match. Some servers incorrectly allow identical duplicates.

## Deep Analysis

### Relevant ABNF Grammar

```
Host = uri-host [ ":" port ]
```

The Host header is a singleton field. Its ABNF does not use the `#` list syntax, meaning only one Host header field line is permitted per request. The grammar makes no provision for combining or deduplicating multiple instances.

### RFC Evidence

**RFC 9112 Section 3.2** covers this case with the same MUST as different-value duplicates:

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

**RFC 9112 Section 3.2** mandates the client obligation:

> "A client MUST send a Host header field in all HTTP/1.1 request messages." -- RFC 9112 Section 3.2

**RFC 9110 Section 7.2** specifies the Host grammar:

> "Host = uri-host [ ':' port ]" -- RFC 9110 Section 7.2

### Chain of Reasoning

1. The test sends two Host headers both containing `localhost:8080` -- identical values.
2. The RFC says "more than one Host header field line" without any qualifier about whether the values differ. The count of Host lines is what matters, not the content.
3. Some servers optimize by deduplicating identical headers before validation, effectively collapsing two identical Host lines into one. This is non-compliant: the RFC counts field lines, not unique values.
4. A server that accepts identical duplicate Host headers may also accept different-value duplicates in certain edge cases (e.g., if case sensitivity or trailing whitespace causes the "identical" check to fail), creating a host injection vulnerability.
5. The 400 requirement is absolute -- no alternative disposition is offered.

### Scoring Justification

**Scored (MUST).** The RFC mandates exactly 400 for any request with more than one Host header field line, regardless of value equality. Connection close without sending 400 is non-compliant. The `AllowConnectionClose` flag is not set because the RFC explicitly requires the 400 status code.

## Sources

- [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
