---
title: "CHUNK-SIZE-OVERFLOW"
description: "CHUNK-SIZE-OVERFLOW test documentation"
weight: 16
---

| | |
|---|---|
| **Test ID** | `MAL-CHUNK-SIZE-OVERFLOW` |
| **Category** | Malformed Input |
| **Expected** | `400` or close |

## What it sends

A chunked request with a chunk size of `FFFFFFFFFFFFFFFF0` — a value exceeding the maximum 64-bit unsigned integer.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
FFFFFFFFFFFFFFFF0\r\n
hello\r\n
0\r\n
\r\n
```

The chunk size `FFFFFFFFFFFFFFFF0` (17 hex digits) exceeds the 64-bit unsigned integer range.


## What the RFC says

> `chunk-size = 1*HEXDIG` — RFC 9112 Section 7.1

> "Recipients MUST anticipate potentially large hexadecimal numerals and prevent parsing errors due to integer conversion overflows or precision loss due to integer representation." — RFC 9112 Section 7.1

`FFFFFFFFFFFFFFFF0` (17 hex digits) exceeds the 64-bit unsigned integer range (max `FFFFFFFFFFFFFFFF` = 16 hex digits), making it impossible to interpret as a valid chunk size.

## Why it matters

Integer overflow in chunk size parsing can lead to incorrect body length calculation, buffer overflows, or server crashes. A robust server must detect overflow and reject the request.

## Deep Analysis

### ABNF context

The chunk-size grammar is deceptively simple:

```
chunked-body = *chunk
               last-chunk
               trailer-section
               CRLF

chunk        = chunk-size [ chunk-ext ] CRLF
               chunk-data CRLF
chunk-size   = 1*HEXDIG
last-chunk   = 1*("0") [ chunk-ext ] CRLF
```

The production `chunk-size = 1*HEXDIG` permits any number of hexadecimal digits with no upper bound on the string length. The value `FFFFFFFFFFFFFFFF0` (17 hex digits) is grammatically valid -- it matches `1*HEXDIG`. However, its numeric value (4,722,366,482,869,645,213,680 in decimal) exceeds the maximum representable 64-bit unsigned integer (`FFFFFFFFFFFFFFFF` = 18,446,744,073,709,551,615).

### RFC evidence

> "Recipients MUST anticipate potentially large hexadecimal numerals and prevent parsing errors due to integer conversion overflows or precision loss due to integer representation." -- RFC 9112 Section 7.1

> "When a server listening only for HTTP request messages, or processing what appears from the start-line to be an HTTP request message, receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection." -- RFC 9112 Section 2.2

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error (e.g., malformed request syntax, invalid request message framing, or deceptive request routing)." -- RFC 9110 Section 15.5.1

The first quote is critical: the RFC explicitly warns implementers about integer overflow in chunk-size parsing and mandates that recipients prevent parsing errors from overflow. This is a direct instruction to detect and handle this exact attack.

### Chain of reasoning

1. The server receives a chunked POST and begins parsing the chunk-size field.
2. It reads `FFFFFFFFFFFFFFFF0` -- 17 hex digits that match `1*HEXDIG` syntactically.
3. The server attempts to convert this hex string to an integer for use as the chunk-data length.
4. If the server uses a 64-bit unsigned integer, the value overflows (17 hex digits exceed 16-digit `FFFFFFFFFFFFFFFF`).
5. Per RFC 9112 Section 7.1, the server MUST anticipate this overflow and prevent parsing errors.
6. The only safe action is to reject the request with 400 and close the connection, since no valid chunk-data length can be derived.
7. If the server instead wraps the value (e.g., truncates to 64 bits), it would compute a much smaller chunk-data length, reading the wrong number of bytes and corrupting message framing.

### Security implications

- **Integer overflow exploitation**: If a parser silently wraps the overflowed value, the computed chunk-data length will be much smaller than intended. The server reads fewer bytes as chunk-data, then interprets the remaining bytes as the next chunk or as a new request -- a classic request smuggling vector.
- **Buffer overflow**: In languages without automatic bounds checking (C, C++), an overflowed chunk-size could lead to heap or stack buffer overflows if the truncated value is used to allocate or index memory.
- **Denial of service**: If the parser interprets the overflowed value as a very large allocation request, it may attempt to allocate gigabytes of memory, causing OOM crashes.
- **Differential parsing**: Different implementations may truncate the hex value at different widths (32-bit vs. 64-bit), leading to disagreements between a proxy and a backend about where the chunk data ends -- enabling smuggling through infrastructure.

## Sources

- [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) — chunk-size = 1*HEXDIG
- [RFC 9110 Section 15.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.1) — 400 Bad Request
