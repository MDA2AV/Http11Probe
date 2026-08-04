---
title: "Case TE — Header Normalization"
description: "A POST request to /echo with a valid Content-Length: 11 for body framing, plus an all-uppercase TRANSFER-ENCODING: chunked header."
weight: 4
---

| | |
|---|---|
| **Test ID** | `NORM-CASE-TE` |
| **Category** | Normalization |
| **Scored** | No |
| **Expected** | Reject/drop (pass), normalize casing (fail), preserve (warn) |

## What it sends

A POST request to `/echo` with a valid `Content-Length: 11` for body framing, plus an all-uppercase `TRANSFER-ENCODING: chunked` header.

```http
POST /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 11\r\n
TRANSFER-ENCODING: chunked\r\n
\r\n
hello world
```

## What the RFC says

RFC 9110 Section 5.1:

> "Each field name [...] is case-insensitive."

This means `TRANSFER-ENCODING` and `Transfer-Encoding` are semantically identical. Servers are required to treat them the same way.

## Pass / Fail / Warn

**Pass:** Server rejects (CL/TE conflict) or drops the header.
**Fail:** Server normalizes casing to `Transfer-Encoding` or `transfer-encoding` in the echo output.
**Warn:** Server preserves the original `TRANSFER-ENCODING` casing.

## Why it matters

This test is **unscored** because case normalization of header names is RFC-compliant and common. It provides visibility into how the server processes header name casing, which is informational for understanding proxy-chain behavior.

If the server processes `TRANSFER-ENCODING: chunked` as a real Transfer-Encoding header, the CL/TE conflict would cause the request to be rejected (which is Pass). The interesting case is when the echo reveals casing transformation without the server acting on the value.
