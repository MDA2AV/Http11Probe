---
title: "TECL-PIPELINE"
description: "TECL-PIPELINE test documentation"
weight: 9
---

| | |
|---|---|
| **Test ID** | `SMUG-TECL-PIPELINE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST close connection |
| **Expected** | `400` or close |

## What it sends

A full TE.CL smuggling payload — the reverse of CLTE. The front-end uses Transfer-Encoding and the body is crafted so the back-end (using Content-Length) sees a smuggled request.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
Content-Length: 30\r\n
\r\n
0\r\n
\r\n
```

Followed immediately on the same connection by:

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

A TE parser sees the `0` chunk as end-of-body. A CL-only parser tries to read 30 bytes and consumes the follow-up `GET` as body data.


## What the RFC says

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling (Section 11.2) or response splitting (Section 11.1) and ought to be handled as an error." — RFC 9112 §6.3

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." — RFC 9112 §6.1

## Why it matters

The TE.CL variant is equally dangerous to CL.TE. Together, they cover both possible orderings of front-end/back-end preference.

## Deep Analysis

### Relevant ABNF

From RFC 9112 Section 6:

```
Transfer-Encoding = #transfer-coding
Content-Length    = 1*DIGIT
message-body     = *OCTET
```

The TE.CL variant reverses the header order compared to CL.TE, but the same precedence rule applies: Transfer-Encoding overrides Content-Length.

### RFC Evidence

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 Section 6.3

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 Section 6.1

> "A sender MUST NOT send a Content-Length header field in any message that contains a Transfer-Encoding header field." -- RFC 9112 Section 6.2

### Chain of Reasoning

1. **TE.CL reverses the parser disagreement.** In this variant, the front-end uses Transfer-Encoding (reads the `0` chunk, considers the POST body complete) while the back-end uses Content-Length (tries to read 30 bytes, consuming the pipelined `GET` as part of the POST body). The fundamental issue is identical to CL.TE: two parsers in the same chain disagree on where the first request's body ends.

2. **The 30-byte Content-Length is carefully chosen.** The chunked body (`0\r\n\r\n`) is only 5 bytes. Content-Length says 30. A CL parser will attempt to read 25 more bytes from the connection, consuming part or all of the follow-up `GET` request. This means the back-end either hangs waiting for data, consumes the next request, or produces an error -- all of which indicate a desync.

3. **The RFC treats both variants identically.** Section 6.3 does not distinguish CL+TE from TE+CL header ordering. The rule is the same: TE overrides CL, the message ought to be treated as an error, and Section 6.1 mandates connection closure regardless of how the server processes the request. A compliant server handles both CLTE and TECL with the same defensive behavior.

4. **Attack scenario.** An attacker sends the TE.CL payload through a proxy. The proxy processes chunked encoding, sees the empty `0` terminator, and forwards the completed POST. The back-end, using Content-Length, reads 30 bytes -- consuming the chunked body plus the beginning of the next legitimate request from the proxy's pipeline. The back-end now has a corrupted view of the request stream, and the attacker can inject arbitrary request fragments that the proxy never inspected.

### Scored / Unscored Justification

This test is **scored** -- a `2xx` response results in a **Fail**. The reasoning mirrors CLTE-PIPELINE exactly: RFC 9112 Section 6.1 uses MUST-level language requiring connection closure after a dual CL+TE request. If the pipelined `GET` receives a `2xx` response, the server kept the connection open and is demonstrably vulnerable to the TE.CL smuggling variant. Both CL.TE and TE.CL are scored because the RFC requirement is the same for both, and both represent direct, exploitable attack payloads.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
- [PortSwigger — HTTP Request Smuggling](https://portswigger.net/web-security/request-smuggling)
