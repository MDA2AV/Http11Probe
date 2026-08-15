---
title: Add a Test
description: "How to add a new HTTP/1.1 compliance, smuggling, or malformed-input test case to Http11Probe, including the test case definition, documentation page, and category index entry."
---

A step-by-step guide to adding a new test to Http11Probe. Every test touches five places: the suite file, the docs URL map (sometimes), a documentation page, the category index, and the RFC Requirement Dashboard.

## 1. Define the test case

Pick the suite that matches your test's category and add a `yield return new TestCase` block.

| Category | File |
|----------|------|
| Compliance | `src/Http11Probe/TestCases/Suites/ComplianceSuite.cs` |
| Smuggling | `src/Http11Probe/TestCases/Suites/SmugglingSuite.cs` |
| Malformed Input | `src/Http11Probe/TestCases/Suites/MalformedInputSuite.cs` |
| Normalization | `src/Http11Probe/TestCases/Suites/NormalizationSuite.cs` |
| Cookies | `src/Http11Probe/TestCases/Suites/CookieSuite.cs` |
| WebSockets | `src/Http11Probe/TestCases/Suites/WebSocketsSuite.cs` |
| Capabilities | `src/Http11Probe/TestCases/Suites/CapabilitiesSuite.cs` |

`CapabilitiesSuite` holds multi-step sequence tests and yields `SequenceTestCase` instead of `TestCase`; follow the existing entries in that file if your test needs several requests on one connection.

```csharp
yield return new TestCase
{
    Id = "COMP-MY-TEST",
    Description = "Description of what the test checks",
    Category = TestCategory.Compliance,
    RfcLevel = RfcLevel.Must,              // Must (default) | Should | May | OughtTo | NotApplicable
    RfcReference = "RFC 9112 §X.X",

    PayloadFactory = ctx => MakeRequest(
        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"
    ),

    Expected = new ExpectedBehavior
    {
        ExpectedStatus = StatusCodeRange.Exact(400),
        AllowConnectionClose = true,
    },

    Scored = true,
};
```

### Test ID naming

| Prefix | Suite |
|--------|-------|
| `COMP-` | Compliance |
| `SMUG-` | Smuggling |
| `MAL-` | Malformed Input |
| `NORM-` | Normalization |
| `COOK-` | Cookies |
| `WS-` | WebSockets |
| `CAP-` | Capabilities |
| `RFC9112-...` or `RFC9110-...` | Compliance (when the test maps directly to a specific RFC section) |

### Validation options

**Simple status check:**

```csharp
Expected = new ExpectedBehavior
{
    ExpectedStatus = StatusCodeRange.Exact(400),
}
```

**Allow connection close as alternative:**

```csharp
Expected = new ExpectedBehavior
{
    ExpectedStatus = StatusCodeRange.Exact(400),
    AllowConnectionClose = true,
}
```

**Custom validator** (takes priority over `ExpectedStatus`):

```csharp
Expected = new ExpectedBehavior
{
    CustomValidator = (response, state) =>
    {
        if (response?.StatusCode == 400) return TestVerdict.Pass;
        if (response?.StatusCode >= 200 && response.StatusCode < 300)
            return TestVerdict.Warn;
        return TestVerdict.Fail;
    },
    Description = "400 = pass, 2xx = warn"
}
```

### Key conventions

- Set `RfcLevel` to match the RFC 2119 keyword for the requirement being tested. The default is `Must` — only set it explicitly for non-Must tests. Available values: `Must`, `Should`, `May`, `OughtTo`, `NotApplicable`. Check the [RFC Requirement Dashboard](/docs/rfc-requirement-dashboard.html) for classification guidance.
- Build payloads with the `MakeRequest` helper each suite defines, and always use `ctx.HostHeader` rather than a hardcoded host.
- Tests are auto-discovered — the `yield return` is the whole registration, there is no list to update.
- Use `Exact(400)` with **no** `AllowConnectionClose` for strict MUST-400 requirements (SP-BEFORE-COLON, MISSING-HOST, DUPLICATE-HOST, OBS-FOLD, CR-ONLY).
- Set `AllowConnectionClose = true` only when connection close is an acceptable alternative to a status code.
- Set `Scored = false` for MAY-level or informational tests.
- Use `"RFC 9112 §5.1"` format for `RfcReference` (section sign, not "Section").
- Give the doc page a readable `title` (e.g. `"My Test — HTTP/1.1 Compliance"`), not the raw test ID — it's rendered as the page heading and browser tab title. Write a `description` that's a specific, one-sentence summary of the request and its RFC basis, not a generic placeholder — it's used as the page's meta description for search results.

## 2. Add a docs URL mapping (if needed)

**File:** `src/Http11Probe.Cli/Reporting/DocsUrlMap.cs`

Tests prefixed with `SMUG-`, `MAL-`, `NORM-`, `COOK-`, or `WS-` are auto-mapped to their doc URL based on the ID. For example, `SMUG-CL-TE-BOTH` maps to `smuggling/cl-te-both`.

Every other prefix — `COMP-`, `RFC*`, `CAP-` — needs an explicit entry in the `ComplianceSlugs` dictionary, otherwise the test result won't link to its documentation:

```csharp
["COMP-MY-TEST"] = "headers/my-test",
```

If the slug doesn't follow the standard pattern (e.g. the filename differs from the ID), add it to `SpecialSlugs` instead.

## 3. Create the documentation page

**File:** `docs/content/docs/{category}/{test-slug}.md`

The `{category}` folder is the slug the doc URL map points at. Compliance tests are split across several folders by topic:

| Category | Folder |
|----------|--------|
| Compliance (line endings) | `line-endings` |
| Compliance (request line) | `request-line` |
| Compliance (headers) | `headers` |
| Compliance (host header) | `host-header` |
| Compliance (content-length) | `content-length` |
| Compliance (body) | `body` |
| Smuggling | `smuggling` |
| Malformed Input | `malformed-input` |
| Normalization | `normalization` |
| Cookies | `cookies` |
| WebSockets | `websockets` |
| Capabilities | `caching` |

Compliance folders are chosen by topic, so pick the one the requirement belongs to rather than deriving it from the test ID.

Use this template:

```markdown
---
title: "My Test — HTTP/1.1 Compliance"
description: "One or two sentences describing the request and what makes it non-conforming, ideally ending with the RFC section it's tested against."
weight: 1
---

| | |
|---|---|
| **Test ID** | `COMP-MY-TEST` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §X.X](https://www.rfc-editor.org/rfc/rfc9112#section-X.X) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

Description of the request and what makes it non-conforming.

## What the RFC says

> "Exact quote from the RFC." -- RFC 9112 Section X.X

## Why it matters

Security and compatibility implications.

## Sources

- [RFC 9112 §X.X](https://www.rfc-editor.org/rfc/rfc9112#section-X.X)
```

## 4. Add a card to the category index

**File:** `docs/content/docs/{category}/_index.md`

Add a card entry in the appropriate section (scored or unscored):

```
{{</* card link="my-test" title="MY-TEST" subtitle="Short description of the test." */>}}
```

The `link` value is the filename without `.md`. Place scored tests before unscored ones.

## 5. Add a row to the RFC Requirement Dashboard

**File:** `docs/content/docs/rfc-requirement-dashboard.md`

This page classifies every test by its RFC 2119 requirement level, so a new test needs a row and the surrounding counts need updating.

Pick the table by requirement level:

| Level | Table |
|-------|-------|
| `MUST` / `MUST NOT` | "MUST-Level Requirements" — use the "Reject with 400" sub-table when the RFC explicitly mandates 400, otherwise "Reject (400 or Connection Close Acceptable)" |
| `SHOULD` / `SHOULD NOT` | "SHOULD-Level Requirements" |
| `MAY` | "MAY-Level Requirements" |
| Any level, `Scored = false` | "Unscored Tests" |

The row carries the test ID, suite name, RFC link, and an exact RFC quote with the keyword bolded (`**MUST**`). Then update every count the new row affects:

- The summary table at the top
- The total test count, in both the `description` frontmatter and the "Total: N tests" line
- The "Requirement Level by Suite" section
- The "RFC Section Cross-Reference" table — increment the existing section or add a row

## 6. Verify

Build and run the probe locally:

```bash
dotnet build Http11Probe.slnx -c Release
dotnet run --project src/Http11Probe.Cli -- --host localhost --port 8080
```

Check that:
- Your test appears in the JSON output with the correct ID
- The verdict makes sense against a known server
- The documentation page renders and is linked from its category index

To preview the site, build it from `web/`:

```bash
cd web && npm ci && node build.mjs
```

No changes are needed in the CI workflow -- new tests are discovered automatically.
