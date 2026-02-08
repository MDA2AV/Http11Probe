---
title: "TE-NOT-FINAL-CHUNKED"
description: "TE-NOT-FINAL-CHUNKED test documentation"
weight: 16
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-NOT-FINAL-CHUNKED` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Transfer-Encoding: chunked, gzip` — chunked is not the final encoding.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked, gzip\r\n
\r\n
0\r\n
\r\n
```


## What the RFC says

> "If a Transfer-Encoding header field is present in a request and the chunked transfer coding is not the final encoding, the message body length cannot be determined reliably; the server MUST respond with the 400 (Bad Request) status code and then close the connection." — RFC 9112 §6.3

This is MUST-level language — servers have no discretion here.

## Why it matters

If chunked isn't the final encoding, the server cannot determine body boundaries. This can be exploited for smuggling.

## Deep Analysis

### ABNF

```
Transfer-Encoding = #transfer-coding       ; RFC 9112 §6.1
transfer-coding   = token                  ; RFC 9110 §10.1.4
token             = 1*tchar                ; RFC 9110 §5.6.2
```

The value `chunked, gzip` is a valid comma-separated list of two transfer-coding tokens. The ordering matters: the last element in the list represents the outermost encoding applied to the message body. In this case, `gzip` is the final (outermost) encoding, not `chunked`.

### RFC Evidence

> "If a Transfer-Encoding header field is present in a request and the chunked transfer coding is not the final encoding, the message body length cannot be determined reliably; the server MUST respond with the 400 (Bad Request) status code and then close the connection." -- RFC 9112 §6.3

> "If any transfer coding other than chunked is applied to a request's content, the sender MUST apply chunked as the final transfer coding to ensure that the message is properly framed." -- RFC 9112 §6.1

> "A recipient MUST be able to parse the chunked transfer coding (Section 7.1) because it plays a crucial role in framing messages when the content size is not known in advance." -- RFC 9112 §6.1

### Chain of Reasoning

1. The test sends `Transfer-Encoding: chunked, gzip` with a zero-length chunked body (`0\r\n\r\n`).
2. In Transfer-Encoding, the codings are listed in the order they were applied. The last coding in the list is the outermost encoding. Here, `gzip` is final, meaning the message body is supposed to be gzip-compressed data that, when decompressed, yields chunked-encoded data.
3. RFC 9112 section 6.1 requires the sender to apply chunked as the **final** transfer coding for requests. The sender violated this by placing `gzip` after `chunked`.
4. RFC 9112 section 6.3 uses **MUST-level** language: when chunked is not the final encoding in a request, "the server MUST respond with the 400 (Bad Request) status code and then close the connection." There is no discretion here.
5. The rationale is clear: if chunked is not the outermost encoding, the server cannot determine message body boundaries. Chunked encoding is the framing mechanism -- without it as the outermost layer, the server has no way to know where the message body ends.
6. The test body (`0\r\n\r\n`) is a valid chunked terminator, but the server is supposed to see the body as gzip-compressed first. Since the body is not valid gzip data, the framing is inherently broken.

### Scored / Unscored Justification

This test is **scored** (MUST reject). RFC 9112 section 6.3 contains one of the most explicit MUST-level requirements in the specification: when chunked is not the final encoding in a request, the server "MUST respond with the 400 (Bad Request) status code and then close the connection." There is no ambiguity and no room for lenient processing.

- **Pass (400 or close):** The server correctly rejects the request per the explicit MUST requirement.
- **Fail (2xx):** The server accepted a request with indeterminate body boundaries, violating a MUST-level requirement.

### Smuggling Attack Scenarios

- **Encoding Order Confusion:** A front-end proxy may only check whether `chunked` appears anywhere in the Transfer-Encoding list and process chunked framing regardless of position. It reads the body using chunk boundaries. A back-end that correctly validates the encoding order rejects the request with `400`. But if the front-end already consumed the body and is reusing the connection, leftover data on the socket becomes the next request.
- **Gzip Decompression Bypass:** A front-end that tries to apply the encodings in order first attempts gzip decompression. Since the body is not valid gzip, it may error or pass through raw bytes. A back-end that ignores encoding order and processes `chunked` directly reads different body boundaries. The mismatch enables request smuggling.
- **Selective Encoding Processing:** Some implementations only process `chunked` and ignore other transfer codings they do not support. If a front-end ignores `gzip` and processes `chunked`, but a back-end treats the entire Transfer-Encoding as invalid and falls back to reading until connection close, the two parsers consume different amounts of data from the connection.

## Sources

- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
- [RFC 9112 §7](https://www.rfc-editor.org/rfc/rfc9112#section-7)
