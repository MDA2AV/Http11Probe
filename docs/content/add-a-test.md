---
title: Add a Test
---

A step-by-step guide to adding a new test to Http11Probe. Every test touches four places: the suite file, the docs URL map (sometimes), a documentation page, and the category index.

## 1. Define the test case

Pick the suite that matches your test's category and add a `yield return new TestCase` block.

| Category | File |
|----------|------|
| Compliance | `src/Http11Probe/TestCases/Suites/ComplianceSuite.cs` |
| Smuggling | `src/Http11Probe/TestCases/Suites/SmugglingSuite.cs` |
| Malformed Input | `src/Http11Probe/TestCases/Suites/MalformedInputSuite.cs` |
| Normalization | `src/Http11Probe/TestCases/Suites/NormalizationSuite.cs` |

```csharp
yield return new TestCase
{
    Id = "COMP-MY-TEST",
    Description = "Description of what the test checks",
    Category = TestCategory.Compliance,
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

- Use `Exact(400)` with **no** `AllowConnectionClose` for strict MUST-400 requirements (SP-BEFORE-COLON, MISSING-HOST, DUPLICATE-HOST, OBS-FOLD, CR-ONLY).
- Set `AllowConnectionClose = true` only when connection close is an acceptable alternative to a status code.
- Set `Scored = false` for MAY-level or informational tests.
- Use `"RFC 9112 §5.1"` format for `RfcReference` (section sign, not "Section").

## 2. Add a docs URL mapping (if needed)

**File:** `src/Http11Probe.Cli/Reporting/DocsUrlMap.cs`

Tests prefixed with `SMUG-`, `MAL-`, or `NORM-` are auto-mapped to their doc URL based on the ID. For example, `SMUG-CL-TE-BOTH` maps to `smuggling/cl-te-both`.

For `COMP-*` or `RFC*` prefixed tests, add an entry to the `ComplianceSlugs` dictionary:

```csharp
["COMP-MY-TEST"] = "headers/my-test",
```

If the slug doesn't follow the standard pattern (e.g. the filename differs from the ID), add it to `SpecialSlugs` instead.

## 3. Create the documentation page

**File:** `docs/content/docs/{category}/{test-slug}.md`

Use this template:

```markdown
---
title: "MY-TEST"
description: "MY-TEST test documentation"
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

## 5. Verify

Build and run the probe locally:

```bash
dotnet build Http11Probe.slnx -c Release
dotnet run --project src/Http11Probe.Cli -- --host localhost --port 8080
```

Check that:
- Your test appears in the JSON output with the correct ID
- The verdict makes sense against a known server
- The documentation page renders correctly with `hugo server` in `docs/`

No changes are needed in the CI workflow -- new tests are discovered automatically.
