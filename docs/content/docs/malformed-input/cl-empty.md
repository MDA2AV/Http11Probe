---
title: "CL-EMPTY"
description: "CL-EMPTY test documentation"
weight: 19
---

| | |
|---|---|
| **Test ID** | `MAL-CL-EMPTY` |
| **Category** | Malformed Input |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Content-Length: ` — a Content-Length header with an empty value (just whitespace after the colon).

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: \r\n
\r\n
```

The Content-Length header has an empty value (no digits).


## What the RFC says

> `Content-Length = 1*DIGIT` — RFC 9110 Section 8.6

The ABNF `1*DIGIT` requires at least one digit. An empty value (zero digits) does not match this grammar and indicates invalid message framing.

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error." — RFC 9110 Section 15.5.1

## Why it matters

Parsers that treat an empty Content-Length as `0` will read no body, while others may reject it or wait for data. This disagreement between parsers can be exploited for smuggling when the request also carries a body.

## Deep Analysis

### ABNF violation

The Content-Length field has a strict grammar:

```
Content-Length = 1*DIGIT

field-line     = field-name ":" OWS field-value OWS
OWS            = *( SP / HTAB )
```

After stripping OWS from `Content-Length: \r\n`, the field-value is the empty string `""`. The ABNF `1*DIGIT` requires **at least one** digit (`1*` means "one or more"). Zero digits does not match this production. The empty string is not a valid Content-Length value.

### RFC evidence

> "Content-Length = 1*DIGIT" -- RFC 9110 Section 8.6

> "A sender MUST NOT generate protocol elements that do not match the grammar defined by the corresponding ABNF rules." -- RFC 9110 Section 5.5

> "When a server listening only for HTTP request messages, or processing what appears from the start-line to be an HTTP request message, receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection." -- RFC 9112 Section 2.2

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error (e.g., malformed request syntax, invalid request message framing, or deceptive request routing)." -- RFC 9110 Section 15.5.1

The empty Content-Length value is an unambiguous ABNF violation. The `1*DIGIT` production does not have a "zero digits means zero length" interpretation -- it simply does not match.

### Chain of reasoning

1. The server receives a POST request with `Content-Length: \r\n`.
2. It parses the field-line: `field-name` is `Content-Length`, then `:`, then OWS (the trailing space), then `field-value`, then OWS.
3. After stripping OWS, the field-value is the empty string.
4. The server attempts to match the empty string against `Content-Length = 1*DIGIT`.
5. The match fails: zero digits do not satisfy `1*DIGIT`.
6. Since the Content-Length value is invalid, the server cannot determine message framing -- it does not know how many body bytes to expect.
7. Per RFC 9112 Section 2.2, the server SHOULD reject with 400 and close the connection.

### Security implications

- **Request smuggling**: If the front-end proxy interprets an empty Content-Length as `0` (no body), but the back-end server waits for body data or rejects differently, the proxy may forward subsequent requests that the back-end interprets as part of the first request's body -- or vice versa. This Content-Length disagreement is a classic smuggling primitive.
- **Parser divergence**: Different implementations handle this edge case differently: some treat empty as `0`, some reject it, some treat it as a missing header. Each interpretation leads to different framing decisions, creating desynchronization opportunities.
- **Body confusion**: A POST with an empty Content-Length and an actual body payload creates ambiguity: does the body exist or not? If the server reads zero bytes but the client sent data, that data sits in the TCP buffer and may be interpreted as the next request.

## Sources

- [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
