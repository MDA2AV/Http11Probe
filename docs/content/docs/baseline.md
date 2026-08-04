---
title: "Baseline Reachability Test"
description: "The COMP-BASELINE sanity check that confirms a target HTTP/1.1 server is reachable and parses well-formed requests before running negative tests."
weight: 4
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
