# GameCasual — CLAUDE.md

Casual mobile incremental resource game. Unity 6 (6000.0.59f2).
Two players collect Wood and Fish, deposit them at build sites to unlock land.
A wolf enemy roams the world and steals resources on contact.

---

## Architecture

```
GameManager (singleton)
├── PlayerController     — movement, inventory, pickup/deploy animations
├── InputManager         — virtual joystick (IDragHandler)
├── UIManager            — HUD counters, upgrade panel
└── LobbyManager         — multiplayer sessions (UGS Relay + Lobby)

MaterialsData            — resource nodes + build sites (one per world object)
MaterialsSO              — ScriptableObject: resource definition (icon, prefab, VFX)

Enemy                    — wolf AI (Wander / Chase / Scared / Returning states)
EnemySpawner             — spawns wolves at NavMesh-valid positions

StealEffect (singleton)  — red screen flash, auto-creates itself
WolfAlert                — "!" floating text above wolf, added to Enemy in Start()
MoveBagPos               — shifts the visual bag position as inventory grows
Tags (static)            — tag string constants
```

---

## Key files

| File | What it does |
|---|---|
| `Scripts/PlayerController.cs` | Extends `NetworkBehaviour`. `IsLocalPlayer` gate disables input on remote clones. `NetWoodCount`/`NetFishCount` for teammate visibility. |
| `Scripts/Enemy.cs` | NavMeshAgent state machine. `WolfAlert` + `StealEffect` wired in `Start()`. Destination update throttled to 10/s. |
| `Scripts/MaterialsData.cs` | Extends `NetworkBehaviour`. `canDrop` property routes to `NetCanDrop` when spawned. `Fill()` respawn timer runs server-side only. |
| `Scripts/Manager/UIManager.cs` | Programmatic HUD. Counter bounces via `PopCounter()` on pickup. |
| `Scripts/Networking/LobbyManager.cs` | Phase 1 — UGS init, Relay allocation, Lobby create/join, 15s heartbeat. |
| `Scripts/Networking/LobbyUI.cs` | Phase 1 — programmatic Canvas overlay. No prefab needed. |
| `Scripts/Networking/PlayerNameTag.cs` | Phase 2 — billboard TMP name tag, hidden on local player. |
| `Scripts/Networking/MultiplayerHUD.cs` | Phase 5 — self-spawning in-session HUD: join code, teammate counts, "waiting for players". No Inspector wiring. |
| `Scripts/Editor/NetworkSetup.cs` | **Tools → Setup Multiplayer** — creates NetworkManager + UnityTransport. |
| `Scripts/Editor/WolfAnimatorSetup.cs` | **Tools → Setup Wolf Animator** — loads clips from `wolf.fbx`, assigns Avatar. |

---

## Multiplayer — setup checklist

The networking code is in Phases 1–3. Here's what still needs to happen in the Unity Editor:

**Phase 1 (done in code)**
- [x] `LobbyManager` + `LobbyUI` scripts exist
- [ ] Add a GameObject to the scene with `LobbyManager` + `LobbyUI` components

**Phase 2 (done in code)**
- [x] `PlayerController` is `NetworkBehaviour` with `IsLocalPlayer` gate
- [x] `PlayerNameTag` exists
- [ ] Run **Tools → Setup Multiplayer** to add `NetworkManager` to scene
- [ ] Add to the Player prefab: `NetworkObject`, `NetworkTransform`, `PlayerNameTag`
- [ ] Assign the Player prefab in `NetworkManager → Player Prefab`

**Phase 3 (done in code)**
- [x] `MaterialsData` is `NetworkBehaviour` with server-authority `Fill()`
- [ ] Add `NetworkObject` component to every scene GameObject that has `MaterialsData`

**Unity Dashboard**
- [ ] Enable Relay service for this project
- [ ] Enable Lobby service for this project

---

## Wolf enemy — how it works

1. `EnemySpawner` uses `NavMesh.SamplePosition` to find a walkable spawn point.
2. Wolf starts in **Wander** state, picks random NavMesh targets near its spawn.
3. On entering **Chase** (player within `_detectionRange`): `WolfAlert.Show()` fires a "!" above the wolf.
4. If player runs directly toward the wolf while close (`_scareRange`, velocity dot > 0.7): enters **Scared**, flees for `_scaredDuration` seconds.
5. On contact (`SphereCollider` trigger): `AttemptSteal()` steals up to `_stealAmount` resources, calls `StealEffect.Instance.Flash()`, then returns home.
6. Animation is driven by `animator.CrossFade("Run"/"Idle")` — **no Animator parameters** are used. The controller must have NO transitions (pure states); otherwise transitions overrule CrossFade.

### Wolf animator — critical notes
- Clips live inside `wolf.fbx` (not in separate .fbx files). Clip names: `wolf_rig|idle`, `wolf_rig|running`.
- The Animator needs the **Avatar** from `wolf.fbx` assigned or bones won't bind (Generic rig requires Avatar).
- Run **Tools → Setup Wolf Animator** after moving the wolf.fbx to regenerate the controller.

---

## Game feel details

| System | What happens | Where |
|---|---|---|
| Resource pickup | Spring pop → arc up → fall into bag → squish → fly to player | `PlayerController.DeployElement()` |
| Resource deposit | Scale-in → fly to build site | `PlayerController.RemoveFunc()` |
| Wolf steals | Squash-stretch on wolf + red screen flash + bagPos correction | `Enemy.AttemptSteal()` + `StealEffect` |
| Wolf alert | "!" bounces above wolf, auto-hides 1.5s | `WolfAlert.Show()` |
| Resource counter | Bounces when count increases | `UIManager.PopCounter()` |
| Build progress | Progress text bounces on each deposit | `MaterialsData.OnProgressAdded()` |
| Resource respawn | Object spins-down, waits 7s, spins back up | `MaterialsData.Fill()` |

---

## Known quirks

- `targetTransform` in `PlayerController` is a `CharacterController` (the physics ghost). Confusing name — don't rename it without checking all references.
- Wolf clips are embedded in `wolf.fbx`. The separate `idle.fbx`/`run.fbx` in the project use a different rig (`Root` vs `wolf_rig`) and **cannot** be used for the wolf.
- `MaterialsData.canDrop` is a C# property: reads `NetCanDrop.Value` when networked, a local field when solo. Always use the property, never the backing field `_canDropLocal`.
- `StealEffect` and `LobbyUI` both create their own Canvases at runtime — intentional so there's nothing to wire up in the Inspector.
- `MoveBagPos._randompositions` must be assigned in the Inspector. If empty, the bag position never changes.

---

## Packages (manifest.json)

```
com.unity.netcode.gameobjects   2.12.0
com.unity.services.core         1.12.5
com.unity.services.authentication 3.7.1
com.unity.services.relay        1.2.0
com.unity.services.lobby        1.2.2    ← package id is "lobby" (singular)
com.unity.ai.navigation         2.0.13   ← NavMesh
com.unity.cinemachine           3.1.7
com.unity.probuilder            6.0.9
```

⚠️ **Lobby namespace gotcha:** the package id is `com.unity.services.lobby` (singular)
but the C# namespace is `Unity.Services.Lobbies` (**plural**). `RelayServerData` lives in
`Unity.Networking.Transport.Relay`, not in the Relay service package. If you see CS0246 errors
for `LobbyService` / `Allocation` / `RelayServerData`, check those two things first — and confirm
the packages are still in `manifest.json` (Unity silently strips entries with invalid versions).

---

## What's next (Phase 4 / 5)

- **Phase 4** (TODO): Sync Enemy over the network. Spawn wolf via `NetworkManager.SpawnWithOwnership`, run AI only on server, broadcast state via `NetworkVariable<byte>`. Remote clients receive position via `NetworkTransform`.
- **Phase 5** (code done — `MultiplayerHUD.cs`): teammate resource counts on HUD, join code as in-game overlay, "waiting for players" banner. Self-spawns at runtime; hidden in solo play. Remaining optional: gate gameplay until both players are present (currently the game is always live).

## NavMesh note (wolf following)

If the wolf shows "!" but won't follow, the editor logs `pathStatus=PathPartial` —
the player is on NavMesh that isn't connected to the wolf's. **Re-bake** the NavMesh
(Window → AI → Navigation → Bake) covering the whole walkable map. Land the player
unlocks at runtime (`MaterialsData.DeployLand`) is NOT in the baked mesh, so the wolf
can't follow there — keep wolves/resources within the baked starting area, or add a
runtime `NavMeshSurface` rebake in a later pass.
