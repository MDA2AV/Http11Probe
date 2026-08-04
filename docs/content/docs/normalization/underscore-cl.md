---
title: "Underscore CL — Header Normalization"
description: "A POST request to /echo with a valid Content-Length: 11 for body framing, plus a malformed Content_Length: 99 header using an underscore instead of a hyphen."
weight: 1
---

| | |
|---|---|
| **Test ID** | `NORM-UNDERSCORE-CL` |
| **Category** | Normalization |
| **Requirement** | Drop or reject |
| **Expected** | Reject/drop (pass), normalize (fail), preserve (warn) |

## What it sends

A POST request to `/echo` with a valid `Content-Length: 11` for body framing, plus a malformed `Content_Length: 99` header using an underscore instead of a hyphen.

```http
POST /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 11\r\n
Content_Length: 99\r\n
\r\n
hello world
```

## What the RFC says

RFC 9110 defines header field names using the `token` production (RFC 9110 Section 5.1), which includes hyphens (`-`) but also underscores (`_`). So `Content_Length` is technically a valid header *name* per the grammar, but it is not the standard `Content-Length` header.

The security concern is whether the server silently maps `Content_Length` to `Content-Length`. If it does, the malformed name becomes a real framing header that upstream proxies may not have recognized.

## Pass / Fail / Warn

**Pass:** Server rejects the request (`400`) or drops the `Content_Length` header (echo does not contain it).
**Fail:** Server normalizes `Content_Length` to `Content-Length` — the echo shows `Content-Length: 99`.
**Warn:** Server preserves the original name — the echo shows `Content_Length: 99`.

## Why it matters

In a proxy chain, an upstream server may pass `Content_Length: 99` through as an unknown header. If the back-end normalizes it to `Content-Length: 99`, the request now has conflicting Content-Length values (11 vs 99), creating a classic request smuggling vector.

This is the same class of attack tested by `SMUG-TRANSFER_ENCODING`, but applied to Content-Length instead of Transfer-Encoding.
