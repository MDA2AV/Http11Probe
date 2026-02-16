---
title: Cookies
description: "Cookies — Http11Probe documentation"
weight: 13
sidebar:
  open: false
---

Cookie parsing is handled by framework-level parsers that run automatically on every request. Malformed `Cookie` headers can crash these parsers, cause memory issues, or produce mangled values. These tests check whether servers and frameworks survive adversarial cookie input.

Cookies are defined by [RFC 6265](https://www.rfc-editor.org/rfc/rfc6265) (not RFC 9110/9112), so all tests are **unscored**.

## Scoring

All cookie tests are **unscored**:

- **Pass** — Server handled the cookie input safely
- **Warn** — Endpoint not available or non-ideal but non-dangerous behavior
- **Fail** — Server crashed (500), preserved dangerous bytes, or lost data it should have parsed

## Echo-Based Tests

These tests target `/echo` and work on all servers. They check whether the server survives adversarial cookie headers without crashing.

{{< cards >}}
  {{< card link="echo" title="ECHO" subtitle="Basic Cookie header echoed back." >}}
  {{< card link="oversized" title="OVERSIZED" subtitle="64KB Cookie header." >}}
  {{< card link="empty" title="EMPTY" subtitle="Empty Cookie header value." >}}
  {{< card link="nul" title="NUL" subtitle="NUL byte in cookie value." >}}
  {{< card link="control-chars" title="CONTROL-CHARS" subtitle="Control characters in cookie value." >}}
  {{< card link="many-pairs" title="MANY-PAIRS" subtitle="1000 cookie key=value pairs." >}}
  {{< card link="malformed" title="MALFORMED" subtitle="Completely malformed cookie syntax." >}}
  {{< card link="multi-header" title="MULTI-HEADER" subtitle="Two separate Cookie headers." >}}
{{< /cards >}}

## Parsed-Cookie Tests

These tests target `/cookie` and check whether the framework's cookie parser correctly extracts key=value pairs. Servers without a `/cookie` endpoint return 404 (Warn).

{{< cards >}}
  {{< card link="parsed-basic" title="PARSED-BASIC" subtitle="Single foo=bar cookie parsed." >}}
  {{< card link="parsed-multi" title="PARSED-MULTI" subtitle="Three cookies parsed from one header." >}}
  {{< card link="parsed-empty-val" title="PARSED-EMPTY-VAL" subtitle="Cookie with empty value." >}}
  {{< card link="parsed-special" title="PARSED-SPECIAL" subtitle="Spaces and = in cookie values." >}}
{{< /cards >}}
