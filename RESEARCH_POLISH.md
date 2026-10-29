# Polish, physics, animation, and world detail

Recorded: 2026-10-28

This is a research note. Nothing in it has been imported or implemented. No movement, animation, cloth, wind, audio, or environment code exists in this repository. The checks below are NOT RUN.

The Unity Editor install for 6000.3.27f1 was still running when this note was written. There is no Unity project folder yet.

## What already exists

| Item | State |
| --- | --- |
| Game design | Written in `GAME_DESIGN.md`. First person, four players, health and lives, class plus weapon, remappable actions. |
| Playable scene | Does not exist. |
| Character controller | Does not exist. |
| Animator | Does not exist. |
| Tests | None. |

Gameplay stays responsive. Visual detail is added after a character can move. Cosmetic motion never decides damage.

## Recommendation

Build the first body with Unity's built-in `CharacterController`. Do not import a third-party controller, cloth solver, or asset pack yet.

Use three visual layers later, from cheap to expensive:

1. Animation clips and a few extra bones for ears, tails, and hair.
2. Unity's Cloth component on a small number of loose garments, with a graphics option to turn it off.
3. A shared wind value that shaders, cloth, and particles read. Grass and leaves move in a shader. They do not each get a physics object.

Official packages that are reasonable later, after the project exists:

- Input System, already planned.
- Animation Rigging, `com.unity.animation.rigging` 1.4.1, listed as a released package for Unity 6000.3.

Cinemachine is required by Unity's Starter Assets. This game does not need it for the first camera. Do not add it only to unlock that sample.

## Movement architecture

A `FirstPersonMotor` reads the named actions `Move`, `Look`, `Jump`, `Sprint`, and later `Dodge`. It moves a `CharacterController`. Combat and animation do not read buttons.

A `BodyMovementProfile` holds the numbers for one body build: acceleration, top speed, jump speed, air control, and land recovery. A goblin profile and a troll profile are different data. The motor code stays one script. Those numbers are not chosen yet.

The motor publishes a gameplay state: grounded, speed, sprinting, airborne, and the current attack phase. The animator reads that state. The animator does not decide whether a hit landed.

The first version has no head bob, no sprint field-of-view kick, and no camera roll. Those effects fight the comfort rules already in `GAME_DESIGN.md`.

Slopes, stairs, and moving platforms wait until flat-ground walk, jump, and land feel right.

Different frame rates must not change jump height. Gravity and acceleration are applied using frame time. That will be tested when the motor exists. It has not been tested.

## Animation architecture

One human-shaped skeleton can share clips through Unity's Humanoid retargeting. A duck, a dragon, and a quadruped do not fit that skeleton. Wings, extra ears, and tails are not human bones. Those species need their own rigs, or a humanoid body with extra bones that the shared clips do not animate.

Plan:

- Human-shaped species share one clip set where the motion still reads correctly.
- Other body families get their own idle, move, and attack clips.
- Extreme scale uses the gameplay profile for speed and a bounded rig scale for the mesh. A cosmetic scale that breaks the skeleton is clamped for animation even if the portrait looks wilder.
- First person shows hands and the weapon. Other players see the full body. Both views read the same gameplay state. The first-person view does not run a second copy of the damage rules.
- Attack clips have anticipation, an active window, and recovery. The active window is the same timing the hitbox uses. A long pretty swing cannot start the hit before the player can react.

Animation Rigging is the later tool for planting a hand on a weapon and for a small aim correction. It is not the first task.

## Cloth, hair, tails, and accessories

Do not simulate every vertex of every costume.

| Thing | Approach | Why |
| --- | --- | --- |
| Horns, armor, hats that should stay stiff | Skinning only | They should not bounce. |
| Ears, a short tail, a ponytail | A few bones with a light follow or spring | Cheap, and it reads at arena distance. |
| A cape, scarf, or skirt on a featured outfit | Unity Cloth on that mesh, constrained to the body | Built in. Sphere and capsule colliders only. |
| Every loose thread on every player | Not simulated | Four players times several garments will miss the frame budget. |

Unity's Cloth component, documented in the Unity 6 manual, works with a Skinned Mesh Renderer. It does not collide with the whole world. You assign sphere and capsule colliders. It does not push other objects back. `External Acceleration` is the hook for wind. Self-collision is expensive. The manual says it can take a significant part of the simulation time.

A graphics setting can set cloth to off, low, or on. Off means the cape plays a short authored sway instead.

Hair that covers the first-person camera is shortened or tied in that view. The other players can still see the long hair.

## Wind

One `WindField` per arena area stores direction and strength. A blast can add a temporary gust with a position and a radius.

Readers:

- Foliage shaders bend vertices from that direction. One material, many plants.
- Cloth garments copy the field into `External Acceleration`.
- Particles use the same direction.
- Banners can be a short bone chain or a cloth strip, not a separate wind system.

The network does not send cloth vertices. If wind ever matters to gameplay, the host sends the field's direction and strength. Cosmetic gusts can differ slightly between machines.

## What the network sends

Send the gameplay state: position, facing, velocity, grounded, body profile id, weapon id, attack phase, block, health, and lives.

Do not send cloth particles, hair bones, particles, footstep sounds, or camera shake. Each machine plays those locally from the replicated state.

A client does not report its own hit from a cosmetic mesh. The host uses the gameplay capsule and the attack hitbox.

## Audio and settings

Not in the first playable scene.

Later, footsteps choose a clip from the surface and from the body profile, so a heavy land and a light land are different sounds. Impacts choose a clip from the pair of materials. Volume is split into music, effects, and ambience. There is no microphone capture.

Graphics options wait until there is something to scale: resolution, a frame-rate cap, VSync, shadows, particles, cloth, foliage, field of view, and motion reduction. No benchmark numbers exist.

## What to build first

1. Finish the Unity project. Do not import the packs below as part of that step.
2. A capsule that walks, looks, jumps, and sprints from our action names, with acceleration and a readable landing.
3. One sword attack whose timing matches its hitbox.
4. One licensed or original humanoid mesh with idle and locomotion, only after the license is copied into the register.
5. One tail or cape bone chain on that mesh.
6. One cloth garment, with an off switch.
7. One wind field driving a banner and that garment.
8. Remote players show the full body from replicated state. Their cloth stays local.

## Resource evaluation

Nothing in this table is approved for the game. `THIRD_PARTY_ASSETS.md` is the register. A page that says CC0 or MIT is a candidate, not permission to import. License text was read from the page named in the source column on 2026-10-28 unless the status says the check was incomplete.

| Resource | Source | Purpose | License and compatibility | Maintenance and risk | Recommendation |
| --- | --- | --- | --- | --- | --- |
| Built-in CharacterController | Unity engine | First-person walk, jump, and collision | Included with the Editor. No extra license. | Official. Does not react to forces by itself. Knockback has to be written in the motor. | Use for the first motor. |
| Built-in Cloth | Unity manual, `class-Cloth` | Capes, skirts, scarves | Included with the Editor. Works with a Skinned Mesh Renderer. Sphere and capsule colliders only. | Official. Self-collision is expensive. Not for every garment or for network sync. | Use later, on a few garments, with an off switch. |
| Animation Rigging 1.4.1 | Unity 6000.3 manual, `com.unity.animation.rigging` | Hand placement and extra bone aim | Released package for this Editor version. | Official and current for 6000.3. | Add when a rig exists. Not before. |
| Input System | Already planned | Actions, rebinding, local players | Released package. See `GAME_DESIGN.md`. | Official. | Keep. Gameplay reads actions, not buttons. |
| Starter Assets: Character Controllers, version 2.0, 4 Sep 2026 | Unity Asset Store package 196526 | Sample first- and third-person controllers | Store page says it is free, uses a non-standard Unity Companion License, and is compatible with Unity 6.3 LTS and older. It installs Input System and Cinemachine. The full Companion License text was not copied into this repo. | Unity sample. Includes Robot Kyle and a humanoid armature. Those are sample characters, not our cast. | Do not import. The first-person capsule is a useful reference. Cinemachine and the sample characters are extra weight. |
| Kinematic Character Controller 3.4.4 | Unity Asset Store package 99131 | Precise collide-and-slide motor | Standard Asset Store EULA. The publisher says it is free and support is not guaranteed. The store compatibility table shown is Unity 2020.3. The page says the examples are built-in render pipeline and that the controller can still be used in URP. Unity 6 was not listed. | Last release date on the store page: 7 Jun 2022. | Do not import. Unity 6 compatibility is unverified, and the built-in controller is enough for the first prototype. |
| unity-first-person-controller | https://github.com/Showerss/unity-first-person-controller | First-person motor | README states MIT and requires Unity 6000.0 or newer and Input System 1.19 or newer. A separate LICENSE file was not opened. | Written for Unity 6. Includes head bob, camera roll, and sprint field-of-view changes that conflict with our camera rules. It ships its own input asset. | Do not import. Read it for acceleration and jump timing ideas only. |
| The First Person | https://github.com/boaheck/TheFirstPerson | First-person motor | GitHub reports MIT. | Last push recorded by the GitHub API: 6 Dec 2021. | Do not import. It is idle and predates this Editor. |
| Mixamo | https://www.mixamo.com/ | Humanoid clips and a test character | An Adobe community FAQ says the service is a free technology preview for commercial and non-commercial use, and that raw character or animation files must not be redistributed as asset packs. Unity's Asset Store validation rejects packages that contain Mixamo files. The current Adobe legal page was not fetched in this pass. | Convenient for prototypes. Easy to violate by committing raw files. Humanoid only. | Do not import until the current Adobe terms are saved in the license register. Not a source for animal rigs. |
| Verlet Chain System | https://github.com/samlletas/verlet-chain-system | Bone or rope chains for capes | The README describes the tool and does not include a license. | No license was found. A missing license is not permission. | Do not import. |
| UnityClothSimulation | https://github.com/ndossche/UnityClothSimulation | Custom CPU and GPU cloth | README says the code is MIT and the included demo assets are not. The shirt and pants are CC Attribution. The tablecloth texture is from Gumroad and its license text was not read. | Research project. A second cloth solver on top of Unity Cloth. | Do not import. |
| Kenney | https://kenney.nl/ | Placeholder models and audio | The support page says game assets on the asset pages are public domain (CC0). The studio logo is not. Each downloaded pack still needs its own license file checked. | Active library of separate packs. Licenses are per pack, not one blanket file in our repo. | Not approved. CC0 candidate only. Do not download a pack until that pack is checked and the owner marks it APPROVED. |
| Quaternius | https://quaternius.com/license.html | Stylized characters | Quaternius Asset License v1.0, last updated 28 Aug 2026 on that page. Commercial use and modification are allowed. Attribution is not required. The assets themselves cannot be resold or redistributed as an asset pack. | Not CC0. Finished game builds may include the assets. | Not approved. Custom license, not the preferred CC0 path. Do not download until a specific pack is accepted in the register. |
| Poly Haven | https://polyhaven.com/license | HDRIs, textures, models | The license page says all assets on the site are CC0. | Photogrammetry textures can look more realistic than this game's style. | Optional later for neutral materials. Not the stylized character style. |
| ambientCG | https://docs.ambientcg.com/license/ | Materials | The docs say assets are CC0, including commercial use and inclusion in a game. | Same style caution as Poly Haven. | Optional later for ground materials. |
| OpenGameArt | https://opengameart.org/ | Mixed art and audio | Not one license. Each submission has its own terms. This pass did not open a specific submission. | Non-commercial and unclear licenses are common. | Do not import any file until that file's page is recorded. |
| Sonniss GDC bundle | https://sonniss.com/gdc-bundle-license/ | Sound effects | Bundle license version 2.0, effective 27 Aug 2026. Royalty-free for commercial games, no attribution required, unlimited projects. The sounds cannot be resold or supplied as a sound library. AI training is prohibited. The license page says these terms apply to every download of the bundle. | Large download. We only need a few sounds at first. | Usable later for effects. Copy this license into the register at download time. Do not download the whole bundle now. |
| Freesound | https://freesound.org/help/faq/ | Individual sounds | The FAQ says uploaders choose CC0, CC BY, or CC BY-NC, and older sounds may still be Sampling+. CC BY-NC is not usable in a paid game. Attribution is required for CC BY. | Easy to grab the wrong license. | Do not bulk download. Each sound needs its own row in the register. |

## Performance risks

- Cloth on every costume, on four players, plus split screen.
- Self-colliding cloth.
- A full Animation Rigging setup on every bone of every species.
- Rendering a detailed full body and a separate first-person body without a plan for which one the camera sees.
- Extreme bone scales that explode a skinned mesh.
- Real-time lights on every torch before the frame time is measured.
- Synchronizing cosmetic transforms.

No frame-rate numbers are claimed. There is no build to profile.

## Security and privacy

Cosmetic simulation is not an authority. Saved body profiles stay local and bounded, as already required. Audio assets are files, not microphone recordings. No analytics package is added for wind, cloth, or audio.

Third-party code imported from Git would be a dependency. The unlicensed chain project and the stale controllers are the reason they stay out.
