---
title: RFC Basics
weight: 1
---

## What is an RFC?

An **RFC** (Request for Comments) is a formal document published by the [Internet Engineering Task Force (IETF)](https://www.ietf.org/) that defines the standards and protocols that power the internet. Despite the informal-sounding name, RFCs are the authoritative specifications that all implementations must follow for interoperability.

## HTTP/1.1 RFCs

HTTP/1.1 is defined by two key RFCs:

| RFC | Title | Scope |
|-----|-------|-------|
| [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110) | HTTP Semantics | The **meaning** of HTTP — methods, status codes, headers, content negotiation |
| [RFC 9112](https://www.rfc-editor.org/rfc/rfc9112) | HTTP/1.1 Message Syntax and Routing | The **wire format** — how requests and responses are framed as bytes on a TCP connection |

These replaced the older RFC 7230–7235 series in June 2022. Http11Probe tests against the current (9110/9112) requirements.

## Requirement Levels

RFCs use specific keywords defined in [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119) and [RFC 8174](https://www.rfc-editor.org/rfc/rfc8174):

| Keyword | Meaning | In Http11Probe |
|---------|---------|----------------|
| **MUST** | Absolute requirement. Violating this means non-compliance. | Test expects exactly the mandated response (e.g., only 400) |
| **MUST NOT** | Absolute prohibition. | Test verifies the server does not exhibit prohibited behavior |
| **SHOULD** | Recommended, but valid reasons to deviate may exist. | Test expects the recommended response but accepts close |
| **MAY** | Optional behavior. | Test rewards stricter behavior but does not penalize lenience |
| **"ought to"** | Weaker than SHOULD — a recommendation with less force. | Test accepts multiple valid responses |

### How Http11Probe Maps Requirement Levels

- **MUST respond with 400** → Only `400` passes. Close or timeout is a fail.
- **MUST reject** (no specific code) → `400` or connection close passes.
- **SHOULD respond with 400** → `400` or connection close passes.
- **MAY accept** → Rejection (`400`/close) passes. Acceptance is RFC-compliant but noted.
- **"ought to" handle as error** → `400` or connection close passes.

## Reading Test IDs

Every test has an ID that encodes its source:

| Prefix | Meaning | Example |
|--------|---------|---------|
| `RFC9112-X.Y-` | RFC 9112, section X.Y | `RFC9112-2.2-BARE-LF-HEADER` |
| `RFC9110-X.Y-` | RFC 9110, section X.Y | `RFC9110-5.4-DUPLICATE-HOST` |
| `COMP-` | General compliance | `COMP-BASELINE` |
| `SMUG-` | Smuggling vector | `SMUG-CL-TE-BOTH` |
| `MAL-` | Malformed input | `MAL-BINARY-GARBAGE` |
