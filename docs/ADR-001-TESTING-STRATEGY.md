# ADR-001: Testing Strategy — Integration Tests for COM, Unit Tests for Pure Logic

**Status**: Accepted
**Date**: 2025-11-02
**Revised**: 2026-09-01 — see [Revision history](#revision-history)
**Decision Makers**: Architecture Team
**Stakeholders**: Development Team, Code Reviewers, Contributors

---

## Context and Problem Statement

VisioMcp is a COM automation library that wraps Visio's COM API. During code review the question
inevitably arises: **"Why so few unit tests?"**

This ADR records where the boundary sits, and why.

---

## Decision

**Anything that touches COM must be an integration test against a real Visio instance.
Pure logic with no COM dependency may be unit tested.**

### Integration test required

- Any code path that reaches a Visio COM object
- Session, batch, STA threading, COM lifetime, timeout and disposal behaviour
- Anything whose correctness depends on how Visio actually behaves

### Unit test acceptable

- Parameter validation and argument-shape assertions
- Result serialisation and DTO mapping
- Enum completeness and action-name mapping
- String, path and formula helpers
- Anything that would run identically with Visio uninstalled

### Never acceptable

- **Mocking a Visio COM object.** This is the practice this ADR exists to forbid.
- Faking `IVisioBatch` or `VisioContext` to assert that a command "worked"
- Any test that passes without Visio while claiming to verify COM behaviour

---

## Rationale

### 1. Visio COM cannot be meaningfully mocked

Visio's COM API is the "database" we automate against. Consider:

```csharp
public OperationResult CreatePage(IVisioBatch batch, string pageName)
{
    return batch.Execute((ctx, ct) =>
    {
        dynamic pages = ctx.Document.Pages;   // COM object
        dynamic newPage = pages.Add();        // COM method
        newPage.NameU = pageName;             // COM property
        return new OperationResult { Success = true };
    });
}
```

What would a mocked unit test prove?

```csharp
var mockDoc = new Mock<dynamic>();               // cannot mock dynamic COM objects
mockDoc.Setup(d => d.Pages).Returns(...);        // runtime binding fails

var result = CreatePage(null!, "Test");          // what is under test?
Assert.True(result.Success);                     // proves nothing
```

It would assert that our mock returns what we told it to. The bugs that actually occur here —
STA thread affinity, COM object leaks, `OleMessageFilter` re-entrancy, `double` vs `int`
conversion, shapes that do not persist — are invisible to a mock **by construction**.

### 2. But not all of our code is COM code

The absolutist version of this ADR was wrong about its own repository. `VisioMcp.Core.Tests` is
entirely unit tests and is the largest single block of passing tests we have. Those tests cover
parameter validation, result shapes and enum mappings — none of which touch COM, all of which
have caught real defects.

Forbidding them would delete working coverage to satisfy a slogan.

### 3. The distinction that matters is the dependency, not the label

"Unit vs integration" is the wrong axis. The right question is:

> **Would this test still be meaningful with Visio uninstalled?**

- If **yes**, it is testing pure logic and belongs in `Unit/`.
- If **no**, it must actually run Visio — otherwise it is testing a mock.

A test that needs Visio but does not use it is not a unit test. It is a test of nothing.

### 4. Industry precedent

Selenium, Playwright and the AWS SDK all integration-test against the real dependency for the
same reason, and all still unit-test their own pure logic. Neither half of that is controversial.

---

## Test layout

| Project | `Unit/` | `Integration/` | Notes |
|---|---|---|---|
| `VisioMcp.Core.Tests` | yes | — | Parameter validation, result shapes, enum mappings |
| `VisioMcp.ComInterop.Tests` | yes | yes | Unit: message filter constants. Integration: session, batch, STA, disposal |
| `VisioMcp.McpServer.Tests` | yes | yes | Integration exercises the real MCP protocol over in-memory pipes |
| `VisioMcp.CLI.Tests` | yes | yes | |
| `VisioMcp.SkillGeneration.Tests` | yes | — | Asserts on generated `SKILL.md` output |

Integration tests carry `[Trait("Category", "Integration")]`. Everything else runs under
`--filter "Category!=Integration"` and needs no Visio installation.

---

## Consequences

### Positive

- Tests of COM behaviour verify actual Visio behaviour, not an abstraction of it
- Pure logic gets fast, deterministic coverage that runs without Visio and can gate every PR in CI
- The policy is followable, so contributors are not forced to violate it to do ordinary work

### Negative

- Integration tests are slow and require Visio installed
- They cannot run on a hosted CI runner without Visio, so the non-integration suite is the
  practical PR gate
- Visio COM is a single machine-wide resource, so integration tests **cannot** be parallelised
  (see `tests/xunit.runner.json`)

### Mitigation

- Run the narrowest filter that covers the change (`--filter "Feature=<name>"`), never the full
  suite by default
- Keep the non-integration suite fast and green so it can gate every PR
- Copy a template document rather than spawning Visio to create fixtures

---

## Alternatives considered

| Alternative | Verdict |
|---|---|
| Mock Visio COM objects | Rejected — cannot mock `dynamic` COM; would assert only that mocks work |
| Record/replay COM interactions | Rejected — recordings drift from real Visio and hide version differences |
| Extract all logic away from COM | Partially adopted — where logic *is* separable it is unit tested; the COM calls themselves remain irreducible |
| Test against Visio interop primary assemblies | Rejected — still requires Visio; adds a typed-wrapper dependency without changing what is verified |

---

## Code review response template

When a reviewer asks "why is this an integration test?":

> Because it touches Visio COM. A mocked version would assert that our mock returns what we told
> it to, and would miss the failure modes that actually occur here — STA affinity, COM leaks,
> filter re-entrancy, type conversion. See `docs/ADR-001-TESTING-STRATEGY.md`.

When a reviewer asks "why is this a unit test?":

> Because it has no COM dependency — it would run identically with Visio uninstalled. Parameter
> validation, result shapes and enum mappings are genuinely unit-testable and are covered that way
> deliberately.

---

## Test execution

```powershell
# Non-integration suite — fast, no Visio required. The practical PR gate.
dotnet test --filter "Category!=Integration"

# Targeted integration — always prefer the narrowest filter that covers your change
dotnet test --filter "Feature=Shape&RunType!=OnDemand"
dotnet test --filter "Feature=Page&RunType!=OnDemand"

# MANDATORY when modifying VisioSession.cs or VisioBatch.cs
dotnet test tests/VisioMcp.ComInterop.Tests --filter "RunType=OnDemand"
```

---

## Revision history

**2026-09-01 — revised (#31).** The original version was titled *"Why VisioMcp Has No Traditional
Unit Tests"* and stated *"We do NOT write traditional unit tests"* and *"❌ Write unit tests for
business logic"*, with status **Accepted**.

That contradicted the repository. `VisioMcp.Core.Tests` was — and is — entirely unit tests and the
largest block of passing tests in the suite. Every other test project also has a `Unit/` directory.
As written, the policy forbade the majority of existing coverage, so either every contributor was
violating it or it was wrong. Contributors and coding agents are told these documents are binding,
so that state was not tenable.

The defensible half of the original argument — that mocked-COM tests prove nothing — is retained
and is now the actual rule. The absolutist framing is removed.

The original was also PowerPoint-worded throughout, referencing `IPptBatch`, `CreateSlide` and
`ctx.Presentation.Slides`, carried over from the ancestor repository.

Renamed from `ADR-001-NO-UNIT-TESTS.md`, whose filename asserted the position being corrected.
