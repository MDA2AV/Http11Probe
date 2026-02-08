---
title: "GET-WITH-CL-BODY"
description: "GET-WITH-CL-BODY test documentation"
weight: 5
---

| | |
|---|---|
| **Test ID** | `COMP-GET-WITH-CL-BODY` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 Section 9.3.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.1) |
| **Requirement** | MAY reject |
| **Expected** | `400` = Pass; `2xx` = Warn |

## What it sends

A GET request with `Content-Length: 5` and a body (`hello`).

```http
GET / HTTP/1.1\r\n
Host: localhost\r\n
Content-Length: 5\r\n
\r\n
hello
```

## What the RFC says

> "Although request message framing is independent of method semantics, content received in a GET request has no generally defined semantics, cannot alter the meaning or target of the request, and might lead some implementations to reject the request and close the connection because of its potential as a request smuggling attack." — RFC 9110 Section 9.3.1

> "A client SHOULD NOT generate content in a GET request unless it is made directly to an origin server that has previously indicated, in or out of band, that such a request has a purpose and will be adequately supported." — RFC 9110 Section 9.3.1

A body on GET is unusual and has no defined semantics. Rejecting it is stricter and safer.

## Pass / Warn explanation

| Response | Verdict | Reasoning |
|---|---|---|
| `400` | Pass | Rejecting a GET body is the safer choice and eliminates a smuggling vector |
| `2xx` | Warn | The RFC permits servers to accept GET bodies, but doing so carries risk |

## Why this test is unscored

The RFC uses deliberately permissive language ("has no generally defined semantics", "might lead some implementations to reject"). There is no MUST or SHOULD — the server is free to accept or reject a GET body. Because both behaviors are RFC-compliant, this test is unscored. A `400` is preferred (Pass) because it is the safer posture, while `2xx` earns a Warn to flag the potential smuggling surface.

## Why it matters

GET-with-body is a known smuggling vector. If a front-end proxy strips the body but a back-end server reads it, the leftover bytes desynchronize the connection. Rejecting GET bodies at the server level eliminates this attack surface.

## Deep Analysis

### Relevant ABNF grammar

From RFC 9112 Section 6 (message body):

```
message-body = *OCTET
```

From RFC 9112 Section 6.3 (message body length, rule 6):

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets."

Message body framing is method-independent. The `Content-Length: 5` header defines the body length regardless of the GET method.

### Direct RFC quotes

> "Although request message framing is independent of method semantics, content received in a GET request has no generally defined semantics, cannot alter the meaning or target of the request, and might lead some implementations to reject the request and close the connection because of its potential as a request smuggling attack." -- RFC 9110 Section 9.3.1

> "A client SHOULD NOT generate content in a GET request unless it is made directly to an origin server that has previously indicated, in or out of band, that such a request has a purpose and will be adequately supported." -- RFC 9110 Section 9.3.1

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." -- RFC 9112 Section 6.3

### Chain of reasoning

1. The test sends a GET request with `Content-Length: 5` and a 5-byte body (`hello`).
2. Per RFC 9112 Section 6.3 rule 6, the server must read exactly 5 bytes of body data because `Content-Length: 5` is present without `Transfer-Encoding`. This framing requirement applies regardless of the HTTP method.
3. RFC 9110 Section 9.3.1 states that content in a GET request "has no generally defined semantics" and "cannot alter the meaning or target of the request." This means the body is meaningless for GET semantics.
4. The RFC further notes that this "might lead some implementations to reject the request and close the connection because of its potential as a request smuggling attack." The word "might" is informational, not normative.
5. The SHOULD NOT applies to the *client* ("A client SHOULD NOT generate content in a GET request"), not to the server. There is no corresponding MUST or SHOULD for the server to reject it.
6. Therefore, the server MAY accept the body (reading and ignoring it) or MAY reject it with `400`. Both are compliant.

### Scored / Unscored justification

**Unscored.** The RFC uses deliberately non-normative language for the server side: "has no generally defined semantics", "might lead some implementations to reject." There is no MUST or SHOULD directed at the server regarding GET bodies. The SHOULD NOT is directed at clients, not servers. Because both accepting and rejecting are compliant behaviors, the test cannot be scored. However, `400` is preferred (Pass) as the safer security posture because GET-with-body is a well-known smuggling vector, while `2xx` earns a Warn to flag the risk.

### Edge cases

- **Smuggling vector**: If a front-end proxy ignores the GET body but the back-end reads it, the extra bytes desynchronize the connection. This is the primary security concern motivating the `400`-preferred posture.
- Some popular servers (nginx, Apache) accept GET bodies by default. Others (certain WAFs) reject them. Both are compliant.
- Elasticsearch historically used GET-with-body for search queries (`GET /_search` with a JSON body), which is the most well-known legitimate use case. RFC 9110 acknowledges this by saying clients SHOULD NOT *unless* the server has indicated support.
- If the server accepts the GET body, it MUST still read exactly 5 bytes per the Content-Length framing. Failing to consume the body would desynchronize the connection on keep-alive.

## Sources

- [RFC 9110 Section 9.3.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.1)
