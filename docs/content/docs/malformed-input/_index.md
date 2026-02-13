---
title: Malformed Input
description: "Malformed Input — Http11Probe documentation"
weight: 11
sidebar:
  open: false
---

These tests send pathological, oversized, or completely invalid payloads. The goal is not RFC compliance (there's no RFC section for "what to do with binary garbage") — it's robustness. A well-implemented server should reject gracefully, not crash, hang, or consume unbounded resources.

## Expected Behavior

- **Binary garbage / empty / incomplete**: `400`, close, or timeout — the server may not even recognize a request was attempted
- **Oversized fields**: `400`, `414 URI Too Long`, `431 Request Header Fields Too Large`, or close
- **Invalid bytes (NUL, control chars, non-ASCII)**: `400` or close
- **Integer overflow**: `400` or close

## Tests

{{< cards >}}
  {{< card link="binary-garbage" title="BINARY-GARBAGE" subtitle="Random non-HTTP bytes." >}}
  {{< card link="long-url" title="LONG-URL" subtitle="100 KB URL." >}}
  {{< card link="long-header-name" title="LONG-HEADER-NAME" subtitle="100 KB header name." >}}
  {{< card link="long-header-value" title="LONG-HEADER-VALUE" subtitle="100 KB header value." >}}
  {{< card link="long-method" title="LONG-METHOD" subtitle="100 KB method name." >}}
  {{< card link="many-headers" title="MANY-HEADERS" subtitle="10,000 headers." >}}
  {{< card link="nul-in-url" title="NUL-IN-URL" subtitle="NUL byte in request target." >}}
  {{< card link="control-chars-header" title="CONTROL-CHARS-HEADER" subtitle="Control characters in header value." >}}
  {{< card link="non-ascii-header-name" title="NON-ASCII-HEADER-NAME" subtitle="Non-ASCII bytes in header name." >}}
  {{< card link="non-ascii-url" title="NON-ASCII-URL" subtitle="Non-ASCII bytes in URL." >}}
  {{< card link="cl-overflow" title="CL-OVERFLOW" subtitle="Content-Length exceeding 64-bit range." >}}
  {{< card link="incomplete-request" title="INCOMPLETE-REQUEST" subtitle="Partial HTTP request." >}}
  {{< card link="empty-request" title="EMPTY-REQUEST" subtitle="Zero bytes sent." >}}
  {{< card link="whitespace-only-line" title="WHITESPACE-ONLY-LINE" subtitle="Only spaces/tabs, no method or URI." >}}
  {{< card link="nul-in-header-value" title="NUL-IN-HEADER-VALUE" subtitle="NUL byte in header value." >}}
  {{< card link="chunk-size-overflow" title="CHUNK-SIZE-OVERFLOW" subtitle="Chunk size integer overflow." >}}
  {{< card link="h2-preface" title="H2-PREFACE" subtitle="HTTP/2 preface sent to HTTP/1.1 server." >}}
  {{< card link="chunk-extension-long" title="CHUNK-EXTENSION-LONG" subtitle="100KB chunk extension value." >}}
  {{< card link="cl-empty" title="CL-EMPTY" subtitle="Empty Content-Length value." >}}
{{< /cards >}}

### Unscored

{{< cards >}}
  {{< card link="cl-tab-before-value" title="CL-TAB-BEFORE-VALUE" subtitle="Tab as OWS before Content-Length value." >}}
{{< /cards >}}
