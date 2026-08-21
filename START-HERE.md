# HermesProxy — Start Here

This is the cold-start entrypoint for humans and agents. Assume no chat history.

## 1. Establish repository reality

Before changing anything, inspect the current branch, HEAD, working tree and recent commits. Do not trust a recorded SHA over Git itself.

## 2. Read the minimum authoritative set

1. `AGENTS.md` — durable rules and source-of-truth hierarchy.
2. `CURRENT-STATE.md` — verified present state, risks and next proof gate.
3. `PROJECT-MAP.md` — ownership and important source/test/deploy seams.
4. `docs/agent-handbook/TASK-GRAPH.json` — active dependencies and conflict groups.

Read `wotlk.md` only when deeper WotLK translation context is needed. Read the live-validation playbook when testing latency/gameplay behavior.

## 3. Current project state

The WotLK 3.4.3.54261 -> 3.3.5a latency/compatibility work has source-level remediation present, but the low-latency gameplay claim is **not live-confirmed** as of 2026-08-21.

The next highest-value action is not more speculative refactoring. It is a fresh launcher-started runtime session proving the rebuilt Hermes binary and collecting bounded latency/compatibility evidence.

## 4. Frozen/dangerous seams

Treat these as high-risk and serialize changes:

- packet opcode translation and parser/serializer contracts;
- 3.4.3 object-update serialization;
- socket send/queue behavior and `TCP_NODELAY` policy;
- item-template wait/release logic;
- cross-version behavior that could regress 1.14.x or 2.5.x support;
- deployment/provenance assumptions shared with ModernWoWLauncher.

## 5. Recommended next task

Run the live latency proof gate in `docs/agent-handbook/LIVE-LATENCY-VALIDATION.md` against a fresh launcher-started session.

Success requires evidence for runtime provenance, `TCP_NODELAY`, packet logging state, lifecycle metrics, targeting/health/combat updates, basic/failed/multi-item/group/master loot, and absence of the known warning/timeout/queue failure signatures.

## 6. Parallel-safe work

Safe in parallel when write sets do not overlap:

- documentation/evidence cleanup;
- test-only additions for independent packet handlers;
- analysis of captured logs without modifying runtime code.

Do not parallelize changes that both touch `WorldClient`, object-update serialization, shared game state, socket send paths, or deployment provenance assumptions.

## 7. If blocked

Do not guess. Record the exact missing runtime, packet capture, server behavior, deployment artifact, log line, or architecture decision. Mark the task `BLOCKED` or `PROBE_REQUIRED` and state the smallest evidence needed to resume.

## 8. Completion gate

A source change is not complete merely because it builds. For protocol/latency work, complete the relevant static tests, independent review, knowledge update, and runtime/live gate where the task claims runtime/live behavior. Release/deployment remain separate decisions.