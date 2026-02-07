---
title: "EMPTY-REQUEST"
description: "EMPTY-REQUEST test documentation"
weight: 13
---

| | |
|---|---|
| **Test ID** | `MAL-EMPTY-REQUEST` |
| **Category** | Malformed Input |
| **Expected** | `400`, close, or timeout |

## What it sends

Zero bytes -- the TCP connection is established and then closed without sending any data.

## Why timeout is acceptable

The server has no indication that a request was even attempted.

## Sources

- No specific RFC section -- this is a robustness test
