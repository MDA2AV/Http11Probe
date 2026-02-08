---
title: "URL-PERCENT-NULL"
description: "URL-PERCENT-NULL test documentation"
weight: 23
---

| | |
|---|---|
| **Test ID** | `MAL-URL-PERCENT-NULL` |
| **Category** | Malformed Input |
| **Expected** | `400` = Pass, `2xx`/`404` = Warn |

## What it sends

A GET request with a percent-encoded NUL byte (`%00`) in the URL path.

```http
GET /path%00.html HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

The percent-encoding `%00` is syntactically valid per the URI grammar:

> `pct-encoded = "%" HEXDIG HEXDIG` — RFC 3986 Section 2.1

However, the decoded value (NUL byte, `0x00`) is dangerous. While RFC 3986 does not explicitly prohibit `%00`, the HTTP semantics layer addresses NUL in field values:

> "Field values containing CR, LF, or NUL characters are invalid and dangerous, due to the varying ways that implementations might parse and interpret those characters." — RFC 9110 Section 5.5

The same principle applies to NUL in the request-target: implementations vary in how they handle it, creating security risks.

## Pass/Warn explanation

- **Pass (400):** The server rejects the request containing `%00` in the URL, preventing null byte injection attacks.
- **Warn (2xx/404):** The server processed the request. It may have decoded `%00` safely, but this is a security risk if the decoded NUL reaches backend systems (path truncation, access control bypass).

## Why it matters

Percent-encoded NUL byte (`%00`) can cause C-based servers to truncate the path string at the null byte. For example, `file%00.php` might be interpreted as `file` while bypassing extension-based access controls.

## Deep Analysis

### Relevant ABNF

```
request-target = origin-form / absolute-form / authority-form / asterisk-form
origin-form    = absolute-path [ "?" query ]
segment        = *pchar
pchar          = unreserved / pct-encoded / sub-delims / ":" / "@"
pct-encoded    = "%" HEXDIG HEXDIG
```

### RFC Evidence

> `pct-encoded = "%" HEXDIG HEXDIG`
> -- RFC 3986 Section 2.1

> "A percent-encoded octet is encoded as a character triplet, consisting of the percent character '%' followed by the two hexadecimal digits representing that octet's numeric value."
> -- RFC 3986 Section 2.1

> "Field values containing CR, LF, or NUL characters are invalid and dangerous, due to the varying ways that implementations might parse and interpret those characters; a recipient of CR, LF, or NUL within a field value MUST either reject the message or replace each of those characters with SP before further processing or forwarding of that message."
> -- RFC 9110 Section 5.5

### Chain of Reasoning

1. **The percent-encoding is syntactically valid.** `%00` conforms to `pct-encoded = "%" HEXDIG HEXDIG`. At the URI grammar level, `/path%00.html` is a valid `origin-form` -- percent-encoded octets are allowed in path segments regardless of the value they encode.

2. **The decoded value is a NUL byte (`0x00`).** When the server percent-decodes the request-target for path resolution, `%00` becomes the NUL character. RFC 9110 Section 5.5 explicitly identifies NUL as "invalid and dangerous" in the context of HTTP field values. While Section 5.5 technically addresses field values rather than URIs, the same danger applies: NUL bytes are interpreted inconsistently across implementations.

3. **C-string truncation is the primary attack.** In C and C++, strings are NUL-terminated. After percent-decoding, `/path\x00.html` may be truncated to `/path` by any function using `strlen()`, `strcmp()`, or similar. This enables: (a) bypassing extension-based access controls (e.g., `.php` restriction bypassed when the extension is truncated), (b) accessing unintended files (the truncated path resolves to a different resource), and (c) file upload filter bypass (e.g., `shell.php%00.jpg` passes a `.jpg` extension check but saves as `shell.php`).

4. **No RFC explicitly prohibits `%00` in URIs.** RFC 3986 does not list `%00` as a forbidden percent-encoding. However, RFC 9110 Section 5.5's treatment of NUL as "dangerous" establishes a clear security principle. A server that rejects `%00` in URLs is applying a reasonable security policy consistent with the RFC's treatment of NUL.

5. **Warn for 2xx/404 reflects the nuance.** Since `%00` is grammatically valid, a server that processes the request is not strictly violating the URI grammar. However, safely handling the decoded NUL requires the server (and every downstream component) to use NUL-safe string operations -- a fragile assumption. Rejection with 400 is the more robust approach.

## Sources

- [RFC 3986 Section 2.1](https://www.rfc-editor.org/rfc/rfc3986#section-2.1) — percent-encoding grammar
- [RFC 9110 Section 5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) — NUL characters are dangerous
- [CWE-158](https://cwe.mitre.org/data/definitions/158.html) — Improper Neutralization of Null Byte
