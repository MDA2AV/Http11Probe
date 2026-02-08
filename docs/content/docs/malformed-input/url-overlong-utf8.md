---
title: "URL-OVERLONG-UTF8"
description: "URL-OVERLONG-UTF8 test documentation"
weight: 22
---

| | |
|---|---|
| **Test ID** | `MAL-URL-OVERLONG-UTF8` |
| **Category** | Malformed Input |
| **Expected** | `400` or close |

## What it sends

A GET request with raw overlong UTF-8 bytes in the URL path. The bytes `0xC0 0xAF` are an overlong encoding of `/` (U+002F).

```http
GET /\xC0\xAF HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

The two bytes after `/` are `0xC0 0xAF` -- an illegal two-byte UTF-8 sequence that decodes to the ASCII forward slash character.

## What the RFC says

Raw bytes `0xC0` and `0xAF` are not valid URI characters. URI paths are limited to `pchar`:

> `pchar = unreserved / pct-encoded / sub-delims / ":" / "@"` — RFC 3986 Section 3.3

All `unreserved` and `sub-delims` characters are ASCII (`0x21-0x7E`). Bytes `0xC0` and `0xAF` fall outside this range and are not percent-encoded, so they violate the URI grammar.

Additionally, RFC 3629 requires rejection of overlong encodings:

> "Implementations of the decoding algorithm above MUST protect against decoding invalid sequences." — RFC 3629 Section 3

The bytes `0xC0 0xAF` are an overlong UTF-8 encoding of `U+002F` (forward slash `/`), which must be encoded as the single byte `0x2F`.

## Why it matters

Overlong UTF-8 sequences encode characters using more bytes than necessary. If a server decodes `0xC0 0xAF` as `/` during path resolution, it can bypass path traversal filters (e.g., `..%c0%af..` becomes `../../`). This was the basis of the infamous IIS Unicode directory traversal exploit (CVE-2000-0884).

## Deep Analysis

### Relevant ABNF

```
request-line   = method SP request-target SP HTTP-version
origin-form    = absolute-path [ "?" query ]
segment        = *pchar
pchar          = unreserved / pct-encoded / sub-delims / ":" / "@"
unreserved     = ALPHA / DIGIT / "-" / "." / "_" / "~"
```

### RFC Evidence

> `pchar = unreserved / pct-encoded / sub-delims / ":" / "@"`
> -- RFC 3986 Section 3.3

> "Implementations of the decoding algorithm above MUST protect against decoding invalid sequences."
> -- RFC 3629 Section 3

> "The security threat is very real. [...] a widespread virus attacking Web servers in 2001 relied on the mishandling of overlong UTF-8 sequences to compromise vulnerable systems."
> -- RFC 3629 Section 10

### Chain of Reasoning

1. **The raw bytes are not valid URI characters.** Bytes `0xC0` and `0xAF` both fall outside the ASCII range used by `pchar`. Neither matches `unreserved` (limited to `ALPHA`, `DIGIT`, `-`, `.`, `_`, `~`, all below `0x7F`), `sub-delims`, `:`, `@`, or `pct-encoded` (requires a leading `%`). The request-target violates the URI grammar at the byte level, independent of any UTF-8 interpretation.

2. **The bytes form an overlong UTF-8 encoding of `/`.** In standard UTF-8, `U+002F` (forward slash) is encoded as the single byte `0x2F`. The two-byte sequence `0xC0 0xAF` uses the `110xxxxx 10xxxxxx` pattern with the value bits `00000 101111` = `0x2F`. This is an overlong encoding: it uses 2 bytes where 1 byte suffices.

3. **Overlong sequences MUST be rejected.** RFC 3629 Section 3 requires that implementations "MUST protect against decoding invalid sequences." Overlong encodings are explicitly invalid because they violate the shortest-form requirement. A conforming UTF-8 decoder must not accept `0xC0 0xAF` as equivalent to `0x2F`.

4. **CVE-2000-0884 exploited exactly this pattern.** Microsoft IIS on Windows decoded overlong UTF-8 sequences in URLs, allowing `..%c0%af..` to be interpreted as `../../`. This enabled remote directory traversal, giving attackers access to files outside the web root. RFC 3629 Section 10 explicitly references this class of attack, noting "a widespread virus attacking Web servers in 2001" exploited overlong UTF-8 mishandling.

5. **Two layers of defense apply.** First, the bytes fail the URI grammar (they are not valid `pchar`), so a strict URI parser will reject the request before any UTF-8 decoding. Second, even if a server attempts UTF-8 decoding, RFC 3629 mandates rejection of the overlong sequence. A server that accepts this request has failed at both layers.

## Sources

- [RFC 3986 Section 3.3](https://www.rfc-editor.org/rfc/rfc3986#section-3.3) — URI path and pchar grammar
- [RFC 3629 Section 3](https://www.rfc-editor.org/rfc/rfc3629#section-3) — UTF-8 decoding requirements
- [RFC 3629 Section 10](https://www.rfc-editor.org/rfc/rfc3629#section-10) — UTF-8 security considerations
- [CVE-2000-0884](https://nvd.nist.gov/vuln/detail/CVE-2000-0884) — IIS Unicode directory traversal
