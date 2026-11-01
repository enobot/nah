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
