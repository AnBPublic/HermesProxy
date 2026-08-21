# HermesProxy Agent Router

This file is the compact canonical router for agents working in this repository. Do not use chat history as project state.

## Read first

1. `START-HERE.md`
2. `CURRENT-STATE.md`
3. `PROJECT-MAP.md`
4. `wotlk.md` when work touches 3.4.3.54261 -> 3.3.5a translation
5. `docs/agent-handbook/TASK-GRAPH.json` for current dependencies/conflicts
6. `docs/agent-handbook/LIVE-LATENCY-VALIDATION.md` for the latency proof gate

The handbook derives from `AnBPublic/Agent-Handbook` Standard 1.0. Root `AGENTS.md` is canonical shared agent policy. Vendor-specific steering files are adapters only and must not become competing sources of truth.

## Repository purpose

HermesProxy is the protocol translation layer between modern WoW clients and legacy server emulators. This working copy also develops Wrath Classic `3.4.3.54261` -> legacy `3.3.5a` compatibility used by ModernWoWLauncher.

## Source-of-truth order

When sources disagree, resolve in this order:

1. actual source code and current Git state;
2. executable tests and deterministic build output;
3. fresh packet/runtime evidence from the exact target client/server/runtime;
4. accepted ADRs/normative protocol constraints;
5. `CURRENT-STATE.md`;
6. `wotlk.md`, handovers, research and older prose;
7. chat history.

Never let stale prose override current code/runtime evidence. Never let static tests create a live-validation claim.

## Status vocabulary

Use only explicit states where practical: `UNKNOWN`, `DISCOVERED`, `PLANNED`, `READY`, `IN_PROGRESS`, `BLOCKED`, `IMPLEMENTED_STATIC`, `RUNTIME_VALIDATED`, `LIVE_VALIDATED`, `ACCEPTED`, `COMPLETE`, `FROZEN`, `PROBE_REQUIRED`, `SUPERSEDED`, `DEPRECATED`, `NOT_PLANNED`.

For WotLK gameplay compatibility, `IMPLEMENTED_STATIC` is not equivalent to `LIVE_VALIDATED`.

## Non-negotiable boundaries

- Preserve proven 1.14.x -> 1.12.x and 2.5.x -> 2.4.3 behavior unless a separately justified change explicitly covers those mappings.
- Keep modern-facing server components (`BNetServer`, `WorldServer`) conceptually separate from legacy-facing clients (`AuthClient`, `WorldClient`).
- Do not claim `3.4.3.54261` -> `3.3.5a` works for a behavior unless the exact behavior was observed live.
- Do not weaken crash diagnostics or compatibility checks to make tests pass.
- Do not alter pooled-buffer/performance paths casually; benchmark or collect runtime evidence when changing latency-sensitive code.
- Protocol/opcode/object-update changes require focused tests and independent review.
- Implementation, release and deployment are separate authorities.

## Current latency program

As of 2026-08-21 the correct project label is:

> `IMPLEMENTED_STATIC` / source-level latency remediation in progress; low-latency gameplay is not yet confirmed live.

The mandatory next proof gate is a fresh launcher-started session using the rebuilt Hermes runtime. Required evidence is defined in `CURRENT-STATE.md` and `docs/agent-handbook/LIVE-LATENCY-VALIDATION.md`.

Do not promote the latency work to `RUNTIME_VALIDATED`, `LIVE_VALIDATED`, `ACCEPTED`, or `COMPLETE` merely because builds/tests pass.

## Cross-repository ownership

- **HermesProxy owns:** protocol translation, packet handling, object-update serialization, socket behavior, proxy metrics/logging behavior.
- **ModernWoWLauncher owns:** runtime selection, provenance/readiness checks, deployment/copy/start orchestration, user-facing launch flow.
- **ProRotationPilot-3.4.3 consumes the resulting gameplay environment** and must not be used as proof that Hermes translation is correct.

When a defect crosses the Hermes/launcher seam, state which repo owns the fix and what evidence the other repo must provide.

## Validation

For source changes, normally run and inspect:

```bash
dotnet build HermesProxy.sln -c Release
dotnet test HermesProxy.sln -c Release
```

For latency/compatibility work, source checks are necessary but insufficient. Collect the exact runtime evidence required by the live-validation playbook.

## Task discipline

Before editing:

- inspect current branch/HEAD/status;
- identify the task capsule, dependencies, write set and conflict group;
- read only the smallest authoritative context set first;
- record unknowns instead of guessing.

During work:

- keep changes bounded to the owning subsystem;
- add/extend characterization tests for risky protocol behavior;
- avoid parallel writes to shared protocol/schema/socket seams unless explicitly serialized.

On completion:

- run required checks and report exact results;
- get independent review for protocol, socket, object-update, release/deploy or cross-repo changes;
- update `CURRENT-STATE.md` and task graph when project truth changes;
- harvest durable findings into docs/tests/ADRs, not chat transcripts;
- leave exact evidence needed for resumption if blocked.

## Stuck protocol

Classify blockers as one of: missing runtime evidence, protocol unknown, architecture decision, test gap, tooling/environment, deployment/provenance, cross-repo dependency, or external/server behavior.

If blocked, record:

- what is known;
- what is unknown;
- failed approaches/evidence;
- the smallest probe needed next;
- exact files/logs/runtime needed to resume.

Do not improvise around a missing live packet capture or deploy mismatch by declaring success.

## Documentation ownership

- `CURRENT-STATE.md` — canonical current verified state and next gate.
- `PROJECT-MAP.md` — architecture/source/test/deploy ownership map.
- `wotlk.md` — WotLK compatibility roadmap/deeper protocol notes, not the sole current-state authority.
- `docs/agent-handbook/TASK-GRAPH.json` — machine-readable active work/dependencies/conflicts.
- `docs/agent-handbook/LIVE-LATENCY-VALIDATION.md` — repeatable live proof procedure.

If a meaningful change makes any of these stale, updating the relevant handbook knowledge is part of completion.