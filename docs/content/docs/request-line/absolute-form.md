---
title: "ABSOLUTE-FORM"
description: "ABSOLUTE-FORM test documentation"
weight: 9
---

| | |
|---|---|
| **Test ID** | `COMP-ABSOLUTE-FORM` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.2) |
| **Requirement** | MUST accept (server) |
| **Expected** | `2xx` = Pass, `400` = Warn (unscored) |

## What it sends

`GET http://host/ HTTP/1.1` — the absolute-form request-target.

```http
GET http://localhost:8080/ HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```


## What the RFC says

> "When making a request to a proxy, other than a CONNECT or server-wide OPTIONS request, a client MUST send the target URI in 'absolute-form' as the request-target." — RFC 9112 Section 3.2.2

> "A server MUST accept the absolute-form in requests even though most HTTP/1.1 clients will only send the absolute-form to a proxy." — RFC 9112 Section 3.2.2

## Why this test is unscored

Although the RFC says servers MUST accept absolute-form, in practice most non-proxy origin servers reject it. Both `400` and `2xx` are observed in the wild, and rejecting absolute-form on an origin server is a common and generally harmless deviation. Since neither behavior is clearly wrong from a practical standpoint, this test records the response without scoring it.

## Why it matters

**Pass:** Server accepts with `2xx` (RFC-compliant).
**Warn:** Server rejects with `400` (common in practice, but non-compliant with MUST accept).

## Deep Analysis

### Relevant ABNF Grammar

```
request-line   = method SP request-target SP HTTP-version
request-target = origin-form / absolute-form / authority-form / asterisk-form
absolute-form  = absolute-URI
```

The `absolute-form` production requires a complete `absolute-URI` as defined in RFC 3986. This is the full URI including scheme, authority, path, and optional query -- for example `http://host/path?query`.

### RFC Evidence

**RFC 9112 Section 3.2.2** mandates server acceptance:

> "A server MUST accept the absolute-form in requests even though most HTTP/1.1 clients will only send the absolute-form to a proxy." — RFC 9112 Section 3.2.2

**RFC 9112 Section 3.2.2** describes the client-side usage:

> "When making a request to a proxy, other than a CONNECT or server-wide OPTIONS request, a client MUST send the target URI in absolute-form as the request-target." -- RFC 9112 Section 3.2.2

**RFC 9112 Section 3.2.2** specifies Host header override behavior:

> "When an origin server receives a request with an absolute-form of request-target, the origin server MUST ignore the received Host header field (if any) and instead use the host information of the request-target." -- RFC 9112 Section 3.2.2

### Chain of Reasoning

1. The `absolute-form` production requires the full `absolute-URI` as the request-target, e.g., `GET http://host/ HTTP/1.1`.
2. Historically, only proxy-targeted requests used absolute-form. Clients sending directly to origin servers used origin-form (`/path`).
3. Despite this convention, the RFC contains a clear MUST: servers MUST accept absolute-form even from direct clients. This ensures interoperability across the request chain.
4. When absolute-form is received, the server MUST use the host from the request-target and ignore the Host header, which prevents host confusion attacks when both are present.
5. A server that rejects absolute-form with `400` is technically non-compliant with the MUST, but since most origin servers are not proxies, rejecting it is a common and pragmatically harmless behavior.

### Scoring Justification

**Unscored.** RFC 9112 uses a server-side MUST to accept absolute-form. In practice, many origin stacks still reject it. To preserve interoperability visibility without hard-failing broad classes of servers, this test is unscored: `2xx` is Pass and `400` is Warn.

### Edge Cases

- **Mismatched Host and request-target authority:** If the absolute-form says `http://a.com/` but the Host header says `b.com`, the server MUST use `a.com` per the RFC. Servers that do not implement this override may route to the wrong virtual host.
- **Scheme mismatch:** A request arriving on HTTPS with `http://` in the absolute-form creates ambiguity about the intended scheme. The RFC does not address this directly.
- **Missing path:** `GET http://host HTTP/1.1` (no trailing slash) is a valid absolute-URI. The server should treat the empty path as `/`.

## Sources

- [RFC 9112 Section 3.2.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.2)
