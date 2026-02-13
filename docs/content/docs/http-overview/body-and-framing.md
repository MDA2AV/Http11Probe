---
title: Body and Framing
description: "Content-Length, chunked transfer encoding, trailers, and why CL+TE conflicts cause request smuggling."
weight: 5
---

HTTP/1.1 messages optionally carry a **message body** after the header section. The critical question for any parser is: **where does the body end?** Getting this wrong is the root cause of HTTP request smuggling.

## When Is a Body Present?

- **Requests** — a body is present if `Content-Length` or `Transfer-Encoding` is set. `GET`, `HEAD`, `DELETE`, and `OPTIONS` typically have no body (though the spec doesn't forbid it).
- **Responses** — all responses to `HEAD` requests and all `1xx`, `204`, and `304` responses have no body. Everything else may have a body.

## Content-Length

The `Content-Length` header declares the exact size of the body in bytes as a decimal integer:

```http
POST /data HTTP/1.1
Host: example.com
Content-Type: text/plain
Content-Length: 13

Hello, World!
```

The parser reads exactly 13 bytes after the empty line, then the next bytes are the start of the next message (on a persistent connection) or the connection ends.

### Rules

- The value **MUST** be a non-negative decimal integer.
- **No leading zeros** — `Content-Length: 007` is invalid.
- **No signs** — `Content-Length: +13` or `Content-Length: -1` are invalid.
- **No whitespace** within the value — `Content-Length: 1 3` is invalid.
- If `Content-Length` **doesn't match** the actual body size, the message is malformed. The server SHOULD close the connection.
- **Multiple `Content-Length` headers** are allowed only if all values are identical. If they differ, the message is malformed and MUST be rejected.

### Why Strictness Matters

Lenient parsing of `Content-Length` is a common source of vulnerabilities:

- `Content-Length: 0x0d` — if parsed as hex, this is 13 bytes. If parsed as decimal, it's invalid. A parser mismatch between front-end and back-end enables smuggling.
- `Content-Length: 13, 14` — a list of two differing values. One parser might take the first, another the last.

## Chunked Transfer Encoding

When the total body size is unknown at the time headers are sent (streaming, server-generated content, compression), HTTP/1.1 uses **chunked transfer encoding**.

### Format

```
chunk-size (hex) CRLF
chunk-data CRLF
...
0 CRLF
[ trailer-section ]
CRLF
```

Each chunk starts with the chunk size in hexadecimal, followed by CRLF, then exactly that many bytes of data, followed by CRLF. A zero-length chunk signals the end of the body.

### Full Example

```http
HTTP/1.1 200 OK
Transfer-Encoding: chunked

4\r\n
Wiki\r\n
7\r\n
pedia i\r\n
B\r\n
n chunks.\r\n
0\r\n
\r\n
```

Decoded body: `Wikipedia in chunks.`

### Chunk Extensions

A chunk-size may be followed by semicolon-separated extensions:

```
a;ext-name=ext-value\r\n
0123456789\r\n
```

Most servers and proxies **ignore** chunk extensions. They exist for potential use cases like per-chunk checksums or metadata, but are rarely used in practice. Some security tools test whether servers handle unexpected extensions safely.

### Trailers

After the final zero-length chunk, **trailer fields** may appear — headers sent after the body:

```http
HTTP/1.1 200 OK
Transfer-Encoding: chunked
Trailer: Checksum

4\r\n
data\r\n
0\r\n
Checksum: abc123\r\n
\r\n
```

Trailers are useful for:
- **Checksums/signatures** — computed as the body streams.
- **Processing status** — whether the server completed successfully.
- **Metadata** — anything that can't be determined until after the body is generated.

The `Trailer` header in the response declares which trailer fields to expect (though this is advisory, not enforced).

### Rules

- Chunk sizes **MUST** be hexadecimal, case-insensitive (`a` and `A` are both valid).
- A zero-length chunk **MUST** be present to terminate the body.
- After the zero-length chunk, the trailer section and final CRLF complete the message.

## Content-Length vs Transfer-Encoding

A message **MUST NOT** contain both `Content-Length` and `Transfer-Encoding`.

RFC 9112 §6.1 is explicit:

> If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and **ought to be handled as an error**.

### The Request Smuggling Problem

This ambiguity is the **root cause of HTTP request smuggling**. Consider a message with both headers:

```http
POST / HTTP/1.1
Host: example.com
Content-Length: 6
Transfer-Encoding: chunked

0\r\n
\r\n
GPOST
```

- A parser that uses **Transfer-Encoding** sees a zero-length chunk → body ends immediately. The remaining bytes (`GPOST`) are the start of the next request.
- A parser that uses **Content-Length** reads 6 bytes (`0\r\n\r\nG`) as the body. `POST` becomes part of the next request with a different method.

If a front-end proxy uses one interpretation and a back-end server uses another, the attacker controls where one request ends and the next begins. This can:
- **Bypass access controls** — smuggle a request to an internal endpoint.
- **Poison caches** — make the cache store an attacker-controlled response for a victim's URL.
- **Hijack connections** — capture another user's request.

### How Servers Should Handle It

Strict servers should:
1. **Reject** messages with both `Content-Length` and `Transfer-Encoding` with a 400 response.
2. If not rejecting, **always prioritize `Transfer-Encoding`** and ignore `Content-Length`.
3. **Never trust `Content-Length`** when `Transfer-Encoding` is present.

This is one of the most critical compliance checks that Http11Probe performs.

## Transfer-Encoding Obfuscation

Attackers may try to hide `Transfer-Encoding` from one parser while making another recognize it:

```http
Transfer-Encoding: chunked
Transfer-Encoding : chunked
Transfer-Encoding: xchunked
Transfer-Encoding: chunked\r\n (extra space)
Transfer-Encoding:
 chunked
```

Each of these variants exploits differences in how parsers handle:
- Whitespace before the colon (forbidden by RFC 9112 §5.1).
- Unknown transfer coding names.
- Obs-fold (deprecated line folding).
- Leading/trailing whitespace in the value.

Strict, RFC-compliant parsing eliminates these attack surfaces.
