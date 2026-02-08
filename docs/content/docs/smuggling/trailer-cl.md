---
title: "TRAILER-CL"
description: "TRAILER-CL test documentation"
weight: 32
---

| | |
|---|---|
| **Test ID** | `SMUG-TRAILER-CL` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

A valid chunked request with a `Content-Length: 50` header in the trailer section (after the last chunk).

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
0\r\n
Content-Length: 50\r\n
\r\n
```

A `Content-Length: 50` header appears in the chunked trailers section.


## What the RFC says

> "Many fields cannot be processed outside the header section because their evaluation is necessary prior to receiving the content, such as those that describe message framing, routing, authentication, request modifiers, response controls, or content format. A sender MUST NOT generate a trailer field unless the sender knows the corresponding header field name's definition permits the field to be sent in trailers." — RFC 9110 §6.5.1

> "A recipient MUST NOT merge a received trailer field into the header section unless its corresponding header field definition explicitly permits and instructs how the trailer field value can be safely merged." — RFC 9112 §7.1.2

Content-Length describes message framing and is therefore prohibited in trailers per the above rule. A compliant server must either reject the request or silently discard the prohibited trailer field.

## Why this test is unscored

The sender clearly violates the RFC by placing Content-Length in a trailer. However, the server's obligation is to either reject or ignore the prohibited trailer. Both `400` (reject) and `2xx` (process body, discard trailer) are defensible responses since the chunked body itself is valid.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (processes body and discards prohibited trailer).

## Why it matters

If a server or proxy processes the `Content-Length` trailer, it could retroactively change its understanding of the message body length — potentially poisoning a cache or re-framing subsequent requests on the same connection.

## Deep Analysis

### Relevant ABNF

From RFC 9112 Section 7.1, the chunked body structure explicitly includes a trailer section:

```
chunked-body    = *chunk
                  last-chunk
                  trailer-section
                  CRLF

trailer-section = *( field-line CRLF )
```

The grammar permits any `field-line` in the trailer section syntactically, but the semantics in RFC 9110 Section 6.5.1 restrict which fields may actually appear there.

### RFC Evidence

> "Many fields cannot be processed outside the header section because their evaluation is necessary prior to receiving the content, such as those that describe message framing, routing, authentication, request modifiers, response controls, or content format. A sender MUST NOT generate a trailer field unless the sender knows the corresponding header field name's definition permits the field to be sent in trailers." -- RFC 9110 Section 6.5.1

> "A recipient MUST NOT merge a received trailer field into the header section unless its corresponding header field definition explicitly permits and instructs how the trailer field value can be safely merged." -- RFC 9112 Section 7.1.2

> "A sender MUST NOT send a Content-Length header field in any message that contains a Transfer-Encoding header field." -- RFC 9112 Section 6.2

### Chain of Reasoning

1. **Content-Length is a message framing field by definition.** RFC 9110 Section 6.5.1 identifies "message framing" as one of the categories of fields that MUST NOT appear in trailers. Content-Length is the primary mechanism for determining message body length in HTTP/1.1 -- it is the quintessential framing field. Its prohibition from trailers is not merely implied; it falls squarely within the explicit prohibition category.

2. **The MUST NOT merge rule provides a second layer of defense.** Even if a server's chunked parser extracts the trailer `Content-Length: 50`, RFC 9112 Section 7.1.2 says it MUST NOT merge this into the header section. The Content-Length field definition does not permit trailer usage, so the merge is unconditionally prohibited. A compliant server must either discard the trailer or reject the message entirely.

3. **Processing the trailer would retroactively reframe the message.** The chunked body contains exactly 5 bytes (`hello`). If a server or intermediary processes the `Content-Length: 50` trailer, it now "believes" the message body is 50 bytes long. Since only 5 bytes were actually sent, the implementation might attempt to read 45 more bytes from the connection -- consuming the next request in the pipeline. This is a direct path to request smuggling.

4. **Attack scenario.** An attacker sends a valid chunked request with `Content-Length: N` in the trailer, where N is chosen to encompass the next request on the connection. A vulnerable intermediary processes the trailer, retroactively changes its understanding of the body length, and reframes the connection stream. The next legitimate request is consumed as "body" of the attacker's request, and the attacker's crafted follow-up is processed as a new request with the victim's credentials.

### Scored / Unscored Justification

This test is **unscored** (`Scored = false`). While the sender clearly violates the RFC by placing Content-Length in a trailer (MUST NOT generate), the server's compliance obligation is to either reject the message or silently discard the prohibited trailer. Both `400` (reject) and `2xx` (accept the valid chunked body, discard the invalid trailer) are correct server behaviors. A `2xx` response does not necessarily mean the server processed the trailer -- it may have properly discarded it. Since the test cannot distinguish "discarded the trailer safely" from "merged the trailer dangerously" based on the status code alone, it flags `2xx` as a warning for manual investigation rather than scoring it as a failure.

## Sources

- [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1)
- [RFC 9110 §6.5.2](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.2)
- [RFC 9112 §7.1.2](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.2)
