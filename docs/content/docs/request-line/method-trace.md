---
title: "METHOD-TRACE"
description: "METHOD-TRACE test documentation"
weight: 14
---

| | |
|---|---|
| **Test ID** | `COMP-METHOD-TRACE` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 Section 9.3.8](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.8) |
| **Requirement** | SHOULD disable |
| **Expected** | `405` or `501` preferred; `200` is a warning |

## What it sends

`TRACE / HTTP/1.1` — a standard TRACE request.

```http
TRACE / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```


## What the RFC says

> "The TRACE method requests a remote, application-level loop-back of the request message. The final recipient of the request SHOULD reflect the message received, excluding some fields described below, back to the client as the content of a 200 (OK) response." -- RFC 9110 §9.3.8

> "A client MUST NOT generate fields in a TRACE request containing sensitive data that might be disclosed by the response." -- RFC 9110 §9.3.8

> "The final recipient of the request SHOULD exclude any request fields that are likely to contain sensitive data when that recipient generates the response content." -- RFC 9110 §9.3.8

TRACE echoes the received request back to the client. While valid per the HTTP spec, it is widely considered a security risk in production.

## Why this test is unscored

The RFC does not mandate that servers reject TRACE; it merely defines the method's behavior. Disabling TRACE is a security best practice rather than an RFC conformance requirement. Both `405`/`501` (disabled) and `200` (enabled) are valid behaviors.

## Why it matters

TRACE can be abused for **Cross-Site Tracing (XST)** attacks — if an attacker can trigger a TRACE request (via XSS or other means), the echoed response may expose cookies, authorization headers, or other sensitive data that `HttpOnly` flags are meant to protect.

Most security hardening guides recommend disabling TRACE entirely. A `405 Method Not Allowed` or `501 Not Implemented` response is ideal.

## Deep Analysis

### Relevant ABNF

```
request-line = method SP request-target SP HTTP-version
method       = token
```

The TRACE method is a valid `token` per the HTTP grammar, and `TRACE / HTTP/1.1` is a syntactically well-formed request-line under RFC 9112 Section 3.

### RFC Evidence

RFC 9110 Section 9.3.8 defines the TRACE method and its purpose:

> "The TRACE method requests a remote, application-level loop-back of the request message. The final recipient of the request SHOULD reflect the message received, excluding some fields described below, back to the client as the content of a 200 (OK) response." -- RFC 9110 Section 9.3.8

The specification acknowledges the security sensitivity of the echoed response:

> "A client MUST NOT generate fields in a TRACE request containing sensitive data that might be disclosed by the response. For example, it would be foolish for a user agent to send stored user credentials or cookies in a TRACE request. The final recipient of the request SHOULD exclude any request fields that are likely to contain sensitive data when that recipient generates the response content." -- RFC 9110 Section 9.3.8

The value of TRACE for diagnostics is also noted:

> "TRACE allows the client to see what is being received at the other end of the request chain and use that data for testing or diagnostic information." -- RFC 9110 Section 9.3.8

### Chain of Reasoning

1. TRACE is a defined HTTP method, so a server is not required to reject it on syntactic grounds.
2. However, TRACE echoes back request headers, which creates a reflected data channel. If an attacker can trigger a TRACE request (e.g., via XSS), the response may expose `HttpOnly` cookies, `Authorization` headers, and other credentials that would otherwise be inaccessible to JavaScript -- the classic **Cross-Site Tracing (XST)** attack vector.
3. The RFC uses SHOULD language for reflecting the message, not MUST -- servers are not obligated to implement TRACE at all. Industry-wide security hardening guides (OWASP, CIS benchmarks) uniformly recommend disabling TRACE in production.
4. A `405 Method Not Allowed` or `501 Not Implemented` response demonstrates the server has been hardened against XST. A `200` response means TRACE is active and the reflected-data attack surface exists.

### Scoring Justification

This test is **unscored** (warning-level). The RFC does not mandate that servers reject TRACE; it merely defines the method's behavior. Disabling TRACE is a security best practice rather than an RFC conformance requirement. Therefore, `405`/`501` = **Pass** (hardened), and `200` = **Warn** (functional but exposes attack surface).

## Sources

- [RFC 9110 Section 9.3.8](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.8)
- [OWASP: Test HTTP Methods](https://owasp.org/www-project-web-security-testing-guide/latest/4-Web_Application_Security_Testing/02-Configuration_and_Deployment_Management_Testing/06-Test_HTTP_Methods)
