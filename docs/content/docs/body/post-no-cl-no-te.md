---
title: "POST-NO-CL-NO-TE"
description: "POST-NO-CL-NO-TE test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `COMP-POST-NO-CL-NO-TE` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3) |
| **Requirement** | MUST treat as zero-length |
| **Expected** | `2xx` or close |

## What it sends

A POST with neither Content-Length nor Transfer-Encoding headers — no body framing at all.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
\r\n
```

## What the RFC says

RFC 9112 Section 6.3 defines a precedence list for determining message body length. After checking for Transfer-Encoding (rule 3/4) and Content-Length (rule 5/6), the final rule for requests states:

> "If this is a request message and none of the above are true, then the message body length is zero (no message body is present)." — RFC 9112 Section 6.3

The RFC also notes that a server may choose to require Content-Length on requests that carry a body:

> "A server MAY reject a request that contains a message body but not a Content-Length by responding with 411 (Length Required)." — RFC 9112 Section 6.3

When a request has no framing headers, the server must assume the body is empty and process the request immediately.

## Why it matters

Some servers hang waiting for a body when they see POST without Content-Length, causing connection timeouts. The RFC is clear: no framing headers means zero-length body.

## Deep Analysis

### Relevant ABNF grammar

From RFC 9112 Section 6:

```
message-body = *OCTET
```

There is no Content-Length or Transfer-Encoding header, so the message body length determination falls through to rule 7 of RFC 9112 Section 6.3.

### RFC 9112 Section 6.3 -- Message Body Length precedence rules

The complete precedence list for determining message body length:

1. Responses to HEAD, and 1xx/204/304 responses: no body.
2. 2xx responses to CONNECT: tunnel mode.
3. Transfer-Encoding present and overrides Content-Length.
4. Chunked as final coding: read chunked data. Non-chunked final coding on request: respond 400.
5. Invalid Content-Length without Transfer-Encoding: unrecoverable error.
6. Valid Content-Length without Transfer-Encoding: read that many octets.
7. **"If this is a request message and none of the above are true, then the message body length is zero (no message body is present)."**

### Direct RFC quotes

> "If this is a request message and none of the above are true, then the message body length is zero (no message body is present)." -- RFC 9112 Section 6.3

> "A server MAY reject a request that contains a message body but not a Content-Length by responding with 411 (Length Required)." -- RFC 9112 Section 6.3

> "A user agent that sends a request that contains a message body MUST send either a valid Content-Length header field or use the chunked transfer coding." -- RFC 9112 Section 6.3

### Chain of reasoning

1. The test sends a POST request with no Content-Length and no Transfer-Encoding headers.
2. Walking through the RFC 9112 Section 6.3 precedence rules: rules 1-2 do not apply (this is a request, not a response). Rule 3/4: no Transfer-Encoding is present. Rule 5/6: no Content-Length is present.
3. We reach rule 7: "If this is a request message and none of the above are true, then the message body length is zero (no message body is present)." This is definitive.
4. The server MUST treat the body length as zero. It must not block waiting for body data that will never arrive.
5. The MAY reject with 411 clause applies when a request "contains a message body but not a Content-Length." In this case, rule 7 has already determined the body length is zero -- there *is* no message body. Therefore the 411 clause does not apply to this test case.
6. The server should process the request as a POST with an empty body and respond normally.

### Scored / Unscored justification

**Scored.** Rule 7 of RFC 9112 Section 6.3 definitively states the body length is zero when no framing headers are present. This is not a SHOULD or MAY -- it is a declarative statement of fact within the normative precedence algorithm. A server that hangs waiting for body data is violating the body-length determination algorithm. The `AllowConnectionClose` flag is set because a server may close the connection for its own reasons, but it must not stall indefinitely.

### Edge cases

- Some servers interpret POST without Content-Length as malformed and return `411 Length Required`. This is only correct if the server believes a body *was intended* but not framed. Since rule 7 says "no message body is present," returning 411 is technically incorrect for this scenario, but the test accepts close as an alternative.
- A server that blocks waiting for a body on this request has confused "POST method" with "POST must have a body." POST does not require a body.
- The third RFC quote ("A user agent that sends a request that contains a message body MUST send either a valid Content-Length header field or use the chunked transfer coding") places the MUST on the *client*. This test verifies the server correctly handles a client that has no body to send and therefore omits both headers.
- Some frameworks (e.g., Express.js, Flask) automatically set `Content-Length: 0` on empty POST bodies, so servers may rarely encounter this in practice. However, raw TCP clients and proxies can produce this pattern.

## Sources

- [RFC 9112 Section 6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
