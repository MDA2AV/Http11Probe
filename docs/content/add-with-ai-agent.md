---
title: Add with AI Agent
description: "Use an AI coding agent to add a new compliance test or HTTP server framework to Http11Probe, following the Add a Test and Add a Framework guides."
---

Use an AI coding agent (Claude Code, Cursor, Copilot, etc.) to add a new test or framework to Http11Probe. Both contribution guides are written to be followed step by step, so an agent can work straight from them:

- [Add a Test](/add-a-test.html) — suite file, docs URL map, documentation page, category index card, dashboard row
- [Add a Framework](/add-a-framework.html) — server implementation, Dockerfile, `probe.json`, server page

## How to use it

Point your agent at the repository and at the guide for the task. Give it the specific behaviour you want covered, or the framework and runtime version you want added, and let it work through the steps.

## Example prompts

### Adding a test

> Follow the Add a Test guide at https://www.http-probe.com/add-a-test.html, then add a new compliance test that checks whether the server rejects requests with a space before the colon in a header field name. The RFC reference is RFC 9112 §5.1.

### Adding a framework

> Follow the Add a Framework guide at https://www.http-probe.com/add-a-framework.html, then add a new Express.js server to the platform. Use Node 22 and make sure all three endpoints are implemented.

## What the agent will do

For a new **test**, the agent will:

1. Add a `yield return new TestCase { ... }` block to the correct suite file, including the correct `RfcLevel` (`Must`, `Should`, `May`, `OughtTo`, or `NotApplicable`)
2. Add a docs URL mapping entry (if the test is `COMP-*` or `RFC*` prefixed)
3. Create a documentation page under `docs/content/docs/{category}/`
4. Add a card to the category index page
5. Add a row to the RFC Requirement Dashboard

For a new **framework**, the agent will:

1. Create a server directory under `src/Servers/`
2. Implement the three required endpoints (`/`, `/echo`, `/cookie`)
3. Write a Dockerfile that builds and runs the server on port 8080
4. Add a `probe.json` with the display name and language
5. Add a server documentation page under `docs/content/servers/`

## Tips

- Both guides have a verification step — make sure the agent runs it before submitting
- No changes to CI workflows are needed for either task; tests and servers are auto-discovered
- For tests, the agent should check the RFC to determine the correct `RfcLevel` (MUST/SHOULD/MAY/"ought to"/N/A) and set it on the `TestCase`. The default is `Must` — only set explicitly for non-Must tests
- The agent should add a row to the [RFC Requirement Dashboard](/docs/rfc-requirement-dashboard.html) and update all counts
