---
title: "TRACE-WITH-BODY"
description: "TRACE-WITH-BODY test documentation"
weight: 20
---

| | |
|---|---|
| **Test ID** | `COMP-TRACE-WITH-BODY` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 §9.3.8](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.8) |
| **Requirement** | SHOULD reject (unscored) |
| **Expected** | `400`/`405` = Pass, `200` = Warn |

## What it sends

A TRACE request that includes a `Content-Length` header and a message body, which clients are prohibited from sending.

```http
TRACE / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
\r\n
hello
```

## What the RFC says

> "A client MUST NOT send content in a TRACE request." -- RFC 9110 §9.3.8

> "A client MUST NOT generate fields in a TRACE request containing sensitive data that might be disclosed by the response." -- RFC 9110 §9.3.8

> "The final recipient of the request SHOULD exclude any request fields that are likely to contain sensitive data when that recipient generates the response content." -- RFC 9110 §9.3.8

While the `MUST NOT send content` prohibition is stated as a client requirement, a server receiving a TRACE request with a body is dealing with a client that has violated the spec. The server should reject the request or ignore the body entirely.

**Pass:** Server rejects with `400` (bad request) or `405` (method not allowed) or `501` (not implemented).
**Warn:** Server accepts with `200` (processes the TRACE despite the body).

## Why this test is unscored

The MUST NOT is directed at clients, not servers. There is no explicit RFC requirement for how a server should handle a TRACE request that contains a body. Both rejecting the request and processing it (ignoring the body) are defensible behaviors, so this test records the response without scoring it.

## Why it matters

TRACE is designed to echo back the request headers for diagnostic purposes. If a server processes a TRACE request with a body, the body content could be reflected back or logged, potentially amplifying Cross-Site Tracing (XST) attacks. A body in a TRACE request is always a sign of a misbehaving or malicious client, and the safest response is rejection.

## Deep Analysis

### Relevant ABNF

```
request-line = method SP request-target SP HTTP-version
method       = token
```

`TRACE / HTTP/1.1` is a syntactically valid request-line. The violation here is not in the request-line grammar but in the semantic constraint on request content for the TRACE method.

### RFC Evidence

The prohibition on request content is stated as a client-side MUST NOT:

> "A client MUST NOT send content in a TRACE request." -- RFC 9110 Section 9.3.8

The purpose of TRACE is to echo the request for diagnostics:

> "The TRACE method requests a remote, application-level loop-back of the request message. The final recipient of the request SHOULD reflect the message received, excluding some fields described below, back to the client as the content of a 200 (OK) response." -- RFC 9110 Section 9.3.8

The specification also restricts sensitive data in TRACE requests and responses:

> "A client MUST NOT generate fields in a TRACE request containing sensitive data that might be disclosed by the response. For example, it would be foolish for a user agent to send stored user credentials or cookies in a TRACE request. The final recipient of the request SHOULD exclude any request fields that are likely to contain sensitive data when that recipient generates the response content." -- RFC 9110 Section 9.3.8

### Chain of Reasoning

1. The `MUST NOT send content` requirement is directed at clients, not servers. The RFC defines no server-side MUST or SHOULD for how to handle a TRACE request that arrives with a body.
2. A server that receives a TRACE request with `Content-Length: 5` and a body is dealing with a non-conformant client. The specification does not prescribe a specific error code for this scenario.
3. Rejecting with `400` (the request violates a protocol constraint), `405` (TRACE not allowed), or `501` (TRACE not implemented) are all defensible. Each prevents the body from being reflected.
4. Accepting with `200` is also defensible -- the server may simply ignore the body and echo only the headers, which is the defined TRACE behavior. However, if the server reflects the body, it amplifies the XST attack surface.
5. Because no server-side normative requirement exists for this scenario, the test cannot fairly penalize either response.

### Scoring Justification

This test is **unscored**. The `MUST NOT` applies to the client, not the server. There is no explicit server-side requirement in RFC 9110 for rejecting TRACE requests that contain a body. `400`/`405`/`501` = **Pass** (defensive), `200` = **Warn** (the server processed a request from a non-conformant client, which may expose attack surface).

## Sources

- [RFC 9110 §9.3.8 -- TRACE](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.8)
- [OWASP: Cross-Site Tracing](https://owasp.org/www-community/attacks/Cross_Site_Tracing)
