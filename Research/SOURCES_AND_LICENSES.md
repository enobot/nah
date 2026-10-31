# NAH 2026 — Technical sources, third-party license notes, and recommended use

Research compiled October 31, 2026. Review the current license/package version at integration time; licenses or availability can change. Source links are included so Claude and Cursor can inspect the original material.

## Networking, core gameplay, and example source code

1. **Unity 6 First Person Multiplayer sample** — https://github.com/Unity-Technologies/U6-First-Person-Multiplayer — actual Unity sample code: walk/sprint/jump/coyote-time/jump-buffer/ground slam; pickup/throw; NetworkManager, Relay, session flow, spawn, physics, up to four players. Reference project, NOT ready-made competitive PvP. License Unity Companion License (https://github.com/Unity-Technologies/U6-First-Person-Multiplayer/blob/main/LICENSE.md); check all dependencies and service charges.
2. **Unity Boss Room multiplayer RPG sample** — https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop — character abilities, latency masking, multiplayer objects, RPCs and pooling. Check LICENSE.md and Third Party Notices. Reference design, not drop-in code for first-person arena fighting.
3. **Unity NGO versus Entities** — https://docs.unity.com/en-us/multiplayer/netcode/netcode — for a small 4-player Unity GameObjects prototype, investigate Netcode for GameObjects first. This is not a guarantee of easy prediction, authoritative movement, or internet matchmaking.
4. **NGO 2.7 Rpc syntax** — https://docs.unity.cn/Packages/com.unity.netcode.gameobjects%402.7/manual/advanced-topics/message-system/rpc.html — [Rpc(SendTo.Server)] methodNameRpc(...); check installed version (Unity sample uses newer versions).
5. **NetworkTransform authority** — https://mp-docs.dl.it.unity3d.com/netcode/current/components/networktransform/ — compare server/owner authority; default server authority alone is not prediction.
6. **NGO NetworkRigidbody and physics** — https://mp-docs.dl.it.unity3d.com/netcode/current/advanced-topics/physics/ — collision and trigger caveats, interpolation, lag.
7. **Multiplayer Play Mode** — https://docs.unity.cn/Packages/com.unity.multiplayer.playmode%402.0/manual/index.html — up to four editor players for local smoke tests; still test separate release builds and real network conditions.

## Movement, animation, camera, and input

8. **CharacterController.Move** — https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/charactercontroller/move — collision-constrained movement; gravity must be handled in code. Good first candidate for the FPS motor.
9. **Unity Input System + split-screen** — https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/set-up-split-screen-local-multiplayer.md — PlayerInput, PlayerInputManager, per-player cameras.
10. **Remappable controls** — https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/user-rebinding-runtime.md — interactive rebinding and saving binding override JSON.
11. **Input System practical videos** — https://learn.unity.com/course/input-system-video-series — official 7-part beginner course.
12. **Unity Animator blend trees** — https://docs.unity.com/en-us/engine/6000.5/manual/animation-section/animation-mecanim/animation-animator-controller/animation-state-machines/animation-blend-trees — speed/directional locomotion animation blending.
13. **Animation Rigging package** — https://docs.unity.cn/Packages/com.unity.animation.rigging%401.3/manual/index.html — two-bone IK, aiming constraints, procedural interactions. Check package version compatibility.
14. **Cinemachine 3** — https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-cinemachine — camera framework; for FPS, measure and avoid unnecessary camera effects.
15. **Humanoid versus Generic rig** — https://docs.unity.com/en-us/engine/6000.0/manual/assets-and-media/asset-types/models/importing/fbximporter-rig — use Humanoid for humanoid skeletons; Generic for animals, dragons, custom rigs.
16. **Unity animation C# jobs samples** — https://github.com/Unity-Technologies/animation-jobs-samples — TwoBoneIK/LookAt/full body IK examples. Verify sample license and compatibility; older sample.

## Physics, interactions, and smooth performance

17. **Unity Cloth** — https://docs.unity.com/en-us/engine/6000.0/manual/physics-section/physics-overview/class-cloth — works with skinned mesh, collision limitations; profile and limit expensive simulations.
18. **Ragdoll Wizard** — https://docs.unity.com/en-us/engine/6000.6/manual/physics-section/physics-overview/ragdoll/wizard-ragdoll-wizard — Rigidbody/joints/colliders on humanoid rigged meshes.
19. **Unity joints** — https://docs.unity.com/en-us/engine/6000.5/manual/physics-section/physics-overview/joints-section — spring, hinge, configurable for physical secondary motion.
20. **FixedUpdate and fixed timestep** — https://docs.unity.com/en-us/engine/6000.3/manual/scripting/managing-time-and-frame-rate/fixed-updates — Rigidbody forces in FixedUpdate; CharacterController gameplay in Update appropriate for simple prototype.
21. **Rigidbody interpolation** — https://docs.unity.com/en-us/engine/7000.0/manual/physics-section/physics-overview/rigidbody/rigidbody-interpolation — inspect equivalent doc in installed Unity version; reduces presentation jitter with small latency tradeoff.
22. **Physics.OverlapSphereNonAlloc** — https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/physics/overlapspherenonalloc — non-allocating hit queries with fixed buffer capacity.
23. **Unity Profiler** — https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/introduction — measure CPU/physics/animation/rendering, allocs; test builds.

## Models, rigging, textures, animations, commercial licensing

24. **Blender** — https://www.blender.org/ and license https://www.blender.org/about/license/ — free/open-source tool. Your original models/animations created with it can be sold; third-party embedded assets retain their own terms.
25. **Kenney assets** — https://kenney.nl/assets and license FAQ https://kenney.nl/support — Kenney states game assets on asset pages are CC0, commercial use permitted, attribution not required. Verify each downloaded pack's included license. Excellent for temporary prototypes.
26. **Poly Haven** — https://polyhaven.com/ and https://polyhaven.com/license — 3D models, textures, HDRIs, CC0, commercial use allowed, no attribution required. Better for world assets/materials than bespoke gameplay animation.
27. **Adobe Mixamo** — https://www.mixamo.com/ and https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html — royalty-free commercial use of animations and characters in games; biped humanoid auto-rigging only; cannot automatically rig quadrupeds/dragons with it. Review Adobe terms for redistribution and use of source data.
28. **Quaternius** — https://quaternius.com/ and https://quaternius.com/license.html — QAL v1.0 last updated August 28, 2026. Commercial integration permitted; standalone redistribution of assets/asset packs prohibited. Beware older FAQ and third-party directories claiming all Quaternius assets are CC0: resolve licenses based on applicable version and purchase/download terms. Generally do NOT put restricted raw assets in a publicly downloadable Unity source repo.
29. **Unity Asset Store EULA** — https://unity.com/legal/as-terms — commercial games generally may incorporate non-restricted assets under conditions, but raw asset redistribution, special provider licenses, extension seats, and AI training restrictions require attention.
30. **Unity Companion License** — https://unity3d.com/legal/licenses/unity_companion_license — Unity samples, when covered, may be incorporated under this Unity-dependent license, subject to its conditions, notices, and third-party assets. Do not call it MIT/CC0.

## Hard requirement for every outside resource

Track asset ID, creator, URL, download URL, license link and exact version, local path, commercial use, modifications, attribution, redistribution of raw source, AI-related restrictions where applicable, access/cost dependencies, last verification, and copy of license notice if permitted. No blank or unknown licensing entries may move to release.

## Prototype code

See `Assets/NAH/Prototype/` for five original C# examples. They are offline/local study code only and not meant to be shipped in online multiplayer unmodified. They have not been compiled or exercised in Unity Editor in this environment.
