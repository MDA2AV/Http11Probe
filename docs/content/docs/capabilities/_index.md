---
title: Capabilities
description: "Capabilities — Http11Probe documentation"
weight: 12
sidebar:
  open: false
---

Capability tests probe optional HTTP features that servers may or may not implement. Unlike compliance tests, these are **unscored** — they map what each server supports rather than what it fails at.

## Scoring

All capability tests are **unscored**:

- **Pass** — Server correctly supports the feature
- **Warn** — Server does not support the feature (not a failure)
- **Fail** — Only for actual errors (unexpected status codes, connection errors)

## Conditional Requests (Caching)

These tests check whether the server supports ETag and Last-Modified based conditional requests (RFC 9110 §13).

{{< cards >}}
  {{< card link="etag-304" title="ETAG-304" subtitle="ETag conditional GET returns 304 Not Modified." >}}
  {{< card link="last-modified-304" title="LAST-MODIFIED-304" subtitle="Last-Modified conditional GET returns 304 Not Modified." >}}
  {{< card link="etag-in-304" title="ETAG-IN-304" subtitle="304 response includes ETag header." >}}
  {{< card link="inm-precedence" title="INM-PRECEDENCE" subtitle="If-None-Match takes precedence over If-Modified-Since." >}}
  {{< card link="inm-wildcard" title="INM-WILDCARD" subtitle="If-None-Match: * on existing resource returns 304." >}}
{{< /cards >}}

## Conditional Request Edge Cases

These tests probe how servers handle invalid or unusual conditional headers — future dates, garbage values, unquoted ETags, and weak comparison (RFC 9110 §13).

{{< cards >}}
  {{< card link="ims-future" title="IMS-FUTURE" subtitle="If-Modified-Since with future date ignored." >}}
  {{< card link="ims-invalid" title="IMS-INVALID" subtitle="If-Modified-Since with garbage date ignored." >}}
  {{< card link="inm-unquoted" title="INM-UNQUOTED" subtitle="If-None-Match with unquoted ETag." >}}
  {{< card link="etag-weak" title="ETAG-WEAK" subtitle="Weak ETag comparison for GET." >}}
{{< /cards >}}
