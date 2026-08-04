---
title: "Underscore TE — Header Normalization"
description: "A POST request to /echo with a valid Content-Length: 11 for body framing, plus a malformed Transfer_Encoding: chunked header using an underscore instead of a hyphen."
weight: 5
---

| | |
|---|---|
| **Test ID** | `NORM-UNDERSCORE-TE` |
| **Category** | Normalization |
| **Requirement** | Drop or reject |
| **Expected** | Reject/drop (pass), normalize (fail), preserve (warn) |

## What it sends

A POST request to `/echo` with a valid `Content-Length: 11` for body framing, plus a malformed `Transfer_Encoding: chunked` header using an underscore instead of a hyphen.

```http
POST /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 11\r\n
Transfer_Encoding: chunked\r\n
\r\n
hello world
```

## What the RFC says

Like `Content_Length`, the name `Transfer_Encoding` is a valid token per the `tchar` production but is not the standard `Transfer-Encoding` header. The security concern is whether the server maps the underscore variant to the real Transfer-Encoding header.

## Pass / Fail / Warn

**Pass:** Server rejects the request (`400`) or drops the `Transfer_Encoding` header.
**Fail:** Server normalizes `Transfer_Encoding` to `Transfer-Encoding` — this creates a CL/TE conflict.
**Warn:** Server preserves the original name — the echo shows `Transfer_Encoding: chunked`.

## Why it matters

This is the Transfer-Encoding counterpart to `NORM-UNDERSCORE-CL` and closely related to `SMUG-TRANSFER_ENCODING`. If a proxy passes `Transfer_Encoding: chunked` through without recognizing it, but the back-end normalizes it to `Transfer-Encoding: chunked`, the back-end will use chunked framing while the proxy used Content-Length. This is a textbook CL.TE smuggling vector.

The existing `SMUG-TRANSFER_ENCODING` test checks if the server *processes* the underscore form. This normalization test additionally checks whether the *name itself* appears normalized in the echo output, regardless of whether the server acted on the value.
