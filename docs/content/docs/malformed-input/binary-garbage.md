---
title: "BINARY-GARBAGE"
description: "BINARY-GARBAGE test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `MAL-BINARY-GARBAGE` |
| **Category** | Malformed Input |
| **Expected** | `400`, close, or timeout |

## What it sends

Random binary bytes that do not constitute any valid HTTP message.

```
[256 bytes of pseudorandom binary data, seeded RNG(42)]
```

Not a valid HTTP request — raw binary bytes with no recognizable structure.


## What the RFC says

> "When a server listening only for HTTP request messages, or processing what appears from the start-line to be an HTTP request message, receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection." — RFC 9112 Section 2.2

Random binary bytes do not match the `request-line` grammar (`method SP request-target SP HTTP-version`), so the server SHOULD respond with 400.

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error." — RFC 9110 Section 15.5.1

## Why timeout is acceptable

The server receives bytes that cannot be parsed as an HTTP request-line. It may not even determine that a request was attempted. Waiting for more data (and eventually timing out) is valid.

## Deep Analysis

### ABNF violation

The HTTP/1.1 message grammar requires a well-formed start-line as the first element:

```
HTTP-message = start-line CRLF
               *( field-line CRLF )
               CRLF
               [ message-body ]

request-line = method SP request-target SP HTTP-version
method       = token
token        = 1*tchar
tchar        = "!" / "#" / "$" / "%" / "&" / "'" / "*"
             / "+" / "-" / "." / "^" / "_" / "`" / "|"
             / "~" / DIGIT / ALPHA
```

Random binary bytes will almost certainly contain octets outside the `tchar` set (e.g., NUL `0x00`, control characters `0x01-0x1F`, DEL `0x7F`, high bytes `0x80-0xFF`), and will lack the required `SP` delimiters and `HTTP-version` suffix. The data does not match `request-line` at all.

### RFC evidence

> "When a server listening only for HTTP request messages, or processing what appears from the start-line to be an HTTP request message, receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection." -- RFC 9112 Section 2.2

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error (e.g., malformed request syntax, invalid request message framing, or deceptive request routing)." -- RFC 9110 Section 15.5.1

> "A server that is expecting to receive and parse a request-line SHOULD ignore at least one empty line (CRLF) received prior to the request-line." -- RFC 9112 Section 2.2

The robustness exception only covers leading blank lines (CRLF), not arbitrary binary content. Once the server encounters bytes that cannot begin a valid `method` token, the grammar match fails immediately.

### Chain of reasoning

1. The server opens a TCP connection and begins reading octets.
2. It attempts to match the incoming bytes against `request-line = method SP request-target SP HTTP-version`.
3. The `method` production requires `1*tchar`, but random binary data contains octets outside the `tchar` character set (control characters, high bytes, NUL).
4. The grammar match fails at the very first non-tchar octet.
5. Per RFC 9112 Section 2.2, the server SHOULD respond with 400 and close the connection.
6. Alternatively, if the server has not yet identified any request boundary, it may wait for more data and eventually time out -- this is also acceptable since no complete request was ever formed.

### Security implications

- **Protocol confusion**: Binary data could be an attempt to speak a non-HTTP protocol (e.g., TLS ClientHello, SMTP, or a custom binary protocol) on an HTTP port. Accepting and processing such data risks protocol-level confusion.
- **Parser exploitation**: Naive parsers that do not validate the `tchar` constraint may attempt to interpret binary data as HTTP, potentially triggering buffer overflows, out-of-bounds reads, or undefined behavior in string operations.
- **Resource exhaustion**: If the server buffers binary data waiting for CRLF delimiters that never arrive, it may consume memory indefinitely. Proper timeout and size limits are essential.

## Sources

- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) — message parsing robustness
- [RFC 9110 Section 15.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.1) — 400 Bad Request
