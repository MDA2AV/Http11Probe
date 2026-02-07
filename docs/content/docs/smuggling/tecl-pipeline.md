---
title: "TECL-PIPELINE"
description: "TECL-PIPELINE test documentation"
weight: 9
---

| | |
|---|---|
| **Test ID** | `SMUG-TECL-PIPELINE` |
| **Category** | Smuggling |
| **Expected** | `400` or close |

## What it sends

A full TE.CL smuggling payload — the reverse of CLTE. The front-end uses Transfer-Encoding and the body is crafted so the back-end (using Content-Length) sees a smuggled request.

## Why it matters

The TE.CL variant is equally dangerous to CL.TE. Together, they cover both possible orderings of front-end/back-end preference.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [PortSwigger — HTTP Request Smuggling](https://portswigger.net/web-security/request-smuggling)
