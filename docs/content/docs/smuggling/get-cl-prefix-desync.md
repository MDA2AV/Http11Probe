---
title: "GET-CL-PREFIX-DESYNC"
description: "GET-CL-PREFIX-DESYNC sequence test documentation"
weight: 62
---

| | |
|---|---|
| **Test ID** | `SMUG-GET-CL-PREFIX-DESYNC` |
| **Category** | Smuggling |
| **Type** | Sequence (2 steps) |
| **Scored** | No |
| **RFC** | [RFC 9110 §9.3.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.1) |
| **RFC Level** | MAY |
| **Expected** | `400/close` preferred; extra response on step 2 = warn |

## What it does

Step 1 sends a `GET` with a `Content-Length` body containing an **incomplete** request prefix (it intentionally omits the blank line that ends the header section). Step 2 begins with a blank line to complete that prefix and then sends a normal `GET`.

If the server fails to fully consume the GET body from step 1, the prefix can remain on the connection. Step 2 can then "complete" it, causing the leftover bytes to be interpreted as a real request.

## What it sends

Step 1:

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 31\r\n
\r\n
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
```

Step 2:

```http
\r\n
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

(Actual `Content-Length` is computed to match the prefix bytes.)

## Why it matters

RFC 9110 notes that content in a GET request "might lead some implementations to reject the request and close the connection because of its potential as a request smuggling attack." Even if a server chooses to accept such a request, it must ensure it stays synchronized by consuming or discarding the body bytes.

This test is unscored because GET-with-body handling is not uniformly defined across deployments, but a desync signal is still valuable telemetry.

## Verdicts

- **Pass**: The server rejects step 1 with `400`, or closes the connection.
- **Warn**: Step 2 yields multiple HTTP status lines (leftover prefix likely executed), or other evidence of desynchronization.

## Sources

- [RFC 9110 §9.3.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.1)
