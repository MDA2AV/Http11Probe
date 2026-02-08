---
title: "CHUNKED-MULTI"
description: "CHUNKED-MULTI test documentation"
weight: 7
---

| | |
|---|---|
| **Test ID** | `COMP-CHUNKED-MULTI` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` |

## What it sends

A valid chunked POST with two data chunks (5 bytes + 6 bytes) followed by the zero terminator.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
6\r\n
 world\r\n
0\r\n
\r\n
```

## What the RFC says

> "The chunked transfer coding wraps content in order to transfer it as a series of chunks, each with its own size indicator." — RFC 9112 Section 7.1

> "A recipient MUST be able to parse and decode the chunked transfer coding." — RFC 9112 Section 7.1

The chunked grammar defines the body as `*chunk last-chunk trailer-section CRLF` — zero or more data chunks followed by the zero-length terminator. The server must concatenate all chunks to reconstruct the full body. This tests that the chunk parser correctly handles multiple consecutive data chunks before the terminator.

## Why it matters

Multi-chunk bodies are the norm in real-world HTTP — streaming uploads, large form submissions, and proxied requests all use multiple chunks. A server that only handles single-chunk bodies has an incomplete chunked decoder.

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

From RFC 9112 Section 7.1.3 (Decoding Chunked), the pseudocode algorithm:

```
length := 0
read chunk-size, chunk-ext (if any), and CRLF
while (chunk-size > 0) {
   read chunk-data and CRLF
   append chunk-data to content
   length := length + chunk-size
   read chunk-size, chunk-ext (if any), and CRLF
}
```

### Direct RFC quotes

> "The chunked transfer coding wraps content in order to transfer it as a series of chunks, each with its own size indicator." -- RFC 9112 Section 7.1

> "A recipient MUST be able to parse and decode the chunked transfer coding." -- RFC 9112 Section 7.1

> "A recipient MUST be able to parse the chunked transfer coding because it plays a crucial role in framing messages when the content size is not known in advance." -- RFC 9112 Section 6.1

### Chain of reasoning

1. The test sends `Transfer-Encoding: chunked` with two data chunks: `5\r\nhello\r\n` (5 bytes) and `6\r\n world\r\n` (6 bytes), followed by the `0\r\n\r\n` terminator.
2. The ABNF production `chunked-body = *chunk last-chunk ...` uses `*chunk`, meaning zero or more data chunks. This test exercises the "more than one" case.
3. Per the decoding pseudocode in RFC 9112 Section 7.1.3, the server enters the while loop, reads chunk 1 (size=5, data="hello"), appends it, then reads chunk 2 (size=6, data=" world"), appends it, then reads chunk-size=0 and exits the loop.
4. The reconstructed content is "hello world" (11 bytes). The server must concatenate all chunk-data segments in order.
5. Both chunks individually conform to the `chunk` production: each has a valid `chunk-size`, followed by CRLF, followed by exactly `chunk-size` octets of `chunk-data`, followed by CRLF.
6. The entire message is a valid `chunked-body`. The MUST requirement to "parse and decode" applies to multi-chunk bodies just as it does to single-chunk bodies.

### Scored / Unscored justification

**Scored.** The MUST requirement ("A recipient MUST be able to parse and decode the chunked transfer coding") applies to the full generality of the chunked ABNF, including the `*chunk` repetition. A server that can only handle a single chunk has not fully implemented the chunked decoder. The `*chunk` production explicitly models the multi-chunk case, making this a MUST-level compliance test.

### Edge cases

- Some servers read only the first chunk and treat chunk-size=0 in the *next* read as a new request, causing desynchronization. This is a critical parsing bug.
- The second chunk begins with a space character (` world`), which is valid chunk-data. Servers must not trim or interpret chunk-data content.
- Servers that buffer the entire body before processing should concatenate to "hello world" (11 bytes). Servers that stream should emit chunks in order.
- The maximum number of chunks is unbounded by the grammar (`*chunk`). This test uses just two, but a compliant server must handle arbitrarily many.

## Sources

- [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
