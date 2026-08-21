# Known Issues

## WotLK 3.4.3 talent preview confirmation was discarded

The 3.4.3 client uses raw opcode `0x3553` for a confirmed player talent-preview batch. HermesProxy now recognizes that otherwise unnamed packet and translates its talent/rank pairs to legacy `CMSG_LEARN_PREVIEW_TALENTS` (`0x4C1`). Static regression coverage is present; the rebuilt runtime still requires an in-game save-and-relog check.

## P2 compatibility-stall controls

- Deferred 3.4.3 object updates now release after a 2-second item-query deadline and log unresolved template IDs instead of holding the world-update stream indefinitely.
- Raw packet capture is disabled by default; enable it only for a controlled reproduction.
- `SMSG_SPELL_EXECUTE_LOG` is mapped to the 3.4.3 wire opcode and translated for the legacy execute-log effect payloads used by the bridge.
- The launcher records the selected Hermes executable, assembly version, profile/build tuple, and waits for all four local Hermes backends (`1119`, `8081`, `8084`, `8086`) before starting the game.
- Legacy-to-modern movement timing/collision packets remain capture-gated where the 3.4.3 opcode mapping is not yet present. `CMSG_CLOSE_INTERACTION` is covered separately below.

## WotLK 3.4.3 quest-reward interaction remains open through HermesProxy

The 3.4.3 client sends `CMSG_CLOSE_INTERACTION` with a packed source GUID when an NPC interaction closes. HermesProxy parses that packet, forwards the empty legacy `CMSG_QUEST_GIVER_CANCEL` (`0x190`) to a 3.3.5a server, and clears matching tracked NPC/game-object interaction state. Static tests cover packet parsing and handler registration; live verification requires restarting the launcher with the rebuilt runtime.

**Follow-up fix — reward frame was closing before a reward could be chosen.** The blanket `CMSG_CLOSE_INTERACTION` → cancel translation above regressed quest turn-ins that offer a reward choice. The modern client tracks Gossip and QuestGiver as distinct interaction types (`PlayerInteractionType`); selecting a quest to turn in from the gossip menu closes the Gossip interaction — firing the same `CMSG_CLOSE_INTERACTION` — right as the request-items/reward frame opens for that NPC, so the proxy was cancelling the turn-in server-side before the player could double-click a reward. `GameSessionData.AwaitingQuestGiverRewardFor` now tracks the NPC currently showing that frame (set in `HandleQuestGiverRequestItems`/`HandleQuestGiverOfferRewardMessage`, cleared once the client responds via `CMSG_QUEST_GIVER_CHOOSE_REWARD`/`CMSG_QUEST_GIVER_COMPLETE_QUEST`), and `CloseInteraction.TryBuildLegacyCancel` swallows the spurious close for that NPC instead of forwarding a cancel. The genuine "player dismissed the frame" signal is the previously-unwired `CMSG_QUEST_GIVER_CLOSE_QUEST` opcode, now enabled in `HandleQuestGiverCloseQuest`. Live verification requires restarting the launcher with the rebuilt runtime and turning in a quest with a reward choice.

## Priest wand `Shoot` cancels in melee range (1.14.x client)

On modern 1.14.x Classic clients the `autoRangedCombat` CVar (default ON) treats wands as ranged weapons and auto-cancels `Shoot` the moment a mob enters melee range, then switches you into auto-attack. Vanilla 1.12 emulators (VMaNGOS, Kronos, CMaNGOS) never expected this — the wand simply dies, you can't finish the mob with it, and you get stuck swinging.

**Workaround — run once in chat:**
```
/console autoRangedCombat 0
```
Or make it persistent by adding this line to `WTF/Config.wtf` before launch:
```
SET autoRangedCombat "0"
```

Priest characters logging in on 1.14+ Classic Era clients receive a one-time chat reminder from the proxy on world-enter. Other classes that occasionally use a wand are affected the same way — apply the same CVar fix if you notice it. Tracked in [#80](https://github.com/Xian55/HermesProxy/issues/80).
