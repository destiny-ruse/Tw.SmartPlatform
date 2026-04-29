# CAP Worker Adapter Boundary P3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the P3 CAP and Worker adapter boundaries from the design into executable follow-on specifications before adapter code is started.

**Architecture:** This P3 plan deliberately produces boundary documents and acceptance tests for future adapter plans. CAP and Worker adapters have different lifecycle, retry, acknowledgement, and cancellation semantics, so they are split into two explicit specs before implementation work begins.

**Tech Stack:** .NET 10, CAP conceptual integration, BackgroundService/HostedService conceptual integration, Tw.Core AOP, documentation review

---

## Execution Order

Run after `2026-04-29-06-grpc-adapter-p2.md` when P3 adapter work is selected. This file is a boundary-definition plan, not part of the P0/P1 first delivery.

## Source Inputs

- Design sections: § 6.5 adapter table, § 10.5, § 12
- Standards: `processes.doc-authoring#flow` and `processes.doc-authoring#rules`, version `1.0.0`, `docs/standards/processes/doc-authoring.md`; `rules.test-strategy#rules` version `1.1.0`, `docs/standards/rules/test-strategy.md`; `rules.asyncapi-conventions#rules` version `1.1.0`, `docs/standards/rules/asyncapi-conventions.md`

## File Structure

- Create: `docs/superpowers/specs/2026-04-29-cap-entry-aop-boundary.md`
- Create: `docs/superpowers/specs/2026-04-29-worker-entry-aop-boundary.md`
- Create: `docs/superpowers/plans/2026-04-29-08-cap-entry-adapter-p3.md`
- Create: `docs/superpowers/plans/2026-04-29-09-worker-entry-adapter-p3.md`
- Modify: `docs/superpowers/specs/2026-04-29-auto-registration-aop-design.md` only to add links to the two P3 boundary specs and successor plans.

## Tasks

### Task 1: Write CAP Entry Boundary Spec

**Files:**
- Create: `docs/superpowers/specs/2026-04-29-cap-entry-aop-boundary.md`

- [ ] **Step 1: Create the CAP boundary spec**

```markdown
# CAP Entry AOP Boundary Specification

**Date:** 2026-04-29
**Parent Design:** `docs/superpowers/specs/2026-04-29-auto-registration-aop-design.md`
**Stage:** P3

## Goal

Define the CAP consumer Entry AOP boundary before implementation.

## In Scope

- Define `ICapMessageFeature` shape
- Define when an Entry interceptor wraps a CAP consumer method
- Define cancellation token source for one message handling attempt
- Define how Service AOP continues to work inside handlers
- Define tests required before adapter implementation is accepted

## Out of Scope

- Taking ownership of CAP acknowledgement semantics
- Taking ownership of CAP retry policy
- Changing CAP subscription discovery
- Replacing CAP's own exception and transaction behavior

## Acknowledgement Boundary

The adapter must not acknowledge, reject, retry, or dead-letter messages directly. It wraps the handler invocation only. CAP remains responsible for acknowledgement, retry, and failure routing according to its configured pipeline.

## Feature Contract

```csharp
public interface ICapMessageFeature
{
    string? MessageId { get; }
    string? Topic { get; }
    IReadOnlyDictionary<string, string?> Headers { get; }
    object? Message { get; }
}
```

## Acceptance Criteria

1. Entry interceptors receive `ICapMessageFeature` during a CAP handler invocation.
2. Non-CAP scenarios return null from `GetFeature<ICapMessageFeature>()`.
3. Handler exceptions are not swallowed by the adapter.
4. The adapter does not call CAP acknowledgement, retry, or dead-letter APIs.
5. Service dependencies resolved inside handlers can still use Castle Service AOP.
```

- [ ] **Step 2: Review against async messaging standards**

Confirm the spec names message id, topic, headers, payload, retry boundary, and failure boundary. Record the review result under the spec heading `## Review Notes`.

- [ ] **Step 3: Commit**

```bash
git add docs/superpowers/specs/2026-04-29-cap-entry-aop-boundary.md
git commit -m "docs: define cap entry aop boundary"
```

### Task 2: Write Worker Entry Boundary Spec

**Files:**
- Create: `docs/superpowers/specs/2026-04-29-worker-entry-aop-boundary.md`

- [ ] **Step 1: Create the Worker boundary spec**

```markdown
# Worker Entry AOP Boundary Specification

**Date:** 2026-04-29
**Parent Design:** `docs/superpowers/specs/2026-04-29-auto-registration-aop-design.md`
**Stage:** P3

## Goal

Define the Worker and HostedService Entry AOP boundary before implementation.

## In Scope

- Define `IWorkerItemFeature` shape
- Define per-item interception for explicit worker item handlers
- Define ambient cancellation token propagation from a worker item token
- Define how Service AOP continues to work inside background processing
- Define tests required before adapter implementation is accepted

## Out of Scope

- Wrapping an entire `BackgroundService.ExecuteAsync` lifetime
- Changing host shutdown semantics
- Swallowing exceptions that should terminate a worker
- Inventing a scheduler, queue, or job framework

## Lifecycle Boundary

The adapter wraps a single worker item invocation. It must not wrap the whole `ExecuteAsync` loop because doing so would blur startup, shutdown, backoff, and item-processing responsibilities.

## Feature Contract

```csharp
public interface IWorkerItemFeature
{
    string? WorkerName { get; }
    string? ItemId { get; }
    object? Item { get; }
}
```

## Acceptance Criteria

1. Entry interceptors receive `IWorkerItemFeature` during a worker item invocation.
2. Non-worker scenarios return null from `GetFeature<IWorkerItemFeature>()`.
3. The adapter uses the per-item cancellation token as ambient token.
4. Exceptions keep the worker implementation's explicit error behavior.
5. Service dependencies resolved inside workers can still use Castle Service AOP.
```

- [ ] **Step 2: Review lifecycle boundaries**

Confirm the spec distinguishes host lifetime, worker loop lifetime, and per-item invocation. Record the review result under the spec heading `## Review Notes`.

- [ ] **Step 3: Commit**

```bash
git add docs/superpowers/specs/2026-04-29-worker-entry-aop-boundary.md
git commit -m "docs: define worker entry aop boundary"
```

### Task 3: Create CAP Adapter Successor Plan

**Files:**
- Create: `docs/superpowers/plans/2026-04-29-08-cap-entry-adapter-p3.md`

- [ ] **Step 1: Create the CAP successor plan shell with concrete tasks**

```markdown
# CAP Entry Adapter P3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement CAP consumer Entry AOP using the approved CAP boundary spec.

**Architecture:** The adapter wraps only CAP handler invocation and exposes `ICapMessageFeature`; CAP owns acknowledgement and retry behavior.

**Tech Stack:** .NET 10, Tw.Core AOP, CAP integration package selected by dependency admission

---

## Preconditions

- `docs/superpowers/specs/2026-04-29-cap-entry-aop-boundary.md` is reviewed.
- CAP package dependency admission is recorded with license, vulnerability scan, owner, and lock file evidence.

## Tasks

### Task 1: Admit CAP Adapter Dependency

**Files:**
- Modify: `backend/dotnet/Build/Packages.ThirdParty.props`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj`
- Modify: `docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md`

- [ ] Pin the selected CAP integration package through central package management.
- [ ] Run restore and vulnerability scan.
- [ ] Record license, owner, replacement option, and scan result.

### Task 2: Implement `ICapMessageFeature`

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Cap/ICapMessageFeature.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Cap/CapMessageFeature.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/Cap/CapMessageFeatureTests.cs`

- [ ] Add feature contract from the boundary spec.
- [ ] Verify message id, topic, headers, and payload access.

### Task 3: Implement CAP Entry Adapter

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Cap/TwCapAopAdapter.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/Cap/TwCapAopAdapterTests.cs`

- [ ] Wrap handler invocation only.
- [ ] Expose `ICapMessageFeature`.
- [ ] Preserve handler exception propagation.
- [ ] Verify the adapter does not call acknowledgement, retry, or dead-letter APIs.
```

- [ ] **Step 2: Commit**

```bash
git add docs/superpowers/plans/2026-04-29-08-cap-entry-adapter-p3.md
git commit -m "docs: add cap entry adapter implementation plan"
```

### Task 4: Create Worker Adapter Successor Plan

**Files:**
- Create: `docs/superpowers/plans/2026-04-29-09-worker-entry-adapter-p3.md`

- [ ] **Step 1: Create the Worker successor plan shell with concrete tasks**

```markdown
# Worker Entry Adapter P3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement per-item Worker Entry AOP using the approved Worker boundary spec.

**Architecture:** The adapter wraps explicit worker item invocations and exposes `IWorkerItemFeature`; host lifetime remains owned by `BackgroundService` or the worker implementation.

**Tech Stack:** .NET 10, Microsoft.Extensions.Hosting, Tw.Core AOP

---

## Preconditions

- `docs/superpowers/specs/2026-04-29-worker-entry-aop-boundary.md` is reviewed.
- The target worker item abstraction is named and owned by the platform team.

## Tasks

### Task 1: Implement `IWorkerItemFeature`

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/Worker/IWorkerItemFeature.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/Worker/WorkerItemFeature.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/Worker/WorkerItemFeatureTests.cs`

- [ ] Add feature contract from the boundary spec.
- [ ] Verify worker name, item id, and item payload access.

### Task 2: Implement Per-Item Invocation Helper

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/Worker/WorkerEntryInvoker.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/Worker/WorkerEntryInvokerTests.cs`

- [ ] Wrap only one item invocation.
- [ ] Set ambient cancellation token from the item token.
- [ ] Preserve the worker implementation's exception behavior.
- [ ] Verify `GetFeature<IWorkerItemFeature>()` is available during interception.
```

- [ ] **Step 2: Commit**

```bash
git add docs/superpowers/plans/2026-04-29-09-worker-entry-adapter-p3.md
git commit -m "docs: add worker entry adapter implementation plan"
```

### Task 5: Link P3 Boundary Documents From Parent Design

**Files:**
- Modify: `docs/superpowers/specs/2026-04-29-auto-registration-aop-design.md`

- [ ] **Step 1: Add P3 links**

Add this section near `### 10.5 P3：后续 Adapter 验收边界`:

```markdown
> P3 边界已拆分为独立规格与执行计划：
>
> - CAP: `docs/superpowers/specs/2026-04-29-cap-entry-aop-boundary.md`，执行计划 `docs/superpowers/plans/2026-04-29-08-cap-entry-adapter-p3.md`
> - Worker: `docs/superpowers/specs/2026-04-29-worker-entry-aop-boundary.md`，执行计划 `docs/superpowers/plans/2026-04-29-09-worker-entry-adapter-p3.md`
```

- [ ] **Step 2: Verify links**

Run:

```powershell
Test-Path docs/superpowers/specs/2026-04-29-cap-entry-aop-boundary.md
Test-Path docs/superpowers/specs/2026-04-29-worker-entry-aop-boundary.md
Test-Path docs/superpowers/plans/2026-04-29-08-cap-entry-adapter-p3.md
Test-Path docs/superpowers/plans/2026-04-29-09-worker-entry-adapter-p3.md
```

Expected: four `True` lines.

- [ ] **Step 3: Commit**

```bash
git add docs/superpowers/specs/2026-04-29-auto-registration-aop-design.md
git commit -m "docs: link p3 adapter boundary plans"
```

## Self-Review Checklist

- [ ] CAP spec explicitly leaves ack, retry, dead-letter, and CAP subscription ownership outside the adapter.
- [ ] Worker spec explicitly avoids wrapping the full `ExecuteAsync` lifetime.
- [ ] CAP and Worker successor plans have numbered filenames after the P2 plan.
- [ ] Parent design links point to existing spec and plan paths.
- [ ] No P3 implementation begins before the boundary specs are reviewed.
