---
title: Body Handling
description: "Body Handling — Http11Probe documentation"
weight: 6
sidebar:
  open: false
---

HTTP/1.1 defines two mechanisms for framing a request body: **Content-Length** (fixed-size) and **Transfer-Encoding: chunked** (variable-size). RFC 9112 Sections 6 and 7 specify how servers must read, validate, and terminate body data.

## Key Rules

**Content-Length** — the sender declares the exact byte count:

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." — RFC 9112 Section 6.2

**Chunked encoding** — body is split into self-terminating chunks:

> "The chunked transfer coding wraps content in order to transfer it as a series of chunks, each with its own size indicator, followed by an OPTIONAL trailer section containing trailer fields." — RFC 9112 Section 7.1

**No CL, no TE** — with neither header, the body length is zero:

> "If this is a request message and none of the above are true, then the message body length is zero (no message body is present)." — RFC 9112 Section 6.3

## Tests

{{< cards >}}
  {{< card link="post-cl-body" title="POST-CL-BODY" subtitle="POST with Content-Length and matching body. Must return 2xx." >}}
  {{< card link="post-cl-zero" title="POST-CL-ZERO" subtitle="POST with Content-Length: 0, no body. Must return 2xx." >}}
  {{< card link="post-no-cl-no-te" title="POST-NO-CL-NO-TE" subtitle="POST with neither CL nor TE. Implicit zero-length body." >}}
  {{< card link="post-cl-undersend" title="POST-CL-UNDERSEND" subtitle="POST with CL:10 but only 5 bytes sent. Incomplete body." >}}
  {{< card link="get-with-cl-body" title="GET-WITH-CL-BODY" subtitle="GET with Content-Length and body. Unusual but allowed." >}}
  {{< card link="chunked-body" title="CHUNKED-BODY" subtitle="Valid single-chunk POST. Must return 2xx." >}}
  {{< card link="chunked-multi" title="CHUNKED-MULTI" subtitle="Valid multi-chunk POST. Must return 2xx." >}}
  {{< card link="chunked-empty" title="CHUNKED-EMPTY" subtitle="Zero-length chunked body. Must return 2xx." >}}
  {{< card link="chunked-no-final" title="CHUNKED-NO-FINAL" subtitle="Chunked body without zero terminator. Incomplete transfer." >}}
  {{< card link="chunked-extension" title="CHUNKED-EXTENSION" subtitle="Chunk extension (valid per RFC). Server may accept or reject." >}}
{{< /cards >}}
