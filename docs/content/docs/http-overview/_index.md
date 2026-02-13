---
title: Understanding HTTP
description: "What HTTP is, how HTTP/1.1 works in depth, its history from 0.9 to 3, and alternatives."
weight: 1
sidebar:
  open: false
---

A comprehensive guide to HTTP — what it is, why it was designed the way it was, and how HTTP/1.1 works at the wire level. Start here before diving into the individual test categories.

{{< cards >}}
  {{< card link="what-is-http" title="What is HTTP?" subtitle="Application-layer request/response protocol, client-server model, stateless design, and core design goals." icon="question-mark-circle" >}}
  {{< card link="message-syntax" title="Message Syntax" subtitle="Request and response structure, methods (GET, POST, PUT...), status codes (1xx–5xx), and the request-line grammar." icon="code" >}}
  {{< card link="headers" title="Headers" subtitle="Header structure, common request and response headers, the Host header, and why it's the only required header." icon="document-text" >}}
  {{< card link="connections" title="Connections" subtitle="Persistent connections, keep-alive, pipelining, head-of-line blocking, Upgrade, and 100 Continue." icon="switch-horizontal" >}}
  {{< card link="body-and-framing" title="Body and Framing" subtitle="Content-Length, chunked transfer encoding, trailers, and why CL+TE conflicts cause request smuggling." icon="document-download" >}}
  {{< card link="caching-and-negotiation" title="Caching and Negotiation" subtitle="Content negotiation with Accept headers, Cache-Control, ETags, conditional requests, and Vary." icon="refresh" >}}
  {{< card link="history-and-future" title="History and Future" subtitle="HTTP/0.9 to HTTP/3, the current IETF work, alternatives to HTTP, and learning resources." icon="clock" >}}
{{< /cards >}}
