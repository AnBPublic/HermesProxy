# Protocol capture and replay fixtures

Protocol changes for `V3_4_3_54261` to `V3_3_5a_12340` use committed, sanitized JSON fixtures under `HermesProxy.Tests/Fixtures/Protocol`. The offline runner invokes real Hermes parsers and serializers; it does not open sockets or require a client or backend.

Every fixture pins direction, source opcode/connection/payload, both builds, backend dialect, expected opcode/connection/payload, semantic fields, case class, and sanitized provenance. Hex contains payload bytes only: never framing, encryption state, credentials, account or character names, chat, or raw production identifiers. Sanitization must preserve widths, masks, ordering, and optional-field presence.

Use `Common` only when identical evidence exists across backends. Otherwise tag `CMaNGOS`, `TrinityCore`, `AzerothCore`, or `Maelstrom` and isolate the observation at the fixture boundary while keeping common translations shared.

Every protocol PR must add or update fixtures for each changed opcode. Each P0 opcode requires positive, missing-optional-field, truncated, and unexpected-value cases. Malformed and deterministic fuzz fixtures must return `rejected` without terminating the replay process. Replays must remain byte-exact and deterministic across repeated and parallel runs, and semantic mismatches must name the damaged field.

The executable seed corpus covers `SMSG_LOOT_LIST` and `CMSG_CLOSE_INTERACTION`. Login ordering; CreateObject/Values for items, containers, GameObjects, and transports; remaining loot; spell start/go/failure/execute/impact; and movement/collision/time-sync remain capture-gated. Do not invent their goldens: promote them only with sanitized known-good 3.4.3 evidence. A 3.4.0 WPP module is not proof of 3.4.3 wire identity, and a documented-layout fixture proves the harness path rather than capture equivalence.

Production raw packet capture remains disabled by default. This harness adds no production payload logging.
