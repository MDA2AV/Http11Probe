---
title: "Tab In Name — Header Normalization"
description: "A POST request to /echo with a valid Content-Length: 11 for body framing, plus a header containing a tab character in the name: Content\\tLength: 99."
weight: 3
---

| | |
|---|---|
| **Test ID** | `NORM-TAB-IN-NAME` |
| **Category** | Normalization |
| **Requirement** | MUST reject (invalid token character) |
| **Expected** | Reject/drop (pass), normalize (fail), preserve (warn) |

## What it sends

A POST request to `/echo` with a valid `Content-Length: 11` for body framing, plus a header containing a tab character in the name: `Content\tLength: 99`.

```http
POST /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 11\r\n
Content[TAB]Length: 99\r\n
\r\n
hello world
```

## What the RFC says

RFC 9110 Section 5.1 defines field names using the `token` production:

> `token = 1*tchar`
> `tchar = "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~" / DIGIT / ALPHA`

The horizontal tab character (0x09) is not a valid `tchar`, so `Content\tLength` is not a valid header name.

## Pass / Fail / Warn

**Pass:** Server rejects the request (`400`) or drops the malformed header.
**Fail:** Server normalizes `Content\tLength` to `Content-Length` — the echo shows `Content-Length: 99`.
**Warn:** Server preserves the original name with the tab character.

## Why it matters

A server that converts a tab to a hyphen (or strips it) silently transforms an invalid header name into a real Content-Length header. Proxies that pass the tab-containing name through without recognizing it create a smuggling vector when the back-end normalizes.
