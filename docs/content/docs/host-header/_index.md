---
title: Host Header
weight: 5
sidebar:
  open: true
---

The Host header is the only header where RFC 9112 **explicitly mandates a 400 response** for violations. This makes the Host header tests the strictest in the entire suite — close or timeout is NOT acceptable.

## The Rule

> "A server **MUST** respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." — RFC 9112 Section 3.2

This single sentence covers three violations:
1. Missing Host header
2. More than one Host header line (duplicate)
3. Host header with an invalid field value

## Tests

{{< cards >}}
  {{< card link="missing-host" title="MISSING-HOST" subtitle="No Host header present. MUST respond with 400." >}}
  {{< card link="duplicate-host" title="DUPLICATE-HOST" subtitle="Two Host headers with different values. MUST respond with 400." >}}
{{< /cards >}}
