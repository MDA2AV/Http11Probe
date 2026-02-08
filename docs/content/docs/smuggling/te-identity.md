---
title: "TE-IDENTITY"
description: "TE-IDENTITY test documentation"
weight: 30
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-IDENTITY` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

A request with `Transfer-Encoding: identity` and `Content-Length: 5`. The `identity` encoding was deprecated and removed in HTTP/1.1 (RFC 7230 and later RFC 9112).

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: identity\r\n
Content-Length: 5\r\n
\r\n
hello
```


## What the RFC says

> "A server that receives a request message with a transfer coding it does not understand SHOULD respond with 501 (Not Implemented)." -- RFC 9112 Section 6.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 Section 6.3

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 Section 6.1

The `identity` transfer coding was listed in RFC 2616 (Section 3.6) as a registered transfer coding meaning "no transformation." It was removed from the transfer coding registry in RFC 7230 and is absent from RFC 9112. Since `identity` is no longer a recognized transfer coding, a server receiving `Transfer-Encoding: identity` is receiving an unknown coding.

## Why it matters

If a front-end treats `identity` as "no encoding" and uses Content-Length, but a back-end rejects the unknown TE, they disagree on how to parse the body. Conversely, if the back-end ignores the TE header entirely, CL is used — but a front-end that rejects may not forward the request at all. Both scenarios create desync potential.

## Deep Analysis

### ABNF

```
Transfer-Encoding = #transfer-coding       ; RFC 9112 §6.1
transfer-coding   = token                  ; RFC 9110 §10.1.4
token             = 1*tchar                ; RFC 9110 §5.6.2
tchar             = "!" / "#" / "$" / "%" / "&" / "'" / "*"
                    / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
                    / DIGIT / ALPHA
```

The token `identity` is syntactically valid per the ABNF (it consists entirely of ALPHA characters). However, `identity` is not a registered transfer coding in RFC 9112. It was present in RFC 2616 section 3.6 as a "no transformation" coding but was explicitly removed in RFC 7230 and remains absent from RFC 9112.

### RFC Evidence

> "A server that receives a request message with a transfer coding it does not understand SHOULD respond with 501 (Not Implemented)." -- RFC 9112 §6.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 §6.3

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 §6.1

### Chain of Reasoning

1. The test sends `Transfer-Encoding: identity` alongside `Content-Length: 5`.
2. The `identity` coding was listed in RFC 2616 (the original HTTP/1.1 spec) as meaning "no transformation." It was removed in RFC 7230 (the revised HTTP/1.1 message syntax) and remains absent from RFC 9112.
3. Since `identity` is not registered in the current HTTP Transfer Coding registry, a compliant server does not understand it. RFC 9112 section 6.1 states that a server receiving an unrecognized transfer coding SHOULD respond with `501 (Not Implemented)`.
4. The presence of both Transfer-Encoding and Content-Length triggers RFC 9112 section 6.3, which states that Transfer-Encoding overrides Content-Length and that such a message "ought to be handled as an error."
5. RFC 9112 section 6.1 additionally requires the server to close the connection after responding to a request with both headers.
6. The `identity` coding is particularly dangerous because legacy servers may still recognize it from their RFC 2616 implementations, treating it as "no transformation" and falling back to Content-Length framing -- while modern servers reject it as unknown.

### Scored / Unscored Justification

This test is **scored** (MUST reject). Although the SHOULD in RFC 9112 section 6.1 for unrecognized transfer codings is not a MUST, the combined presence of Transfer-Encoding and Content-Length triggers the MUST-level requirement in section 6.1 to close the connection. The server cannot safely process `Transfer-Encoding: identity` because it is not a recognized coding, and the dual-header scenario mandates connection closure at minimum.

- **Pass (400 or close):** The server correctly rejects the unknown transfer coding or closes the connection per the dual-header rule.
- **Fail (2xx):** The server accepted a request with an unrecognized transfer coding and conflicting Content-Length, violating the connection-closure requirement.

### Smuggling Attack Scenarios

- **Legacy vs. Modern Desync:** An attacker sends `Transfer-Encoding: identity` with `Content-Length: 5`. A legacy proxy still running RFC 2616 logic recognizes `identity` as "no transformation" and uses Content-Length for framing. A modern back-end treats `identity` as an unknown coding and responds with an error. But if the proxy already forwarded the body, leftover bytes on the connection become a smuggled request.
- **Identity-as-Passthrough Exploit:** Some server implementations treat any unrecognized Transfer-Encoding as a no-op, effectively implementing `identity` behavior. If a front-end rejects the unknown coding but the back-end silently accepts it and uses Content-Length, the attacker controls body-boundary interpretation through the framing disagreement.
- **Encoding Priority Confusion:** When a server sees `Transfer-Encoding: identity`, it must decide whether Transfer-Encoding is "present" (and thus overrides Content-Length per section 6.3) or "unrecognized" (and thus potentially ignorable). This ambiguity in priority logic is exactly what attackers exploit to create desync conditions.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
- [RFC 2616 §3.6](https://www.rfc-editor.org/rfc/rfc2616#section-3.6) (obsolete -- defined `identity` as a transfer coding)
