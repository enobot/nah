# Third-party asset register

This register is the approval list for anything not created for this game. A resource is not approved because an earlier note called it free, CC0, or open source.

This is not a legal opinion and it does not mean a resource is free of every legal risk. Before a Steam release, a qualified intellectual-property attorney should review the game name, the major characters, and this register.

Status meanings:

- APPROVED: source and license checked, and the owner has accepted it for this commercial project.
- REVIEW REQUIRED: more proof is needed, or the owner has not accepted it. Do not import it.
- REJECTED: the license is missing, noncommercial, or otherwise unfit. Do not import it.

Nothing below is APPROVED. No third-party model, animation, texture, sound, font, or script has been copied into this repository.

## Audit of the repository

Checked: 2026-10-31. The repository now contains a Unity 6000.3.27f1 URP project and five original offline prototype scripts supplied by the owner in `NAH_Research_Code.zip`. Those scripts are project code, not a third-party package. No Kenney, Poly Haven, Quaternius, Mixamo, or Unity sample project has been imported.

## Sources checked on 2026-10-31

These were read from their public pages or from the search results of those pages. None is APPROVED. None is imported.

| Resource | Source checked | License found | Use in this project |
| --- | --- | --- | --- |
| U6 First-Person Multiplayer | https://github.com/Unity-Technologies/U6-First-Person-Multiplayer | Unity Companion License. The license text at https://unity.com/legal/licenses/unity-companion-license says use is limited to Unity-licensed projects, and that Unity owns derivative works of the sample itself. The sample also expects Relay and Vivox. | REVIEW REQUIRED. Do not copy it into this repo. Study it, then write original code. Do not enable Vivox or Relay without a cost and privacy decision. |
| Boss Room | https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/LICENSE.md | Unity Companion License. Unity's NGO 2.13 manual says Boss Room is no longer actively maintained as of 2026. | REVIEW REQUIRED. Reference only. Do not import its characters or code. |
| Netcode for GameObjects 2.13.3 | https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/license/LICENSE.html | Unity Companion License. The same manual says it supports Unity 6.0 and later. | REVIEW REQUIRED. Not installed. |
| Unity Relay and Lobby prices | https://unity.com/products/gaming-services/pricing | Public price page, not a game-asset license. Relay lists 50 average monthly concurrent users free, then $0.16 per additional average user, with bandwidth charges after the included amount. Lobby lists 10 GiB per month free per regional group. | Not enabled. Prices can change. Do not treat the free tier as unlimited. |
| CharacterController, Cloth, Ragdoll Wizard, Profiler, Blend Trees | Unity manual pages linked from `Research/SOURCES_AND_LICENSES.md` | Engine features under the Unity editor license. Not separate assets. | Available. The prototype uses CharacterController only. |
| Kenney, Poly Haven, Quaternius, Mixamo, Blender | Pages already listed above and in `Research/SOURCES_AND_LICENSES.md` | Unchanged from the earlier rows. Quaternius is QAL, not CC0. Mixamo's current Adobe terms were still not fetched. Blender is a tool license, not a license for other people's models. | REVIEW REQUIRED. Not downloaded. |

## Audit of the repository, earlier pass

Checked: 2026-10-28. That pass found project notes only. The Unity project was added later.

| Item in the repo | Class | Notes |
| --- | --- | --- |
| `GAME_DESIGN.md`, `BASELINE.md`, `RESEARCH_POLISH.md`, `CLAUDE.md`, `.cursor/rules/commercial-assets.mdc`, this file | Project text | Written for this project. Not a third-party asset. |
| Unity Editor installer and Unity CLI on `G:` | Outside the repo | Unity software under Unity's own terms. It is not a game asset. Which Unity plan may be used for a commercial project depends on Unity's eligibility rules and was not settled here. |

No existing game file needs deletion.

## Candidates discussed earlier

These were researched. They were not downloaded into the game. Site-wide claims are not approval of a specific file.

| Resource | Creator or publisher | Source checked | License found | Status |
| --- | --- | --- | --- | --- |
| Kenney game-asset pages | Kenney | https://kenney.nl/support | Support page says assets on the asset pages are CC0. The logo is not. No individual pack or license file has been saved. | REVIEW REQUIRED |
| Poly Haven assets | Poly Haven | https://polyhaven.com/license | Site page says the site's assets are CC0. No specific file was checked for other rights. | REVIEW REQUIRED |
| ambientCG assets | ambientCG | https://docs.ambientcg.com/license/ | Docs say the assets are CC0 1.0. No specific file was checked. | REVIEW REQUIRED |
| Quaternius packs | Quaternius | https://quaternius.com/license.html | Quaternius Asset License v1.0, dated 28 Aug 2026 on that page. It allows a commercial product and forbids reselling the files as an asset pack. Not CC0. No pack was downloaded. | REVIEW REQUIRED |
| Sonniss GDC audio bundle | Sonniss | https://sonniss.com/gdc-bundle-license/ | Bundle license 2.0, effective 27 Aug 2026. Allows use in a commercial game and forbids supplying the sounds as a sound library. Not CC0. Not downloaded. | REVIEW REQUIRED |
| Unity Starter Assets 2.0 | Unity | Asset Store package 196526 | Store page says Unity Companion License, non-standard. The full license text is not in this repo. | REVIEW REQUIRED |
| Kinematic Character Controller 3.4.4 | Philippe St-Amand | Asset Store package 99131 | Standard Asset Store EULA. Unity 6 compatibility was not shown. Not downloaded. | REVIEW REQUIRED |
| Showerss first-person controller | Showerss | GitHub README | README says MIT. The LICENSE file was not opened. Not imported. | REVIEW REQUIRED |
| The First Person | boaheck | GitHub API | GitHub reports MIT. Last push found: 6 Dec 2021. Not imported. | REVIEW REQUIRED |
| Mixamo | Adobe | Community FAQ and Unity Asset Store validation note | Current Adobe legal terms were not fetched. Raw-file redistribution is a known problem. Not downloaded. | REVIEW REQUIRED |
| Verlet Chain System | samlletas | GitHub README | No license in the README. | REJECTED |
| UnityClothSimulation repository | ndossche | GitHub README | Code described as MIT. Demo shirt and pants are CC BY. Tablecloth license was not read. Do not import the repository as a bundle. | REJECTED |
| OpenGameArt, any file | Per submission | Site not opened file by file | Licenses differ. Noncommercial submissions are prohibited. | REJECTED until a specific file is reviewed |
| Freesound, any file | Per uploader | https://freesound.org/help/faq/ | CC0, CC BY, CC BY-NC, and older Sampling+ exist. CC BY-NC and any noncommercial term are prohibited. | REJECTED until a specific CC0 or reviewed CC BY file is accepted |

CC BY can be usable in a commercial game when attribution is actually kept. It is still REVIEW REQUIRED until that file and its notice are recorded. CC0 remains the preferred third-party category, and a CC0 label still needs a check of the specific file.

## Approved resources

None.

## Required fields for a future approval

- Resource name
- Creator
- Original source
- Exact license and version
- Date verified
- Commercial-use permission
- Permission to modify
- Permission to distribute inside a sold game
- Attribution and required notices
- Redistribution limits
- Third-party rights concerns
- Bundled dependencies and their licenses
- Files imported
- Approval status
- Where the license snapshot is stored
