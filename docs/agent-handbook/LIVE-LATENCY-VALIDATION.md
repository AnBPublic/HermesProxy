# Live Latency Validation Playbook

Use this procedure whenever a change claims improved WotLK 3.4.3.54261 -> 3.3.5a gameplay latency or compatibility.

## Preconditions

- Current source builds and tests pass.
- The intended Hermes artifact has been rebuilt.
- ModernWoWLauncher starts the exact rebuilt artifact.
- Runtime provenance is logged or otherwise captured.
- Raw packet logging is disabled for the latency baseline unless a capture is the explicit purpose of that run.
- Lifecycle metrics are temporarily enabled for the measurement run.

If any precondition is missing, mark the live task `BLOCKED` or `PROBE_REQUIRED` rather than continuing with a success claim.

## Capture before login

Record:

- launcher build/commit or artifact identity;
- Hermes build/commit/artifact identity;
- exact Hermes runtime path;
- effective config relevant to packet logging and metrics;
- effective NoDelay/`TCP_NODELAY` state;
- target client build and server/realm.

## Test sequence

Run each case separately enough that timestamps can be correlated.

1. **Target switching** — repeatedly switch between nearby units; observe target identity and health updates.
2. **Combat state** — enter/leave combat and observe HP/aura/state changes.
3. **Basic loot** — open and loot a normal single-item corpse.
4. **Failed loot** — exercise a legitimate loot-failure response and verify the client receives a response rather than stalling.
5. **Rapid corpse changes** — alternate loot targets quickly to stress ordering/state.
6. **Multi-item loot** — loot a corpse with multiple items and measure item-to-item delay.
7. **Group loot** — exercise group loot behavior.
8. **Master loot** — exercise master-loot list/candidate behavior and repeated responses.
9. **Quest interaction close** — verify the modern close-interaction path no longer produces an unknown-handler warning.
10. **Spell execute behavior** — exercise a representative spell that generates the translated execute log.

## Required negative checks

The fresh run must be searched for:

- unknown `SMSG_LOOT_LIST`;
- unknown `SMSG_SPELL_EXECUTE_LOG`;
- unknown `CMSG_CLOSE_INTERACTION`;
- object-update serialization/translation failures;
- item-query timeout releases during normal scenarios;
- queue-full/drop events;
- unhandled exceptions or disconnects.

## Latency evidence

Do not report "faster" from subjective feel alone. Capture enough timing evidence to separate, where instrumentation permits:

- server response/RTT;
- Hermes processing time;
- queue/send delay;
- client-visible reaction time.

For multi-item loot, report per-item spacing because the current architecture may still pay one legacy round trip per item.

## Promotion criteria

A specific behavior may move from `IMPLEMENTED_STATIC`/`PROBE_REQUIRED` to `LIVE_VALIDATED` only when:

- the exact rebuilt runtime was proven active;
- the exact scenario was exercised against the target client/server;
- expected behavior was observed;
- relevant negative checks were clean;
- evidence is retained or summarized in `CURRENT-STATE.md` with a path/reference to the raw log/capture.

Do not generalize one successful loot case into object-update, movement, socket-latency, or all-server compatibility acceptance.

## After the run

Update:

- `CURRENT-STATE.md` with exact promoted/failed workstreams;
- `docs/agent-handbook/TASK-GRAPH.json` statuses/dependencies;
- `wotlk.md` only where roadmap/deeper protocol facts changed;
- tests/ADRs/known-issues when the run reveals durable behavior.

If a defect remains, create the smallest bounded next task and record the exact evidence needed to reproduce it.