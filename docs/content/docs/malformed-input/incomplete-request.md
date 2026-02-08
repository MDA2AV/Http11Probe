---
title: "INCOMPLETE-REQUEST"
description: "INCOMPLETE-REQUEST test documentation"
weight: 12
---

| | |
|---|---|
| **Test ID** | `MAL-INCOMPLETE-REQUEST` |
| **Category** | Malformed Input |
| **Expected** | `400`, close, or timeout |

## What it sends

A partial HTTP request -- the request-line and some headers, but the connection is closed before the final CRLF.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
X-Test: value
```

The request ends abruptly after the `X-Test` header value — no `\r\n` line terminator and no blank line to signal end of headers.


## What the RFC says

An HTTP/1.1 message requires a complete header section terminated by an empty line (CRLF CRLF). The field section grammar from RFC 9112 is:

> `HTTP-message = start-line CRLF *( field-line CRLF ) CRLF [ message-body ]` — RFC 9112 Section 2.1

Without the final blank line, the message is incomplete and does not match this grammar.

> "When a server listening only for HTTP request messages...receives a sequence of octets that does not match the HTTP-message grammar...the server SHOULD respond with a 400 (Bad Request) response and close the connection." — RFC 9112 Section 2.2

## Why timeout is acceptable

The server may be waiting for the rest of the headers. It has received a valid prefix but not a complete request. The connection was closed by the client before the message was finished, so the server may respond with 400, close, or timeout.

## Deep Analysis

### ABNF violation

The HTTP/1.1 message grammar requires a complete structure with explicit delimiters:

```
HTTP-message  = start-line CRLF
                *( field-line CRLF )
                CRLF
                [ message-body ]

field-line    = field-name ":" OWS field-value OWS
```

The test sends:
- `GET / HTTP/1.1\r\n` -- valid `start-line CRLF`
- `Host: localhost:8080\r\n` -- valid `field-line CRLF`
- `X-Test: value` -- **no trailing CRLF**, and **no empty-line delimiter**

The grammar requires each `field-line` to be followed by `CRLF`, and the header section must end with a bare `CRLF` (the empty line). Without the terminating `\r\n` on the last header line, the `field-line CRLF` production does not match. Without the subsequent empty `CRLF`, the message structure is incomplete.

### RFC evidence

> "HTTP-message = start-line CRLF *( field-line CRLF ) CRLF [ message-body ]" -- RFC 9112 Section 2.1

> "When a server listening only for HTTP request messages, or processing what appears from the start-line to be an HTTP request message, receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection." -- RFC 9112 Section 2.2

> "The normal procedure for parsing an HTTP message is to read the start-line into a structure, read each header field line into a hash table by field name until the empty line, and then use the parsed data to determine if a message body is expected." -- RFC 9112 Section 2.2

The third quote describes the expected parsing flow: the server reads header lines "until the empty line." If the connection closes before the empty line arrives, the parsing procedure cannot complete -- the message is incomplete.

### Chain of reasoning

1. The server receives a valid request-line (`GET / HTTP/1.1\r\n`) and begins reading headers.
2. It reads `Host: localhost:8080\r\n` -- a complete field-line.
3. It begins reading the next line: `X-Test: value`.
4. The server expects a `\r\n` to terminate this field-line, followed by either another field-line or the empty-line delimiter.
5. Instead, the connection closes (EOF). The server has a partial field-line with no terminator.
6. The `HTTP-message` grammar is not satisfied: the `CRLF` after the last `field-line` is missing, and the mandatory empty `CRLF` delimiter is missing.
7. The server may:
   - **Return 400**: It has enough data to recognize a request was started but not completed. Per RFC 9112 Section 2.2, this is the SHOULD response.
   - **Close silently**: The request was never complete, so no response is strictly required.
   - **Timeout**: If the connection was not closed by the client but merely stalled, the server waits for more data until its timeout expires.

### Security implications

- **Slowloris attack**: Sending partial requests and never completing them is the core technique of the Slowloris denial-of-service attack. The server keeps the connection open, waiting for the rest of the headers, consuming a connection slot. Thousands of such partial connections exhaust the server's connection pool.
- **Resource leak**: Each incomplete request consumes memory (for the partially parsed headers) and a file descriptor (for the socket). Without proper timeouts and cleanup, these resources are never freed.
- **Timeout calibration probing**: An attacker can measure how long the server waits before timing out an incomplete request. This reveals the server's timeout configuration, which informs the attacker how to tune a Slowloris attack for maximum effectiveness.
- **Parser state confusion**: Some servers may attempt to process a partial request upon connection close, using whatever headers were received. If the incomplete header `X-Test: value` is used without its terminator, the parser may include trailing garbage or buffer contents in the header value.

## Sources

- [RFC 9112 Section 2.1](https://www.rfc-editor.org/rfc/rfc9112#section-2.1) — HTTP message grammar
- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) — message parsing robustness
