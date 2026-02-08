---
title: "NUL-IN-URL"
description: "NUL-IN-URL test documentation"
weight: 7
---

| | |
|---|---|
| **Test ID** | `MAL-NUL-IN-URL` |
| **Category** | Malformed Input |
| **Expected** | `400` or close |

## What it sends

A request with a NUL byte (`\x00`) embedded in the URL.

```http
GET /\x00test HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

The URL contains a NUL byte (`\x00`) between `/` and `test`.


## What the RFC says

The request-target in an HTTP/1.1 request must conform to the URI grammar from RFC 3986. The `pchar` rule defines valid path characters:

> `pchar = unreserved / pct-encoded / sub-delims / ":" / "@"` — RFC 3986 Section 3.3

> `unreserved = ALPHA / DIGIT / "-" / "." / "_" / "~"` — RFC 3986 Section 2.3

A raw NUL byte (`0x00`) is not included in any of these productions, making it an invalid character in a URI path.

> "When a server listening only for HTTP request messages...receives a sequence of octets that does not match the HTTP-message grammar...the server SHOULD respond with a 400 (Bad Request) response and close the connection." — RFC 9112 Section 2.2

## Why it matters

NUL bytes terminate strings in C/C++. A NUL in the URL could cause path truncation in backend systems, allowing path traversal or access to unintended resources.

## Deep Analysis

### Relevant ABNF

```
request-line   = method SP request-target SP HTTP-version
origin-form    = absolute-path [ "?" query ]
absolute-path  = 1*( "/" segment )
segment        = *pchar
pchar          = unreserved / pct-encoded / sub-delims / ":" / "@"
unreserved     = ALPHA / DIGIT / "-" / "." / "_" / "~"
pct-encoded    = "%" HEXDIG HEXDIG
sub-delims     = "!" / "$" / "&" / "'" / "(" / ")"
               / "*" / "+" / "," / ";" / "="
```

### RFC Evidence

> "A URI is composed from a limited set of characters consisting of digits, letters, and a few graphic symbols."
> -- RFC 3986 Section 2

> `pchar = unreserved / pct-encoded / sub-delims / ":" / "@"`
> -- RFC 3986 Section 3.3

> "When a server listening only for HTTP request messages...receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection."
> -- RFC 9112 Section 2.2

### Chain of Reasoning

1. **NUL (`0x00`) matches no URI production.** Walking through every alternative in `pchar`: `unreserved` covers only `ALPHA` (`%x41-5A`, `%x61-7A`), `DIGIT` (`%x30-39`), and four specific symbols; `pct-encoded` requires a leading `%`; `sub-delims` lists specific ASCII punctuation; `:` is `%x3A`; `@` is `%x40`. The byte `0x00` is below the lowest value in any of these sets.

2. **NUL is not even a valid request-line octet.** The `request-line` grammar requires `method SP request-target SP HTTP-version`. The `SP` character is `%x20`. The `method` is a `token` (requires `tchar`, minimum `%x21`). There is no production in the HTTP/1.1 grammar that accommodates `%x00` anywhere in the request-line.

3. **The grammar violation triggers the rejection rule.** Since the `request-target` contains an octet that does not match the URI grammar, the entire request-line fails to match `HTTP-message`. RFC 9112 Section 2.2 instructs the server to respond with 400 and close.

4. **C-string truncation is the primary exploit vector.** In C and C++, strings are NUL-terminated. A NUL byte at position 1 in `/\x00test` would cause `strlen()` to return 1, making the path appear as just `/`. This can bypass path-based access controls, allow directory listing where only specific files should be served, or truncate filenames to access unintended resources.

5. **No robustness exception applies.** The only robustness exception in RFC 9112 Section 2.2 is ignoring empty CRLF lines before the request-line. NUL bytes receive no such exception.

## Sources

- [RFC 3986 Section 3.3](https://www.rfc-editor.org/rfc/rfc3986#section-3.3) — URI path and pchar grammar
- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) — rejection of invalid messages
