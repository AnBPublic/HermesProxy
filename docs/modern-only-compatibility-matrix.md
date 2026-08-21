# Modern-only compatibility-response matrix

Scope: `V3_4_3_54261` client to `V3_3_5a_12340` server. The runtime emits `modern_only_opcode` counters with the investigation ID, direction, subsystem, context, and classification; diagnostics are emitted for the first occurrence and each 100th repeat.

| Direction | Count | Context / subsystem | Classification | Investigation records |
| --- | ---: | --- | --- | --- |
| Client to server | 9 | Login/character select/in-world: Battle Pay, VAS, undelete, calendar, countdown, battle pets, GM tickets | Safe minimal response | MODERN-SVC-001–004, 008–012 |
| Client to server | 3 | Login: client-variable, addon, and keybinding reporting | Safe ignored notification | MODERN-SVC-013–015 |
| Client to server | 1 | In-world: PvP rewards | Capture required | MODERN-SVC-005 |
| Client to server | 1 | In-world: forced reactions | Required translation | MODERN-SVC-006 |
| Server to client | 1 | In-world: forced reactions | Required translation | MODERN-SVC-007 |
| Server to client | 4 | In-world: spell impacts, trainer purchase, equipment sets, instance difficulty | Capture required | P4-SPELL-IMPACT, P4-TRAINER-BUY-SUCCEEDED, P4-LOAD-EQUIPMENT-SET, P4-INSTANCE-DIFFICULTY |

Unmapped wire opcodes are named `MODERN-WIRE-C2S-0xNNNN` or `MODERN-WIRE-S2C-0xNNNN`, classified `CaptureRequired`, and blocked. They are never logged as `MSG_NULL_ACTION` and are never forwarded to the legacy server.

`StartupLoginBaseline` is an exact fixture list. Any addition or removal fails `ModernOnlyCompatibilityMatrixTests` until it is given a disposition and an investigation record. The baseline is exercised by the isolated protocol-replay test project, so unrelated full-suite dependencies cannot mask it.
