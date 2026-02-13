---
title: "TE-XCHUNKED"
description: "TE-XCHUNKED test documentation"
weight: 5
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-XCHUNKED` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST reject |
| **Expected** | `400`/`501` or close |

## What it sends

`Transfer-Encoding: xchunked` with a Content-Length header. The TE value `xchunked` is not a recognized encoding.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: xchunked\r\n
Content-Length: 5\r\n
\r\n
hello
```


## What the RFC says

> "A server that receives a request message with a transfer coding it does not understand SHOULD respond with 501 (Not Implemented)." -- RFC 9112 Section 6.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 Section 6.3

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 Section 6.1

## Why it matters

If the front-end ignores the unknown TE and uses CL, but the back-end strips the `x` and processes it as `chunked`, a smuggling vector exists. Some real-world proxies have exhibited this exact behavior.

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

The token `xchunked` is syntactically valid per the ABNF -- it consists entirely of ALPHA characters, all of which are valid `tchar`. However, `xchunked` is not a registered transfer coding in the IANA HTTP Transfer Coding registry. The only similarity to `chunked` is visual; syntactically, `xchunked` is an entirely different token.

### RFC Evidence

> "A server that receives a request message with a transfer coding it does not understand SHOULD respond with 501 (Not Implemented)." -- RFC 9112 §6.1

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 §6.3

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 §6.1

### Chain of Reasoning

1. The test sends `Transfer-Encoding: xchunked` alongside `Content-Length: 5`.
2. The token `xchunked` is not registered in the HTTP Transfer Coding registry. It is an unknown transfer coding.
3. RFC 9112 section 6.1 states that a server receiving an unrecognized transfer coding SHOULD respond with `501 (Not Implemented)`. While this is a SHOULD (not MUST), rejecting is the expected behavior.
4. The presence of both Transfer-Encoding and Content-Length triggers RFC 9112 section 6.3, which states that Transfer-Encoding overrides Content-Length and such a message "ought to be handled as an error."
5. RFC 9112 section 6.1 further requires the server to close the connection after responding to any request containing both headers, regardless of how it processes the request. This is a MUST-level requirement.
6. The `xchunked` value is specifically designed to test whether servers perform fuzzy matching or prefix stripping on transfer coding names. Some implementations have been observed to strip an `x` prefix (treating it as an extension marker, similar to MIME `x-` prefixes) and process the remainder as `chunked`.

### Scored / Unscored Justification

This test is **scored** (MUST reject). The MUST-level connection-closure requirement in RFC 9112 section 6.1 applies to all requests containing both Transfer-Encoding and Content-Length. Additionally, `xchunked` is not a recognized transfer coding, so the SHOULD-level guidance to respond with `501` reinforces rejection. The server cannot safely process a body framed with an unknown coding.

- **Pass (400/501 or close):** The server rejects the unknown transfer coding or closes the connection per the dual-header rule.
- **Fail (2xx):** The server accepted a request with an unrecognized transfer coding and conflicting Content-Length, violating the connection-closure requirement.

### Smuggling Attack Scenarios

- **Prefix-Stripping Desync:** An attacker sends `Transfer-Encoding: xchunked`. A front-end proxy does not recognize the coding and falls back to Content-Length framing. A back-end with a buggy parser strips the `x` prefix and processes `chunked` framing. The front-end read 5 bytes using Content-Length; the back-end reads until a chunked terminator. The attacker controls the boundary difference.
- **Fuzzy Matching Exploit:** Some server implementations perform approximate matching on transfer coding names (e.g., substring search for "chunked"). These servers would process `xchunked`, `chunked1`, or `_chunked` as valid chunked encoding. If a strict front-end rejects the unrecognized coding while a fuzzy-matching back-end accepts it, the desync enables smuggling.
- **Unknown-TE Fallback Behavior:** Different servers handle unknown transfer codings differently: some respond with `501`, some with `400`, some close the connection, and some ignore the header entirely and fall back to Content-Length. An attacker can probe a proxy chain to find a combination where one hop rejects and another accepts, then exploit the behavioral gap for smuggling.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
