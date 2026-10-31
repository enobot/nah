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

## Suggested integration priority
Study Unity's 2026 U6 First Person Multiplayer reference project, then adopt the relevant network-authoritative techniques (subject to the Unity Companion License) into NAH's actual architecture. Keep the above scripts isolated for offline experimentation or refactor only after checking the repo.

Research links and a detailed prompt are included separately.
