---
title: "HTTP10-NO-HOST"
description: "HTTP10-NO-HOST test documentation"
weight: 7
---

| | |
|---|---|
| **Test ID** | `COMP-HTTP10-NO-HOST` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | MAY (unscored) |
| **Expected** | `200` = Warn, `400` = Pass |

## What it sends

An HTTP/1.0 request with no `Host` header at all.

```http
GET / HTTP/1.0\r\n
\r\n
```

No `Host` header is present, and the HTTP version is 1.0.

## What the RFC says

The `Host` header requirement was introduced in HTTP/1.1. HTTP/1.0 predates this requirement, so an HTTP/1.0 request without a `Host` header is not technically a protocol violation:

> "A client MUST send a Host header field in all HTTP/1.1 request messages." -- RFC 9112 Section 3.2

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

Note the MUST-400 requirement explicitly says "HTTP/1.1 request message." For HTTP/1.0, the server may choose to accept the request (routing to a default virtual host) or reject it.

**Pass:** Server rejects with `400` (strict -- good security practice).
**Warn:** Server accepts with `200` (valid -- HTTP/1.0 did not require Host).

## Why this test is unscored

The RFC's MUST-400 requirement for missing Host applies only to HTTP/1.1 requests. Since this test sends an HTTP/1.0 request, there is no normative requirement to reject it. Both accepting and rejecting are valid behaviors, making a strict pass/fail determination inappropriate.

## Why it matters

In a virtual hosting environment, a request without a `Host` header gives the server no indication of which site is being targeted. Accepting such requests means the server must fall back to a default host, which could serve unintended content. Rejecting HTTP/1.0 requests without `Host` is the safer approach, especially since legitimate modern clients always send a `Host` header regardless of HTTP version.

## Deep Analysis

### Relevant ABNF Grammar

```
HTTP-version = HTTP-name "/" DIGIT "." DIGIT
Host         = uri-host [ ":" port ]
```

The request uses `HTTP/1.0` as its version. The Host header grammar is unchanged between versions, but the obligation to send it is version-specific.

### RFC Evidence

**RFC 9112 Section 3.2** scopes the MUST to HTTP/1.1:

> "A client MUST send a Host header field in all HTTP/1.1 request messages." -- RFC 9112 Section 3.2

**RFC 9112 Section 3.2** scopes the MUST-400 to HTTP/1.1:

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

**RFC 9112 Section 9.3** addresses HTTP/1.0 connection behavior:

> "A proxy server MUST NOT maintain a persistent connection with an HTTP/1.0 client." -- RFC 9112 Section 9.3

### Chain of Reasoning

1. The test sends `GET / HTTP/1.0` with no Host header.
2. The MUST-400 requirement in RFC 9112 Section 3.2 explicitly applies to "any HTTP/1.1 request message that lacks a Host header field." The HTTP/1.0 version falls outside this scope.
3. The second clause ("any request message that contains more than one Host header field line or a Host header field with an invalid field value") uses "any request message" without a version qualifier. However, the absence of a Host header is covered only by the first, version-scoped clause.
4. HTTP/1.0 predates the Host header requirement. The original HTTP/1.0 specification (RFC 1945) did not define Host as a request header at all.
5. A server that rejects HTTP/1.0 requests without Host is being stricter than required, which is a good security practice in virtual hosting environments. A server that accepts them is also compliant.

### Scoring Justification

**Unscored (MAY).** The RFC's MUST-400 for missing Host applies only to HTTP/1.1. Since this is an HTTP/1.0 request, no normative requirement exists to reject it. The test records 400 as Pass (the server applied the stricter, safer behavior) and 200 as Warn (the server accepted the request, which is valid but less secure). No result is recorded as Fail because neither behavior violates the specification.

## Sources

- [RFC 9112 Section 3.2 -- Request Target](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
- [RFC 9110 Section 7.2 -- Host and :authority](https://www.rfc-editor.org/rfc/rfc9110#section-7.2)
