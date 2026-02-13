---
title: Message Syntax
description: "HTTP/1.1 request and response message structure, methods, and status codes."
weight: 2
---

This page covers the wire-level structure of HTTP/1.1 messages as defined by **RFC 9112** (HTTP/1.1 Message Syntax and Routing).

## General Message Format

Every HTTP/1.1 message — whether request or response — follows the same structure:

```
start-line CRLF
*( header-field CRLF )
CRLF
[ message-body ]
```

The start-line is either a **request-line** or a **status-line**. Headers follow as `field-name: field-value` pairs, each terminated by CRLF. An empty line (bare CRLF) separates headers from the optional body.

## Request Message

```
method SP request-target SP HTTP-version CRLF
*( field-name ":" OWS field-value OWS CRLF )
CRLF
[ message-body ]
```

Example — a `POST` with a JSON body:

```http
POST /api/users HTTP/1.1
Host: example.com
Content-Type: application/json
Content-Length: 27

{"name":"Alice","age":30}
```

Key rules (RFC 9112 §3):
- Exactly **one SP** (space, `0x20`) between method, request-target, and HTTP-version.
- The request-target is usually an absolute path (`/index.html`) or an asterisk (`*`) for `OPTIONS`.
- The HTTP-version **MUST** be `HTTP/1.1` (or `HTTP/1.0` for legacy).
- The request-line **MUST** end with CRLF. No extra whitespace, no trailing characters.

## Response Message

```
HTTP-version SP status-code SP [ reason-phrase ] CRLF
*( field-name ":" OWS field-value OWS CRLF )
CRLF
[ message-body ]
```

Example:

```http
HTTP/1.1 200 OK
Content-Type: text/html; charset=utf-8
Content-Length: 1234
Cache-Control: max-age=3600

<!DOCTYPE html>...
```

The reason-phrase (e.g., `OK`, `Not Found`) is purely informational — clients **MUST NOT** depend on its content. HTTP/2 and HTTP/3 removed it entirely.

## Methods

HTTP/1.1 defines a set of **request methods** that indicate the desired action on a resource:

| Method | Safe | Idempotent | Purpose |
|--------|------|------------|---------|
| `GET` | Yes | Yes | Retrieve a representation of the resource. |
| `HEAD` | Yes | Yes | Same as `GET` but without the response body. Used to check headers/existence. |
| `POST` | No | No | Submit data to the resource. Often creates a new sub-resource or triggers processing. |
| `PUT` | No | Yes | Replace the target resource entirely with the request payload. |
| `DELETE` | No | Yes | Remove the target resource. |
| `PATCH` | No | No | Apply a partial modification to the resource (RFC 5789). |
| `OPTIONS` | Yes | Yes | Describe the communication options for the target resource. Used in CORS preflight. |
| `TRACE` | Yes | Yes | Echo back the received request. Useful for debugging proxies. Often disabled for security. |
| `CONNECT` | No | No | Establish a tunnel to the server, typically for HTTPS through a proxy. |

### Safe vs Idempotent

- **Safe** methods do not modify server state. A `GET` request should never create, update, or delete a resource. Caches and prefetchers rely on this guarantee.
- **Idempotent** methods produce the same result whether called once or many times. `PUT /user/1` with the same body always results in the same state. `POST` is not idempotent — calling it twice might create two resources.

### Method Registration

Methods are maintained in the [IANA HTTP Method Registry](https://www.iana.org/assignments/http-methods/http-methods.xhtml). Servers that receive an unrecognized method SHOULD respond with `501 Not Implemented`. If the method is recognized but not allowed for the target resource, the server responds with `405 Method Not Allowed` and a required `Allow` header listing permitted methods.

## Status Codes

Responses carry a three-digit **status code** grouped into five classes:

| Range | Class | Meaning |
|-------|-------|---------|
| `1xx` | Informational | Request received, continuing process. |
| `2xx` | Successful | Request received, understood, and accepted. |
| `3xx` | Redirection | Further action needed to complete the request. |
| `4xx` | Client Error | Request contains bad syntax or cannot be fulfilled. |
| `5xx` | Server Error | Server failed to fulfill a valid request. |

### 1xx — Informational

| Code | Name | Usage |
|------|------|-------|
| `100` | Continue | Server has received the request headers and the client should proceed to send the body. Sent in response to `Expect: 100-continue`. |
| `101` | Switching Protocols | Server agrees to switch protocols via the `Upgrade` header (e.g., WebSocket). |

### 2xx — Successful

| Code | Name | Usage |
|------|------|-------|
| `200` | OK | Standard success response. Body contains the requested resource. |
| `201` | Created | Resource was successfully created. `Location` header points to the new resource. |
| `204` | No Content | Success, but no body to return (e.g., after a `DELETE`). |
| `206` | Partial Content | Range request fulfilled. Used for resumable downloads. |

### 3xx — Redirection

| Code | Name | Usage |
|------|------|-------|
| `301` | Moved Permanently | Resource has been permanently moved. Clients should update bookmarks. |
| `302` | Found | Temporary redirect. Original URL should still be used in the future. |
| `304` | Not Modified | Conditional request matched — the cached version is still valid. No body sent. |
| `307` | Temporary Redirect | Like 302, but the method and body MUST NOT change. |
| `308` | Permanent Redirect | Like 301, but the method and body MUST NOT change. |

### 4xx — Client Error

| Code | Name | Usage |
|------|------|-------|
| `400` | Bad Request | Malformed syntax. The server MUST return this for specific violations (missing Host, duplicate Host, space before colon, etc.). **This is what Http11Probe primarily tests.** |
| `401` | Unauthorized | Authentication required. Must include `WWW-Authenticate` header. |
| `403` | Forbidden | Server understood the request but refuses to fulfill it. |
| `404` | Not Found | Resource does not exist. |
| `405` | Method Not Allowed | Method is recognized but not supported for this resource. Must include `Allow` header. |
| `408` | Request Timeout | Server timed out waiting for the request. |
| `411` | Length Required | Server refuses the request without a `Content-Length`. |
| `413` | Content Too Large | Request body exceeds the server's limits. |
| `414` | URI Too Long | Request-target exceeds the server's limits. |
| `431` | Request Header Fields Too Large | Headers are too large. |

### 5xx — Server Error

| Code | Name | Usage |
|------|------|-------|
| `500` | Internal Server Error | Generic server failure. |
| `501` | Not Implemented | Server does not recognize the request method. |
| `502` | Bad Gateway | The server, acting as a gateway/proxy, received an invalid response from upstream. |
| `503` | Service Unavailable | Server is temporarily unable to handle the request (overloaded, maintenance). |
| `504` | Gateway Timeout | The server, acting as a gateway/proxy, did not receive a timely response from upstream. |
| `505` | HTTP Version Not Supported | The server does not support the HTTP version used in the request. |
