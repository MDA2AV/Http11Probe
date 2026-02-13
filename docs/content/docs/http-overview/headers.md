---
title: Headers
description: "HTTP header structure, common request and response headers, and the Host header requirement."
weight: 3
---

Headers are the primary extension mechanism in HTTP. They carry metadata about the message, the resource, the connection, and the client/server.

## Structure

```
field-name ":" OWS field-value OWS CRLF
```

- **field-name** is case-insensitive and MUST NOT contain whitespace or colons. It must be a valid `token` — one or more characters from `!#$%&'*+-.^_|~`, digits, and letters.
- **OWS** (optional whitespace) may appear between the colon and the value, and after the value.
- **No space before the colon** — RFC 9112 §5.1 forbids whitespace between the field-name and the colon. Servers that receive it **MUST** reject the message with 400 or strip the whitespace before processing.
- Header field values can span multiple lines using **obs-fold** (obsolete line folding — a CRLF followed by at least one space or tab), but this is deprecated. Servers **MUST** either reject obs-fold with 400 or replace it with a single space before processing.

## Header Categories

HTTP headers fall into several categories based on their scope:

| Category | Description | Examples |
|----------|-------------|---------|
| **Request headers** | Sent by the client to provide context about the request. | `Host`, `Accept`, `Authorization`, `User-Agent` |
| **Response headers** | Sent by the server to provide context about the response. | `Server`, `Set-Cookie`, `WWW-Authenticate` |
| **Representation headers** | Describe the body content in either direction. | `Content-Type`, `Content-Length`, `Content-Encoding` |
| **Hop-by-hop headers** | Consumed by the next intermediary, not forwarded. Listed in the `Connection` header. | `Connection`, `Transfer-Encoding`, `Keep-Alive`, `Upgrade` |
| **End-to-end headers** | Forwarded by intermediaries to the final recipient. | Everything not listed in `Connection`. |

## Common Request Headers

| Header | Purpose |
|--------|---------|
| `Host` | **Required** in HTTP/1.1. Identifies the target host and port. Enables virtual hosting. |
| `Content-Type` | Media type of the request body (e.g., `application/json`, `multipart/form-data`). |
| `Content-Length` | Size of the request body in bytes. Must be an exact decimal integer. |
| `Transfer-Encoding` | Body encoding (e.g., `chunked`). Mutually exclusive with `Content-Length` in practice. |
| `Accept` | Media types the client can handle (e.g., `text/html, application/json`). |
| `Accept-Encoding` | Compression algorithms the client supports (e.g., `gzip, deflate, br`). |
| `Accept-Language` | Preferred natural languages (e.g., `en-US, pt;q=0.8`). |
| `Authorization` | Credentials for authenticating the client (e.g., `Bearer <token>`, `Basic <base64>`). |
| `User-Agent` | Identifies the client software and version. |
| `Connection` | Controls connection persistence (`keep-alive`, `close`) and lists hop-by-hop headers. |
| `Cookie` | Sends stored cookies to the server. |
| `If-None-Match` | Conditional request — send the resource only if the ETag doesn't match (for caching). |
| `If-Modified-Since` | Conditional request — send the resource only if modified after this timestamp. |
| `Expect` | Indicates expectations the server must meet (e.g., `100-continue`). |
| `Referer` | URL of the page that linked to the current request. |

## Common Response Headers

| Header | Purpose |
|--------|---------|
| `Content-Type` | Media type of the response body (e.g., `text/html; charset=utf-8`). |
| `Content-Length` | Size of the response body in bytes. |
| `Transfer-Encoding` | Body encoding applied to the response (e.g., `chunked`). |
| `Cache-Control` | Caching directives (e.g., `no-cache`, `max-age=3600`, `private`). |
| `ETag` | Opaque identifier for a specific version of the resource. Used for conditional requests. |
| `Last-Modified` | Timestamp of last modification. Used with `If-Modified-Since`. |
| `Set-Cookie` | Sends a cookie to the client for storage. |
| `Location` | URL to redirect to (used with 3xx and 201 status codes). |
| `Server` | Identifies the server software. |
| `WWW-Authenticate` | Defines the authentication scheme for 401 responses. |
| `Vary` | Lists request headers that affect the response (important for caching). |
| `Allow` | Lists permitted methods for the resource (required with 405 responses). |
| `Retry-After` | Suggests how long the client should wait before retrying (used with 429/503). |

## The Host Header

The `Host` header is the **only header that HTTP/1.1 requires** in every request. It was introduced to support **virtual hosting** — multiple websites served from the same IP address and port.

### Why It's Required

Before HTTP/1.1, each website needed its own IP address. The `Host` header allows a server to distinguish between `example.com` and `other.com` even when both resolve to the same IP. Without it, the server has no way to determine which virtual host the request is for.

### Rules

RFC 9112 §3.2 defines strict requirements:

- A client **MUST** send a `Host` header in every HTTP/1.1 request.
- A server **MUST** respond with **400 Bad Request** if:
  - The `Host` header is **missing**.
  - There are **multiple** `Host` headers.
  - The `Host` value is **invalid**.
- The `Host` value must match the URI authority (hostname and optional port).

```http
GET / HTTP/1.1
Host: example.com
```

```http
GET /api/data HTTP/1.1
Host: api.example.com:8443
```

### Host vs :authority

In HTTP/2 and HTTP/3, the `Host` header is replaced by the `:authority` pseudo-header in the request. However, `Host` is still sent for backward compatibility with intermediaries.
