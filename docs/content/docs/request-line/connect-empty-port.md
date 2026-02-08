---
title: "CONNECT-EMPTY-PORT"
description: "CONNECT-EMPTY-PORT test documentation"
weight: 11
---

| | |
|---|---|
| **Test ID** | `COMP-CONNECT-EMPTY-PORT` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2.3](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.3) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`CONNECT host: HTTP/1.1` — authority-form with empty port.

```http
CONNECT localhost: HTTP/1.1\r\n
Host: localhost:\r\n
\r\n
```

The authority-form has a colon but no port number.


## What the RFC says

> "The 'authority-form' of request-target is only used for CONNECT requests. It consists of only the uri-host and port number of the tunnel destination, separated by a colon (':')." -- RFC 9112 §3.2.3

> "authority-form = uri-host ':' port" -- RFC 9112 §3.2.3

An empty port (the colon is present but no digits follow) does not satisfy the `port` production rule, making this an invalid authority-form. Since the request-target does not match the required grammar, the server should reject it with `400`.

## Why it matters

CONNECT with an empty port is syntactically invalid. Accepting it could cause undefined proxy behavior.

## Deep Analysis

### Relevant ABNF Grammar

```
request-line    = method SP request-target SP HTTP-version
request-target  = origin-form / absolute-form / authority-form / asterisk-form
authority-form  = uri-host ":" port
port            = *DIGIT
```

The `authority-form` requires a `uri-host`, a literal colon, and a `port`. While the `port` production technically allows zero digits (`*DIGIT`), RFC 3986 Section 3.2.3 clarifies that the URI scheme defines the semantics of the port component, and HTTP requires a valid port number for CONNECT tunnel establishment.

### RFC Evidence

**RFC 9112 Section 3.2.3** defines the authority-form restriction:

> "The 'authority-form' of request-target is only used for CONNECT requests (Section 9.3.6 of [HTTP])." -- RFC 9112 Section 3.2.3

**RFC 9112 Section 3.2.3** specifies what the client must send:

> "When making a CONNECT request to establish a tunnel through one or more proxies, a client MUST send only the host and port of the tunnel destination as the request-target." — RFC 9112 Section 3.2.3

**RFC 9112 Section 3** provides the fallback for invalid request-lines:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." -- RFC 9112 Section 3

### Chain of Reasoning

1. The `authority-form` grammar requires `uri-host ":" port`. The CONNECT method mandates that the request-target contain "the host and port of the tunnel destination."
2. In `CONNECT localhost: HTTP/1.1`, the colon is present but the port component is empty (zero digits).
3. While `*DIGIT` technically matches zero digits at the ABNF level, the semantic requirement is clear: a CONNECT request must identify a tunnel destination with both host **and** port. An empty port does not identify a destination.
4. The RFC requires the client to send "only the host and port" -- an empty port means no port was specified, violating this requirement.
5. A server that accepts this must guess the port (e.g., defaulting to 80 or 443), which creates ambiguity in tunnel establishment.

### Scoring Justification

**Scored (MUST).** The RFC mandates that CONNECT targets include both host and port. An empty port violates the MUST requirement that the client "send only the host and port of the tunnel destination." A server that accepts an empty port is processing a syntactically incomplete authority-form, which could lead to undefined proxy behavior. The test expects `400` or connection close.

### Edge Cases

- **Port 0:** `CONNECT host:0 HTTP/1.1` is syntactically valid (port = 1*DIGIT) but semantically meaningless since port 0 is reserved. Most servers should reject this.
- **Port > 65535:** `CONNECT host:99999 HTTP/1.1` matches the ABNF but exceeds the valid TCP port range. Servers should validate the numeric range.
- **Trailing colon with spaces:** `CONNECT host:  HTTP/1.1` would be parsed as a request-line where the space after the colon is the SP delimiter. The port is still empty.

## Sources

- [RFC 9112 Section 3.2.3](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.3)
