---
title: "OPTIONS-STAR"
description: "OPTIONS-STAR test documentation"
weight: 7
---

| | |
|---|---|
| **Test ID** | `COMP-OPTIONS-STAR` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2.4](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.4) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` |

## What it sends

`OPTIONS * HTTP/1.1` — the valid asterisk-form request.

```http
OPTIONS * HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```


## What the RFC says

> "The 'asterisk-form' of request-target is only used for a server-wide OPTIONS request." -- RFC 9112 §3.2.4

> "When a client wishes to request OPTIONS for the server as a whole, as opposed to a specific named resource of that server, the client MUST send only '*' (%x2A) as the request-target." -- RFC 9112 §3.2.4

> "The OPTIONS method requests information about the communication options available for the target resource, at either the origin server or an intervening intermediary." -- RFC 9110 §9.3.7

## Why it matters

This is the only valid use of `*` as a request-target. A compliant server should accept it and respond with 2xx (typically 200 with Allow header).

## Deep Analysis

### Relevant ABNF

```
request-line   = method SP request-target SP HTTP-version
request-target = origin-form / absolute-form / authority-form / asterisk-form
asterisk-form  = "*"
method         = token
```

The `asterisk-form` is one of four valid `request-target` productions. `OPTIONS * HTTP/1.1` is a perfectly valid request-line: the method is `OPTIONS`, the request-target matches `asterisk-form`, and the version is `HTTP/1.1`.

### RFC Evidence

RFC 9112 Section 3.2.4 explicitly ties the asterisk-form to the OPTIONS method:

> "The 'asterisk-form' of request-target is only used for a server-wide OPTIONS request." -- RFC 9112 Section 3.2.4

The client's obligation when making such a request is stated as a MUST:

> "When a client wishes to request OPTIONS for the server as a whole, as opposed to a specific named resource of that server, the client MUST send only '*' (%x2A) as the request-target." -- RFC 9112 Section 3.2.4

RFC 9110 Section 9.3.7 describes the server-side semantics:

> "The OPTIONS method requests information about the communication options available for the target resource, at either the origin server or an intervening intermediary." -- RFC 9110 Section 9.3.7

The specification also defines what a successful response should contain:

> "A server generating a successful response to OPTIONS SHOULD send any header that might indicate optional features implemented by the server and applicable to the target resource (e.g., Allow), including potential extensions not defined by this specification." -- RFC 9110 Section 9.3.7

### Chain of Reasoning

1. `OPTIONS * HTTP/1.1` is syntactically valid per the ABNF. The asterisk-form is one of four defined request-target forms, and it is specifically reserved for this use case.
2. A server that implements HTTP/1.1 MUST be able to parse all four request-target forms. Rejecting `*` as a request-target would be a parsing failure.
3. The expected response is `200 OK` with an `Allow` header listing the server's supported methods. This is the canonical "ping" or capability-discovery mechanism for HTTP servers.
4. Any response other than `2xx` (such as `400` or `501`) indicates the server cannot handle the asterisk-form, which is a conformance gap.

### Scoring Justification

This test is **scored**. The asterisk-form is a defined part of the HTTP/1.1 grammar, and the combination `OPTIONS *` is the only valid use of it. The server MUST accept this well-formed request. `2xx` = **Pass**, any non-2xx response = **Fail**.

## Sources

- [RFC 9112 Section 3.2.4](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.4)
- [RFC 9110 Section 9.3.7](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.7)
