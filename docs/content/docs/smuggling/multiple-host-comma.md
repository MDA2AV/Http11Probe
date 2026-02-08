---
title: "MULTIPLE-HOST-COMMA"
description: "MULTIPLE-HOST-COMMA test documentation"
weight: 52
---

| | |
|---|---|
| **Test ID** | `SMUG-MULTIPLE-HOST-COMMA` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §7.2](https://www.rfc-editor.org/rfc/rfc9110#section-7.2) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A Host header with two comma-separated hostnames.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080, other.example.com\r\n
\r\n
```

The Host header contains `localhost:8080, other.example.com` — two distinct hostnames in a single header value.


## What the RFC says

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." — RFC 9112 §3.2

The Host header is not a list-based field. A comma in the Host value does not indicate multiple list elements — it means the value itself contains two distinct hostnames, which is an invalid field value. The server MUST reject such a request.

## Why it matters

If a front-end proxy extracts the first hostname (`localhost:8080`) for routing but the back-end extracts the second (`other.example.com`), routing confusion occurs. An attacker could use this to bypass virtual host restrictions, access internal services, or poison caches for the wrong host. This is a host-header injection vector that enables cache poisoning and SSRF attacks.

## Deep Analysis

### Relevant ABNF

The Host header has a specific grammar that does not permit list syntax:

```
Host = uri-host [ ":" port ]
```

Unlike most HTTP headers, Host is a singleton field -- it is not defined as a comma-separated list (`#element`). A comma in the Host value is not a list separator; it is a literal character that makes the entire value invalid.

### RFC Evidence

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

> "A client MUST send a Host header field in an HTTP/1.1 request even if the request-target is in absolute-form." -- RFC 9112 Section 3.2.2

> "If the target URI's authority component is empty, the client MUST send a Host header field with an empty field-value." -- RFC 9110 Section 7.2

### Chain of Reasoning

1. **The ABNF for Host is a single `uri-host` with optional port.** Unlike headers such as `Accept` or `Cache-Control`, which use `#element` (comma-separated list) syntax, the Host header is defined as a single value. The value `localhost:8080, other.example.com` does not match `uri-host [ ":" port ]` because the comma and second hostname make the value syntactically invalid per the grammar.

2. **RFC 9112 Section 3.2 mandates rejection.** The requirement uses MUST language: the server MUST respond with `400` to any request with "a Host header field with an invalid field value." A comma-separated list of hostnames is an invalid field value for the Host header. There is no MAY or SHOULD qualifier -- this is an absolute requirement.

3. **Parser disagreement on comma-separated Host enables routing attacks.** Different implementations may extract different hostnames from `localhost:8080, other.example.com`. A proxy that takes the first value routes to `localhost:8080`. A back-end that takes the last value routes to `other.example.com`. A third implementation might use the entire string as-is, failing to match any virtual host. This inconsistency is the foundation of host-header injection attacks.

4. **Attack scenario.** An attacker sends `Host: legitimate.com, attacker.com` to a CDN. The CDN extracts `legitimate.com` for cache key computation and routes the request to the legitimate origin. The origin extracts `attacker.com` and generates a response with links, redirects, or resource URLs pointing to `attacker.com`. The CDN caches this poisoned response under the `legitimate.com` cache key. Every subsequent visitor to `legitimate.com` receives the poisoned response with attacker-controlled URLs.

### Scored / Unscored Justification

This test is **scored**. RFC 9112 Section 3.2 uses unconditional MUST language: the server "MUST respond with a 400 (Bad Request) status code" for requests with an invalid Host field value. A comma-separated Host value is syntactically invalid per the Host ABNF. There is no ambiguity in the requirement and no alternative interpretation that would allow a `2xx` response. A server that accepts this request is in direct violation of a MUST-level requirement and is vulnerable to host-header injection, cache poisoning, and routing confusion attacks.

## Sources

- [RFC 9110 §7.2](https://www.rfc-editor.org/rfc/rfc9110#section-7.2)
- [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
