# HermesProxy — Current State

**State date:** 2026-08-21

This file is the canonical handbook-level project state. It distinguishes source implementation from runtime/live proof. Git and fresh runtime evidence supersede stale prose.

## Executive status

**Overall:** `IN_PROGRESS`

**Latency remediation:** `IMPLEMENTED_STATIC`

**Low-latency gameplay:** `PROBE_REQUIRED`

The correct current label is: **source-level latency remediation in progress; live low-latency behavior unconfirmed.**

The latest known gameplay log predates the rebuilt Hermes runtime. Therefore old-session warnings do not prove the rebuilt runtime still has the same defects, while source changes do not prove the rebuilt runtime fixed them.

## Evidence boundary

Known review evidence as of 2026-08-21:

- latest gameplay log ended around 21:34:48;
- rebuilt Hermes binaries were produced around 21:42;
- therefore no post-build gameplay session exists in the supplied evidence;
- active runtime configuration still had packet logging enabled and lifecycle metrics disabled;
- the old live session still reported unknown `SMSG_LOOT_LIST`, `SMSG_SPELL_EXECUTE_LOG`, and `CMSG_CLOSE_INTERACTION`.

Any newer source/runtime evidence must update this file rather than being left only in chat or a transient handover.

## Workstream state

| Workstream | Source state | Live/runtime state | Handbook status |
|---|---|---|---|
| `SMSG_LOOT_LIST` translation | Handler/parser present | No post-build loot proof | `IMPLEMENTED_STATIC` |
| Loot failure responses | Forwarding path present | Not confirmed in-game | `IMPLEMENTED_STATIC` |
| Loot target ordering | Locked/FIFO state added | Out-of-order behavior untested | `PROBE_REQUIRED` |
| Multi-item looting | One legacy request per item remains | RTT impact unmeasured | `PROBE_REQUIRED` |
| 3.4.3 partial object updates | Serializer substantially implemented | No byte-capture/client acceptance proof | `PROBE_REQUIRED` |
| Target/health/combat updates | Code paths present | No live target/HP/aura/combat proof | `PROBE_REQUIRED` |
| Item/GameObject creates | Many filters removed | No live proof; transport variants remain constrained | `PROBE_REQUIRED` |
| Missing item-template waits | bounded release timeout present | Can still produce visible delay | `IMPLEMENTED_STATIC` |
| `TCP_NODELAY` | forced-NoDelay policy present | No fresh runtime log/A-B proof | `PROBE_REQUIRED` |
| Modern/client socket writes | bounded async queue present | burst behavior untested | `PROBE_REQUIRED` |
| Legacy/server socket writes | partial-send handling present; synchronous locked leg remains | burst behavior untested | `PROBE_REQUIRED` |
| End-to-end metrics | lifecycle instrumentation present | disabled in active runtime | `BLOCKED` until enabled for test |
| Packet logging overhead | source/runtime policy work exists | active baseline still had packet logging enabled | `PROBE_REQUIRED` |
| Spell execute compatibility | mapping added | no fresh gameplay proof | `PROBE_REQUIRED` |
| Movement timing/collision | incomplete/capture-gated | unverified | `PROBE_REQUIRED` |
| Quest interaction close | handler added | old runtime still showed unhandled opcode | `PROBE_REQUIRED` |

## Highest risks

1. **Runtime/deployment mismatch.** A rebuilt Hermes artifact has not yet been proven in a new launcher-started session.
2. **Object-update correctness.** The 3.4.3 serializer is load-bearing and not yet validated by packet capture plus live client behavior during target, health, aura, combat and movement changes.
3. **Loot end-to-end correctness and latency.** Basic, failed, rapid-target, multi-item, group and master-loot cases need timestamped live proof.
4. **No latency decomposition baseline.** With lifecycle metrics disabled, server RTT, proxy processing, socket queueing and client-visible update delay cannot yet be separated.
5. **Packet-capture overhead.** Raw packet logging in the active runtime can distort the very latency baseline being evaluated.
6. **Blocking legacy send leg.** Synchronous sends under lock remain a plausible burst-lag source until measured.

## Mandatory next proof gate

Run one fresh session launched through the current ModernWoWLauncher source/runtime path using the rebuilt Hermes artifact.

Required conditions/evidence:

- log exact Hermes provenance/version/path/hash or equivalent identity;
- log/verify effective `TCP_NODELAY`/NoDelay state;
- disable raw packet capture for baseline latency testing;
- temporarily enable lifecycle metrics;
- test repeated target switching plus HP/aura/combat updates;
- test basic loot, failed loot, rapid corpse changes, multi-item loot, group loot and master loot;
- verify no unknown `SMSG_LOOT_LIST` warning;
- verify no object-update serialization/client-rejection failure;
- verify no item-query timeout release during normal cases;
- verify no queue-full/drop event;
- capture timestamps sufficient to separate server RTT, proxy processing and client-visible delay.

Detailed procedure: `docs/agent-handbook/LIVE-LATENCY-VALIDATION.md`.

## Promotion rules

- Static build/tests may promote a task to `IMPLEMENTED_STATIC` only.
- Fresh process/config/provenance observation may promote the relevant deployment behavior to `RUNTIME_VALIDATED`.
- Actual gameplay observation against the target client/server may promote the exact tested behavior to `LIVE_VALIDATED`.
- `ACCEPTED` requires independent review of load-bearing protocol/latency evidence.
- `COMPLETE` additionally requires state/task-graph/knowledge updates and a clean checkpoint.

Do not promote unrelated behaviors based on one successful scenario.