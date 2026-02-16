---
title: Add with AI Agent
---

Use an AI coding agent (Claude Code, Cursor, Copilot, etc.) to add a new test or framework to Http11Probe. The repository includes a machine-readable contribution guide at [`AGENTS.md`](https://github.com/MDA2AV/Http11Probe/blob/main/AGENTS.md) designed specifically for LLM consumption.

## How to use it

Point your AI agent at the repository and reference the `AGENTS.md` file. It contains precise, unambiguous instructions for both tasks:

- **Task A** — Add a new test (4 steps: suite file, docs URL map, documentation page, category index card)
- **Task B** — Add a new framework (3 files: server implementation, Dockerfile, probe.json)

## Example prompts

### Adding a test

> Read AGENTS.md, then add a new compliance test that checks whether the server rejects requests with a space before the colon in a header field name. The RFC reference is RFC 9112 §5.1.

### Adding a framework

> Read AGENTS.md, then add a new Express.js server to the platform. Use Node 22 and make sure all five endpoints are implemented.

## What the agent will do

For a new **test**, the agent will:

1. Add a `yield return new TestCase { ... }` block to the correct suite file, including the correct `RfcLevel` (`Must`, `Should`, `May`, `OughtTo`, or `NotApplicable`)
2. Add a docs URL mapping entry (if the test is `COMP-*` or `RFC*` prefixed)
3. Create a documentation page under `docs/content/docs/{category}/`
4. Add a card to the category index page
5. Add a row to the RFC Requirement Dashboard

For a new **framework**, the agent will:

1. Create a server directory under `src/Servers/`
2. Implement the server with all required endpoints (GET, HEAD, POST, OPTIONS on `/`, GET/POST on `/echo`, and GET/POST on `/cookie`)
3. Write a Dockerfile that builds and runs the server on port 8080
4. Add a `probe.json` with the display name

## Tips

- The `AGENTS.md` file includes verification checklists — make sure the agent runs them before submitting
- No changes to CI workflows are needed for either task; tests and servers are auto-discovered
- For tests, the agent should check the RFC to determine the correct `RfcLevel` (MUST/SHOULD/MAY/"ought to"/N/A) and set it on the `TestCase`. The default is `Must` — only set explicitly for non-Must tests
- The agent should add a row to the [RFC Requirement Dashboard](docs/content/docs/rfc-requirement-dashboard.md) and update all counts
