---
title: Header Normalization
description: "Echo-based tests checking whether servers normalize malformed header names — underscore-to-hyphen, tab in name, or case folding on Transfer-Encoding."
weight: 8
sidebar:
  open: false
---

Header normalization tests examine how servers transform malformed header names when they accept them rather than rejecting. A server that silently converts `Content_Length` to `Content-Length` creates a smuggling vector: an upstream proxy might pass the underscore form through without acting on it, while the back-end treats it as a real Content-Length.

## How the Echo Endpoint Works

Each normalization test sends a `POST /echo` request with a valid `Content-Length` for body framing, plus an additional malformed header. The `/echo` endpoint reflects all received headers back in the response body, one per line:

```
Host: localhost:8080
Content-Length: 11
Content_Length: 99
```

Http11Probe then parses the echo response to determine what happened to the malformed header name.

## Verdict Logic

| Echo Result | Verdict | Meaning |
|---|---|---|
| Standard header name with probe value | **Fail** | Server normalized the name (smuggling risk) |
| Original malformed name with probe value | **Warn** | Server preserved the name (mild proxy-chain risk) |
| Neither found | **Pass** | Server dropped or rejected the header |
| 400 / 4xx / 5xx | **Pass** | Server rejected the request |
| Connection closed | **Pass** | Server refused the connection |

## Tests

### Scored

{{< cards >}}
  {{< card link="underscore-cl" title="UNDERSCORE-CL" subtitle="Content_Length with underscore instead of hyphen." >}}
  {{< card link="sp-before-colon-cl" title="SP-BEFORE-COLON-CL" subtitle="Space before colon in Content-Length header." >}}
  {{< card link="tab-in-name" title="TAB-IN-NAME" subtitle="Tab character embedded in header name." >}}
  {{< card link="underscore-te" title="UNDERSCORE-TE" subtitle="Transfer_Encoding with underscore instead of hyphen." >}}
{{< /cards >}}

### Unscored

{{< cards >}}
  {{< card link="case-te" title="CASE-TE" subtitle="All-uppercase TRANSFER-ENCODING — case normalization check." >}}
{{< /cards >}}
