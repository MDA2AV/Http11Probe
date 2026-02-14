# Http11Probe — AI Agent Contribution Guide

This file is designed for LLM/AI agent consumption. It contains precise, unambiguous instructions for adding a new test or a new framework to the Http11Probe platform.

## Project overview

Http11Probe is an HTTP/1.1 compliance and security tester. It sends raw TCP requests to servers and validates responses against RFC 9110/9112. The codebase is C# / .NET 10. Documentation is a Hugo + Hextra static site under `docs/`.

---

## TASK A: Add a new test

Adding a test requires changes to **5 locations** (sometimes 4 if URL mapping is automatic).

### Step 1 — Add the test case to the suite file

Choose the correct suite file based on category:

| Category | File path |
|----------|-----------|
| Compliance | `src/Http11Probe/TestCases/Suites/ComplianceSuite.cs` |
| Smuggling | `src/Http11Probe/TestCases/Suites/SmugglingSuite.cs` |
| Malformed Input | `src/Http11Probe/TestCases/Suites/MalformedInputSuite.cs` |
| Normalization | `src/Http11Probe/TestCases/Suites/NormalizationSuite.cs` |

Append a `yield return new TestCase { ... };` inside the `GetTestCases()` method. Here is the full schema:

```csharp
yield return new TestCase
{
    // REQUIRED fields
    Id = "COMP-EXAMPLE",                           // Unique ID. Prefix conventions below.
    Description = "What this test checks",         // One-line human description.
    Category = TestCategory.Compliance,            // Compliance | Smuggling | MalformedInput | Normalization
    PayloadFactory = ctx => MakeRequest(           // Builds the raw HTTP bytes to send.
        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"
    ),
    Expected = new ExpectedBehavior                // How to validate the response. See below.
    {
        ExpectedStatus = StatusCodeRange.Exact(400),
    },

    // OPTIONAL fields
    RfcReference = "RFC 9112 §5.1",                // Use § not "Section". Omit if no RFC applies.
    Scored = true,                                 // Default true. Set false for MAY/informational tests.
    AllowConnectionClose = false,                  // On Expected. See validation rules below.
    FollowUpPayloadFactory = ctx => ...,           // Second request on same connection (pipeline tests).
    RequiresConnectionReuse = false,               // True if FollowUpPayloadFactory needs same TCP connection.
    BehavioralAnalyzer = (response) => ...,        // Optional Func<HttpResponse?, string?> for analysis notes.
};
```

**Test ID prefix conventions:**

| Prefix | Suite |
|--------|-------|
| `COMP-` | Compliance |
| `SMUG-` | Smuggling |
| `MAL-` | Malformed Input |
| `NORM-` | Normalization |
| `RFC9112-X.X-` or `RFC9110-X.X-` | Compliance (maps directly to an RFC section) |

**Validation patterns — choose ONE:**

Pattern 1 — Exact status, no alternatives:
```csharp
Expected = new ExpectedBehavior
{
    ExpectedStatus = StatusCodeRange.Exact(400),
}
```
Use for strict MUST-400 requirements (e.g. SP-BEFORE-COLON, MISSING-HOST, DUPLICATE-HOST, OBS-FOLD, CR-ONLY).

Pattern 2 — Status with connection close as alternative:
```csharp
Expected = new ExpectedBehavior
{
    ExpectedStatus = StatusCodeRange.Exact(400),
    AllowConnectionClose = true,
}
```
Use when close is acceptable instead of a status code.

Pattern 3 — Custom validator (takes priority over ExpectedStatus):
```csharp
Expected = new ExpectedBehavior
{
    CustomValidator = (response, state) =>
    {
        if (state == ConnectionState.ClosedByServer && response is null) return TestVerdict.Pass;
        if (response is null) return TestVerdict.Fail;
        if (response.StatusCode == 400) return TestVerdict.Pass;
        if (response.StatusCode >= 200 && response.StatusCode < 300) return TestVerdict.Warn;
        return TestVerdict.Fail;
    },
    Description = "400 or close = pass, 2xx = warn",
}
```
Use for pass/warn/fail logic, timeout acceptance, or multi-outcome tests.

**Available StatusCodeRange factories:**
- `StatusCodeRange.Exact(int code)` — single status code
- `StatusCodeRange.Range(int start, int end)` — inclusive range
- `StatusCodeRange.Range2xx` — 200-299
- `StatusCodeRange.Range4xx` — 400-499
- `StatusCodeRange.Range4xxOr5xx` — 400-599

**Available TestVerdict values:** `Pass`, `Fail`, `Warn`, `Skip`, `Error`

**Available ConnectionState values:** `Open`, `ClosedByServer`, `TimedOut`, `Error`

**Helper method available in all suites:**
```csharp
private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
```

**Critical rules:**
- NEVER set `AllowConnectionClose = true` for MUST-400 requirements where the RFC explicitly says "respond with 400".
- Set `Scored = false` only for MAY-level or purely informational tests.
- Always use `ctx.HostHeader` (not a hardcoded host) in payloads.
- Tests are auto-discovered — no registration step needed. The `GetTestCases()` yield return is sufficient.

### Step 2 — Add docs URL mapping (conditional)

**File:** `src/Http11Probe.Cli/Reporting/DocsUrlMap.cs`

This step is **only needed** for `COMP-*` and `RFC*` prefixed tests. The following prefixes are auto-mapped:
- `SMUG-XYZ` → `smuggling/xyz` (lowercased)
- `MAL-XYZ` → `malformed-input/xyz` (lowercased)
- `NORM-XYZ` → `normalization/xyz` (lowercased)

For compliance tests, add an entry to the `ComplianceSlugs` dictionary:
```csharp
["COMP-EXAMPLE"] = "headers/example",
```

If the doc filename doesn't match the auto-mapping convention, add to `SpecialSlugs` instead:
```csharp
["MAL-CHUNK-EXT-64K"] = "malformed-input/chunk-extension-long",
```

### Step 3 — Create the documentation page

**File:** `docs/content/docs/{category-slug}/{test-slug}.md`

Category slug mapping:

| Category | Slug |
|----------|------|
| Compliance (line endings) | `line-endings` |
| Compliance (request line) | `request-line` |
| Compliance (headers) | `headers` |
| Compliance (host header) | `host-header` |
| Compliance (content-length) | `content-length` |
| Compliance (body) | `body` |
| Compliance (upgrade) | `upgrade` |
| Smuggling | `smuggling` |
| Malformed Input | `malformed-input` |
| Normalization | `normalization` |

Use this exact template:

```markdown
---
title: "EXAMPLE"
description: "EXAMPLE test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `COMP-EXAMPLE` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §X.X](https://www.rfc-editor.org/rfc/rfc9112#section-X.X) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A request with [description of the non-conforming element].

\```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
[malformed element]\r\n
\r\n
\```

## What the RFC says

> "Exact quote from the RFC with the MUST/SHOULD/MAY keyword." -- RFC 9112 Section X.X

Explanation of what the quote means for this test.

## Why it matters

Security and compatibility implications. Why this matters for real-world deployments.

## Sources

- [RFC 9112 §X.X](https://www.rfc-editor.org/rfc/rfc9112#section-X.X)
```

**Requirement field values:** `MUST`, `SHOULD`, `MAY`, `"ought to"`, `Implicit MUST (grammar violation)`, `Unscored`, or a descriptive phrase like `MUST reject or replace with SP`.

### Step 4 — Add a card to the category index

**File:** `docs/content/docs/{category-slug}/_index.md`

Find the `{{</* cards */>}}` block and add a new card entry. Place scored tests before unscored tests.

```
{{</* card link="example" title="EXAMPLE" subtitle="Short description of what the test checks." */>}}
```

The `link` value is the filename without `.md`.

### Step 5 — Add a row to the RFC Requirement Dashboard

**File:** `docs/content/docs/rfc-requirement-dashboard.md`

This page classifies every test by its RFC 2119 requirement level. You must:

1. **Add a row** to the correct table based on the test's requirement level:
   - `MUST` / `MUST NOT` → "MUST-Level Requirements" table (use the "Reject with 400" sub-table if the RFC explicitly mandates 400, otherwise the "Reject (400 or Connection Close Acceptable)" sub-table)
   - `SHOULD` / `SHOULD NOT` → "SHOULD-Level Requirements" table
   - `MAY` → "MAY-Level Requirements" table
   - `Scored = false` → "Unscored Tests" table (regardless of RFC keyword)

2. **Update the counts** in:
   - The summary table at the top (increment the matching requirement level)
   - The total test count in both the `description` frontmatter and the "Total: N tests" line
   - The "Requirement Level by Suite" section (increment the matching suite + level)
   - The "RFC Section Cross-Reference" table (increment existing section count or add a new row)

3. **Include** the test ID, suite name, RFC link, and an exact RFC quote with the keyword bolded (e.g., `**MUST**`).

### Verification checklist

After making all changes:

1. `dotnet build Http11Probe.slnx -c Release` — must compile without errors.
2. The new test ID appears in the output of `dotnet run --project src/Http11Probe.Cli -- --host localhost --port 8080`.
3. Hugo docs render: `cd docs && hugo server` — the new page is accessible and linked from its category index.

---

## TASK B: Add a new framework

Adding a framework requires creating **3 files** in a new directory, plus a **documentation page** on the website.

### Step 1 — Create the server directory

Create a new directory: `src/Servers/YourServer/`

### Step 2 — Implement the server

Your server MUST listen on **port 8080** and implement these endpoints:

| Endpoint | Method | Behavior |
|----------|--------|----------|
| `/` | `GET` | Return `200 OK` |
| `/` | `HEAD` | Return `200 OK` with no body |
| `/` | `POST` | Read the full request body and return it in the response body |
| `/` | `OPTIONS` | Return `200 OK` |
| `/echo` | `POST` | Return all received request headers in the response body, one per line as `Name: Value` |

The `/echo` endpoint is critical for normalization tests. It must echo back all headers the server received, preserving the names as the server internally represents them.

Example `/echo` response body:
```
Host: localhost:8080
Content-Length: 11
Content-Type: text/plain
```

### Step 3 — Add a Dockerfile

Create `src/Servers/YourServer/Dockerfile` that builds and runs the server.

Key requirements:
- The container runs with `--network host`, so bind to `0.0.0.0:8080`.
- Use `ENTRYPOINT` (not `CMD`) for the server process.
- The Dockerfile build context is the repository root, so paths like `COPY src/Servers/YourServer/...` are correct.

Example:
```dockerfile
FROM python:3.12-slim
WORKDIR /app
RUN pip install --no-cache-dir flask
COPY src/Servers/YourServer/app.py .
ENTRYPOINT ["python3", "app.py", "8080"]
```

### Step 4 — Add probe.json

Create `src/Servers/YourServer/probe.json` with exactly one field:

```json
{"name": "Your Server Display Name"}
```

This name appears in the leaderboard and PR comments.

### Step 5 — Create the server documentation page

**File:** `docs/content/servers/{server-name-lowercase}.md`

Use this template:

```markdown
---
title: "Server Name"
toc: false
breadcrumbs: false
---

**Language:** Language · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/YourServer)

## Dockerfile

\```dockerfile
[Complete Dockerfile contents]
\```

## Source — `filename.ext`

\```language
[Complete source file contents]
\```
```

Rules:
- Include one `## Source — \`filename\`` section per source file (exclude `probe.json`).
- Use the correct syntax-highlight language for each code block (e.g., `python`, `javascript`, `text`, `html`).
- The Dockerfile section always comes first, followed by source files.

### Verification checklist

1. Build the Docker image: `docker build -f src/Servers/YourServer/Dockerfile -t yourserver .`
2. Run: `docker run --network host yourserver`
3. Verify endpoints:
   - `curl http://localhost:8080/` returns 200
   - `curl -X POST -d "hello" http://localhost:8080/` returns "hello"
   - `curl -X POST -d "test" http://localhost:8080/echo` returns headers
4. Run the probe: `dotnet run --project src/Http11Probe.Cli -- --host localhost --port 8080`

No changes to CI workflows, configs, or other files are needed. The pipeline auto-discovers servers from `src/Servers/*/probe.json`.
