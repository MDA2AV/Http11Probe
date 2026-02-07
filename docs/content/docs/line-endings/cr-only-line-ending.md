---
title: "CR-ONLY-LINE-ENDING"
description: "CR-ONLY-LINE-ENDING test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `RFC9112-3-CR-ONLY-LINE-ENDING` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | MUST |
| **Expected** | `400` |

## What it sends

A request where lines are terminated with `\r` (bare CR) instead of `\r\n` (CRLF).

## What the RFC says

> "A sender **MUST NOT** generate a bare CR (a CR character not immediately followed by LF) within any protocol elements other than the content. A recipient of such a bare CR **MUST** consider that element to be invalid or replace each bare CR with SP before processing the element or forwarding the message."

This is a MUST with two alternatives: consider the element invalid (reject), or replace each bare CR with a space and continue. Unlike bare LF, which is MAY-accept, bare CR has a mandatory handling requirement.

## Why it matters

Bare CR that is silently ignored creates a discrepancy between what different parsers see. If one parser treats CR as a line ending and another ignores it, the resulting disagreement can be exploited for smuggling.

## Sources

- [RFC 9112 Section 2.2 — Message Parsing](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
