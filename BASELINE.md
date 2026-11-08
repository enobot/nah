# Baseline

This file records the state of the project before major work. Future changes should be compared with the latest baseline, not with memory.

Before a major feature, inspect the project, run the tests that exist, and update this file with what actually happened. Use these result labels:

- PASS: the check ran and succeeded.
- FAIL: the check ran and failed.
- BLOCKED: the check could not run because a specific dependency or environment limit stopped it.
- NOT RUN: the check was not executed.

## Baseline 0: empty repository

Recorded: 2026-10-28

Location: `G:\Projects\Nah`

Remote: `https://github.com/enobot/nah.git`

Branch: `main`

### What exists

- A Git repository with no commits.
- `origin` points at `https://github.com/enobot/nah.git`.
- `git ls-remote origin` exits 0 and returns no branches or tags. The remote has no commits.
- Opening that GitHub URL without a login returns 404. This machine is not logged in to the GitHub CLI. Git itself can reach the remote.

### What does not exist

- No game code.
- No Unity project files.
- No Unity Editor installed on this machine.
- No tests.
- No scenes, characters, combat, or multiplayer.

### Environment facts

- `C:` had about 239 MB free. That is not enough for a Unity Editor install.
- `G:` had about 853 GB free.
- .NET SDK 10.0.103 is installed. It does not replace the Unity Editor.
- Visual Studio Community is present at `G:\VSCommunity`. The Unity workload was not verified.
- Git version: 2.56.0.windows.1

### Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Repository contains source files | PASS | Directory listing showed only `.git`. |
| Git remote is configured | PASS | `git remote -v` showed `origin` for fetch and push. |
| Remote has commits | PASS | `git ls-remote origin` exited 0 with no refs. The absence of commits is the observed result. |
| Unit tests | NOT RUN | No test project exists. |
| Play Mode tests | NOT RUN | No Unity project exists. |
| Editor opens the project | BLOCKED | Unity Editor is not installed. |
| Gameplay | NOT RUN | No game exists. |

### Known problems

1. There is nothing playable yet.
2. The remote has no history, so there is no previous version to roll back to.
3. Low free space on `C:` can break installs that default to that drive.
4. Netcode for GameObjects 2.13.3 is marked pre-release in the Unity 6.3 manual. It is not installed. Do not treat it as a stable dependency until that status is checked again.

### Approved next step

Install Unity Hub tooling and Editor 6000.3.27f1 on `G:`, then create a local URP project in this folder. Do not enable Unity Cloud for the project. Do not add Netcode, analytics, ads, or Steam in this step.

### Documentation added after Baseline 0

`GAME_DESIGN.md` records the game design. The N.A.H. franchise notes (`UNIVERSE_BIBLE.md`, `WORLD_TIMELINE.md`, `REALMS_AND_KINGDOMS.md`, `GODS_AND_MYTHOLOGY.md`, `CHAMPION_LORE.md`, `NEXUS_MYSTERY.md`, `FRANCHISE_ROADMAP.md`) separate canon from proposals. The earlier story files remain proposals. `RESEARCH_POLISH.md` records candidate research. `THIRD_PARTY_ASSETS.md` is the commercial license register. No third-party asset is APPROVED, and none has been imported. No gameplay code has been added. Related tests are NOT RUN.

## Baseline 1: editor installed, not licensed

Recorded: 2026-10-29, before creating the Unity project.

### What exists

- One commit on `main`, `2df978c`, the same as `origin/main`. The working tree was clean before this entry.
- Unity Editor 6000.3.27f1, x86_64, with the Windows module, at `G:\Unity\Editors\6000.3.27f1\Editor\Unity.exe`.
- Unity CLI 1.0.0-beta.14 at `G:\Unity\cli\bin\unity.exe`. It is a beta tool.
- The CLI log shows the first editor install failed with "not enough space on the disk" (os error 112). The second attempt finished with exit code 0.

### What does not exist

- No Unity project, game code, scenes, or tests.
- No active Unity license. `unity license status` reports "none active" and "Signed in: no".
- The Unity Editor Software Terms for 6000.3.27f1 are not accepted. `unity terms status 6000.3.27f1` reports `not-accepted`.

### Environment facts

- `C:` had 14.78 GB free. `G:` had 844.53 GB free.
- The Unity Hub download cache is `C:\Users\dinos\AppData\Roaming\UnityHub\downloads`. It is empty.
- The Package Manager global cache can be moved with the `UPM_CACHE_ROOT` environment variable or `cacheRoot` in the user `.upmconfig.toml`, according to the Unity 6000.3 manual page "Customize the global cache location". It has not been moved yet.

### Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Editor 6000.3.27f1 installed | PASS | `unity editors --installed` lists it with the Windows module. |
| Editor Software Terms accepted | FAIL | `unity terms status 6000.3.27f1` returned `not-accepted`. |
| Unity license active | FAIL | `unity license status` exited 4 with "none active". |
| Create the URP project | BLOCKED | The editor needs accepted terms and an active license. |
| Unit tests | NOT RUN | No test project exists. |
| Play Mode tests | NOT RUN | No Unity project exists. |

### Owner steps before the project can be created

Accepting terms and signing in belong to the owner. Which Unity plan fits a commercial project depends on Unity's eligibility rules, which the owner should read before activating.

1. Accept the Editor Software Terms for 6000.3.27f1.
2. Sign in to a Unity account.
3. Activate a license.

### Next step after that

Create the local URP project in this folder with Unity Cloud off, keep the Package Manager cache on `G:`, then start roadmap step 1: one first-person character that walks, runs, jumps, lands, and stops through named input actions.

## Baseline 2: two-player gray arena on one PC

Recorded: 2026-10-31. Compared with commits `987fd45` and `6259026` on `main`, plus the uncommitted network files found at the start of this pass.

### What existed at the start of this pass

- Unity 6000.3.27f1, URP 17.3.0, Input System 1.20.1. Netcode for GameObjects 2.13.3 was added to `Packages/manifest.json` but not committed.
- `PlaceholderArena.unity`: the local heavy walk from the owner's prototype scripts.
- Uncommitted: `NetworkArena.unity`, `NAHNetworkPlayer.prefab`, `NAHNetworkPlayer.cs`, `NAHNetworkMatch.cs`, `NAHNetworkSession.cs`, the arena builder, and one Edit Mode test.
- That test had last run at 14:05 and FAILED: "Opponent was not eliminated." (`G:\temp\nah-net-results.xml`).
- `Recordings/heavy-local-walk.mp4` was 0 bytes. Only the PNG frames existed.
- No animation, rig, character model, Relay, Lobby, or voice chat.

### Problems found and fixed

1. The test waited with `WaitForSeconds`. In an Edit Mode test that resumes after one editor tick (Unity Test Framework `EditModeRunner.cs` handles only its own yield types), so the opponent never respawned. The wait now polls real time until the respawn refills health. Every assertion is unchanged.
2. **NAH → Create Network Arena** failed the second time in one editor session with a null `NetworkConfig`. NGO creates that config through an editor hook that Play Mode clears until the next script reload (`NetworkManager.cs:32`, `NetworkManagerHelper.cs:47`). The builder now creates the config itself, with the default prefab list the hook would add.
3. The host accepted NaN and infinity in a client's input message. One bad value broke the fighter's rotation on the server. Those messages are now dropped.

### Added

- `NAHNetworkDemoDriver.cs`: development builds only, started by `--nah-demo`. It plays the local fighter through the same input messages as the keyboard and saves frames.
- `Editor/NahBuild.cs`: **NAH → Build Windows Development Player**.
- `--nah-listen` option in `NAHNetworkSession.cs`, so same-PC tests listen on 127.0.0.1 only.
- Two test checks: only the local fighter's camera and audio listener are on, and the server ignores non-finite input.

### Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Edit Mode: a host hit costs health and the last life ends the match | PASS | Unity batch run, 2 of 2 passed, exit code 0. Before the wait fix it FAILED as recorded above. |
| Edit Mode: only the local camera and listener are on | PASS | Same run. |
| Edit Mode: server ignores NaN and infinity in input | PASS | Same run. With the check disabled it FAILED with Unity's quaternion assertion, so the test catches the bug. |
| Windows development build | PASS | 0 errors, 0 warnings, 171.6 MB. First build took 3 min 36 s, the rebuild 21 s. |
| Two separate processes connect over loopback UDP | PASS | Host and client logs both show `phase=Fighting`, then `phase=Finished`. |
| Both fighters move and see each other | PASS | Logged positions change on both sides. Frames show the other gray capsule. |
| Host-decided hits change health and lives on both screens | PASS | Client went 100 → 66 → … → 0 hp, 3 → 0 lives, then OUT, in both logs. |
| Match ends and both windows agree on the winner | PASS | Both logs: "match finished. Winner client 0." |
| Client survives the host process being killed | PASS | Client logged the disconnect and exited by itself with code 0, 30.1 s after the kill. |
| Round trip on one PC (Unity Transport RTT) | Measured | 2–7 ms once joined. Up to 206 ms during the first second after joining. Both windows were saving frames at the time. |
| Frame rate in the development build | Measured, not a benchmark | 135–680 fps per window while saving 10 frames a second. Not a release build and not profiled. |
| Two PCs on a real network | NOT RUN | Only one PC was used. |
| Latency, packet loss, and jitter simulation | NOT RUN | |
| Three or four players | NOT RUN | |
| Unity Profiler capture | NOT RUN | |
| Play by a person with the keyboard | NOT RUN | Every run used the demo driver. |

### Known problems after this pass

| Severity | Problem |
| --- | --- |
| MEDIUM | When the host crashes, a client keeps showing a frozen match for 30 s. That is Unity Transport's default disconnect timeout (`NetworkParams.cs:47`). |
| MEDIUM | No client-side prediction. Over the internet the joining player's own movement will feel late. |
| MEDIUM | The joining client's own view moves in 30-per-second steps because the local body is not smoothed. |
| LOW | Input is sent every rendered frame (hundreds a second here) instead of at the 30 Hz network tick. |
| LOW | A fifth player is disconnected after joining instead of being refused by connection approval. |
| LOW | Running the Edit Mode tests rebuilds `NetworkArena.unity` and the player prefab, so the files change on disk. |
| LOW | The Edit Mode test logs "2 audio listeners" warnings for a moment while the host starts. The standalone logs had none. |

The video of the two-window match is `Recordings/two-player-arena.mp4` (ignored by Git). `Recordings/heavy-local-walk.mp4` now holds the earlier 24 heavy-walk frames at 4 frames a second. Those frames were saved every eighth game frame, so it is not real-time speed.

## Baseline 3: host-side stats from body and weapon

Recorded: 2026-10-31, after commit `8a51ad4`. Built from the owner's rule in `GAME_DESIGN.md`, "Body, weapon size, and stats". All numbers are placeholders.

### What changed

- `NAHStatRules` turns body shape (size, fatness, roundness, each 0 to 1) and weapon size into max health, absorption, shielding, and attack damage. A hit removes the attacker's damage less the share the target absorbs. Absorption is capped at 90 percent.
- `NAHWeaponDefinition` holds a weapon's category, size, and base damage.
- The placeholder numbers live in `NAHPlaceholderStatRules.asset` and `NAHPlaceholderWeapon.asset`. An average body keeps the old prototype's 100 health and 34 damage. Because an average body now absorbs 15 percent, an average fight takes 4 hits per life instead of 3.
- The fixed `MaxHealthValue` and `HitDamage` constants were removed from `NAHNetworkPlayer`. The rules test now reads health from the fighter and checks the exact damage of a hit, which is stricter than before.
- The host accepts one body choice per fighter, only before that fighter has attacked or been hit. NaN and out-of-range values are refused.
- `--nah-body size,fatness,roundness` chooses a body until a character creator exists. The placeholder screen shows health as current/max.

### Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Edit Mode tests | PASS | 12 of 12, exit code 0, no compiler warnings. |
| Tests catch real mistakes | PASS | With the body lock removed and absorption ignored, 3 tests failed with the expected messages ("A second body choice was accepted", health 66.0 instead of 71.1, damage 40 instead of 30). Both mistakes were then reverted and all 12 passed again. |
| Windows development build | PASS | 0 errors, 0 warnings. |
| Two windows: biggest body against smallest | PASS | Host 140 max health, client 60. The client joined at 100 and its body request over the network set it to 60. Big hit small for 39 (34 × 1.15, nothing absorbed). Small hit big for 20 (28.9 with 30 percent absorbed). Both logs agree on the winner. Video: `Recordings/big-vs-small-stats.mp4`. |
| Hitbox follows body size | NOT BUILT | The capsule and hitbox are the same size for every body. |
| Shielding has an effect | NOT BUILT | Blocking does not exist. |

### Known problems added by this pass

| Severity | Problem |
| --- | --- |
| MEDIUM | A big body gets more health and absorption, but it is not a bigger target and has no other downside yet. In the demo the biggest body won without losing a life. |
| LOW | Body choice arrives by RPC right after spawning. A real character select should send it before joining, through connection approval. |

## Baseline 4: size, weapon weight and length, race and class ratios

Recorded: 2026-10-31, after commit `4a725c6`. Built from the owner's second stat decision in `GAME_DESIGN.md`. All numbers are placeholders.

### What changed

- Bigger bodies now move slower (0.75x to 1.25x of the approved heavy walk) and have bigger hitboxes: height follows size, width follows fatness and roundness (0.7x to 1.3x). The capsule, the real collider, and the eye height all follow the body on every screen.
- Weapons have weight and length. Heavier hits harder and swings slower. Longer reaches farther and swings slower. The old `size` value carries over as `weight`.
- Race and class definitions apply ratios to health, armor, damage, move speed, and swing speed. No race or class assets exist yet.
- An average body with the placeholder weapon keeps the old numbers: 100 health, 34 damage, the 2.2/3.4 m/s heavy walk, a 0.45 s swing, 1.5 m reach, and the same capsule.
- Fix: fighters spawned with the capsule touching the floor. When the first frame took 0.33 s, gravity moved a fighter 3.11 m in one step and it fell through the floor. They now spawn 10 cm higher. This bug existed before this pass; the new floor check found it.
- The demo driver now aims up and down at the opponent.

### Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Edit Mode tests | PASS | 17 of 17, exit code 0, no compiler warnings. |
| Tests catch real mistakes | PASS | With bigger bodies made faster, weapon length ignored, and the real collider left unsized, exactly the 3 matching tests failed. All three were reverted and 17 of 17 passed. |
| Floor fall-through fix | PASS | Before: logged height went from 1.0 to −2.11 after one 0.333 s frame, and kept falling. After: the fighter stayed grounded at 1.08 through a 0.30 s first frame. |
| Windows development build | PASS | 0 errors, 0 warnings. |
| Two windows: biggest body against smallest | PASS | Host 140 health, 1.3x hitbox; client 60 health, 0.7x hitbox. Logged speeds: about 1.7 m/s (target 1.65) and about 2.6 m/s (target 2.75), measured from positions logged every 0.5 s. Video: `Recordings/big-vs-small-hitbox.mp4`. |
| Same match before the driver aimed down | Observed | The small fighter won without losing health, because every swing from the giant passed over its head. This is recorded as an open design question about vertical aim. |
| Race and class in a real match | NOT RUN | No race or class assets exist yet. Covered only by unit tests. |
| Heavy swing animation | NOT BUILT | No animations exist yet. |

### Addendum: fighting-game hit volume

Same day, after the owner chose option B. The swing changed from a ball at eye level, which followed the camera up and down, to a box in front of the body. The box runs from below the feet to above the head and is as long as the weapon's reach. Facing and reach still decide a hit; camera pitch does not.

| Check | Result | Evidence |
| --- | --- | --- |
| New test: a giant looking straight ahead hits a tiny fighter in front of it; nobody behind or out of reach is hit | PASS | It FAILED before the change ("missed a tiny fighter standing in front of it") and PASSED after. |
| Edit Mode tests | PASS | 18 of 18, exit code 0, no compiler warnings. |
| Windows development build | PASS | 0 errors, 0 warnings. |
| Two windows: biggest against smallest | PASS | The giant hit the tiny fighter and won with 79 health left. Video: `Recordings/fighting-game-hitbox.mp4`. |
| Lock-on button (option C) | PASS | Built after the push of `23b9320`. See below. |

### Addendum: lock-on button

Option C, chosen by the owner. A `LockOn` action uses temporary keys: middle click or Q, and right-stick click on a gamepad. Lock-on picks the opponent nearest the center of view within 12 m and a 60-degree cone, turns the local camera toward them at up to 360 degrees a second, and releases on a second press, when the target is out, or beyond 16 m. It changes only the yaw and pitch the owner already sends; the host still decides every hit.

| Check | Result | Evidence |
| --- | --- | --- |
| New test: lock on to a fighter 45 degrees to the right, face them, release, break at 40 m, refuse a fighter behind | PASS | With the camera turning switched off, the test FAILED ("did not turn the fighter to face the target", 45 degrees). Restored, it passed. |
| Edit Mode tests | PASS | 19 of 19, exit code 0, no compiler warnings. |
| Windows development build | PASS | 0 errors, 0 warnings. |
| Two windows, both locked on | PASS | Both overlays show "Locked on". Both logs agree on the winner. The client used the new after-match disconnect exit. Video: `Recordings/lock-on.mp4`. |
| A person pressing the real keys | NOT RUN | Every run used the demo driver. |

### Addendum: target switching and placeholder races and classes

After the push of `fff0a16`. The owner said yes to switching lock-on targets, and asked for placeholder races and classes with suggested names, to be chosen later.

- Lock-on: a quick sideways flick of the mouse or stick (25 degrees of turn) switches to the nearest opponent on that side. A failed switch keeps the current target.
- `NAHRoster` lists the races and classes in a fixed order. `Assets/NAH/Prototype/Roster/` holds 12 placeholder races (Human, Elf, Dwarf, Bear, Wolf, Goblin, Troll, Fairy, Flower Folk, Dog, Duck, Chicken) and 8 placeholder classes (Oathwarden, Dawnhammer, Skypiercer, Veilstalker, Wrathborn, Aetherweaver, Lifesinger, Wildspeaker). The class names were picked from a small word-root generator.
- A player sends body, race, and class together as one choice. The host refuses out-of-range indices and broken ratios, then locks the choice like the body choice. Every screen shows each fighter's race and class.

| Check | Result | Evidence |
| --- | --- | --- |
| New test: switch left, refuse a switch with nobody further left, switch right | PASS | With the side reversed, it FAILED ("Switching left found nobody"). |
| New test: race and class lookup, refusal of bad indices, ratios applied, choice shared, RPC path | PASS | With the ratios not applied, it FAILED (health 100 instead of 110). The test reads roster entries by position, so renaming them will not break it. |
| Edit Mode tests | PASS | 21 of 21, exit code 0, no compiler warnings. |
| Windows development build | PASS | 0 errors, 0 warnings. |
| Two windows: Troll Oathwarden (biggest body) against Fairy Veilstalker (smallest body) | PASS | Health 193 (140 × 1.20 × 1.15) and 38 (60 × 0.75 × 0.85), matching the formulas. The overlay shows both names. Video: `Recordings/troll-vs-fairy.mp4`. |
| Target switching by a real mouse or stick flick | NOT RUN | Only the switch itself was tested. |

### Addendum: shield and Block

After the push of `31af80b`. Owner decision: shielding blocks a certain amount of damage, then goes into cooldown. A `Block` action uses temporary keys (right click, left bumper). While a fighter blocks and faces the attacker (120-degree cone), hits go into the shield first. The shield amount is the Shielding stat, now 20 to 60 by body size plus 40 for a carried shield (placeholders; the old 0 to 30 broke on any hit). A hit bigger than what is left breaks the shield, and the rest goes through armor to health. A broken shield cools down for 3 seconds, then comes back full. A damaged, unbroken shield refills at 10 a second after 1.5 seconds without a hit. A blocking fighter cannot swing.

| Check | Result | Evidence |
| --- | --- | --- |
| New test: shield takes a whole hit, breaks on the next with the rest going through armor, does not block while broken, refills after cooldown, does not block from behind, blocking fighter cannot swing | PASS | Its first run FAILED with a real bug: the shared "blocking" flag stayed on for one frame after the shield broke. The flag is now cleared at the moment of the break. |
| Edit Mode tests | PASS | 22 of 22, exit code 0, no compiler warnings. |
| Windows development build | PASS | 0 errors, 0 warnings. |
| Two windows: Human Wrathborn against a blocking Dwarf Oathwarden | PASS | The overlay shows "shield 40/40 (blocking)", then "shield broken" after one 42.5 hit, with health 127 to 125 (2.5 × (1 − 0.225) went through armor). Video: `Recordings/shield-block.mp4`. |
| A person holding the real Block key | NOT RUN | Every run used the demo driver. |

## Baseline 5: network stability and four players

Recorded: 2026-11-01, after commit `b9a142a`. This pass works on Phase 3 of the master directive ("expand toward four players", "improve online stability") and on four known problems from Baseline 2. No artistic choices were made.

### What changed

- **A fifth player is refused before joining.** The host now uses Netcode's connection approval. A player who would be the fifth gets "This arena holds 4 players." and no fighter is made for them. The old kick-after-joining stays as a backup, because two players approved in the same frame can both see one free place.
- **Disconnect timeout: 30 s, owner decision.** A 10 s timeout was tried first (the client noticed a crashed host 10.4 s after it stopped). The owner chose 30 s on 2026-11-01, the same as Unity Transport's default; it is now set explicitly and tested. A joining player's screen now says why the connection ended.
- **Movement and aim go out 30 times a second, not every frame.** That is the network tick rate, the same rate the host sends positions back. Moments where a wait would be felt still go out at once: pressing or releasing Block or Sprint, starting or stopping to move, and a jump or attack press.
- **Development builds can simulate a bad connection.** `--nah-lag delay,jitter,drop` (for example `75,15,3`) delays this peer's packets in both directions and drops a share of them. Netcode's own simulator setting no longer does anything in version 2.13.3, so this uses Unity Transport's simulator stage through the transport's documented driver hook. No package was added. Release builds do not contain it.
- The demo driver logs input messages per second, measures how long a fighter's own walk takes to show on its screen, and quits by itself when the host refuses it.

### A bug found and fixed during this pass

The first version of the lag simulator applied every delay twice. A setting of 75 ms each way gave median round trips of 303 to 352 ms in the game and 345 to 440 ms in a direct test, instead of about 150 ms. Cause: in the editor and in development builds, Netcode's `UnityTransport.cs` already puts a simulator stage in every default pipeline (its lines 4 and 5), and the hook added a second one. A temporary diagnostic test (since deleted) showed the added time was 2× the setting in every mode: about 150 ms extra for 75 ms on sends only, about 400 ms for 200 ms. The hook now adds the stage only where it is missing. The direct test then measured a median round trip of 3 ms with no lag and 150 ms with 75 ms each way.

### Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Edit Mode tests | PASS | 28 of 28, exit code 0, no compiler warnings. Six are new: the fifth-player rule, host approval and the disconnect timeout, input once per tick, reading lag settings, the direct lag measurement, and a host starting with the simulator. |
| Tests catch real mistakes | PASS | Nine rules were broken on purpose, one at a time or in groups that cannot hide each other. Each matching test failed with the expected message, for example "Input went out 2004 times in 32 ticks (2004 frames)", "A fifth player was let in", "Expected: 10000 But was: 30000" (during the 10 s trial), "Starting to walk waited for the next tick", and "The simulator added much more than the delay in both directions … was 299". The files were restored from backups and checked by hash. One attempt was invalid: a script error meant the file was not changed, so the tests ran on correct code. It was redone and the redo failed as expected. |
| Windows development build | PASS | 0 errors, 0 warnings, 171.6 MB. |
| Input rate, two windows, no lag | PASS | Median 30 to 32 input messages a second per window, while each window drew a median of 384 to 418 frames a second. Before, one message went out per frame. Round trip median 6 ms. |
| Two windows, client at 75 ms each way, ±15 ms, 3% loss | PASS | Round trip median 185 ms (lowest 150, highest 329). The match finished and both windows logged "Winner client 0". Video: `Recordings/lag-75ms.mp4`. |
| How late a fighter sees its own walk start | Measured, 2 samples each | Host: 86 to 110 ms over both runs (no network; this is the time the heavy body takes to cover 2 cm). Client on one PC: 239 to 307 ms. Client at 75 ms each way: 368 to 375 ms. The client is about 130 to 220 ms later than the host even on one PC, more than one network tick explains. The cause is not known yet. |
| Four windows, one host and three clients | PASS | All four joined, fought, and logged "Winner client 0". With four windows saving frames on one PC, round trip medians were 23 to 49 ms (highest 420 ms) and frame rate medians 113 to 155. Video: `Recordings/four-players.mp4`, aligned on the match end. |
| Fifth window | PASS | The host logged "refused a player. This arena holds 4 players." The fifth window logged the same reason, never got a fighter, and quit by itself. |
| Host crash, 10 s trial | PASS | The host was force-stopped 14 s into a match. The client logged a protocol timeout and quit 10.4 s later (Baseline 2: 30.1 s). |
| Host crash, 30 s owner decision | PASS | Rebuilt with the 30 s timeout (0 errors, 0 warnings) and the tests rerun (28 of 28). The host was force-stopped 14 s into a match; the client logged a protocol timeout and quit 30.5 s later. |
| Two PCs, real internet | NOT RUN | One PC only. The lag simulator is not a real network. |
| Play by a person with the keyboard | NOT RUN | Every run used the demo driver. |
| Unity Profiler capture or release build | NOT RUN | |

### Known problems after this pass

| Severity | Problem |
| --- | --- |
| MEDIUM | No client-side prediction. Measured above: a joining player sees their own walk start 240 to 310 ms after the input on one PC, and about 370 ms at a 150 ms round trip. |
| MEDIUM | The joining client's own view moves in 30-per-second steps (unchanged). |
| Owner decision | A crashed host leaves the other screens frozen for about 30 s before they notice. The owner chose this timeout on 2026-11-01. |
| LOW | The disconnect message shows Netcode's raw text, for example "[Disconnect Event][Client-1]…[ProtocolTimeout] Connection closed due to timed out." Placeholder screen. |
| LOW | Running the Edit Mode tests rebuilds `NetworkArena.unity` and the player prefab (unchanged). |
| INFORMATIONAL | Unity development builds announce themselves on the local network for the Profiler ("Player connection … [IP] 192.168.1.152" in the logs), even when the game itself listens only on 127.0.0.1. Release builds do not. |
| Not built | Owner decision 2026-11-01: players may not join a match that has already started. Today they can; in the four-window test the third and fourth players joined during the fight. The fight starts as soon as a second player arrives, so a start rule (open question for the owner) is needed before joins can be closed. |

Resolved from Baseline 2: input sent every frame, and a fifth player being kicked after joining instead of refused.

### Addendum: Ready and Start

After the push of `fee0882`. Owner decisions on 2026-11-01: nobody joins a match that has already started, the host starts the match by pressing Start, and everyone must ready up first. "Everyone" was read as including the host.

- The fight no longer starts by itself when a second player arrives. Each player presses **Ready up**; the host's **Start match** works only with at least two players and everyone ready, and otherwise says who is not ready. Both buttons are placeholders on the gray connection screen.
- Once started, the host refuses new players during connection approval with "This match has already started." A backup kick covers a player approved just before Start.
- Prototype behavior added to support this, changeable by the owner: nobody can be hurt before Start; Start puts everyone back at their spawn point at full health; readiness cannot change after Start.
- Four existing tests now ready everyone and press Start before their hits, because hits only count in a started match. Their assertions did not change.
- **Bug found and fixed:** fighters barely moved at very high frame rates. Unity's character controller drops any move shorter than its minimum move distance, which defaulted to 1 mm. In the editor test at about 2,500 frames a second, a fighter speeding up moved less than that per frame, and walked 0.14 m in 0.6 s instead of about 0.9 m. Unity's documentation says this value should be left at 0 in most situations; the arena builder and the player prefab now use 0. The local offline prototype scene (`PlaceholderArena.unity`) still has the 1 mm default and was not changed.

| Check | Result | Evidence |
| --- | --- | --- |
| Edit Mode tests | PASS | 30 of 30, no compiler warnings. New: a join rule test for started matches, and a test for Start and Ready (start refused alone and with anyone not ready, no hits while waiting, walking while waiting, reset to spawn, refused join and frozen readiness after Start, hits after Start). |
| A new test's first runs | FAIL, then fixed | First run: the test compared against the wrong spawn point, because the test helper that moves a fighter also moves its spawn point (a test mistake). Second run: "The host walked only 0.13 m", which was the 1 mm bug above. |
| Tests catch real mistakes | PASS | Six rules broken one at a time; each matching test failed: "The host walked only 0.14 m in 0.6 s (1486 frames)" with the 1 mm setting, "The match started by itself", "A player joined a fight in progress", "A swing hit someone before the match started", "A match started with nobody ready", "The host did not start at its spawn point", "Readiness changed after the start". Files restored and checked by hash. |
| Windows development build | PASS | 0 errors, 0 warnings. |
| Two windows | PASS | Both readied, the host logged "pressed Start with 2 players, all ready", both logged "Winner client 0". |
| Late join | PASS | A third window was started 18 s in, after Start. The host logged "refused a player. This match has already started."; the window logged the same reason, never got a fighter, and quit; the match finished normally. |
| Four windows and a fifth | PASS | The host pressed Start only once all four were in and ready. All four logged "Winner client 0". The fifth window, started after Start, was refused with "This match has already started." Video: `Recordings/ready-and-start.mp4`. The first recording showed the host's waiting screen too short to show the Leave button; the screen was made taller and the run recorded again. |
| Own walk delay after the 1 mm fix | Measured, 2 samples each | Host 80 to 81 ms (was 86 to 110). Client 216 to 261 ms on one PC (was 239 to 307) and 379 to 383 ms at 75 ms each way. The client's extra delay is still not explained. |
| A person clicking Ready and Start | NOT RUN | Every run used the demo driver. |

| Severity | Problem |
| --- | --- |
| LOW | There is no rematch: after a match ends, every window must be restarted. A started match cannot be cancelled. |
| LOW | The local offline prototype scene keeps the 1 mm minimum move distance. |

Resolved from the list above: players joining a match that has already started.

### Addendum: own-movement prediction and smoothing fix

Same day, not committed yet. This works on the MEDIUM problem above: a joining player saw their own walk start 216 to 261 ms after the input even on one PC, while the host saw its own after about 80 ms. Phase 3 of the master directive asks for "Appropriate prediction and reconciliation" and "Interpolation for remote players". No artistic choices were made.

**Cause.** Netcode's network transform smooths a body by showing it slightly in the past. On a joining player's screen it draws the host's updates about 50 ms behind (its server time buffer) plus one network tick, then eases toward them over up to 0.1 s more. The player script turned that smoothing off for the local fighter on each screen. But Netcode copies the host's setting to every other screen and overrides a local change; the field's own documentation says non-authoritative instances "will be overridden by the next authoritative state update" (`NetworkTransform.cs`, the `Interpolate` field, and `ApplyUpdatedState`). So the setting did the opposite of what was intended: a joining player's own fighter was smoothed, which delayed it, and the host's own fighter was not smoothed for anyone else.

**Correction to Baselines 2 and 5.** Both said the joining client's own view moved in 30-per-second steps. That was assumed from the code, not measured, and it was wrong. Measured on 2026-11-01 (run26b): while walking, the client's own view moved 178 to 570 frames a second (smoothed), and the host's fighter on the client's screen moved 0 to 8 frames a second (steps).

**Evidence before the fix.** The demo driver gained a walk test (`--nah-walk-test N`: stand still, walk until the camera moves 2 cm, repeat) so one run gives 16 samples per window instead of 2. The first version counted a slow drift of the smoothed view as walking (three client samples of 38 to 50 ms); stillness now means staying within 2 mm of one point, and the run was repeated.

| Run, one PC, 16 walks per window | Host's own walk shows after | Client's own walk shows after |
| --- | --- | --- |
| Code as it was (run26b) | 80 to 83 ms | 234 to 341 ms, median about 250, settling near 235 as Netcode's clock settles |
| Temporary experiment: smoothing off for every fighter on the host (run26c, reverted) | 81 to 83 ms, one 150 | 85 to 104 ms, median 99 |

So smoothing explained about 135 to 150 ms of the client's delay. The remaining 15 to 20 ms over the host is the wait for the next network tick and the trip over loopback.

**Options considered.**

1. Turn smoothing off for every fighter. Fast (the experiment above), but every screen would see the other fighters move in 30-per-second steps. Rejected.
2. Netcode's built-in `AnticipatedNetworkTransform`. It is in the installed package, but it turns off Netcode's buffered smoothing for all viewers, so the other fighters would need hand-written smoothing. Not chosen now.
3. Let each player's game move its own fighter and tell the host where it is. Instant, but then a client chooses the position used for hits, which the directives forbid. Rejected.
4. **Chosen:** the joining player predicts only its own view. Netcode's smoothing stays on for everyone else. The host still moves the real body and decides every hit.

**What changed.**

- The player script no longer touches the smoothing setting, so the prefab's setting (on) reaches every screen. Every screen now smooths every other fighter, the host's included.
- The movement rules are one method used by both the host's simulation and the prediction, so the two cannot drift apart in code.
- Input messages carry a number. The host ignores a message older than the newest one it has used; before, an older unreliable message arriving late could undo newer input.
- On a joining player's screen, a hidden local capsule of the same size moves every frame with the input that was sent to the host, and the camera rides on it. The networked body still follows the host, and the other screens see that body.
- Each network tick the host sends that player its fighter's position and speed, the newest input number it had used, and for how long. The player's screen finds its own prediction for that same moment (`NAHMotionHistory.cs`) and corrects only the difference. Small corrections are spread over 0.1 s. One over 1 m is shown at once, for example the return to the spawn point when the match starts after a long walk. While the fighter is down or out, the view follows the networked body, and the prediction starts over when the fighter can move again.
- Only the owner receives its fighter's speed ratio, which the prediction needs.

**A bug found and fixed during this pass.** The first version took the host's state as the truth whenever a report could not be matched, and cleared its record. Over a slow connection the host reports an input sent a round trip ago, which a freshly cleared record never holds, so every report cleared it again. With 75 ms each way the client logged zero corrections for a whole match and saw its walk after 211 to 253 ms: the prediction never ran. On one PC the round trip is shorter than a tick, which hid the problem. A new test reproduced it ("A report older than the record moved the prediction 0.48 m away from the body"). Now a report older than the record is skipped. If none matches for about 2 s of reports, the prediction starts over from the host.

| Check | Result | Evidence |
| --- | --- | --- |
| Edit Mode tests | PASS | 35 of 35, no compiler warnings. New: the prediction record finds the host's moment (without a network), every fighter is smoothed on other screens, an older input cannot undo a newer one, a predicted walk of a slower-than-average body ends within 1 cm of where the host moved it, a 20 cm correction is spread out and a 2 m one is shown at once, and a report older than the record is skipped. Two existing tests call the input message directly and now pass its number; their checks did not change. |
| Tests catch real mistakes | PASS | Eight rules broken in six runs (rules checked by different tests were broken together). Each matching test failed: "The host's own fighter is not smoothed", "An older input undid a newer one: the fighter moved -0.70 m forward", "A corrected disagreement was counted again", "the prediction was 0.039 m from where the host moved the body" (prediction at average speed) and 0.038 m (speed ratio not shared), "The view jumped 0.200 m in one frame", "A 2 m correction was not counted as a jump", "Wrong disagreement along x" (no blending in the record). Files restored and checked by hash. |
| Windows development build | PASS | 0 errors, 0 warnings. |
| Own walk delay, one PC, 16 walks per window (run28a) | PASS | Client 77 to 85 ms, median 82 (was 234 to 341). Host 79 to 84 ms. |
| Own walk delay, client at 75 ms each way, ±15 ms, 3% loss (run28b) | PASS | Round trip median 153 ms. Client 67 to 88 ms, median about 80 (was 379 to 383). Some samples are below the host's 82 ms, probably because corrections shift the view slightly; not checked. |
| Other fighters smoothed | PASS | While the host walked, its fighter on the client's screen moved up to 193 frames a second (was 0 to 8). |
| Size of corrections, largest per half second | Measured | One PC: up to 0.5 cm walking alone, 0.2 to 2.1 cm while fighting. At 75 ms each way: up to 1.2 cm walking alone, 1.2 to 11 cm while fighting. Four windows: up to 18.1 cm while fighting. No jumps in any run. |
| Four windows and a fifth (run29) | PASS | Start after all four were ready, all four logged "Winner client 0", the fifth was refused with "This match has already started." |
| Video | Recorded | `Recordings/prediction-lag.mp4`: host and client side by side, client at 75 ms each way, frames captured at 30 a second (new `--nah-capture-fps`). |
| Two PCs, real internet | NOT RUN | One PC only. |
| Play by a person with the keyboard and mouse | NOT RUN | Every run used the demo driver. How corrections feel is not judged. |

| Severity | Problem |
| --- | --- |
| MEDIUM | On a joining player's screen the prediction capsule passes through other fighters, because their controllers are off there; on the host the bodies collide. Pushing into another fighter therefore gives repeated corrections, up to 18 cm per half second in a crowded four-player fight. |
| MEDIUM | No lag compensation. Other fighters are shown slightly in the past on every screen (Netcode's smoothing), and the host decides hits at its own current positions. Over a slow connection, a swing that looks like a hit can miss. |
| LOW | Jumps are predicted, but the host decides them. If the host refuses a jump (for example, not on the ground there), the view corrects during the jump. Not measured. |
| LOW | Host walk samples have rare outliers: 109 to 190 ms in several runs, and once 29 ms in the half second the client joined (run28b). The host does not run the prediction. Not investigated. |
| INFORMATIONAL | The host log shows "Cannot find matching control scheme" once per run when the second fighter spawns. Present before this pass (run22). |

Resolved from Baseline 5: no client-side prediction, and the claim that the joining client's own view moved in steps (it was smoothed; see the correction above).

### Addendum: the prediction collides with other fighters

Found uncommitted in the working tree on 2026-11-02. File times show it was written on 2026-11-01, after the addendum above, for its first MEDIUM problem. No artistic choices were made.

**What changed.**

- On a joining player's screen the other fighters' character controllers now stay on. Only the host moves bodies with them; on that screen they are solid obstacles for the prediction. The player's own networked body keeps its controller off, because it trails the prediction and would block it.
- Before each prediction step, the screen hands the other fighters' current places to the physics engine (`Physics.SyncTransforms`), because the network moves them by setting their transforms.
- The demo driver gained `--nah-push`: keep walking into the opponent, to measure corrections while two bodies push.

| Check | Result | Evidence |
| --- | --- | --- |
| Compile check | PASS | Both prototype assemblies (runtime, and Editor with the tests) compiled with the Roslyn compiler that ships with Unity 6000.3.27f1, against the editor's and the project's package assemblies. No warnings other than CS0649 for fields assigned in the Inspector. Every commit made from the work found uncommitted was checked the same way. |
| Edit Mode tests | NOT RUN | Unity on this PC has no activated license; batch mode stops with "No valid Unity Editor license found. Please activate your license." The test file now has 37 tests. The last recorded full run (35 of 35) was before this change. |
| What the two new tests can show | Limited | `ControllersAreOnWhereTheyShouldBe` checks only the on/off rule. `PredictionStopsAtAnotherFighterLikeTheHost` runs the prediction on the host, where every controller was already on, so it would probably pass without this change too; it does check that the prediction stops where the host body stops. Only a run with a separate joining window covers that player's screen. |
| Windows build, two and four windows with `--nah-push` | NOT RUN | Needs the Unity license. |

| Severity | Problem |
| --- | --- |
| MEDIUM | Not verified: whether a joining player's corrections while pushing are smaller now. That screen collides with other fighters as it shows them, slightly in the past, while the host uses where they are now, so some correction remains whenever either one moves. |

Changed from the list above: the prediction passing through other fighters is addressed in code but not verified.

### Addendum: smoothing and timing switches in the demo driver

Found uncommitted with the change above. These are development-build test switches for the next steps, choosing how other fighters are smoothed and measuring how far in the past each screen shows them (the rewind the owner's middle-ground hit rule needs). They change no gameplay. `--nah-smoothing type,lerp,seconds` changes Netcode's smoothing of the other fighters on one screen; `--nah-track file` writes positions on a clock shared by every window on one PC; the log adds the opponent's "other stalls" share. See the prototype README.

| Check | Result | Evidence |
| --- | --- | --- |
| Compile check | PASS | As above. |
| Used for a recorded result | NO | A host-side track file from one run, dated 2026-11-01, was left untracked in the project root. No matching file from a second window was found, and no conclusion was recorded. |

## Baseline 6: new PC, before Unity is activated

Recorded: 2026-11-04, at commit `d1a8bc0` on `main`, the same as `origin/main`. This pass only inspected and compiled. No project file changed except this entry.

### What changed since Baseline 5

- The project moved to a new PC. The drive that was `G:` is now `E:`. The project is at `E:\Projects\Nah` and the editor at `E:\Unity\Editors\6000.3.27f1`.
- On 2026-11-04 the Git history was rewritten to edit commit messages only. Files, authors, and dates are unchanged, but every commit got a new hash. The hashes in Baselines 0 to 5 (for example `2df978c`, `987fd45`, `8a51ad4`, `b9a142a`) belong to the old history and are no longer on `main`. The old history is saved in `.git/backup-before-history-clean.bundle`.
- Two commits since the last full test run: `0b44e8d` (the prediction collides with other fighters) and `e817d62` (the demo driver's smoothing and timing switches). Both are described in the last two addenda of Baseline 5.

### Environment facts

- `Unity.exe` 6000.3.27f1 is present. Unity Hub is not installed.
- `E:\Unity\cli\bin\unity.exe license status`: "License: none active", "Signed in: no".
- `unity terms status 6000.3.27f1`: "No installed editor found for version 6000.3.27f1". The CLI still looks for the editor at its old path.
- Git refuses this folder ("detected dubious ownership"), because the folder belongs to the old PC's Windows account. Every Git command in this pass used `git -c safe.directory=E:/Projects/Nah`. No Git user name or email is set on this PC.
- The generated `.csproj` files, which Git ignores, still point at `G:\`. Unity regenerates them when it opens the project. `Library/` was copied from the old PC.
- Free space: `C:` 1.7 TB, `E:` 852 GB.

### Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Working tree and remote | PASS | `git status -sb` showed `## main...origin/main` and nothing else. `git ls-remote origin` returned `d1a8bc0` for `main`. No stash. |
| Compile `Nah.Prototype` (17 files) | PASS | The Roslyn compiler that ships with 6000.3.27f1 (`Editor\Data\DotNetSdkRoslyn\csc.dll`), using the references, defines, and source generators from the generated `.csproj` with `G:\` mapped to `E:\`, the package assemblies in `Library\ScriptAssemblies`, and the source list from `git ls-files`. Exit 0, 0 errors, 13 warnings, all CS0649 (fields assigned in the Inspector). |
| Compile `Nah.Prototype.Editor` (6 files, including both test files) | PASS | Same method, against the assembly built above. Exit 0, 0 errors, 0 warnings. |
| The compile check reports a real error | PASS | A temporary file outside the project assigned an `int` to a `NAHNetworkPlayer`. The compiler exited 1 with CS0029. |
| Edit Mode tests | BLOCKED | No activated Unity license. 37 test cases exist: 23 in `NahNetworkRulesTest.cs`, 14 in `NahStatRulesTest.cs`. The last full run was 35 of 35 in Baseline 5, before `0b44e8d` and `e817d62`. |
| Netcode code processing (IL post-processing) | NOT RUN | The compile check does not run it. Only Unity does. |
| Windows development build, two and four windows with `--nah-push` | BLOCKED | Needs the license. |
| Two PCs, real internet | NOT RUN | |
| Play by a person with the keyboard and mouse | NOT RUN | |
| Unity Profiler capture or release build | NOT RUN | |
| Performance | NOT RUN | Nothing was measured in this pass. |

### Known problems after this pass

All problems from Baseline 5 and its addenda remain. The two that matter most: whether the prediction's corrections are smaller while pushing into another fighter is not verified, and there is no lag compensation.

| Severity | Problem |
| --- | --- |
| BLOCKING | Tests and builds cannot run on this PC until the owner signs in to Unity, activates a license, and accepts the Editor terms for 6000.3.27f1. Those steps belong to the owner, as in Baseline 1. |
| LOW | Git refuses the folder until it is marked safe, and no commit identity is set. |
| LOW | The Unity CLI does not find the editor at its new path. |
| LOW | Commit hashes in Baselines 0 to 5 refer to the history before 2026-11-04. |

### Next step

1. Owner: install Unity Hub, sign in, activate a license, and accept the Editor terms.
2. Run the Edit Mode tests. The run rewrites `NetworkArena.unity` and the player prefab (Baseline 2), so check that diff before keeping or restoring it.
3. Build a Windows development player and run two and four windows with `--nah-push` to measure corrections while fighters push into each other.

### Addendum: pose record for the middle-ground hit rule

Recorded: 2026-11-07, after commit `ff222b9`. This is the first part of the owner's middle-ground rule for hits across network delay (`GAME_DESIGN.md`, "Multiplayer and security", 2026-11-01). It is not wired in: hits are still decided at current positions. No artistic choices were made.

- `NAHPoseHistory.cs` records where a fighter stood and which way it faced, keeps about one second, and gives the pose at any moment in that time, blended between the two poses around it. Facing blends the short way round. `RewindTime` limits how far back a swing may be checked: never past the limit, never into the future, and no rewind at all for a broken report or a limit that is zero or not a number.
- `Editor/NahPoseHistoryTest.cs` adds 10 Edit Mode tests. The project now has 47 test cases.
- Unity could not run to create the two `.meta` files, so they were written in the same two-line form as the existing scripts' `.meta` files, with new IDs checked against the 79 already in `Assets`.

| Check | Result | Evidence |
| --- | --- | --- |
| Compile both assemblies | PASS | Same method as in Baseline 6, with untracked files included. `Nah.Prototype` 18 files, `Nah.Prototype.Editor` 7 files, 0 errors. The same 13 CS0649 warnings, no new ones. |
| Pose record tests, run outside Unity | PASS | 10 of 10. The production file and its test file were compiled together with Unity's Roslyn against the editor's `UnityEngine.CoreModule.dll` and the project's Unity NUnit (`com.unity.ext.nunit`). A small reflection runner, kept outside the repository, ran them on the .NET 6.0.21 runtime that ships with the editor. This works only for plain-logic tests and is not a Unity test run. |
| Tests catch real mistakes | PASS | Nine rules were broken one at a time in a scratch copy: facing blended the long way, late poses accepted, no blending between poses, rewind past the limit, rewind into the future, old poses never dropped, no safety limit, a clear that did nothing, broken positions accepted. Each made the matching test fail, for example "Facing turned the long way round, through 180 degrees" and "A rewind went past the limit". Two first attempts were invalid, because the search text did not match the file's line endings and nothing changed; they were redone and failed as expected. The project file was never edited and was checked unchanged afterwards. |
| Edit Mode tests in Unity (47) | BLOCKED | No activated Unity license. |
| Performance | NOT RUN | Nothing calls the record yet. |

| Severity | Problem |
| --- | --- |
| Owner decision | The rewind limit was not chosen at the time of this entry. Resolved 2026-11-08: the owner chose 150 ms, to be tuned (`NAHPoseHistory.RewindLimitSeconds`). |
| Not built | The host does not record poses yet, the attacker's screen does not report which host time it was showing, and the swing check does not use past positions. That wiring changes live network behavior and needs Unity to test. |
| Not built | How "the defender was blocking on their own screen when the hit lands" maps onto what the host knows has not been worked out in code. |

Follow-up, 2026-11-08: the owner chose 150 ms. `NAHPoseHistory.RewindLimitSeconds` holds it, and a new test checks that a screen 120 ms behind is checked where it showed the target while one 200 ms behind is held to 150 ms. The project now has 48 test cases. Compile: PASS, 0 errors, no new warnings. Pose record tests outside Unity: PASS, 11 of 11. Tests catch real mistakes: PASS, the nine earlier breaks plus a 100 ms and a 200 ms limit each failed a test. Edit Mode tests in Unity: BLOCKED, no license.

## Baseline 7: Unity runs on the new PC

Recorded: 2026-11-08, after commit `ac6f647`. No artistic choices were made.

### What changed

- The owner signed in to Unity, activated Unity Personal, and accepted the Editor terms for 6000.3.27f1, all with the Unity CLI (`E:\Unity\cli\bin\unity.exe`). Nothing was downloaded: the editor already on `E:` was registered with `unity editor add`, and `unity editors verify 6000.3.27f1` reported the editor and its Windows module as ok. The CLI keeps that registration per user profile, so it must be done from the profile that runs the CLI.
- The first run on this PC found one failing test, described below. With the owner's approval on 2026-11-08, the test's turn input changed from a fixed amount per frame to 90 degrees a second. Its assertions did not change.

### The failing test

`OwnPredictionMatchesTheHostAndCorrectsSmoothly` failed with "The host walked only 0.11 m in 0.6 s (3511 frames)", below its 0.2 m check.

- Reproduced: two more runs failed the same way (0.16 m both times).
- Not caused by `0b44e8d`: the same test failed twice (0.10 m) in an exported copy of `cdaaab7`, the commit before it, whose 35 tests all passed on the old PC in Baseline 5.
- Cause, measured in an instrumented copy outside the repository: the test turned the view 0.2 degrees on every frame, so the turn speed followed the frame rate. This PC runs the Edit Mode tests at about 5,600 frames a second, so the fighter turned 640 degrees in 0.6 s. The heavy walk accelerates at 6 m/s² toward where the body faces, and could not build speed while facing spun that fast: the path was 0.206 m long and ended 0.127 m from the start. With the turn removed, on the same PC at the same frame rate, the fighter walked 0.820 m and the whole test passed.
- The game itself does not have this problem: mouse look uses the pixels moved, and gamepad look uses degrees per second times the frame time (`NAHNetworkPlayer.cs`, `ReadLocalInput`).
- The input-rate test also turns a fixed amount per frame, on purpose ("as it does under a moving mouse"). It only counts messages per tick, so it was left unchanged.

### Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Edit Mode tests, first run on this PC (48) | FAIL | 47 of 48, exit code 2, 121 s including the reimport after the move. The one failure is described above. |
| The fixed test alone | PASS | Two runs. The host walked 0.79 m. |
| Edit Mode tests after the fix (48) | PASS | 48 of 48, exit code 0, 79 s. No compiler errors or warnings in the Unity log. |
| The fixed test catches real mistakes | PASS | Three breaks of the prediction, each in a copy outside the repository, each failed the 1 cm prediction check: predicting with the view's own turn instead of the turn sent to the host (0.026 m apart), ignoring the turn (0.410 m), and ignoring the body's speed ratio (0.086 m). The unbroken copy passed. The real player script was checked unchanged. |
| Pose record tests in Unity | PASS | Part of the 48. They had only run outside Unity before. |
| `0b44e8d`, prediction bumping into other fighters | PARTIAL | Its two tests pass. As Baseline 5 noted, they run on the host, where every controller was already on, so only a joining window shows the change. |
| Scene and prefab rewritten by the test runs | Restored | With every file ID and the Netcode object hash replaced by a placeholder, both files matched the committed versions line for line, so the changes were only renumbering and reordering. Both were restored. |
| Windows development build, two and four windows with `--nah-push` | NOT RUN | |
| Two PCs, play by a person, Unity Profiler, release build | NOT RUN | |
| Performance | NOT RUN | The 5,600 frames a second is the Edit Mode test loop, not a measurement of the game. |

### Known problems after this pass

| Severity | Problem |
| --- | --- |
| LOW | The Unity test run created an untracked `.vscode/` folder (editor settings from Unity's code-editor package). It is not in `.gitignore`. Whether to ignore it is the owner's choice. |
| LOW | Running the Edit Mode tests still rewrites `NetworkArena.unity` and the player prefab with new IDs (Baseline 2). |
| INFORMATIONAL | The Unity CLI offers an update from 1.0.0-beta.14 to 1.0.0-beta.17. Not needed for anything above; not installed. |

All earlier known problems remain, including no lag compensation (the pose record is built and tested but not used).

### Next step

1. Build a Windows development player and run two and four windows with `--nah-push`, to verify `0b44e8d` on a joining player's screen.
2. Then wire the pose record into the host's hit check, one tested step at a time.
