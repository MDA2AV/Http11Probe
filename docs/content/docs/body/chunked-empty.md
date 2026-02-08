---
title: "CHUNKED-EMPTY"
description: "CHUNKED-EMPTY test documentation"
weight: 8
---

| | |
|---|---|
| **Test ID** | `COMP-CHUNKED-EMPTY` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` or close |

## What it sends

A chunked POST with only the zero terminator — a zero-length body.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Transfer-Encoding: chunked\r\n
\r\n
0\r\n
\r\n
```

## What the RFC says

> "A recipient MUST be able to parse and decode the chunked transfer coding." — RFC 9112 Section 7.1

The chunked grammar defines `last-chunk = 1*("0") [ chunk-ext ] CRLF`. A zero-size first chunk is the terminator and indicates an empty body. The server must recognize it and not block waiting for additional data.

The grammar allows `*chunk` (zero or more data chunks) before the `last-chunk`, so a chunked body containing only the zero terminator is syntactically valid.

## Why it matters

Empty chunked bodies occur when a client starts a chunked transfer but has nothing to send, or when a proxy rewrites a zero-length CL body into chunked encoding. The server must handle this edge case cleanly.

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

> "A recipient MUST be able to parse and decode the chunked transfer coding." -- RFC 9112 Section 7.1

> "A recipient MUST be able to parse the chunked transfer coding because it plays a crucial role in framing messages when the content size is not known in advance." -- RFC 9112 Section 6.1

> "The chunked transfer coding wraps content in order to transfer it as a series of chunks, each with its own size indicator, followed by an OPTIONAL trailer section containing trailer fields." -- RFC 9112 Section 7.1

### Chain of reasoning

1. The test sends `Transfer-Encoding: chunked`, activating chunked body parsing per RFC 9112 Section 6.1.
2. The ABNF production `chunked-body = *chunk last-chunk trailer-section CRLF` uses `*chunk`, meaning **zero or more** data chunks are valid before the `last-chunk`.
3. The first (and only) line of the body is `0\r\n`, which matches `last-chunk = 1*("0") [ chunk-ext ] CRLF`. This is the zero-length terminator with no preceding data chunks.
4. The `trailer-section` production is `*( field-line CRLF )` -- zero or more trailer fields. In this test, there are none.
5. The final `\r\n` satisfies the trailing CRLF in the `chunked-body` production.
6. The complete body `0\r\n\r\n` is a valid instance of `chunked-body` with zero data chunks, zero trailer fields. The grammar explicitly permits this.
7. A server that blocks waiting for additional data after seeing the zero-length chunk has failed to correctly implement the chunked decoder.

### Scored / Unscored justification

**Scored.** The requirement uses MUST ("A recipient MUST be able to parse and decode the chunked transfer coding"). The `*chunk` production (zero or more) explicitly allows an empty body. The server must accept this and respond with `2xx` or close the connection cleanly. The `AllowConnectionClose` flag is set because some servers may close the connection after processing a zero-length chunked body, which is acceptable behavior.

### Edge cases

- Some servers interpret a zero-length chunked body as "no body at all" and respond with `411 Length Required`, which is incorrect because the framing headers (Transfer-Encoding: chunked) are present and well-formed.
- Proxies may rewrite `Content-Length: 0` into chunked encoding, producing exactly this payload. Servers must handle it.
- A server that hangs waiting for data after the `0\r\n\r\n` terminator has a bug in its chunked state machine -- it is not recognizing the last-chunk production.
- Some implementations require at least one non-zero chunk before the terminator, which contradicts the `*chunk` (zero-or-more) ABNF.

## Sources

- [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
