# HermesProxy — Project Map

This map routes agents to the owning subsystem before they edit.

## Product boundary

HermesProxy translates between a modern WoW client-facing protocol and a legacy server-facing protocol. It does not own launcher UX/deployment orchestration or rotation logic.

## Major repository areas

| Area | Purpose | Risk |
|---|---|---|
| `HermesProxy/` | Main proxy runtime | High |
| `HermesProxy/World/Client/` | Legacy-server-facing world client, incoming legacy packet translation | High |
| `HermesProxy/World/Server/` | Modern-client-facing world server, incoming modern packet translation | High |
| `HermesProxy/World/Objects/` | Object/game-state representation and version-specific serialization | Very high |
| `HermesProxy/World/Objects/Version/V3_4_3_54261/` | Wrath Classic 3.4.3 object-update implementation | Very high |
| `HermesProxy.Tests/` | Unit/integration coverage | Medium |
| `HermesProxy.Benchmarks/` | Performance/benchmark coverage | Medium |
| `Framework/` | Shared protocol/framework primitives | High/shared |
| `wotlk.md` | WotLK roadmap/protocol context | Documentation, not runtime proof |
| `docs/` | Known issues and supporting documentation | Documentation |

## Load-bearing latency/compatibility seams

### Loot

Primary handlers:

- `HermesProxy/World/Client/PacketHandlers/LootHandler.cs`
- `HermesProxy/World/Server/PacketHandlers/LootHandler.cs`

Shared state used for target/order/master-loot behavior must be treated as a conflict seam.

### Object updates

Primary 3.4.3 serializer area:

- `HermesProxy/World/Objects/Version/V3_4_3_54261/ObjectUpdateBuilder.cs`
- associated update handlers under `HermesProxy/World/Client/PacketHandlers/`

Any change here requires focused tests and fresh live/client evidence before a live-valid claim.

### Socket/queue behavior

World/client send paths and shared locks/queues are performance-sensitive. Changes require explicit measurement; do not infer lower latency merely from asynchronous code shape.

### Item-template waits

Template lookup/wait/release paths can convert missing metadata into visible gameplay stalls. Treat timeout behavior as user-visible latency behavior.

## Cross-repository contracts

### ModernWoWLauncher

**Launcher owns:**

- selecting the Hermes runtime artifact;
- copying/staging/deploying it;
- generating/effecting runtime configuration;
- process startup/readiness/provenance;
- user-facing launch flow.

**Hermes owns:**

- effective socket behavior after startup;
- packet translation;
- metrics implementation;
- packet logging implementation;
- object/loot/spell/movement protocol behavior.

A launcher-started session is required to prove the deployed artifact is actually the intended Hermes build.

### ProRotationPilot-3.4.3

Consumer only for this seam. Rotation behavior cannot substitute for protocol correctness evidence.

## Validation map

| Claim | Minimum evidence |
|---|---|
| Source compiles | `dotnet build HermesProxy.sln -c Release` |
| Tests pass | `dotnet test HermesProxy.sln -c Release`, exact result inspected |
| Packet handler works structurally | focused test/characterization evidence |
| Runtime config/provenance effective | fresh process log/config evidence |
| `TCP_NODELAY` effective | fresh socket/runtime evidence |
| Gameplay translation works | exact live client/server scenario |
| Latency improved | timestamped before/after or decomposed runtime metrics under comparable conditions |

## Conflict groups

Serialize work within these groups unless proven independent:

- `protocol-object-update`
- `protocol-loot-state`
- `socket-send-path`
- `game-state-shared`
- `runtime-config-metrics`
- `launcher-hermes-provenance` (cross-repo)

Documentation/test work can run in parallel when it does not write the same canonical state or shared fixture.

## Extension rule

When adding a new opcode/version path, prefer the existing versioned handler/packet architecture. Do not leak WotLK-specific assumptions into proven older-version paths without an explicit compatibility reason and regression coverage.