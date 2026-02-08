---
title: "URL-BACKSLASH"
description: "URL-BACKSLASH test documentation"
weight: 21
---

| | |
|---|---|
| **Test ID** | `MAL-URL-BACKSLASH` |
| **Category** | Malformed Input |
| **Expected** | `400` = Pass, `2xx`/`404` = Warn |

## What it sends

A GET request with a backslash in the URL path.

```http
GET /path\file HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

The valid characters in a URI path segment are defined by `pchar`:

> `pchar = unreserved / pct-encoded / sub-delims / ":" / "@"` — RFC 3986 Section 3.3

> `unreserved = ALPHA / DIGIT / "-" / "." / "_" / "~"` — RFC 3986 Section 2.3

> `sub-delims = "!" / "$" / "&" / "'" / "(" / ")" / "*" / "+" / "," / ";" / "="` — RFC 3986 Section 2.2

Backslash (`\`, `0x5C`) is not included in `unreserved`, `sub-delims`, `pct-encoded`, `":"`, or `"@"`. It is therefore not a valid URI path character.

## Pass/Warn explanation

- **Pass (400):** The server rejects the request because backslash is not a valid URI character.
- **Warn (2xx/404):** The server processed the request despite the invalid URI character. This may indicate the server normalizes `\` to `/`, which is a path traversal risk.

## Why it matters

Some servers (especially on Windows) normalize `\` to `/`, which can enable path traversal attacks if used to bypass URL filters. For example, a WAF blocking `../` might not block `..\`, allowing an attacker to traverse directories on servers that treat backslash as a path separator.

## Deep Analysis

### Relevant ABNF

```
request-line   = method SP request-target SP HTTP-version
origin-form    = absolute-path [ "?" query ]
absolute-path  = 1*( "/" segment )
segment        = *pchar
pchar          = unreserved / pct-encoded / sub-delims / ":" / "@"
unreserved     = ALPHA / DIGIT / "-" / "." / "_" / "~"
sub-delims     = "!" / "$" / "&" / "'" / "(" / ")"
               / "*" / "+" / "," / ";" / "="
```

### RFC Evidence

> "A URI is composed from a limited set of characters consisting of digits, letters, and a few graphic symbols."
> -- RFC 3986 Section 2

> `pchar = unreserved / pct-encoded / sub-delims / ":" / "@"`
> `unreserved = ALPHA / DIGIT / "-" / "." / "_" / "~"`
> -- RFC 3986 Section 3.3

> "A recipient SHOULD NOT attempt to autocorrect and then process the request without a redirect, since the invalid request-line might be deliberately crafted to bypass security filters along the request chain."
> -- RFC 9112 Section 3

### Chain of Reasoning

1. **Backslash (`\`, `0x5C`) is not a valid URI character.** Enumerating every alternative in `pchar`: `unreserved` allows only `ALPHA`, `DIGIT`, `-`, `.`, `_`, `~`; `pct-encoded` requires a leading `%`; `sub-delims` lists `!`, `$`, `&`, `'`, `(`, `)`, `*`, `+`, `,`, `;`, `=`; the remaining alternatives are `:` and `@`. Backslash appears in none of these. It is also absent from `gen-delims` (`:`, `/`, `?`, `#`, `[`, `]`, `@`) and `reserved`.

2. **The request-target violates the URI grammar.** Since `\` at `0x5C` is not a `pchar`, the `segment` containing `path\file` fails to parse. The entire `origin-form` production fails, and therefore the `request-line` does not match the grammar.

3. **Autocorrection is explicitly discouraged.** RFC 9112 Section 3 warns that a recipient "SHOULD NOT attempt to autocorrect and then process the request without a redirect, since the invalid request-line might be deliberately crafted to bypass security filters." A server that silently normalizes `\` to `/` is performing exactly the kind of autocorrection the RFC warns against.

4. **Windows path separator confusion is the exploit vector.** On Windows systems, `\` and `/` are interchangeable path separators. A WAF or reverse proxy that blocks `../` in URLs will not match `..\` -- but if the backend server normalizes backslash to forward slash, the attacker achieves path traversal through the filter gap.

5. **Warn for 2xx/404 reflects the ambiguity.** A server that returns 2xx or 404 has processed the request despite the invalid character. It may have handled the backslash safely (e.g., treated it as a literal filename character), but the acceptance of non-URI characters is still a concern because it indicates the parser is more permissive than the grammar allows.

## Sources

- [RFC 3986 Section 3.3](https://www.rfc-editor.org/rfc/rfc3986#section-3.3) — path and pchar grammar
- [RFC 3986 Section 2.2](https://www.rfc-editor.org/rfc/rfc3986#section-2.2) — reserved characters
- [RFC 3986 Section 2.3](https://www.rfc-editor.org/rfc/rfc3986#section-2.3) — unreserved characters
