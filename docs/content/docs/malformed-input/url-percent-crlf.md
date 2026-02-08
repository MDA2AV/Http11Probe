---
title: "URL-PERCENT-CRLF"
description: "URL-PERCENT-CRLF test documentation"
weight: 24
---

| | |
|---|---|
| **Test ID** | `MAL-URL-PERCENT-CRLF` |
| **Category** | Malformed Input |
| **Expected** | `400` = Pass, `2xx`/`404` = Warn |

## What it sends

A GET request with percent-encoded CRLF characters (`%0d%0a`) in the URL, followed by an injected header.

```http
GET /path%0d%0aX-Injected:%20true HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

The percent-encodings `%0d` and `%0a` are syntactically valid per the URI grammar:

> `pct-encoded = "%" HEXDIG HEXDIG` — RFC 3986 Section 2.1

However, the decoded values (CR and LF) are HTTP message delimiters. If the server percent-decodes the request-target before parsing is complete, the decoded CR LF bytes can be interpreted as header line terminators:

> "A sender MUST NOT generate a bare CR (a CR character not immediately followed by LF) within any protocol elements other than the content." — RFC 9112 Section 2.2

The RFC explicitly treats CR and LF as dangerous in field values:

> "Field values containing CR, LF, or NUL characters are invalid and dangerous, due to the varying ways that implementations might parse and interpret those characters." — RFC 9110 Section 5.5

## Pass/Warn explanation

- **Pass (400):** The server rejects the request containing `%0d%0a` in the URL, preventing CRLF injection.
- **Warn (2xx/404):** The server processed the request without injecting headers. It may have handled the encoded CRLF safely, but accepting this input is a risk if other components in the pipeline decode differently.

## Why it matters

Percent-encoded CRLF (`%0d%0a`) in the URL is a header injection vector if the server percent-decodes during initial request parsing. This could allow injecting arbitrary HTTP headers, splitting the response, or poisoning caches.

## Deep Analysis

### Relevant ABNF

```
request-target = origin-form / absolute-form / authority-form / asterisk-form
origin-form    = absolute-path [ "?" query ]
segment        = *pchar
pchar          = unreserved / pct-encoded / sub-delims / ":" / "@"
pct-encoded    = "%" HEXDIG HEXDIG

field-vchar    = VCHAR / obs-text
VCHAR          = %x21-7E
```

### RFC Evidence

> `pct-encoded = "%" HEXDIG HEXDIG`
> -- RFC 3986 Section 2.1

> "A percent-encoded octet is encoded as a character triplet, consisting of the percent character '%' followed by the two hexadecimal digits representing that octet's numeric value."
> -- RFC 3986 Section 2.1

> "Field values containing CR, LF, or NUL characters are invalid and dangerous, due to the varying ways that implementations might parse and interpret those characters; a recipient of CR, LF, or NUL within a field value MUST either reject the message or replace each of those characters with SP before further processing or forwarding of that message."
> -- RFC 9110 Section 5.5

### Chain of Reasoning

1. **The percent-encodings are syntactically valid.** `%0d` and `%0a` conform to `pct-encoded = "%" HEXDIG HEXDIG`. At the URI grammar level, the request-target `/path%0d%0aX-Injected:%20true` is a valid `origin-form` -- percent-encoded octets are allowed in path segments.

2. **The danger arises from premature decoding.** If a server percent-decodes the request-target before completing HTTP message parsing, `%0d%0a` becomes `CR LF` (`0x0D 0x0A`). These are the HTTP line terminator characters. The decoded result would appear to the parser as a line break followed by `X-Injected: true` -- an injected header field.

3. **CR and LF are explicitly called out as dangerous.** RFC 9110 Section 5.5 uses strong language: "invalid and dangerous, due to the varying ways that implementations might parse and interpret those characters." The MUST-level requirement to reject or replace applies to the decoded values if they reach the field-value layer.

4. **The correct parsing order prevents injection.** A properly implemented HTTP/1.1 parser first splits the message on raw CRLF boundaries to identify the request-line and header fields, then percent-decodes the request-target during URI interpretation. In this order, `%0d%0a` remains encoded during the structural parsing phase and never creates a spurious line break.

5. **Warn for 2xx/404 reflects implementation-dependent safety.** A server returning 2xx or 404 may have handled the percent-encoded CRLF safely (correct parse order), but the acceptance creates risk if other components in the pipeline (reverse proxies, WAFs, backend applications) decode at a different stage. The request is a valid probe for CRLF injection vulnerabilities across the request chain.

## Sources

- [RFC 3986 Section 2.1](https://www.rfc-editor.org/rfc/rfc3986#section-2.1) — percent-encoding grammar
- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) — bare CR prohibition
- [RFC 9110 Section 5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) — CR/LF characters are dangerous
- [CWE-113](https://cwe.mitre.org/data/definitions/113.html) — Improper Neutralization of CRLF Sequences
