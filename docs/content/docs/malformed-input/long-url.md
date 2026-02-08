---
title: "LONG-URL"
description: "LONG-URL test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `MAL-LONG-URL` |
| **Category** | Malformed Input |
| **RFC** | [RFC 9112 Section 3](https://www.rfc-editor.org/rfc/rfc9112#section-3) |
| **Expected** | `400`, `414`, `431`, or close |

## What it sends

A request with a ~100 KB URL.

```http
GET /AAAA...{100,000 × 'A'}... HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

The URL path is 100,001 bytes long (a `/` followed by 100,000 `A` characters).


## What the RFC says

> "The 414 (URI Too Long) status code indicates that the server is refusing to service the request because the target URI is longer than the server is willing to interpret." — RFC 9110 Section 15.5.15

> "It is RECOMMENDED that all HTTP senders and recipients support, at a minimum, request-line lengths of 8000 octets." — RFC 9112 Section 3

A 100KB URL far exceeds the recommended minimum. The server may also respond with 400 (general client error) or close the connection.

## Deep Analysis

### Relevant ABNF

```
request-line   = method SP request-target SP HTTP-version
request-target = origin-form / absolute-form / authority-form / asterisk-form
pchar          = unreserved / pct-encoded / sub-delims / ":" / "@"
unreserved     = ALPHA / DIGIT / "-" / "." / "_" / "~"
```

### RFC Evidence

> "HTTP does not place a predefined limit on the length of a request-line."
> -- RFC 9112 Section 3

> "A server that receives a request-target longer than any URI it wishes to parse MUST respond with a 414 (URI Too Long) status code."
> -- RFC 9112 Section 3

> "It is RECOMMENDED that all HTTP senders and recipients support, at a minimum, request-line lengths of 8000 octets."
> -- RFC 9112 Section 3

> "The 414 (URI Too Long) status code indicates that the server is refusing to service the request because the target URI is longer than the server is willing to interpret."
> -- RFC 9110 Section 15.5.15

### Chain of Reasoning

1. **The request is syntactically valid.** A 100,000-character path of `A` characters is composed entirely of `ALPHA`, which satisfies `unreserved` and therefore `pchar`. The request-line grammar itself is not violated.

2. **No maximum length is mandated.** RFC 9112 Section 3 explicitly states that HTTP does not place a predefined limit on request-line length. However, servers are free to impose their own limits.

3. **The MUST-level requirement applies.** When a server encounters a request-target longer than it is willing to parse, RFC 9112 Section 3 uses MUST-level language: the server "MUST respond with a 414 (URI Too Long) status code." This is one of the few places the RFC mandates a specific status code for length violations.

4. **The 8,000-octet recommendation sets a floor.** The RECOMMENDED minimum of 8,000 octets means a 100,001-byte URL (100,000 `A` + leading `/`) exceeds the recommended minimum by over 12x. Any server implementing the recommended minimum would reject this.

5. **Alternative responses are acceptable.** A server may also respond with 400 (general syntax error), 431 (header fields too large, if the entire request-line is counted toward header limits), or simply close the connection. All indicate the server is protecting itself from oversized input.

## Sources

- [RFC 9110 Section 15.5.15](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.15) — 414 URI Too Long
- [RFC 9112 Section 3](https://www.rfc-editor.org/rfc/rfc9112#section-3) — request-line length recommendation
