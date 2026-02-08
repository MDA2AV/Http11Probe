---
title: "CHUNKED-NO-FINAL"
description: "CHUNKED-NO-FINAL test documentation"
weight: 9
---

| | |
|---|---|
| **Test ID** | `COMP-CHUNKED-NO-FINAL` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST read until terminator |
| **Expected** | `400`, close, or timeout |

## What it sends

A chunked POST with one data chunk but no zero terminator. The connection then goes silent.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
```

## What the RFC says

> "A recipient MUST be able to parse and decode the chunked transfer coding." — RFC 9112 Section 7.1

The chunked grammar requires a `last-chunk` (zero-size chunk) to signal the end of the body: `chunked-body = *chunk last-chunk trailer-section CRLF`. Without the `0\r\n\r\n` terminator, the transfer is incomplete. The server must continue waiting for more chunks until its read timeout fires, because the chunked framing has not been satisfied.

## Why it matters

A server that responds before seeing the zero terminator risks connection desynchronization — subsequent requests on the same connection could be misinterpreted. This is analogous to the Content-Length undersend scenario but for chunked encoding.

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
```

From RFC 9112 Section 6.3 (rule 4 for chunked):

> "the message body length is determined by reading and decoding the chunked data until the transfer coding indicates the data is complete."

### Direct RFC quotes

> "A recipient MUST be able to parse and decode the chunked transfer coding." -- RFC 9112 Section 7.1

> "The message body length is determined by reading and decoding the chunked data until the transfer coding indicates the data is complete." -- RFC 9112 Section 6.3

> "If the sender closes the connection or the recipient times out before the indicated number of octets are received, the recipient MUST consider the message to be incomplete and close the connection." -- RFC 9112 Section 6.3

### Chain of reasoning

1. The test sends `Transfer-Encoding: chunked`, triggering chunked body parsing.
2. The server reads chunk-size `5`, CRLF, 5 bytes of chunk-data (`hello`), and the trailing CRLF. This satisfies one `chunk` production.
3. Per the ABNF, `chunked-body` requires `last-chunk` after `*chunk`. The server must now attempt to read the next chunk-size to determine if it is a data chunk or the `last-chunk` (zero terminator).
4. The test sends **no more data**. The connection goes silent.
5. The server is now blocked reading the next chunk-size. Per RFC 9112 Section 6.3, the message body length for chunked encoding is "determined by reading and decoding the chunked data until the transfer coding indicates the data is complete." The data is NOT complete because no `last-chunk` has been received.
6. The server must either: (a) wait until its read timeout fires and then close the connection, (b) return `400` after detecting the incomplete transfer, or (c) close the connection immediately upon detecting the stall.
7. A `2xx` response at this point would mean the server processed the request before the chunked body was complete, which desynchronizes the connection.

### Scored / Unscored justification

**Scored.** The MUST requirement to "parse and decode the chunked transfer coding" implicitly requires the server to follow the complete `chunked-body` grammar, which includes the mandatory `last-chunk`. The server MUST NOT treat a partial chunked body as complete. Responding with `2xx` before seeing the zero terminator is a protocol violation because it means the server did not fully consume the chunked body, leaving bytes on the connection that could be misinterpreted as a subsequent request.

### Edge cases

- Some servers set aggressive read timeouts (e.g., 5 seconds) and close the connection quickly. This is acceptable -- the test allows `400`, close, or timeout.
- A server that responds `2xx` immediately after the first chunk (without waiting for the terminator) has a critical desynchronization bug. On a persistent connection, the leftover `0\r\n\r\n` of a subsequent proper request could be misinterpreted.
- Some servers attempt to detect incomplete chunked bodies and return `400 Bad Request` -- this is the cleanest error-handling approach.
- Reverse proxies may impose their own chunked read timeouts, which could mask this behavior from the origin server.

## Sources

- [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
