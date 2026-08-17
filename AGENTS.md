# HermesProxy Repository Guidance

**Purpose:** Translation layer between modern WoW Classic clients and legacy private server emulators. This repository is the AnBPublic working copy of upstream Xian55/HermesProxy, with active WotLK 3.4.3 support development (see `wotlk.md`).

**Canonical Target:** Modern clients (1.14.0-1.14.2 Classic Era, 2.5.2-2.5.3 TBC Classic) connecting to legacy servers (1.12.1-1.12.3 Vanilla, 2.4.3 TBC). WotLK Classic 3.4.3.54261 → 3.3.5a server support is in active development (feature/wotlk-classic-v3.4.3 branch, wotlk.md roadmap).

## Architectural Boundaries

- **BNetServer:** Modern client Battle.net handshake endpoint (BNetPort, default 1119)
- **AuthClient:** Legacy authentication client (connects to realmd on Address:Port)
- **WorldServer:** Modern game world endpoint (RealmPort, InstancePort)
- **WorldClient:** Legacy world client (connects to mangosd/worldserver)
- **Do not conflate** the modern server components with the legacy server components

## Cross-Repository Contracts

- **ModernWoWLauncher:** Consumes HermesProxy as managed bridge; launcher sets portal to BNetPort
- **ProRotationPilot-3.4.3:** Expects to run through HermesProxy against 3.3.5a-ruleset servers
- Protocol compatibility changes **must not** break existing 1.14.x/2.5.x client support

## Compatibility Constraints

**Supported Version Mappings (proven):**
- 1.14.x modern client → 1.12.x legacy server
- 2.5.x modern client → 2.4.3 legacy server

**WotLK 3.4.3 Support (in development):**
- 3.4.3.54261 modern client → 3.3.5a legacy server
- ObjectUpdate format changed to descriptor-based system (see wotlk.md § "Why this is a large effort")
- Do **not** claim this mapping works without evidence from live testing
- Phase 0-5 roadmap in `wotlk.md` must be followed

**MUST NOT assume a client/server combination works** without:
1. Verification in `wotlk.md` phased roadmap completion
2. Live end-to-end testing evidence
3. Corresponding feature branch merged to master

## Configuration Precedence

Layered overrides applied in order (later overrides earlier):

1. `appsettings.json` — base configuration (required)
2. `appsettings.{Environment}.json` — environment-specific overlay (optional)
3. `HERMES_*` environment variables — `HERMES_Section__Key=Value` format
4. CLI args — `--Section:Key=Value` (native) or `--set Key=Value` (legacy)

**TLS Certificate:** Default TrinityCore-compatible cert (`CN=*.*`). Custom cert via `CertificatePfxPath`/`CertificatePfxPassword` in `ProxyNetworkOptions`. Do not change default unless targeting system trust store validation.

## Validation Commands

```bash
# Build and test
dotnet build HermesProxy.sln -c Release
dotnet test HermesProxy.sln -c Release

# Run with diagnostics
dotnet run --config appsettings.json --LoggingOptions:PacketLevel=Debug --metrics

# Verify crash diagnostics
# Dumps written to bin/<Release|Debug>/Logs/crash-<pid>.dmp
```

**Required:** Run `dotnet test` and verify same pass count before committing.

## Git/Change-Safety Rules

- **master branch** tracks upstream with AnBPublic-specific additions
- **feature/wotlk-classic-v3.4.3** contains phased WotLK support (do not merge prematurely)
- **Perf branches** (perf/union-*) are optimization workstreams
- **fix/ branches** are targeted bug fixes
- Do **not** force-push to master
- Rebase feature work on current master before PR

## Things an Agent MUST NOT Do

- Claim a client/server version combination works without live verification
- Modify upstream core without maintaining 1.14.x/2.5.x compatibility
- Change BnetTcpSession pooled-buffer/performance paths without benchmarking
- Remove or weaken existing crash diagnostics (`DOTNET_DbgEnableMiniDump`)
- Assume WotLK 3.4.3 support is complete (it is not — see wotlk.md)
- Merge feature/wotlk-classic-v3.4.3 to master before Phase 5 completion

## Documentation That Must Be Updated

- **wotlk.md:** After any WotLK 3.4.3 development milestone
- **docs/known-issues.md:** When new client/server quirks are discovered
- **README.md:** Only if upstream README changes are pulled in

## Live vs Static Validation

- **Static:** `dotnet build`, `dotnet test`, configuration parsing
- **Live:** Actual client connection, packet capture analysis, end-to-end gameplay
- **WotLK 3.4.3:** Currently **static only** — no live validation claimed
- Always distinguish: "builds and tests pass" vs "verified in-game"

## Related Repositories

- **ModernWoWLauncher:** Primary consumer (launcher integration)
- **ProRotationPilot-3.4.3:** Secondary consumer (rotation addon for WotLK)
