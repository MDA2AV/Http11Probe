---
title: What is HTTP?
description: "What HTTP is, its core characteristics, and the design goals behind the protocol."
weight: 1
---

## Overview

HTTP (HyperText Transfer Protocol) is an **application-layer, request/response protocol** for exchanging data between clients and servers. A client — a web browser, CLI tool, mobile app, or another service — sends a request message, and the server returns a response message.

## Core Characteristics

- **Client-server model** — one side initiates (the client), the other responds (the server). Roles are fixed for a given exchange. The client is always the party that opens the connection and sends the first message.
- **Stateless** — each request is independent. The server retains no memory of previous requests unless the application layer (cookies, sessions, tokens) adds state. This simplifies server implementation and enables horizontal scaling.
- **Text-based wire format (in HTTP/1.1)** — request lines, headers, and status lines are human-readable ASCII terminated by CRLF (`\r\n`). This makes the protocol easy to inspect and debug with tools like `curl`, `telnet`, or `netcat`.
- **Layered over a reliable transport** — HTTP/1.1 requires an ordered, reliable byte stream, almost always TCP. TLS may be layered between TCP and HTTP to provide encryption (HTTPS).

## Design Goals

HTTP was designed as a **universal interface for web resources**:

- **Human-readable messages** — developers can craft and read raw requests by hand, making debugging straightforward. You can literally `telnet` to a server and type a valid request.
- **Extensibility via headers** — new capabilities (authentication, caching, content negotiation, security policies) are added through headers without changing the core protocol grammar. This is how HTTP has evolved for over 30 years without breaking backward compatibility.
- **Content negotiation** — clients express preferences for language (`Accept-Language`), encoding (`Accept-Encoding`), and media type (`Accept`), and servers select the best matching representation. A single URL can serve HTML to a browser and JSON to an API client.
- **Support for intermediaries** — proxies, caches, CDNs, gateways, and load balancers can inspect, transform, cache, and forward messages because the format is well-defined and semantically layered. The protocol was explicitly designed with intermediaries in mind.
- **Method semantics** — standardized methods (`GET`, `POST`, `PUT`, `DELETE`, etc.) give shared meaning to operations, enabling generic tooling and middleware. A cache knows `GET` is safe to cache; a proxy knows `CONNECT` means tunnel.
- **Resource-oriented** — every interaction targets a **resource** identified by a URI. This abstraction decouples the client from server implementation details — the resource might be a file, a database row, a computed result, or a proxy to another service.
