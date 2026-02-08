---
title: "CHUNKED-TRAILER-VALID"
description: "CHUNKED-TRAILER-VALID test documentation"
weight: 11
---

| | |
|---|---|
| **Test ID** | `COMP-CHUNKED-TRAILER-VALID` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §7.1.2](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.2) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` |

## What it sends

A valid chunked POST with a single 5-byte chunk, a zero terminator, and a trailer field (`X-Checksum: abc`) after the final chunk.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
0\r\n
X-Checksum: abc\r\n
\r\n
```

The trailer section appears between the zero-length terminating chunk and the final empty line.

## What the RFC says

> "A trailer section allows the sender to include additional fields at the end of a chunked message in order to supply metadata that might be dynamically generated while the content is sent, such as a message integrity check, digital signature, or post-processing status." — RFC 9112 Section 7.1.2

> "A recipient that removes the chunked coding from a message MAY selectively retain or discard the received trailer fields." — RFC 9112 Section 7.1.2

The chunked encoding grammar explicitly includes an optional trailer section:

```
chunked-body = *chunk last-chunk trailer-section CRLF
trailer-section = *( field-line CRLF )
```

Trailer fields are valid metadata that follow the zero-length terminating chunk. A compliant HTTP/1.1 server must be able to parse them as part of the chunked body, even if it chooses to discard them.

## Why it matters

Trailer fields are used in practice for checksums, signatures, and streaming metadata that cannot be known until the body has been fully generated. A server that rejects a valid chunked body just because it contains a trailer section has an incomplete chunked encoding parser. This can break interoperability with legitimate clients and proxies that use trailers.

## Deep Analysis

### Relevant ABNF grammar

From RFC 9112 Section 7.1:

```
chunked-body   = *chunk
                 last-chunk
                 trailer-section
                 CRLF

last-chunk     = 1*("0") [ chunk-ext ] CRLF
trailer-section = *( field-line CRLF )
```

The `trailer-section` is a mandatory part of the `chunked-body` grammar (not optional in brackets), but its content is `*( field-line CRLF )` -- zero or more field lines. So the trailer section is always present syntactically; it just may contain zero fields.

### Direct RFC quotes

> "A trailer section allows the sender to include additional fields at the end of a chunked message in order to supply metadata that might be dynamically generated while the content is sent, such as a message integrity check, digital signature, or post-processing status." -- RFC 9112 Section 7.1.2

> "A recipient that removes the chunked coding from a message MAY selectively retain or discard the received trailer fields." -- RFC 9112 Section 7.1.2

> "A recipient MUST NOT merge a received trailer field into the header section unless its corresponding header field definition explicitly permits and instructs how the trailer field value can be safely merged." -- RFC 9112 Section 7.1.2

### Chain of reasoning

1. The test sends a valid chunked body with one data chunk (`5\r\nhello\r\n`), the zero terminator (`0\r\n`), one trailer field (`X-Checksum: abc\r\n`), and the final empty line (`\r\n`).
2. Parsing against the ABNF: `*chunk` matches the one data chunk, `last-chunk` matches `0\r\n`, `trailer-section` matches `X-Checksum: abc\r\n` (one `field-line CRLF`), and the final CRLF completes the `chunked-body`.
3. The `trailer-section` is part of the `chunked-body` grammar. A server that implements chunked decoding MUST parse through the trailer section to reach the end of the message. Stopping at the `last-chunk` without consuming the trailer and final CRLF would leave data on the connection.
4. RFC 9112 Section 7.1.2 says recipients "MAY selectively retain or discard the received trailer fields." The MAY applies to what the server *does* with the trailers, not whether it parses them. The server must parse them to complete the chunked body.
5. The decoding pseudocode in RFC 9112 Section 7.1.3 explicitly includes reading trailer fields after the zero-size chunk, confirming that trailer parsing is part of the chunked decoding algorithm.

### Scored / Unscored justification

**Scored.** The MUST requirement to "parse and decode the chunked transfer coding" (RFC 9112 Section 7.1) encompasses the entire `chunked-body` grammar, including the `trailer-section`. A server that rejects a message because it has trailer fields has failed to implement the full chunked decoder. The server is free to discard the trailer values (MAY retain or discard), but it MUST parse past them to correctly delimit the message.

### Edge cases

- Some servers treat any data after the `0\r\n` terminator as the start of a new request, causing desynchronization when trailers are present. This is a critical bug.
- The `X-Checksum` trailer is an unregistered extension field. Servers should not reject unknown trailer field names -- they should discard or store them per RFC 9112 Section 7.1.2.
- Servers that implement HTTP/2 or HTTP/3 origin handling may have different trailer semantics, but HTTP/1.1 chunked trailers must still be parsed correctly on HTTP/1.1 connections.
- A trailer field whose name matches a header field (e.g., `Content-MD5`) must not be merged into the header section unless that field's definition explicitly allows it (per the MUST NOT merge rule).

## Sources

- [RFC 9112 §7.1.2 -- Chunked Trailer Section](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.2)
- [RFC 9110 Section 6.5 -- Trailer Fields](https://www.rfc-editor.org/rfc/rfc9110#section-6.5)
