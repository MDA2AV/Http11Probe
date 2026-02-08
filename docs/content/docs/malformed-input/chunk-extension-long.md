---
title: "CHUNK-EXT-64K"
description: "CHUNK-EXT-64K test documentation"
weight: 18
---

| | |
|---|---|
| **Test ID** | `MAL-CHUNK-EXT-64K` |
| **Category** | Malformed Input |
| **Expected** | `400`/`431` = Pass, `2xx` = Warn, close = Pass |

## What it sends

A chunked request with a chunk extension containing 64KB (65,536 bytes) of data.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5;ext=aaaa...{65,536 x 'a'}...\r\n
hello\r\n
0\r\n
\r\n
```

The chunk extension value is 65,536 bytes of `a` characters.


## What the RFC says

Chunk extensions are syntactically valid per the ABNF:

> `chunk-ext = *( BWS ";" BWS chunk-ext-name [ BWS "=" BWS chunk-ext-val ] )` — RFC 9112 Section 7.1.1

> `chunk-ext-name = token` — RFC 9112 Section 7.1.1

> `chunk-ext-val = token / quoted-string` — RFC 9112 Section 7.1.1

However, the RFC explicitly recommends limiting their size:

> "A recipient MUST ignore unrecognized chunk extensions. A server ought to limit the total length of chunk extensions received in a request to an amount reasonable for the services provided, in the same way that it applies length limitations and timeouts for other parts of a message, and generate an appropriate 4xx (Client Error) response if that amount is exceeded." — RFC 9112 Section 7.1.1

## Pass/Warn explanation

- **Pass (400/431):** The server rejects the oversized chunk extension, following the RFC recommendation to limit extension length.
- **Warn (2xx):** The server accepted the 64KB extension. While syntactically valid, accepting such large extensions without limits is a resource exhaustion risk.

## Why it matters

While chunk extensions are syntactically valid, a 64KB extension is pathological. CVE-2023-39326 demonstrated that Go's `net/http` library could be exploited via large chunk extensions to cause excessive memory consumption and DoS. A robust server should limit chunk extension size.

## Deep Analysis

### ABNF context

The chunk extension grammar is syntactically permissive:

```
chunk     = chunk-size [ chunk-ext ] CRLF
            chunk-data CRLF
chunk-ext = *( BWS ";" BWS chunk-ext-name
               [ BWS "=" BWS chunk-ext-val ] )

chunk-ext-name = token
chunk-ext-val  = token / quoted-string
token          = 1*tchar
```

A 64KB string of `a` characters is a valid `token` per the grammar -- each `a` is an `ALPHA` and therefore a valid `tchar`. The extension `ext=aaaa...` is syntactically correct. There is no ABNF upper bound on extension length.

### RFC evidence

> "A server ought to limit the total length of chunk extensions received in a request to an amount reasonable for the services provided, in the same way that it applies length limitations and timeouts for other parts of a message, and generate an appropriate 4xx (Client Error) response if that amount is exceeded." -- RFC 9112 Section 7.1.1

> "A recipient MUST ignore unrecognized chunk extensions." -- RFC 9112 Section 7.1.1

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error (e.g., malformed request syntax, invalid request message framing, or deceptive request routing)." -- RFC 9110 Section 15.5.1

The first quote is the key normative guidance: while the grammar allows unlimited extension length, the RFC explicitly instructs servers to impose practical limits and respond with a 4xx error when those limits are exceeded. A 64KB extension is far beyond any reasonable limit.

### Chain of reasoning

1. The client sends a chunked POST with `5;ext=aaaa...` where the extension value is 65,536 bytes.
2. The server parses the chunk-size (`5`) and then encounters the chunk-ext production.
3. Per the ABNF, it reads `; ext = aaaa...` -- syntactically valid but extraordinarily long.
4. A well-implemented server enforces a length limit on chunk extensions (just as it limits header sizes and request-line length) and rejects the request with 400 or 431.
5. A server that does not limit extension length will buffer 64KB of useless metadata per chunk, creating a denial-of-service vector.

### Security implications

- **Denial of service (CVE-2023-39326)**: Go's `net/http` library prior to the fix allowed attackers to send requests with very large chunk extensions, causing excessive memory allocation. An attacker could send many chunks, each with a large extension, amplifying memory consumption far beyond the actual body size.
- **Memory amplification**: The body data is only 5 bytes (`hello`), but the chunk metadata is 64KB. A stream of such chunks forces the server to allocate orders of magnitude more memory for metadata than for payload.
- **Slowloris-style attacks**: Large extensions slow down chunk parsing and keep connections open longer, reducing the server's capacity to handle legitimate requests.
- **Proxy bypass**: Intermediaries that strip or ignore chunk extensions may forward only the 5-byte body, while the origin server is still burdened by parsing the extension -- creating an asymmetric resource consumption attack.

## Sources

- [RFC 9112 Section 7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) — chunk extensions and size limits
- [CVE-2023-39326](https://nvd.nist.gov/vuln/detail/CVE-2023-39326) — Go net/http chunk extension DoS
