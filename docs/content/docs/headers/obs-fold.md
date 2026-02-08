---
title: "OBS-FOLD"
description: "OBS-FOLD test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `RFC9112-5.1-OBS-FOLD` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 5.2](https://www.rfc-editor.org/rfc/rfc9112#section-5.2) |
| **Requirement** | MUST |
| **Expected** | `400` |

## What it sends

A request with an obsolete line-folded header value — a continuation line that starts with whitespace:

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
X-Test: value\r\n
 continued\r\n
\r\n
```

The `X-Test` header value is split across two lines. The second line starts with a space (obs-fold / line folding).


## What the RFC says

> "A server that receives an obs-fold in a request message that is not within a 'message/http' container MUST either reject the message by sending a 400 (Bad Request), preferably with a representation explaining that obsolete line folding is unacceptable, or replace each received obs-fold with one or more SP octets prior to interpreting the field value or forwarding the message downstream." -- RFC 9112 Section 5.2

This is a MUST with two alternatives: send 400 or silently fix it. Http11Probe scores rejection (400) as a pass because it's the stricter option.

## Why it matters

Obs-fold creates ambiguity: is the continuation line part of the previous header's value, or a new header/request? Different parsers may disagree, creating a smuggling vector.

## Deep Analysis

### Relevant ABNF Grammar

```
field-line   = field-name ":" OWS field-value OWS
obs-fold     = OWS CRLF RWS
             ; obsolete line folding
```

The `obs-fold` production allows a field value to span multiple lines by following a CRLF with required whitespace (RWS -- at least one SP or HTAB). This was permitted in older HTTP versions but is now deprecated.

### RFC Evidence

**RFC 9112 Section 5.2** establishes the sender prohibition:

> "A sender MUST NOT generate a message that includes line folding (i.e., that has any field line value that contains a match to the obs-fold rule) unless the message is intended for packaging within the 'message/http' media type." -- RFC 9112 Section 5.2

**RFC 9112 Section 5.2** mandates two server alternatives:

> "A server that receives an obs-fold in a request message that is not within a 'message/http' container MUST either reject the message by sending a 400 (Bad Request), preferably with a representation explaining that obsolete line folding is unacceptable, or replace each received obs-fold with one or more SP octets prior to interpreting the field value or forwarding the message downstream." -- RFC 9112 Section 5.2

**RFC 9112 Section 5.2** also governs intermediaries:

> "A proxy or gateway that receives an obs-fold in a response message that is not within a 'message/http' container MUST either discard the message and replace it with a 502 (Bad Gateway) response...or replace each received obs-fold with one or more SP octets prior to interpreting the field value or forwarding the message downstream." -- RFC 9112 Section 5.2

### Chain of Reasoning

1. The `obs-fold` rule allows a CRLF followed by whitespace to appear within a field value, making one logical header span multiple lines.
2. Different parsers may disagree on whether the continuation line is part of the previous header value or the start of a new header (or even a new request). This ambiguity is a smuggling vector.
3. The RFC offers servers two MUST-level alternatives: reject with 400, or normalize by replacing obs-fold with SP. Both are compliant.
4. Http11Probe scores 400 as Pass because rejection is the stricter and safer option. A server that normalizes obs-fold would need to be tested differently (by verifying the resulting field value), which is outside the scope of this probe.

### Scoring Justification

**Scored (MUST).** Although the RFC provides two compliant alternatives (reject with 400 or replace with SP), Http11Probe can only observe the rejection path from the outside. A 400 response demonstrates the server recognized and rejected the deprecated syntax. A 2xx response is ambiguous -- it could mean the server correctly normalized the obs-fold, or it could mean the server blindly accepted malformed input. Since the probe cannot distinguish these cases, 400 is scored as Pass.

## Sources

- [RFC 9112 Section 5.2 -- Obsolete Line Folding](https://www.rfc-editor.org/rfc/rfc9112#section-5.2)
