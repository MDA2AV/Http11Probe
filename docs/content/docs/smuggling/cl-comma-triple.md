---
title: "CL Comma Triple — Request Smuggling"
description: "A POST request with three comma-separated identical Content-Length values, extending the duplicate-value merge test. Tested against RFC 9110 §8.6."
weight: 61
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-COMMA-TRIPLE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

A POST request with three comma-separated identical Content-Length values, extending the duplicate-value merge test.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5, 5, 5\r\n
\r\n
hello
```

## What the RFC says

> "A recipient of a Content-Length header field value consisting of the same decimal value repeated as a comma-separated list (e.g, 'Content-Length: 42, 42') MAY either reject the message as invalid or replace that invalid field value with a single instance of the decimal value, since this is likely the result of a duplicate being appended by an intermediary." -- RFC 9110 Section 8.6

## Why it matters

While the two-value case (`5, 5`) is the example given in the RFC, real-world intermediaries may append the header multiple times, producing three or more repetitions. Servers that handle the two-value case correctly may fail on three values if their parsing logic only checks for exactly one comma. This test verifies that the merge-or-reject logic generalizes beyond the minimum RFC example. A server that rejects is being strict (pass); a server that merges to the single value is RFC-compliant (warn).

## Sources

- [RFC 9110 §8.6 -- Content-Length](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
