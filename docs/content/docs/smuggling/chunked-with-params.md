---
title: "CHUNKED-WITH-PARAMS"
description: "CHUNKED-WITH-PARAMS test documentation"
weight: 32
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNKED-WITH-PARAMS` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

`Transfer-Encoding: chunked;ext=val` — parameters on the chunked coding.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked;ext=val\r\n
Content-Length: 5\r\n
\r\n
hello
```

The `chunked` encoding name has parameters appended (`;ext=val`).


## What the RFC says

> "The chunked coding does not define any parameters. Their presence SHOULD be treated as an error."
>
> — RFC 9112 §7.1

## Why this test is unscored

The RFC says parameters on `chunked` SHOULD be treated as an error -- this is a SHOULD-level requirement, not MUST. Some servers strip the parameters and decode the chunked body normally; others reject with 400. Both behaviors are defensible, so neither outcome is marked as a failure.

## Why it matters

If a front-end strips the parameter and forwards the body as chunked, but the back-end rejects the Transfer-Encoding value entirely and falls back to Content-Length, the two parsers disagree on message framing -- a classic CL/TE desynchronization vector.

## Deep Analysis

### Relevant ABNF (RFC 9112 §7.1)

```
chunked-body = *chunk last-chunk trailer-section CRLF

chunk        = chunk-size [ chunk-ext ] CRLF
               chunk-data CRLF
chunk-size   = 1*HEXDIG
```

Note: The `chunk-ext` grammar in §7.1.1 applies to extensions on individual **chunk-size lines**, not to the `Transfer-Encoding` header value itself. The `chunked` coding name in the header is a transfer coding token, separate from chunk-level extensions.

### RFC Evidence

**RFC 9112 §7.1** explicitly states:

> "The chunked coding does not define any parameters. Their presence SHOULD be treated as an error."

This is a SHOULD-level requirement (not MUST), meaning implementations are strongly recommended to treat parameters as an error but are not strictly required to do so.

**RFC 9112 §7** establishes the baseline parsing obligation:

> "A recipient MUST be able to parse and decode the chunked transfer coding."

**RFC 9112 §7** also covers unrecognized codings:

> "A server that receives a request message with a transfer coding it does not understand SHOULD respond with 501 (Not Implemented)."

### Step-by-Step Analysis

1. The server receives `Transfer-Encoding: chunked;ext=val`.
2. It must parse the transfer coding token. The coding name is `chunked`.
3. After the coding name, `;ext=val` appears as a parameter on the coding itself (not a chunk extension on a chunk-size line).
4. Per §7.1, the chunked coding defines no parameters. The text says their presence "SHOULD be treated as an error."
5. A strict server rejects with 400. A lenient server strips the parameter and processes the body as chunked. Both behaviors are defensible under SHOULD-level language.
6. Critically, this request also includes `Content-Length: 5`. If the server rejects the Transfer-Encoding value and falls back to Content-Length, it reads the raw body `hello` as a flat 5-byte body rather than as chunked data.

### Real-World Smuggling Scenario

This is a classic **CL/TE desynchronization** vector:

**Attack vector:** The request includes both `Transfer-Encoding: chunked;ext=val` and `Content-Length: 5`. A front-end proxy recognizes `chunked` (ignoring the parameter) and processes the body using chunked framing. The back-end rejects the parameterized `chunked;ext=val` as invalid and falls back to `Content-Length: 5`, reading 5 bytes of raw body. The remaining bytes after those 5 are left in the connection buffer and get prepended to the next request -- this is request smuggling.

This pattern was demonstrated in James Kettle's original HTTP request smuggling research (2019) and is the foundation of CL/TE attacks. CVE-2021-22959 (llhttp) and CVE-2022-32213 (Node.js) involved similar Transfer-Encoding parsing discrepancies where malformed TE values caused some parsers to fall back to Content-Length.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
