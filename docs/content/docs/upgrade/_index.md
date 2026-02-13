---
title: Upgrade / WebSocket
description: "Upgrade / WebSocket — Http11Probe documentation"
weight: 12
sidebar:
  open: false
---

The HTTP `Upgrade` mechanism allows a client to request switching to a different protocol on the same connection. The most common use is the WebSocket handshake. RFC 9110 Section 7.8 and RFC 6455 define strict requirements for when a server may respond with `101 Switching Protocols`.

## Key Rules

**Connection header required** — a server must not switch protocols unless the request includes `Connection: Upgrade`:

> "A server MUST NOT switch to a protocol that was not indicated by the client in the corresponding request's Upgrade header field." — RFC 9110 Section 7.8

**Method** — RFC 6455 Section 4.1 requires the WebSocket opening handshake to use GET. A POST with Upgrade headers must not trigger a 101.

**Version negotiation** — if the server does not support the requested WebSocket version, it should respond with `426 Upgrade Required` and include `Sec-WebSocket-Version` listing supported versions.

## Tests

### Scored

{{< cards >}}
  {{< card link="upgrade-post" title="UPGRADE-POST" subtitle="WebSocket upgrade via POST must not return 101." >}}
  {{< card link="upgrade-missing-conn" title="UPGRADE-MISSING-CONN" subtitle="Upgrade without Connection: Upgrade must not switch." >}}
  {{< card link="upgrade-unknown" title="UPGRADE-UNKNOWN" subtitle="Upgrade to unknown protocol must not return 101." >}}
{{< /cards >}}

### Unscored

{{< cards >}}
  {{< card link="upgrade-invalid-ver" title="UPGRADE-INVALID-VER" subtitle="WebSocket upgrade with unsupported version. Ideally 426." >}}
{{< /cards >}}
