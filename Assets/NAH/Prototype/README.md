# NAH Unity movement / combat / interaction prototypes

Copied from the owner's `NAH_Research_Code.zip` on 2026-10-31. They had not been compiled there. This repository has not play-tested them yet.

These five C# files are **original example code**, not copied from a third-party repository. They are written for a Unity 6 project with Unity's **Input System**, but **have not been compiled or executed in a Unity Editor** here. They are **LOCAL/OFFLINE PROTOTYPES**, not secure online multiplayer implementations.

## Setup (prototype)
1. Inspect the existing `enobot/nah` repo, Unity Editor version, Input System version, and current architecture. Do **not** overwrite existing scripts.
2. Create an Input Actions asset with an action map for `Move` (Vector2), `Look` (Vector2), `Jump`, `Sprint`, `Attack`, and `Interact` (Buttons). Bind desired keyboard/mouse/gamepad inputs. Add PlayerInput to the player prefab and assign the asset.
3. Put CharacterController and `NAHLocalMotor` on player root; add a child Camera Pivot Transform containing Camera; assign pivot. Assign this Camera to NAHLocalInteractor.
4. Add `NAHLocalHealth`, `NAHLocalMelee`, and `NAHLocalInteractor` to player root. Set Melee Attack Origin child transform, hittableLayers, and environmentOccluders. Give each target colliders (on relevant layers) and `NAHLocalHealth`.
5. Add `NAHLocalHealPickup` and a Collider to any placeholder pickup; ensure interactionMask includes its layer.
6. Test in a throwaway scene. Do not use these scripts in an online session as-is.

## Important caveats
- Health, damage, items, movement, and hit results are LOCAL ONLY. Online PvP needs server-authoritative validation, anti-cheat, lag compensation, state replication, match flow, and reconciliation.
- Current attack is an instantaneous overlap sphere, **not** a finished Soulcalibur-like combo, hitbox timeline, cancel window, shield, critical hit, or animation system. Replace it with a state-driven ability and animation system after approving combat feel.
- Input setup and camera enable/disable must be adjusted for multiplayer per-player ownership. Controller action delta and mouse look sensitivity must be tuned/approved.
- For local split-screen, configure PlayerInputManager, camera association, input pairing, and separate UI per player.
- `GetComponentsInParent` interaction lookup allocates when interaction is pressed; profile if used frequently.
- `OverlapSphereNonAlloc` uses a fixed-size buffer, so it may truncate results. Account for that when building production collision queries.
- Camera clips through geometry and look pitch/gamepad acceleration are not yet tuned. Ground snapping, moving platforms, sliding, climbing, dashing, dodging, knockback, and flying are not included.
- `NAHLocalMotor` should not coexist on the same root with a dynamic Rigidbody-driven motor. Pick the movement/physics integration deliberately.
- No character designs, motion personalities, arena colors, or character rigs are finalized here. Creative approval is required.

## Two-player network arena (gray placeholder)

These files are project code written for this repository. They use Netcode for GameObjects 2.13.3 with direct UDP. There is no Relay, Lobby, or voice chat.

| File | What it does |
| --- | --- |
| `NAHNetworkPlayer.cs` | One fighter. The owner sends input. The host moves the body, checks hits, and changes health, lives, and elimination. A client never chooses the target, the damage, or the position used for a hit. Input with NaN or infinity is dropped. The host computes this fighter's stats and accepts one body choice, only before the fighter has attacked or been hit. The body size is shared, so every screen draws and hits the same capsule. Fighters spawn 10 cm above the floor so a long first frame cannot drop them through it. |
| `NAHStatRules.cs` | Body shape (size, fatness, roundness, each 0 to 1), weapon weight and length, and race and class ratios become stats: max health, armor (absorption), shielding, attack damage, move speed, swing time, reach, and hitbox size. A hit removes the attacker's damage less the share the target absorbs. Every number is a placeholder. |
| `NAHWeaponDefinition.cs` | A weapon's category, weight, length, and base damage, as gameplay data. Heavier hits harder and swings slower; longer reaches farther and swings slower. |
| `NAHRaceDefinition.cs`, `NAHClassDefinition.cs` | Ratios a race or class applies on top of body and weapon. No race or class assets exist yet; the proposals in `GAME_DESIGN.md` wait for approval. |
| `NAHPlaceholderStatRules.asset`, `NAHPlaceholderWeapon.asset` | The placeholder numbers. Tune them here in the Inspector, not in code. The arena builder creates them once and then leaves them alone. |
| `NAHNetworkMatch.cs` | Host-side roster. It decides when the match starts and who wins. |
| `NAHNetworkSession.cs` | Placeholder connect screen (Host, Join, address field) and health/lives text. Not the final UI. |
| `NAHNetworkDemoDriver.cs` | Development builds only. With `--nah-demo`, it plays the local fighter through the same input messages as the keyboard and can save frames for a video. It is a test tool, not an AI opponent. |
| `Editor/NahNetworkArenaBuilder.cs` | Menu **NAH → Create Network Arena**. Rebuilds `NetworkArena.unity` and `NAHNetworkPlayer.prefab`. |
| `Editor/NahBuild.cs` | Menu **NAH → Build Windows Development Player**. Builds only the network arena. |
| `Editor/NahNetworkRulesTest.cs` | Edit Mode tests: a host hit removes exactly what the stat rules say, the last life ends the match, only the local camera and listener are on, the server ignores non-finite input, and a body is chosen once before combat. |
| `Editor/NahStatRulesTest.cs` | Edit Mode tests for the stat rules: bigger bodies get more health, armor, shielding, and damage, move slower, and have bigger hitboxes; heavier weapons hit harder and swing slower; longer weapons reach farther and swing slower; race and class act as ratios; broken data and invalid bodies are rejected; an average body keeps the old prototype numbers. |

### Play it by hand on one PC

1. Build with **NAH → Build Windows Development Player** (output: `Builds/Windows-Dev/NAH.exe`, ignored by Git).
2. Start the exe twice. In the first window press **Host**. In the second keep `127.0.0.1` and press **Join**.
3. Temporary keys: WASD, mouse, Space, Shift, left click, and middle click or Q to lock on.

Command-line options: `--nah-role host|client`, `--nah-address <ip>`, `--nah-body size,fatness,roundness` (three numbers from 0 to 1, until a character creator exists), and `--nah-listen 127.0.0.1` (same-PC tests stay off the network, so Windows does not ask to open the firewall). On two PCs, the host uses the default listen address and Windows may ask the owner to allow the game through the firewall. That choice belongs to the owner.

### Known limits

- No client-side prediction yet. A joining client sees its own movement after a round trip to the host. On one PC that is a few milliseconds; over the internet it will feel late.
- The joining client's own view is not smoothed between network updates (30 per second).
- Input is sent every rendered frame, not at the network tick rate. That wastes bandwidth at high frame rates.
- A fifth player is disconnected after joining instead of being refused by connection approval.
- If the host crashes, a client shows a frozen match for 30 seconds before it notices. That is Unity Transport's default disconnect timeout.
- Shielding is computed but does nothing, because blocking is not built.
- A swing is a box in front of the body, from feet to head, as long as the weapon's reach (fighting-game style, owner decision). Looking up or down does not change a hit.
- Lock-on (middle click or Q, right-stick click on a gamepad; temporary keys) turns only the local camera toward the opponent nearest the center of view. There is no way yet to switch between targets while locked.
- There are no animations yet, so "heavy weapons look slow" is only a slower swing timer for now.
- No bows, spells, or abilities yet. Race and class have no assets, so every fighter uses neutral ratios.

## Suggested integration priority
Study Unity's 2026 U6 First Person Multiplayer reference project, then adopt the relevant network-authoritative techniques (subject to the Unity Companion License) into NAH's actual architecture. Keep the above scripts isolated for offline experimentation or refactor only after checking the repo.

Research links and a detailed prompt are included separately.
