# Hermes Packet Replay Oracle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add sanitized, deterministic, bidirectional offline packet fixtures that compare Hermes translations byte-for-byte and report semantic field damage.

**Architecture:** Keep capture artifacts and replay orchestration in `HermesProxy.Tests`; invoke real Hermes packet parsers and serializers through narrow adapters. JSON pins protocol builds, direction, connection, dialect, bytes, semantics, and provenance without production payload logging.

**Tech Stack:** .NET 10, xUnit v3, System.Text.Json, Hermes `WorldPacket`.

## Global Constraints

- Modern client `V3_4_3_54261`; legacy server `V3_3_5a_12340`; primary profile `maelstrom`.
- Preserve dirty work, do not deploy while WoW runs, and do not treat WPP 3.4.0 as a 3.4.3 golden.

---

### Task 1: Schema and corpus
- [x] Add strict fixture records, deterministic loading, build/dialect/provenance validation, and copied JSON content.

### Task 2: Bidirectional oracle
- [x] Replay real Hermes parsers/serializers, compare wire bytes and semantic fields, and contain malformed input.

### Task 3: Seed P0 cases
- [x] Add positive, missing-optional, truncated, unexpected, and fuzz cases for loot-list and interaction-close.

### Task 4: Evidence policy and verification
- [x] Require protocol PR fixtures, document capture-gated flows, run focused tests, diff checks, and status after WoW exits; record the unrelated full-build blocker separately.
