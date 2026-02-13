---
title: Caching and Negotiation
description: "Content negotiation with Accept headers, Cache-Control, ETags, conditional requests, and Vary."
weight: 6
---

HTTP/1.1 includes built-in mechanisms for content negotiation and caching. These features reduce bandwidth, latency, and server load without requiring application-level changes.

## Content Negotiation

Content negotiation lets the client and server agree on the best **representation** of a resource. A single URL can serve different formats, languages, or encodings depending on the client's capabilities and preferences.

### Proactive (Server-Driven) Negotiation

The client sends preferences in `Accept*` headers, and the server chooses the best match:

```http
GET /document HTTP/1.1
Host: example.com
Accept: text/html, application/json;q=0.9
Accept-Language: en-US, pt;q=0.8
Accept-Encoding: gzip, br
```

#### Quality Values

The `q` parameter (quality value, 0.000–1.000) indicates preference weight:

- `text/html` — no `q` value means `q=1.0` (highest preference).
- `application/json;q=0.9` — acceptable, but HTML is preferred.
- `pt;q=0.8` — Portuguese is acceptable, but English is preferred.

The server picks the best match and indicates what it chose via `Content-Type`, `Content-Language`, and `Content-Encoding` response headers.

#### Accept Header Negotiation

| Accept Header | What It Negotiates |
|---------------|-------------------|
| `Accept` | Media type (e.g., `text/html`, `application/json`, `image/webp`). |
| `Accept-Language` | Natural language (e.g., `en-US`, `pt-BR`, `ja`). |
| `Accept-Encoding` | Compression algorithm (e.g., `gzip`, `deflate`, `br`, `zstd`). |
| `Accept-Charset` | Character encoding (largely obsolete — UTF-8 is near-universal). |

#### Wildcard Matching

- `*/*` — accept any media type.
- `text/*` — accept any text subtype.
- `*` in `Accept-Encoding` — accept any encoding.

### Reactive (Agent-Driven) Negotiation

Instead of guessing, the server tells the client what's available:

- **`300 Multiple Choices`** — the server lists available representations and the client picks one.
- **`406 Not Acceptable`** — no representation matches the client's preferences.

Reactive negotiation is less common because it requires an extra round-trip.

## Caching

HTTP/1.1 has a sophisticated caching model defined in RFC 9111. Caches can exist at multiple layers:

- **Browser cache** — private, per-user cache in the client.
- **Proxy cache** — shared cache at a forward proxy or CDN edge node.
- **Gateway/reverse-proxy cache** — shared cache at the origin's front door (e.g., Varnish, Nginx).

### Cache-Control

The `Cache-Control` header is the primary mechanism for controlling caching behavior:

#### Request Directives

| Directive | Meaning |
|-----------|---------|
| `no-cache` | The cache must revalidate with the origin before using a stored response. |
| `no-store` | The cache MUST NOT store any part of the request or response. |
| `max-age=N` | Accept a cached response that is at most N seconds old. |
| `max-stale[=N]` | Accept a response that has been stale for up to N seconds. |
| `min-fresh=N` | Require the response to be fresh for at least N more seconds. |
| `only-if-cached` | Only return a cached response; don't contact the origin. Return `504` if nothing is cached. |

#### Response Directives

| Directive | Meaning |
|-----------|---------|
| `max-age=N` | The response is fresh for N seconds from the time it was generated. |
| `s-maxage=N` | Like `max-age`, but only applies to shared caches (CDNs, proxies). Overrides `max-age`. |
| `no-cache` | The response may be stored but MUST be revalidated before each use. |
| `no-store` | The response MUST NOT be stored by any cache. |
| `private` | The response is intended for a single user. Shared caches MUST NOT store it. |
| `public` | The response may be stored by any cache, even if it would normally be non-cacheable. |
| `must-revalidate` | Once stale, the cache MUST revalidate before using. MUST NOT serve stale on error. |
| `immutable` | The response body will not change. Prevents revalidation even on user refresh. |
| `stale-while-revalidate=N` | Serve stale for up to N seconds while revalidating in the background. |

### Conditional Requests

Conditional requests let a cache check whether its stored response is still valid without downloading the full body again.

#### ETag / If-None-Match

1. Server sends a response with an `ETag`:

```http
HTTP/1.1 200 OK
ETag: "abc123"
Content-Length: 5000

...body...
```

2. Client stores the response. On the next request, it sends the ETag back:

```http
GET /resource HTTP/1.1
Host: example.com
If-None-Match: "abc123"
```

3. If the resource hasn't changed, the server responds with no body:

```http
HTTP/1.1 304 Not Modified
ETag: "abc123"
```

ETags can be **strong** (`"abc123"`) or **weak** (`W/"abc123"`). Strong ETags guarantee byte-for-byte identity. Weak ETags indicate semantic equivalence — the content is "close enough" that a cached version is acceptable.

#### Last-Modified / If-Modified-Since

A timestamp-based alternative to ETags:

1. Server sends `Last-Modified`:

```http
HTTP/1.1 200 OK
Last-Modified: Wed, 21 Oct 2024 07:28:00 GMT
```

2. Client sends `If-Modified-Since`:

```http
GET /resource HTTP/1.1
If-Modified-Since: Wed, 21 Oct 2024 07:28:00 GMT
```

3. If unmodified, the server responds with `304 Not Modified`.

ETags are more precise (a resource can change and change back within the same second), but `Last-Modified` is simpler and works well for static files.

### Vary

The `Vary` header tells caches which **request headers** affect the response. Without `Vary`, a cache might serve a gzip-compressed response to a client that doesn't support gzip.

```http
HTTP/1.1 200 OK
Content-Encoding: gzip
Vary: Accept-Encoding
```

This tells caches: "the response depends on the `Accept-Encoding` request header." The cache must store separate copies for each unique `Accept-Encoding` value.

Common `Vary` values:
- `Vary: Accept-Encoding` — different compression levels.
- `Vary: Accept-Language` — different language versions.
- `Vary: Accept` — different media types (HTML vs JSON).
- `Vary: Cookie` — personalized content (effectively disables shared caching).
- `Vary: *` — every request is unique; never serve from cache.

### Age

The `Age` header indicates how many seconds a response has been in a cache:

```http
HTTP/1.1 200 OK
Cache-Control: max-age=3600
Age: 600
```

This response has been cached for 600 seconds and has 3000 seconds of freshness remaining.

### Caching Flow Summary

```
Client sends request
  ↓
Cache checks for stored response
  ├── No stored response → forward to origin → store response → return
  ├── Fresh stored response → return immediately (Age incremented)
  └── Stale stored response
        ├── must-revalidate → conditional request to origin
        │     ├── 304 → update freshness, return stored response
        │     └── 200 → store new response, return
        └── stale-while-revalidate → return stale, revalidate in background
```
