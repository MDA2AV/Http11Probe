---
title: "BASELINE"
description: "BASELINE test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `COMP-BASELINE` |
| **Category** | Compliance |
| **Expected** | `2xx` |

## What it sends

A well-formed minimal HTTP/1.1 GET request.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## Why it matters

This is the sanity check for reachability and parser baseline. If this fails, later negative tests are not meaningful.
