---
title: "POST-CL-UNDERSEND"
description: "POST-CL-UNDERSEND test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `COMP-POST-CL-UNDERSEND` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) |
| **Requirement** | MUST read declared length |
| **Expected** | `400`, close, or timeout |

## What it sends

A POST declaring `Content-Length: 10` but sending only 5 bytes (`hello`). The connection then goes silent.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Content-Length: 10\r\n
\r\n
hello
```

## What the RFC says

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." — RFC 9112 Section 6.3

> "For messages that do include content, the Content-Length field value provides the framing information necessary for determining where the data (and message) ends." — RFC 9112 Section 6.2

The server is obligated to read exactly 10 bytes as declared. Since only 5 arrive, the server must continue waiting for the remaining 5 bytes until its read timeout fires. Responding prematurely would leave leftover bytes on the connection.

## Why it matters

A server that responds before reading the full declared body risks desynchronizing the connection — leftover bytes from the incomplete body could be interpreted as the start of the next request, creating a smuggling vector.

## Deep Analysis

### Relevant ABNF grammar

From RFC 9110 Section 8.6 (Content-Length):

```
Content-Length = 1*DIGIT
```

From RFC 9112 Section 6:

```
message-body = *OCTET
```

The message body length is determined by the Content-Length value (10 octets), but only 5 octets arrive before the connection stalls.

### Direct RFC quotes

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." -- RFC 9112 Section 6.3

> "If the sender closes the connection or the recipient times out before the indicated number of octets are received, the recipient MUST consider the message to be incomplete and close the connection." -- RFC 9112 Section 6.3

> "For messages that do include content, the Content-Length field value provides the framing information necessary for determining where the data (and message) ends." -- RFC 9112 Section 6.2

### Chain of reasoning

1. The test sends `Content-Length: 10` but only 5 bytes of body data (`hello`), then the connection goes silent.
2. Per RFC 9112 Section 6.3 rule 6, the server must read exactly 10 bytes because the Content-Length "defines the expected message body length in octets."
3. After reading 5 bytes, the server has received only half the declared body. It must continue reading, waiting for the remaining 5 bytes.
4. No more data arrives. The server's read operation blocks.
5. RFC 9112 Section 6.3 explicitly addresses this scenario: "If the sender closes the connection or the recipient times out before the indicated number of octets are received, the recipient MUST consider the message to be incomplete and close the connection."
6. The MUST keyword applies directly: the server MUST treat this as an incomplete message. A `2xx` response would violate this requirement because it implies the server processed the request successfully despite having an incomplete body.
7. Acceptable outcomes are: `400` (explicit rejection), connection close (per the MUST close requirement), or timeout (the server waited for the full 10 bytes, which never arrived).

### Scored / Unscored justification

**Scored.** RFC 9112 Section 6.3 uses MUST: "the recipient MUST consider the message to be incomplete and close the connection." This is a direct, unambiguous requirement. A server that responds `2xx` after reading only 5 of the declared 10 bytes has violated this MUST by treating an incomplete message as complete. The three acceptable outcomes (`400`, close, timeout) all represent correct handling of the incomplete message.

### Edge cases

- **Smuggling risk**: If the server responds `2xx` after reading only 5 bytes, the remaining 5 bytes of body data (when eventually sent or from a subsequent request) could be interpreted as a new HTTP request, enabling request smuggling.
- Some servers read the full Content-Length worth of data from the socket before processing, and thus correctly block when only 5 bytes are available. Others process in a streaming fashion and may prematurely respond.
- A server that sends `408 Request Timeout` instead of `400` is also acceptable -- it correctly identifies the incomplete body as a timeout condition.
- The connection closure requirement means keep-alive should not be used after an incomplete message. The server must not attempt to read a subsequent request on this connection.

## Sources

- [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2)
