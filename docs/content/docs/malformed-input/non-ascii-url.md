---
title: "NON-ASCII-URL"
description: "NON-ASCII-URL test documentation"
weight: 10
---

| | |
|---|---|
| **Test ID** | `MAL-NON-ASCII-URL` |
| **Category** | Malformed Input |
| **Expected** | `400` or close |

## What it sends

A request with non-ASCII bytes in the URL.

```http
GET /caf\xC3\xA9 HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

The URL `/café` contains UTF-8 encoded `é` (`\xC3\xA9`) — non-ASCII bytes in the request-target.


## What the RFC says

> "A URI is composed from a limited set of characters consisting of digits, letters, and a few graphic symbols." — RFC 3986 Section 2

The path component of a URI is constrained to `pchar`:

> `pchar = unreserved / pct-encoded / sub-delims / ":" / "@"` — RFC 3986 Section 3.3

> `unreserved = ALPHA / DIGIT / "-" / "." / "_" / "~"` — RFC 3986 Section 2.3

All allowed characters are ASCII. Raw bytes `0xC3 0xA9` (UTF-8 for `e` with acute accent) are not valid URI characters -- they must be percent-encoded as `%C3%A9`.

> "A percent-encoded octet is encoded as a character triplet, consisting of the percent character '%' followed by the two hexadecimal digits representing that octet's numeric value." — RFC 3986 Section 2.1

## Deep Analysis

### Relevant ABNF

```
request-target = origin-form / absolute-form / authority-form / asterisk-form
origin-form    = absolute-path [ "?" query ]
absolute-path  = 1*( "/" segment )
segment        = *pchar
pchar          = unreserved / pct-encoded / sub-delims / ":" / "@"
unreserved     = ALPHA / DIGIT / "-" / "." / "_" / "~"
pct-encoded    = "%" HEXDIG HEXDIG
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

1. **The URI character set is strictly ASCII.** RFC 3986 Section 2 establishes that URIs are composed from a "limited set of characters." Every production rule -- `unreserved`, `reserved`, `sub-delims`, `gen-delims` -- references only characters in the ASCII range (`%x00-7F`). Non-ASCII octets have no place in a raw URI.

2. **The bytes `0xC3 0xA9` are not valid `pchar`.** These are the UTF-8 encoding of U+00E9 (e with acute accent). Neither `0xC3` nor `0xA9` match `unreserved` (limited to ASCII letters, digits, and four symbols), `sub-delims`, `:`, `@`, or `pct-encoded` (which requires a leading `%`). They fail to match any alternative in the `pchar` production.

3. **Percent-encoding is the correct representation.** The character `e` should appear as `%C3%A9` in the URI. RFC 3986 Section 2.1 specifies that "A percent-encoded octet is encoded as a character triplet, consisting of the percent character '%' followed by the two hexadecimal digits representing that octet's numeric value."

4. **The request-line grammar is violated.** Since the `request-target` contains bytes that do not match the `origin-form` production, the entire `request-line` fails to match the grammar defined in RFC 9112 Section 3.

5. **The server must not silently accept.** RFC 9112 Section 3 explicitly warns against autocorrection. A server that silently normalizes raw UTF-8 bytes into percent-encoded form and processes the request could be exploited by attackers crafting invalid URIs to bypass security filters in intermediaries.

## Sources

- [RFC 3986 Section 2](https://www.rfc-editor.org/rfc/rfc3986#section-2) — URI character set
- [RFC 3986 Section 2.1](https://www.rfc-editor.org/rfc/rfc3986#section-2.1) — percent-encoding
- [RFC 3986 Section 3.3](https://www.rfc-editor.org/rfc/rfc3986#section-3.3) — path and pchar ABNF
