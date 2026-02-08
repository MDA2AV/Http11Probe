---
title: "CHUNKED-BODY"
description: "CHUNKED-BODY test documentation"
weight: 6
---

| | |
|---|---|
| **Test ID** | `COMP-CHUNKED-BODY` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` |

## What it sends

A valid chunked POST with a single 5-byte chunk followed by the zero terminator.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
0\r\n
\r\n
```

## What the RFC says

> "The chunked transfer coding wraps content in order to transfer it as a series of chunks, each with its own size indicator, followed by an OPTIONAL trailer section containing trailer fields." — RFC 9112 Section 7.1

> "A recipient MUST be able to parse and decode the chunked transfer coding." — RFC 9112 Section 7.1

> "A recipient MUST be able to parse the chunked transfer coding because it plays a crucial role in framing messages when the content size is not known in advance." — RFC 9112 Section 6.1

A server that supports HTTP/1.1 must be able to decode chunked transfer encoding. This is a MUST-level requirement.

## Why it matters

Chunked encoding is fundamental to HTTP/1.1 — it enables streaming, server-sent data, and requests where the body size isn't known in advance. If a server can't decode a basic chunked body, it cannot fully participate in HTTP/1.1.

## Deep Analysis

### Relevant ABNF grammar

From RFC 9112 Section 7.1:

```
chunked-body   = *chunk
                 last-chunk
                 trailer-section
                 CRLF

chunk          = chunk-size [ chunk-ext ] CRLF
                 chunk-data CRLF
chunk-size     = 1*HEXDIG
last-chunk     = 1*("0") [ chunk-ext ] CRLF

chunk-data     = 1*OCTET ; a sequence of chunk-size octets
trailer-section = *( field-line CRLF )
```

### Direct RFC quotes

> "The chunked transfer coding wraps content in order to transfer it as a series of chunks, each with its own size indicator, followed by an OPTIONAL trailer section containing trailer fields." -- RFC 9112 Section 7.1

> "A recipient MUST be able to parse and decode the chunked transfer coding." -- RFC 9112 Section 7.1

> "A recipient MUST be able to parse the chunked transfer coding because it plays a crucial role in framing messages when the content size is not known in advance." -- RFC 9112 Section 6.1

### Chain of reasoning

1. The test sends `Transfer-Encoding: chunked`, which triggers chunked body parsing per RFC 9112 Section 6.1.
2. Per the ABNF, the server must parse the chunk-size `5` (1*HEXDIG = "5"), read the CRLF, then read exactly 5 octets of chunk-data (`hello`), then read the trailing CRLF.
3. The next line is `0\r\n`, which matches `last-chunk = 1*("0") [ chunk-ext ] CRLF` -- this signals the end of chunked data.
4. The final `\r\n` satisfies the trailing CRLF in the `chunked-body` production.
5. The entire message is syntactically valid against the ABNF grammar. The server has no grounds to reject it.
6. RFC 9112 Section 7.1 uses "MUST be able to parse and decode" -- the strongest normative keyword. Failure to accept this request is a protocol violation.

### Scored / Unscored justification

**Scored.** The requirement uses MUST ("A recipient MUST be able to parse and decode the chunked transfer coding"). This is a non-negotiable RFC requirement. Any server claiming HTTP/1.1 support that rejects a syntactically valid single-chunk body is non-compliant. The test expects `2xx` with no fallback to `400` because there is no ambiguity in the grammar or the requirement level.

### Edge cases

- Some servers reject chunked encoding on POST if they expect `Content-Length` only -- this violates RFC 9112 Section 6.1 which mandates chunked parsing support.
- Servers behind load balancers may never see chunked requests if the LB de-chunks first, but the server itself must still support it.
- A few lightweight embedded HTTP servers omit chunked support entirely, treating it as an HTTP/1.0-only implementation. This test correctly flags that deficiency.

## Sources

- [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
