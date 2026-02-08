---
title: "TRAILER-TE"
description: "TRAILER-TE test documentation"
weight: 33
---

| | |
|---|---|
| **Test ID** | `SMUG-TRAILER-TE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

A valid chunked request with a `Transfer-Encoding: chunked` header in the trailer section.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
0\r\n
Transfer-Encoding: chunked\r\n
\r\n
```

A `Transfer-Encoding: chunked` header appears in the chunked trailers section.


## What the RFC says

> "Many fields cannot be processed outside the header section because their evaluation is necessary prior to receiving the content, such as those that describe message framing, routing, authentication, request modifiers, response controls, or content format. A sender MUST NOT generate a trailer field unless the sender knows the corresponding header field name's definition permits the field to be sent in trailers." — RFC 9110 §6.5.1

> "A recipient MUST NOT merge a received trailer field into the header section unless its corresponding header field definition explicitly permits and instructs how the trailer field value can be safely merged." — RFC 9112 §7.1.2

Transfer-Encoding describes message framing and is therefore prohibited in trailers per the above rule. A compliant server must either reject the request or silently discard the prohibited trailer field.

## Why this test is unscored

The sender violates the RFC by placing Transfer-Encoding in a trailer. However, the server's obligation is to either reject or ignore the prohibited trailer. Both `400` (reject) and `2xx` (process body, discard trailer) are defensible responses since the chunked body itself is valid.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (processes body and discards prohibited trailer).

## Why it matters

If a server processes the `Transfer-Encoding` trailer, it could attempt to re-decode the already-decoded body or change framing expectations for the next message on the connection. A compliant server should either reject the request or silently discard the prohibited trailer field.

## Deep Analysis

### Relevant ABNF

From RFC 9112 Section 7.1:

```
chunked-body    = *chunk
                  last-chunk
                  trailer-section
                  CRLF

trailer-section = *( field-line CRLF )
```

And the Transfer-Encoding header (from RFC 9112 Section 6.1):

```
Transfer-Encoding = #transfer-coding
```

Transfer-Encoding is the primary message framing field for HTTP/1.1. It determines how the message body is encoded and decoded, making it categorically prohibited from trailers.

### RFC Evidence

> "Many fields cannot be processed outside the header section because their evaluation is necessary prior to receiving the content, such as those that describe message framing, routing, authentication, request modifiers, response controls, or content format. A sender MUST NOT generate a trailer field unless the sender knows the corresponding header field name's definition permits the field to be sent in trailers." -- RFC 9110 Section 6.5.1

> "A recipient MUST NOT merge a received trailer field into the header section unless its corresponding header field definition explicitly permits and instructs how the trailer field value can be safely merged." -- RFC 9112 Section 7.1.2

> "A recipient MUST be able to parse and decode the chunked transfer coding." -- RFC 9112 Section 7.1

### Chain of Reasoning

1. **Transfer-Encoding is the defining framing field.** RFC 9110 Section 6.5.1 identifies "message framing" as the first category of prohibited trailer fields. Transfer-Encoding literally determines how the message body is framed -- whether it uses chunked encoding, compression, or other transfer codings. Its value must be known before the first body byte is read, because the value dictates the decoding algorithm. Placing it in a trailer (which arrives after the body) is a temporal impossibility under normal processing.

2. **The trailer creates a circular dependency.** The message is received with `Transfer-Encoding: chunked` in the headers. The server decodes the chunked body, reads the `0` terminator, and then encounters `Transfer-Encoding: chunked` again in the trailer section. If the server were to process this trailer, it would need to retroactively re-apply chunked decoding to a body that has already been decoded. This is logically impossible for the current message, but it can corrupt the server's state for the next message on the connection.

3. **State corruption is the primary attack vector.** Unlike Content-Length (which retroactively changes body length) or Host (which changes routing), a Transfer-Encoding trailer targets the server's framing state machine. If an intermediary processes the trailer and updates its Transfer-Encoding state, it may expect the next message on the connection to be chunked when it is not, or vice versa. This framing desync between the intermediary and the origin can cause entire requests to be misinterpreted.

4. **Attack scenario.** An attacker sends a chunked POST with `Transfer-Encoding: chunked` in the trailer to a reverse proxy. The proxy correctly decodes the chunked body and forwards the dechunked content to the origin. But if the proxy processes the trailer and updates its state to expect chunked encoding on the next message, the next legitimate request (sent without chunked encoding) will be misinterpreted. The proxy may attempt to parse the raw request bytes as chunk-size lines, causing a complete protocol-level desync that the attacker can exploit to inject arbitrary requests.

### Scored / Unscored Justification

This test is **unscored** (`Scored = false`). The sender violates the RFC (MUST NOT generate Transfer-Encoding as a trailer), but the server has two compliant responses: reject the message (`400`) or accept the valid chunked body while discarding the prohibited trailer (`2xx`). A `2xx` response does not inherently indicate vulnerability -- the server may have correctly discarded the Transfer-Encoding trailer. The test cannot distinguish "discarded safely" from "processed dangerously" based on the status code, so it flags `2xx` as a warning. The real risk depends on whether any intermediary in the request chain processes the trailer and corrupts its framing state, which requires behavioral analysis beyond a single response code.

## Sources

- [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1)
- [RFC 9112 §7.1.2](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.2)
