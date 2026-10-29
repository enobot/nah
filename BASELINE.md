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
