---
title: "TRAILER-HOST"
description: "TRAILER-HOST test documentation"
weight: 34
---

| | |
|---|---|
| **Test ID** | `SMUG-TRAILER-HOST` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §6.5.2](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.2) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

A valid chunked request with `Host: evil.example.com` in the trailer section, while the actual Host header in the request points to the real server.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
0\r\n
Host: evil.example.com\r\n
\r\n
```

A `Host: evil.example.com` header appears in the chunked trailers section.


## What the RFC says

> "Many fields cannot be processed outside the header section because their evaluation is necessary prior to receiving the content, such as those that describe message framing, routing, authentication, request modifiers, response controls, or content format. A sender MUST NOT generate a trailer field unless the sender knows the corresponding header field name's definition permits the field to be sent in trailers." — RFC 9110 §6.5.1

> "A recipient MUST NOT merge a received trailer field into the header section unless its corresponding header field definition explicitly permits and instructs how the trailer field value can be safely merged." — RFC 9112 §7.1.2

Host is a routing field that controls which virtual host processes the request. It must not appear in trailers because its evaluation is necessary prior to receiving the content, and it could alter message routing semantics after the body has been processed.

## Why this test is unscored

The sender violates the RFC by placing Host in a trailer. The server must either reject the request or silently discard the prohibited trailer field. Both `400` (reject) and `2xx` (process body, discard trailer) are defensible since the chunked body itself is valid.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (processes body and discards prohibited trailer).

## Why it matters

If a server or middleware reads the Host trailer and uses it for routing decisions, an attacker could redirect requests to a different virtual host or backend after the message body has already been accepted. This is a form of late-binding host injection.

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

And the Host header itself (from RFC 9110 Section 7.2):

```
Host = uri-host [ ":" port ]
```

Host is a singleton routing field that must be evaluated before content processing begins -- making it categorically prohibited from appearing in trailers.

### RFC Evidence

> "Many fields cannot be processed outside the header section because their evaluation is necessary prior to receiving the content, such as those that describe message framing, routing, authentication, request modifiers, response controls, or content format. A sender MUST NOT generate a trailer field unless the sender knows the corresponding header field name's definition permits the field to be sent in trailers." -- RFC 9110 Section 6.5.1

> "A recipient MUST NOT merge a received trailer field into the header section unless its corresponding header field definition explicitly permits and instructs how the trailer field value can be safely merged." -- RFC 9112 Section 7.1.2

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

### Chain of Reasoning

1. **Host is a routing field -- the most dangerous trailer category.** RFC 9110 Section 6.5.1 lists "routing" as a prohibited trailer field category. Host is the primary routing field in HTTP/1.1 -- it determines which virtual host processes the request, which back-end the proxy selects, and which cache key is used. Its value must be known before the server begins processing the request, not after the body has been received.

2. **The Host trailer creates a temporal paradox for routing.** When the server receives the request headers, it routes to `localhost:8080` based on the Host header. The body is received and processed. Then the trailer arrives with `Host: evil.example.com`. If any component processes this trailer, the routing decision made at header-time is now contradicted by a value received after the fact. This is not merely a parsing disagreement -- it is a deliberate attempt to change the request's routing identity after the body has been accepted.

3. **The MUST NOT merge rule is the critical defense.** RFC 9112 Section 7.1.2 prohibits merging trailer fields into the header section unless the field definition explicitly allows it. The Host field definition does not permit trailer usage. If a server or intermediary violates this rule and merges the Host trailer, it effectively has two Host values -- the original (`localhost:8080`) and the trailer (`evil.example.com`). This creates the same "more than one Host" condition that RFC 9112 Section 3.2 says MUST be rejected with 400.

4. **Attack scenario.** An attacker sends a chunked POST to a caching proxy. The headers contain `Host: legitimate.com`, so the proxy routes to the legitimate origin and begins computing the cache key. The body is received normally. In the trailer, the attacker sends `Host: attacker.com`. If the proxy merges the trailer Host into its request metadata, it may either (a) use `attacker.com` for the cache key, poisoning the cache for that host, or (b) forward a request with conflicting Host values to the origin, causing the origin to route to the wrong virtual host. In either case, the attacker has injected a Host value that bypassed the proxy's initial routing check.

### Scored / Unscored Justification

This test is **unscored** (`Scored = false`). The sender is in clear violation (MUST NOT generate Host as a trailer), but the server has two compliant responses: reject (`400`) or accept the valid chunked body while discarding the prohibited Host trailer (`2xx`). A `2xx` response is ambiguous -- the server may have correctly discarded the trailer (safe) or may have processed it (vulnerable). The real danger depends on whether any component in the request chain -- the server, a reverse proxy, a WAF, or a caching layer -- reads and acts on the Host trailer. This cannot be determined from a single status code, so the test flags `2xx` as a warning for manual investigation.

## Sources

- [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1)
- [RFC 9110 §6.5.2](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.2)
- [RFC 9112 §7.1.2](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.2)
