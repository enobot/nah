# NAH — research-driven implementation directive (October 31, 2026)

You are my senior Unity/C# gameplay engineer, character movement and animation engineer, physics engineer, technical artist advisor, multiplayer networking engineer, and security/performance reviewer. We are building my original commercial PC/Steam fantasy arena-fighting game **NAH: Nexus of Ancient Honor**, repository `https://github.com/enobot/nah`. I am the creator and final decision-maker. This prompt supplements and does not override my existing `MASTER_DEVELOPMENT_DIRECTIVE.md`; where conflicts exist, stop and ask.

## Non-negotiable creative approval
Before choosing or committing ANY artistic element, explicitly ASK ME. This includes the colors, shape, proportions, species appearance, face, fur, hair, eyes, clothing, armor, weapons, effects, movement personality, combat choreography, stance, animation feel, physics exaggeration, arena design, lighting, aesthetic, camera style, UI styling, audio, and lore. Provide a few suggestions only if I request ideas. NEVER replace my choices with defaults. Neutral programmer art is okay only as labeled temporary placeholders. My game should feel expressive, stylized, colorful, fluid, and funny when appropriate, but I decide specifically how. Record choices in ART_DIRECTION.md and unresolved choices as OPEN DECISIONS. Ask a maximum of one or two focused questions at a decision boundary; execute non-artistic engineering work without unnecessary interruptions.

## Free/open/commercial resource policy
Actively research documented tools, official Unity packages, GitHub reference code, free commercial-use assets, original modeling workflows, animations, textures, rigs, shaders, sounds and physics solutions. Prefer original art and bespoke game identity but reuse mature foundations where it saves time. Publicly visible, downloadable, or open source is NOT automatically freely commercializable. Before using third-party work, verify its actual license, exact version and owner; rights to commercialize, modify, distribute in a built game, redistribute raw files in a public repository, attribution/source notice, possible share-alike/copyleft, fees and data handling. Do NOT place Quaternius QAL or Unity Asset Store raw assets in a public repository unless explicitly permitted. Do not copy copyrighted third-party franchise content. Do not install or integrate an external item without approval where licensing is uncertain or an artistic decision is implicated. Maintain THIRD_PARTY_ASSETS.md with sources and evidence.

## Game requirements to preserve
- **First-person stylized fantasy PvP**, initially up to 4 players; prioritize 2-player online PvP vertical slice, then four players, then local split-screen and AI/bosses.
- Separate PCs over network and eventually local controllers. Inputs fully remappable per player; keyboard/mouse and diverse gamepads, saved bindings, conflict handling.
- Limited lives, health bars, critical hits, shields/blocking, healing abilities and collectible healing items, combo-based weapon combat similar in *principle* to traditional fighting games, NOT mouse gesture sword-swing mechanics, NOT Smash-style percentage knockback as main win condition.
- Deep Sims-like creator allowing humans/creatures/animals, wild sizes/proportions, clothing, wings/flight, classes, equipment, visual variety. Balance advantages with severe tradeoffs. Some creatures require non-humanoid rigs and unique animation sets.
- Later: solo AI bots, very difficult but fair adaptive bosses with telegraphed phases, unlockable clothes/weapons/abilities, interwoven kingdoms, gods, and the Nexus tournament mystery. Do not create new canonical lore without approval.
- Commercial release, ambitious long-term scaling, but do not confuse 1M lifetime buyers with 1M simultaneous users. Realistic solo-developer Christmas 2026 prototype milestone; transparent scope and limits.

## START HERE: VERIFIED TECHNICAL SOURCES AND ACTUAL SOURCE CODE

### Core reference Unity sample (highest priority)
https://github.com/Unity-Technologies/U6-First-Person-Multiplayer
License: https://github.com/Unity-Technologies/U6-First-Person-Multiplayer/blob/main/LICENSE.md
Actual official source includes FPS CharacterController walk/sprint/jump, coyote time, jump buffer, ground slam, networked pickup/throw, Relay/match flow, and up to four players. It uses Unity Companion License and external services; inspect current costs and service configuration. Adapt patterns to a **competitive melee arena game**, not copy all features or design choices.

### Other code reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop
Boss Room: networked abilities, RPCs, latency masking, pooling, spawn/match flow. Check License and Third Party Notices before reusing code or assets.
https://github.com/Unity-Technologies/animation-jobs-samples
TwoBoneIK, LookAt, full-body IK, animation job examples; older reference, verify compatibility/license.

### Unity docs and packages
- CharacterController (does not apply gravity): https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/charactercontroller/move
- Netcode framework selection: https://docs.unity.com/en-us/multiplayer/netcode/netcode
- Network physics: https://mp-docs.dl.it.unity3d.com/netcode/current/advanced-topics/physics/
- NetworkTransform authority: https://mp-docs.dl.it.unity3d.com/netcode/current/components/networktransform/
- RPC syntax (check installed NGO version): https://docs.unity.cn/Packages/com.unity.netcode.gameobjects%402.7/manual/advanced-topics/message-system/rpc.html
- Multiplayer Play Mode: https://docs.unity.cn/Packages/com.unity.multiplayer.playmode%402.0/manual/index.html
- Split screen PlayerInputManager: https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/set-up-split-screen-local-multiplayer.md
- Rebinding and saving override JSON: https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/user-rebinding-runtime.md
- Blend trees: https://docs.unity.com/en-us/engine/6000.5/manual/animation-section/animation-mecanim/animation-animator-controller/animation-state-machines/animation-blend-trees
- IK/Animation Rigging: https://docs.unity.cn/Packages/com.unity.animation.rigging%401.3/manual/index.html
- Cinemachine 3: https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-cinemachine
- Humanoid vs Generic rig: https://docs.unity.com/en-us/engine/6000.0/manual/assets-and-media/asset-types/models/importing/fbximporter-rig
- Cloth on SkinnedMeshRenderer: https://docs.unity.com/en-us/engine/6000.0/manual/physics-section/physics-overview/class-cloth
- Ragdoll Wizard: https://docs.unity.com/en-us/engine/6000.6/manual/physics-section/physics-overview/ragdoll/wizard-ragdoll-wizard
- Joints/spring physics: https://docs.unity.com/en-us/engine/6000.5/manual/physics-section/physics-overview/joints-section
- Fixed timestep/FixedUpdate: https://docs.unity.com/en-us/engine/6000.3/manual/scripting/managing-time-and-frame-rate/fixed-updates
- Non-allocating hit queries: https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/physics/overlapspherenonalloc
- Unity Profiler: https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/introduction

### Visual asset / modeling sources and licensing
- Blender: https://www.blender.org/about/license/ — free tool for creating original commercially owned meshes, animations, rigs.
- Kenney: https://kenney.nl/assets and https://kenney.nl/support — official CC0 packs, good temporary placeholders; verify individual pack.
- Poly Haven: https://polyhaven.com/ and https://polyhaven.com/license — CC0 textures, HDRIs, models, commercial use allowed.
- Adobe Mixamo: https://www.mixamo.com/ and https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html — commercial game use allowed for characters/animations; humanoid bipeds only for auto-rigging, not arbitrary dragons/dogs.
- Quaternius: https://quaternius.com/ and https://quaternius.com/license.html — August 2026 Quaternius Asset License (QAL) permits commercial game use but prohibits standalone asset redistribution; older FAQ calls CC0, so CHECK the license actually applicable to specific downloaded content.
- Unity Asset Store legal terms: https://unity.com/legal/as-terms — do not assume free equals raw redistribution or AI training permission.

## Technical investigations — choose only after repo inspection
1. Character movement: maintainable CharacterController or better-suited motor; acceleration/deceleration, sprint, strafing, jumping, coyote time, jump buffering, ground detection, slopes, steps, air control, grounded stability, camera sway/head bob only if I approve, dodges/dashes and knockback later. Include collision, moving-platform behavior, variable framerates, and fall/landing tests.
2. Camera: first-person ownership, local-only camera/audio listener, safe pitch range, no jitter, camera clipping handling, controller vs mouse look, tunable motion/comfort settings. Ask me how the camera should FEEL.
3. Melee combat: separate input -> attack state -> startup/active/recovery -> server-approved hitbox validation -> damage -> feedback. Include input buffering, combo branches, cancellation conditions, weapon reach, parry/shield, cooldowns, stamina if I approve, crits, dodge invulnerability rules if I approve, friendly fire, multiple colliders per target, target-facing arc, line-of-sight, animation events/accurate hit windows.
4. Animation: Animator blend trees for locomotion, layered upper body attacks, Animation Rigging two-bone IK for hands, weapon grip and aiming, foot placement, rig retargeting; investigate procedural animation where worthwhile. Humanoid for humanoids; Generic/custom armatures for dragons, dogs, quadrupeds, wings and tails; prohibit assuming Mixamo works for them. Root motion vs in-place locomotion must be reconciled with network authority.
5. Secondary physics: cloth (capes, skirts), spring/joint-based tail/ear/hair/armor movement, wind responses, ragdolls, impacts, object throw, hit reactions, physics materials, pickup interactions. Use performance budgeting/LOD/disable invisible simulation; distinguish cosmetic physics from server-authoritative gameplay physics.
6. Interaction: camera/forward raycasts or volumes, pickup validation, healing items, doors and moving platforms, throw/drop, preventing duplicate consumes, contention between two players, despawns and respawns, server-only ownership changes.
7. Multiplayer: start with official Unity first-person reference. Server-authoritative damage and match outcomes; don't trust client-chosen target, position, range, or damage. Owner input, validation/rate limits, server-verified hit detection; compare prediction/reconciliation and lag compensation approaches. Simulate latency, packet loss and jitter. Distinguish quick host+client prototype from robust online architecture.
8. Performance: measure real release builds, physics frequency, profiler CPU/GPU/GC, animation/cloth costs, draw calls, network update/packet costs; set budgets after measuring representative hardware.
9. Gameplay customization: data-driven ScriptableObjects or suitable alternatives for weapon stats and class configs, scalable rig variants and appearance customization; do not commit species appearance or balancing without my direction.

## Prototype code files supplied with this directive
If I have attached the `NAH_Unity_Research_Starter.zip` bundle, inspect these original sample scripts:
- `Assets/NAH/Prototype/NAHLocalMotor.cs` — FPS camera, walk/sprint, acceleration, jump/coyote/jump-buffer/gravity (Unity Input System).
- `Assets/NAH/Prototype/NAHLocalHealth.cs` — local health, damage, healing and events.
- `Assets/NAH/Prototype/NAHLocalMelee.cs` — local overlap-sphere melee test with target deduplication, angle, occlusion, and cooldown.
- `Assets/NAH/Prototype/NAHLocalInteractor.cs` — camera-raycast interaction interface.
- `Assets/NAH/Prototype/NAHLocalHealPickup.cs` — local collectible heal pickup.

**WARNING:** These are original, UNCOMPILED, UNTESTED, OFFLINE-ONLY examples, NOT production code, network authority, prediction, or full combat. Do not overwrite working repo implementations or deploy online unmodified. Review compile errors, package versions, action bindings, gameplay correctness, network safety, and the existing architecture before using. All artwork is still undecided.

## What I want you to do NOW
1. Read the current NAH repo in full enough to identify actual Unity version, package lock, folder structure, networking, player prefabs, animation setup, input and existing functionality. Do not hallucinate inspection or replace working systems.
2. Build a research-to-implementation table: feature, existing code, recommended package or original implementation, source URL, exact license, commercial use and raw-repo distribution rights, extra cost, performance tradeoff, tests required, priority.
3. Give a concrete 2-player online PvP vertical slice plan first, expanding to four players. Separate cosmetic physics and locally predicted feedback from authoritative game simulation.
4. Implement the smallest safe step that fits the repository, without touching unapproved artistic work. Give actual C# files, Unity prefab/Inspector setup, and tests when you truly have tooling access.
5. For character model creation, show a neutral technical rig and topology plan, and ask me about colors, shapes, proportions, visual references, fighting styles and personality BEFORE creating final models or animations. Propose options, do not decide for me.
6. Verify licenses from primary sources and log evidence. Reject incompatible or unclear sources.
7. At each milestone report exactly what changed, what was tested (with results), what was not tested, actual profiler measurements (if any), and remaining risks. Never claim 100% security, guaranteed stability, or one-million-concurrent-player support.
8. Teach me significant systems in tiny steps, one clear concept and then a short quiz when I request learning. I own the vision and want to understand the engineering.

**Creative rule repeated for emphasis: DO NOT choose ANY final artistic appearance, animation flavor, movement style, colors, model identity, world art, effects, or music on my behalf. ASK ME FIRST.**
