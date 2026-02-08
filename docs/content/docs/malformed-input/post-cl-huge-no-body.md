---
title: "POST-CL-HUGE-NO-BODY"
description: "POST-CL-HUGE-NO-BODY test documentation"
weight: 26
---

| | |
|---|---|
| **Test ID** | `MAL-POST-CL-HUGE-NO-BODY` |
| **Category** | Malformed Input |
| **RFC** | [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) |
| **Expected** | `400`/close/timeout |

## What it sends

A POST request declaring a ~1GB body via Content-Length but sending no body data at all.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 999999999\r\n
\r\n
```

No body follows the empty line. The connection remains open.

## What the RFC says

> `Content-Length = 1*DIGIT` — RFC 9110 Section 8.6

> "When a message does not have a Transfer-Encoding header field, a Content-Length header field can provide the anticipated size, as a decimal number of octets, for potential content." — RFC 9112 Section 6.2

The value `999999999` (~1GB) is a syntactically valid Content-Length, but no body data follows. The server must determine the message body length from Content-Length and wait for that many bytes:

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error." — RFC 9110 Section 15.5.1

A server may reject the request with 400 if the declared body size exceeds its limits, close the connection, or timeout waiting for body data that never arrives.

## Why it matters

Tests whether the server pre-allocates memory for the declared body size or waits for data to arrive. A server that allocates 1GB upfront from a Content-Length header is vulnerable to memory exhaustion DoS -- an attacker can send many such requests cheaply to exhaust server memory. The correct behavior is to either stream the body incrementally, reject absurdly large Content-Length values, or timeout waiting for the body data that never arrives.

## Deep Analysis

### Relevant ABNF

```
Content-Length = 1*DIGIT
message-body   = *OCTET
```

### RFC Evidence

> "When a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets."
> -- RFC 9112 Section 6.2

> "If the sender closes the connection or the recipient times out before the indicated number of octets are received, the recipient MUST consider the message to be incomplete and close the connection."
> -- RFC 9112 Section 6.2

> "A server MAY reject a request that contains a message body but not a Content-Length by responding with 411 (Length Required)."
> -- RFC 9112 Section 6.2

### Chain of Reasoning

1. **The request is syntactically valid.** `Content-Length: 999999999` conforms to the `1*DIGIT` production. The headers are well-formed. The request-line `POST / HTTP/1.1` is valid. There is no grammar violation in the request itself.

2. **The server expects ~1 GB of body data.** The `Content-Length` value of 999,999,999 declares that approximately 953 MB of body data should follow the empty line. Per RFC 9112 Section 6.2, this value "defines the expected message body length in octets."

3. **No body data arrives.** The connection remains open but no octets are sent after the CRLF CRLF that terminates the header section. The server is left waiting for data that will never come.

4. **The timeout/incompleteness rule applies.** RFC 9112 Section 6.2 states that if "the recipient times out before the indicated number of octets are received, the recipient MUST consider the message to be incomplete and close the connection." This MUST-level requirement ensures the server eventually reclaims resources.

5. **Pre-allocation is the vulnerability.** A naive server implementation that allocates a buffer of `Content-Length` bytes upon receiving the headers would allocate ~1 GB for a single request. An attacker sending many such requests with no body can exhaust server memory with minimal bandwidth. This maps directly to [CWE-770](https://cwe.mitre.org/data/definitions/770.html) (Allocation of Resources Without Limits or Throttling).

6. **The correct defensive behavior is one of three options**: (a) reject the request immediately with 400 because the Content-Length exceeds a configured maximum; (b) stream the body incrementally without pre-allocation, eventually timing out; or (c) respond with 413 (Content Too Large) indicating the declared body exceeds server limits.

## Sources

- [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) — Content-Length grammar
- [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) — Content-Length body framing
- [RFC 9110 Section 15.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.1) — 400 Bad Request
- [CWE-770](https://cwe.mitre.org/data/definitions/770.html) — Allocation of Resources Without Limits
