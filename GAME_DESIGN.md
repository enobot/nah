# Game design

This document records the game we are building. It separates decisions from ideas, and ideas from code that exists.

Status labels:

- Confirmed: the project owner has chosen this direction.
- Proposed: a recommendation that still needs a decision.
- Future: intended later, not part of the first prototype.
- Implemented: present in the project and checked.

Nothing in this file is implemented. The repository has design notes and a baseline record. It does not yet have a Unity scene, combat code, or tests of combat.

Soulcalibur, Super Smash Bros., The Elder Scrolls V: Skyrim, World of Warcraft, and The Sims are broad references for gameplay ideas. This game does not use their characters, names, move names, art, animation, audio, UI, or other protected expression.

## Identity

Confirmed.

This is an original first-person fantasy arena fighter with a single-player adventure beside the competitive modes.

- Up to four combatants in a match. A combatant can be a human or a bot.
- Windows PC first, with Steam as the intended store later.
- Online play, local split-screen, and mixed play are planned. They are not built at the same time.
- Characters can look beautiful, cute, ugly, or absurd.
- Species do not lock classes. A duck can be an assassin. A polar bear can be a mage.
- A class and a weapon are chosen separately. The class does not assign a fixed weapon.
- Matches use health and a limited number of lives. The game does not use a damage-percentage system.
- At zero health, the player loses a life. Falling out of the arena also costs a life.
- Players respawn while lives remain. The last player alive wins a free-for-all.
- Cosmetic appearance and competitive stats are separate. A costume does not grant an advantage unless a rule explicitly says that item has a gameplay effect.

## Working title and story

Chosen direction. Not implemented. Not a cleared commercial name.

The working title is **N.A.H. — Nexus of Ancient Honor**. The three letters stay in the title. H is read as Honor. The line used in design discussion is "Honor is not given. It is earned."

No domain has been registered. No logo has been made. A general web search on 2026-10-28 did not return a game with this exact title. It did find other uses of "Nexus" and an unrelated multiverse tournament in a licensed cartoon game. Those are reasons to keep our fiction original. They are not a trademark search. A lawyer should review the name before release.

Champions from many species and kingdoms are brought to the Nexus. They are told to fight. They are not told who summoned them, why they were chosen, or whether anyone goes home. That mystery is the adventure. Arena matches can ignore the story. The story does not change the combat resolver.

The first game has to stand alone. Later visits to a champion's home realm are possible stories, not systems in this project. No open world is planned for this installment.

The proposed kingdoms, six bosses, timeline, and endings are not canon until you approve them. Franchise notes use four labels: CANON, PROPOSED, MYSTERY, and FUTURE.

- `UNIVERSE_BIBLE.md`
- `WORLD_TIMELINE.md`
- `REALMS_AND_KINGDOMS.md`
- `GODS_AND_MYTHOLOGY.md`
- `CHAMPION_LORE.md`
- `NEXUS_MYSTERY.md`
- `FRANCHISE_ROADMAP.md`
- `WORLD_BIBLE.md`
- `STORY_TIMELINE.md`
- `CHARACTER_BIBLE.md`
- `FACTIONS_AND_KINGDOMS.md`
- `BOSS_DESIGN.md`

## How a class and a weapon combine

Confirmed.

A weapon decides the fighting style: reach, speed, attack shape, stamina cost, and which defensive tools fit that weapon.

A class decides what that style is infused with: the character's strengths, weaknesses, and signature abilities.

The same class can use different weapons. Two mages are still mages. One can use a staff and cast ranged spells. The other can use a greatsword and make slower melee attacks that carry a magical effect. The combat code does not need a separate program for "mage with staff" and "mage with greatsword." It reads a weapon definition, then applies the class's modifiers.

```text
Weapon definition          Class definition
reach, speed, attacks  +   modifiers and signature abilities
        \                        /
         \                      /
          one combat resolver
```

Example, not a final balance sheet:

| Build | Weapon supplies | Class supplies |
| --- | --- | --- |
| Mage with a staff | Long casting motions, a ranged attack shape, slow melee recovery | Spell damage, a magical guard, a signature spell |
| Mage with a greatsword | Slow, long melee arcs and high stamina cost | The same magical damage type and signature spell, delivered through the sword |
| Warrior with daggers | Short, fast attacks and a close-range weakness | Higher physical critical chance, no spell package |
| Warrior with a greatsword | The same slow arcs as the mage's sword | Physical damage and a warrior ability, not a spell |

Ridiculous bodies stay legal. A tiny chicken can parry a giant troll's hammer. The parry is a timing check against an incoming attack. It does not ask whether the chicken model is large enough to cover the hammer model. Body size still matters through documented stats: health, speed, reach, and the authoritative hitbox. Those numbers are not chosen yet.

## Weapon styles

Confirmed as categories. The numbers are not chosen. These are roles, not final statistics.

| Weapon | Fighting style | Weakness |
| --- | --- | --- |
| Sword | Balanced reach, speed, and damage | No extreme specialty |
| Greatsword | High damage and long reach | Slow attacks and a high stamina cost |
| Dagger | Fast attacks and critical-strike potential | Short reach |
| Spear | Long thrusts | Weak when an opponent is already in contact |
| Hammer | Strong impact and the ability to break a guard | Slow recovery |
| Axe | Strong attacks that punish armor | Weaker defensive options |
| Staff | Magic attacks, including ranged spells | Poor ordinary melee |
| Unarmed | Punches, kicks, and close grappling attempts | Short reach and no weapon guard |

Each category has its own attack definitions. Weapons in one category share a style. Individual weapons can still differ by data, such as a heavier hammer inside the hammer category. They do not each get a private copy of the combat program.

Cosmetic weapon appearance is separate. A glowing sword and a plain sword can share a fighting style. Appearance grants an advantage only when the item definition says it has a gameplay effect.

Armor, headgear, gloves, boots, clothing, and accessories follow the same rule. A princess dress on a polar bear is cosmetic. A heavy shield that slows movement is a gameplay item, and that effect must be written in its definition.

## Body, weapon size, and stats

Owner decision, 2026-10-31. The formulas and numbers are not chosen.

Damage comes from stats, and damage lowers the health bar. The stats come from the character's body and equipment:

- **Body.** How big, how fat, and how round the character is sets its stats. A bigger, fatter body has more health, absorbs more of each hit, and has more shielding.
- **Weapon.** The weapon's size helps set how much damage its attacks do.
- **Shield.** Carrying a shield adds shielding ability on top of what the body gives.

```text
body shape (size, fatness, roundness) ─┐
weapon (size, category)                ├─► stats ─► damage of a hit ─► lower health bar
class and equipment (shield, armor)    ─┘           (less what the target absorbs)
```

What this means for the code:

- Hits per life is not a setting. It comes from the target's health, the attacker's damage, and how much the target absorbs.
- The host computes stats from the body and weapon values in data. A client sends its choices; the host checks them against the limits in the data and does the math.
- The body and weapon sizes that set stats are gameplay values stored in data, not the visible mesh. As "How a class and a weapon combine" already says, body size also sets the authoritative hitbox. A purely cosmetic item, such as a hat, a dress, or a color, still changes no stat.

Still open:

- What a big or fat body gives up in exchange. The master directive asks that strong advantages carry real disadvantages.
- What shielding does: a separate bar used up before health, or how much of a hit a block stops.
- Every formula and number. The first version uses placeholder numbers in `NAHPlaceholderStatRules.asset`. `BASELINE.md` records what was built.

## Combat mechanics that fit this game

Confirmed as direction. Not all of them belong in the first combat scene.

These ideas fit the arena we already planned:

- Weapon reach and attack speed, because spacing matters even in first person.
- A small set of attack directions: a horizontal cut, a vertical cut, and a thrust.
- Combos made from those attacks, with a recovery gap so a combo cannot continue forever.
- Blocking, with guard stamina that can run out.
- A parry that rewards timing and opens a short counterattack.
- Dodging, with a cost so it is not free movement.
- Ring-outs. Leaving the arena already costs a life. The fighting-game name for that idea is a ring-out. We keep our existing life rule.
- Hit feedback through sound, a hit marker, and a brief weapon reaction. The camera stays stable.

Defense has a cost:

- A heavy guard blocks more and slows movement.
- A light character dodges sooner and blocks heavy hits poorly.
- A magical shield is a class or item ability. It is not available to every build.
- Guard breaks, parries, and counters are limited by timing and stamina.

## Mechanics to treat carefully in first person

Proposed. These need a playable test before they become core controls.

Soulcalibur-style fighting is normally third person and one-on-one. The player can see both bodies, the weapon arc, and the space beside them. Our camera is first person, and up to four people can attack at once. Some ideas survive that change. Some need a simpler form.

| Idea | Why it is harder here | First version to test |
| --- | --- | --- |
| Horizontal, vertical, and thrust attacks | The player cannot see their whole body. The weapon motion has to be readable inside the view. | Show the weapon in view. Use three attacks with different hitboxes. |
| Directional blocking | A four-player match has hits from behind and from the side. A block that only covers one angle will fail often. | Block protects a cone in front of the camera. Side and back hits are not blocked. |
| Sidestep | Third-person sidestep is a dedicated spacing tool. First person already has strafe. | Use strafe plus one dodge. Add a special sidestep only if strafe feels insufficient. |
| Parry | The player must recognize an incoming attack without seeing their own arms clearly. | One parry timing against a practice dummy, then against one other player. |
| Grappling | A grab forces two characters into a shared animation and a shared camera. With four players, someone else can hit both of them. | Leave grapples until basic hits, blocks, and parries are readable. |
| Long cinematic attacks | Forced camera spins and large shakes hide the arena and cause motion sickness. | Keep the camera on the head. Sell the hit with the weapon, sound, and UI. |
| Infinite or very long combos | A stunned player in a four-player match can be hit by everyone else before they recover. | Every combo has a recovery window. The target can block, dodge, or parry after that window. |

Controls stay small. The first combat test should not require a fighting-game motion vocabulary.

## Initial combat prototype

Proposed. Do not build this until the Unity project exists and basic movement works.

The smallest combat scene is local and single-player against a stationary dummy:

1. One sword.
2. Three attacks: horizontal, vertical, and thrust. Each has its own speed, reach, stamina cost, and hitbox.
3. The weapon is visible in first person.
4. The camera does not shake or spin.
5. A block that spends guard stamina and fails when that stamina is empty.
6. One two-hit sequence, light then light, with a gap before another attack is allowed.
7. A practice dummy with health. Hitting it reduces that health on this machine only.

Not in that scene: a second weapon, classes, parries, grapples, networking, critical strikes, or character customization.

After that scene is tested, the next combat step is a second weapon with a clearly different hitbox, such as a spear or a dagger. The class modifier comes after two weapon styles work through the same resolver.

## Architecture

Confirmed as the boundary. The scripts are not written.

One combat resolver reads data. It does not contain a function per species, class, or weapon.

Separate data and responsibilities:

| Piece | Responsibility |
| --- | --- |
| Weapon definition | Category, reach, speed, stamina costs, which attacks it owns |
| Attack definition | Direction, startup, active time, recovery, hitbox, damage request |
| Combo definition | Which attacks can follow which, and the allowed timing gap |
| Class definition | Modifiers and signature abilities applied to an attack |
| Hit detection | Asks whether an active hitbox meets a gameplay body |
| Damage calculation | Applies attack, class, defense, and critical rules |
| Guard | Blocking, parry timing, guard stamina, guard break |
| Stamina | Spends and recovers attack and guard stamina |
| Animation | Plays the matching motion. It does not decide damage |
| Network sync | Later. Sends requests and receives host results |

Cosmetic equipment chooses the visible mesh. The weapon definition chooses the hitbox. A giant cosmetic hammer cannot enlarge the authoritative hitbox by itself.

Startup, active time, and recovery are the three parts of an attack. Startup is the windup. Active time is when the hitbox can connect. Recovery is the opening where the attacker can be countered. Those words will be used the same way in code and in this document.

No balance numbers are final. When a number is chosen, it belongs in data with a note explaining why, not as an unnamed constant copied into several scripts.

## Classes

Confirmed.

Planned classes include warrior, mage, assassin, ranger, paladin, healer, berserker, and summoner. The first implementation should use two classes at most, after weapons work.

A class can change:

- Which damage type an attack carries.
- A signature ability on its own cooldown.
- A defensive option, such as a magical shield for a mage or paladin.
- A weakness, such as lower health or slower guard recovery.

A class cannot remove the weapon's identity. A dagger used by a berserker is still short. A greatsword used by a mage is still slow.

Healing can come from a class ability. Every player can still pick up a healing item when that rule exists. Item pickup is future work.

## Customization and equipment

Confirmed as the goal. Future for the prototype.

Players can eventually change headgear, armor, gloves, boots, weapons, accessories, and colors. Extreme and silly combinations are allowed.

Examples that must remain possible:

- A polar bear in a dress with a greatsword.
- A tiny goblin in oversized knight armor.
- A duck in a wizard hat with two daggers.
- A giant troll in tiny boots.
- A dragon dressed as a knight.
- A tiny chicken that parries a troll's hammer.

Technical limits still apply. Saved values are checked for type, length, and range. Cosmetic scale can be wild. The gameplay body, speed, health, and hitbox stay inside the limits written in the data. Invalid files are rejected.

## Multiplayer and security

Confirmed as rules for later network play. Networking is not part of the first combat prototype.

The planned online model is a listen host: one player's game acts as the server. That choice is not implemented. Netcode for GameObjects 2.13.3 is still marked pre-release in the Unity 6.3 manual, so it is not installed yet.

Clients may send requests such as "attack started" or "block pressed." The host decides whether the hit connected, whether a parry was in time, whether a critical strike happened, and how much health remains.

The host must reject:

- Damage numbers supplied by the client.
- Parries and counters that fall outside the active timing window.
- Attacks that ignore startup and recovery.
- Cooldowns that fire again too soon.
- Hits from a weapon the player is not holding.
- Duplicate hits from one attack's active window.
- Movement or attack rates outside the weapon and body limits.
- Malformed or flooded messages.

A four-player fight needs rules for more than one attacker:

- Each incoming attack is resolved on its own.
- A front block does not automatically stop a hit from behind.
- Two hits in the same moment both resolve. One does not erase the other.
- A combo's recovery window still applies while other players can hit the same target.
- A disconnect removes that player from the match without letting a client declare itself the winner.

This will not be cheat-proof. The host can alter the match locally. A dedicated server is a later option if that risk becomes unacceptable.

## First-person comfort

Confirmed.

- The camera stays attached to the head at a fixed height in the first tests.
- No head bob, forced spin, or strong camera shake until a specific effect is tested and kept only if it does not hide the fight.
- Keyboard, mouse, and gamepad are both required. The details are in Controls.
- Reduced camera shake is an accessibility setting. Until a shake effect exists, the setting has nothing to reduce, and the default remains a stable camera.

## Presentation, cloth, and wind

Proposed. Not approved as an import list, and not implemented.

Players should notice cloth, hair, tails, landings, and wind. Those effects are cosmetic. They do not decide hits, damage, or movement authority.

The research note is `RESEARCH_POLISH.md`. Its recommendation is:

- Move with Unity's built-in character controller and our own motor.
- Drive animation from gameplay state.
- Use a few bones for ears, tails, and hair.
- Use Unity Cloth only on selected loose garments, with an option to turn it off.
- Drive grass, banners, cloth, and particles from one wind value.
- Do not synchronize cloth or hair over the network.
- Do not import the controllers, cloth solvers, or asset packs reviewed there until a specific pack is approved and its license is recorded.

Human-shaped characters can share humanoid clips. Ducks, dragons, and other non-human skeletons need their own rigs. One humanoid clip set does not animate wings.

## Controls

Confirmed as a requirement. Not implemented. There is no input code and no `.inputactions` asset in the repository.

Every player can play with a compatible keyboard and mouse, or a compatible gamepad, using bindings they choose. Gameplay code listens to named actions such as `LightAttack` or `Dodge`. It does not listen for a particular physical button.

### Actions

The player action map will include:

- Move
- Look
- Jump
- Sprint
- LightAttack
- HeavyAttack
- Block
- Parry
- Dodge
- SpecialAbility
- UltimateAbility
- Interact
- UseItem

A separate menu action map will include navigation, confirm, and cancel. Menu bindings can reuse a physical control that gameplay also uses, because only one map is active at a time. Inside the player map, two different attacks should not silently share one control.

Light and heavy attacks match the combo idea of light, then heavy. They are actions, not a promise that every weapon has both. A weapon definition says which of these actions it uses.

### What Unity already provides

Verified from the Input System 1.20.1 manual and scripting API. The package is `com.unity.inputsystem`. Unity 6000.3.27f1 release notes include this package at 1.20.1. The 1.20 manual says Editor 6.7 and newer adds it to some project templates, and older editors install it from Package Manager. This project targets 6000.3.27f1, so the package is added explicitly. The version written into this project is not known until that file exists.

Verified behavior we will use:

- Actions are the recommended way to read input.
- `PlayerInput` plus `PlayerInputManager` supports local players. Each `PlayerInput` has its own copy of the actions, filtered to that player's devices. Reading the shared `InputSystem.actions` asset bypasses that filter, so per-player code uses `playerInput.actions`.
- `PerformInteractiveRebinding` starts a `RebindingOperation` that waits for a control and stores it as a binding override. The operation must be disposed.
- The operation can limit control type, ignore controls, and cancel through a chosen control.
- `SaveBindingOverridesAsJson` and `LoadBindingOverridesFromJson` save and restore overrides. Unity's manual shows `PlayerPrefs` as the local store.
- `RemoveAllBindingOverrides` restores the defaults.
- Binding groups, such as the `Gamepad` group in the API examples, keep keyboard bindings and gamepad bindings separate.
- Generic paths such as `<Gamepad>/buttonNorth` name a position on a standard gamepad. They do not name an Xbox button.
- A `scaleVector2` override is a documented way to scale a two-axis binding. That is the planned store for camera sensitivity.

Not verified in this pass:

- The exact supported-device page for Input System 1.20.1 did not load. This document does not claim that any specific Xbox, PlayStation, Nintendo, or other pad has been tested.
- The stick dead-zone processor name will be read from the installed package manual before it is used.
- No controller was connected during this review.

### Profiles

Each local player slot, from 1 to 4, has its own keyboard profile and its own gamepad profile. Saving slot 1 does not write slot 2.

A profile stores:

- Binding overrides for one control scheme.
- Look sensitivity for each axis.
- Whether the vertical look axis is inverted.
- Whether the horizontal look axis is inverted.
- Dead zone, once the processor name is confirmed.
- Block and sprint as hold or toggle.
- Vibration on or off, and a strength from 0 to 1.
- Whether camera shake is reduced.
- Which prompt set to show when the device layout is ambiguous.

Storage is local. The Unity manual's example uses `PlayerPrefs`. That keeps preferences on this machine and does not need an account or cloud save. The JSON is control paths and numbers, not a name, email, or account id.

Loaded JSON is untrusted:

- Reject empty and oversized text.
- If it does not load as binding overrides, keep the defaults and say the file was not used.
- After a load, menu confirm, menu cancel, and the reset action must still have a binding. If they do not, restore defaults.
- Clamp sensitivity, dead zone, and vibration to a fixed range. Reject non-numeric values.

Reset puts that profile back on the default bindings. The reset control itself stays available on the keyboard even while a gamepad is being rebound, so a bad gamepad profile cannot trap the player in the menu.

### Prompts and controller differences

The game shows a prompt from the active device layout when that layout identifies a known pad. When it does not, the prompt is generic, such as "South button" or "Right trigger," or a prompt style the player picked in settings.

Steam or other tools can present a PlayStation or Nintendo pad to the game as an Xbox-style device. The prompt then follows what the Input System reports. A manual prompt choice covers the case where that report is misleading.

Analog features are optional. Some pads have digital triggers. The first combat actions are buttons and sticks, so a pad without analog triggers can still play. Vibration is skipped when the connected device cannot rumble. That capability is checked per device, and it has not been tested here.

### Accessibility

- Block and sprint can be hold or toggle. Toggle is gameplay behavior on the action, so a player does not have to keep a finger down.
- Look sensitivity and inverted axes are per profile.
- Camera shake defaults off. The reduced-shake option stays in the profile for later effects.
- Default binds avoid chords. A special or ultimate is one control, not two buttons at once.
- A left-handed preset swaps the move stick and the look stick, and swaps the primary and secondary mouse clicks. The player can keep editing after applying the preset.
- Interactive rebinding ignores mouse movement when the action expects a button, so a small mouse twitch does not capture the binding.
- Text labels sit with the prompts. Color is not the only way to tell actions apart.

### Local multiplayer

`PlayerInputManager` joins a local player when a new device presses the join action, up to four players. Each joined device stays assigned to that player.

One keyboard is one device. Four local players need separate gamepads. Sharing one keyboard between two local players is not part of the first input setup, because splitting a keyboard is easy to get wrong and has not been tested.

Player 1's `PlayerInput.actions` is a different collection from player 2's. Load and save take the collection that belongs to that slot.

### Implementation plan

Waiting for approval. No input code is included in this change.

1. Add `com.unity.inputsystem` through Package Manager after the Unity project exists. Record the resolved version.
2. Create one action asset with the player and menu maps, keyboard and gamepad binding groups, and playable defaults.
3. Movement reads `Move`, `Look`, `Jump`, and `Sprint` only.
4. Add a local profile store with save, load, reset, and validation. One slot is enough to prove it.
5. Add the rebinding screen, including conflict warnings and unassigned warnings.
6. Add a second local player and show that the two profiles do not overwrite each other.
7. Combat, when it exists, calls the same actions. It does not grow its own button checks.

Tests to add with that work, currently NOT RUN:

- A rebind changes the override and the default returns after reset.
- A saved profile loads into the same slot.
- A malformed profile does not replace working defaults.
- Slot 1's save does not change slot 2's bindings.
- A second gamepad does not move player 1.
- Menu cancel still works after a load.
- An unassigned attack is reported before the profile is saved.

## Modes

Confirmed as direction. Not implemented.

### Arena

Up to four combatants. Any mix of humans and bots is allowed, including one human against three bots, two and two, three humans and one bot, or four humans. Bots use the same health, lives, weapons, and combat resolver as humans. The last combatant alive wins the free-for-all.

### Adventure

A separate single-player path. The player clears levels, meets enemies, and defeats bosses. Each level teaches a small set of mechanics. Adventure progress does not change the combat resolver. It records what was cleared and what was unlocked.

These level ideas are concepts, not final names, layouts, or art. Proposed character names for the same roles are in `BOSS_DESIGN.md`. They are not canon yet.

| Order | Place | What the player practices | Boss concept |
| --- | --- | --- | --- |
| 1 | Goblin village | Move, attack, block, dodge | Goblin King |
| 2 | Enchanted forest | Ranged and magical enemies, moving platforms | Mushroom Monarch |
| 3 | Frozen kingdom | Ice hazards and unusual animals | Frost Bear |
| 4 | Dragon's mountain | Flying enemies and volcanic hazards | Dragon Empress |
| 5 | Chaotic castle | Unusual abilities | Royal Jester |

All of those characters and places must be original. None of them are implemented.

The owner set two later places on 2026-10-28. Tracea is not the first boss. She is not the seventh either.

| Order | What happens |
| --- | --- |
| 7 | An unnamed boss. It cannot be killed. It kills the player. That is the design, not a mistake. |
| 8 | Tracea's challenge, after she has already saved the player. She wins the fight and still accepts them. |

The seventh boss has no damage state that defeats it and no secret weak point. Each death and return counts as one try. The count is kept in the local save. On the 17th death, Tracea interrupts and saves the player. Sixteen deaths do not unlock her. Seventeen is the owner's chosen number. The fight stays impossible after she arrives. She is the way out, not a new health bar on that boss.

She is worried. She tries to leave without the player. The player insists on coming with her. She then challenges them, to see if they can keep up. She beats them by the rules of that fight. She does not kill them. She accepts them. After that she grants powers across the rest of the journey. Those powers are free adventure unlocks. Competitive matches still use the standard loadout.

## Bots

Confirmed as direction. Not implemented.

A bot is not a second combat system. A human presses actions such as `Move` and `LightAttack`. A bot brain chooses those same actions. The motor and the combat resolver do not care who produced the action.

The first brain is a finite state machine. That is a short list of states with rules for when to switch. A first arena bot needs only: idle, approach, attack, block, retreat, and defeated. It decides a few times a second, not on every frame.

Behavior trees and utility scoring are later options if the state list becomes hard to follow. Machine learning and paid AI services are not part of this game. Bots run on the local machine. In an online match, the host runs the bots. Clients do not get to say that a bot hit them.

Navigation, when the bot must walk around obstacles, uses Unity AI Navigation. Package `com.unity.ai.navigation` 2.0.16 is a released package for Unity 6000.3. It is not installed yet. A flat arena with no obstacles does not need it.

Difficulty is a documented modifier, not a hidden stat boost.

| Setting | What changes |
| --- | --- |
| Easy | Slower reactions, simple approach, more missed blocks |
| Normal | Basic block timing and a retreat when health is low |
| Hard | Better spacing and fewer missed defenses |
| Expert | Cleaner combo timing and target switching |

Reaction time has a floor so Expert is not a machine that blocks on the same frame every time. Extra health, damage, or speed for a harder bot must be written in the difficulty definition. It is not added quietly in code.

Dark Souls, God of War, and The Elder Scrolls are references for hard, readable combat. They are not sources for characters, names, moves, or assets.

## Adaptive combat AI

Confirmed as direction. Not implemented. There is no combat code in the repository.

The brain still outputs the same actions a human can press. Adaptation is a layer in front of that action choice. It does not get its own damage rules.

```text
What the bot can see
        |
        v
Perception --> Memory --> Prediction --> Decision --> Actions
                                              |
                                              v
                                    same motor and combat as a player
```

| Piece | Job |
| --- | --- |
| Perception | Notices events that already happened: a dodge and its direction, an attack, a block, a heal, distance, and velocity. |
| Memory | Keeps a short list of those events. Old events count less. |
| Prediction | Estimates the next likely action. The estimate can be wrong. |
| Decision | Picks one legal action after a reaction delay. |
| Execution | The existing state list: idle, approach, attack, block, retreat, defeated. |

The decision step scores a few candidate actions from data: distance, health, the personality profile, and the current prediction. The highest score wins, unless a cooldown or the reaction delay says the bot must wait. That is utility selection. It stays a table of numbers, not a trained model. No paid AI service and no machine learning.

The bot does not read the player's next button. It does not see a queued dodge that has not started. It does not know a hidden cooldown unless that information is something a fighter could see, such as an attack that is visibly recovering.

### First adaptive behavior

After a basic bot can already move and attack, the first prediction is dodge direction.

The memory counts recent dodges to the left and to the right. If left is clearly more common, and the count is above a confidence threshold, the bot may choose an attack whose hitbox covers the left side. If the counts are close, it does not commit. If the player then dodges right several times, the left count decays and the prediction becomes wrong. That is the test that changing habits beats the prediction.

A reaction delay sits between the observation and the punishing attack. The delay is data on the difficulty profile. Expert is faster than Easy. Expert is not instant.

### Personalities

Personalities are data, not separate combat programs.

| Profile | Behavior |
| --- | --- |
| Reckless | Steps in often, retreats when hurt. A goblin can use this. |
| Guarded | Blocks and punishes a heavy attack's recovery. A knight can use this. |
| Distant | Keeps range and uses area attacks. A magical creature can use this. |
| Veteran | Uses the dodge-direction memory. |
| Boss | Several of the above, plus phase changes. |

Not every enemy is a veteran. A reckless enemy can ignore the memory.

### Limits so the bot is not perfect

- Memory decays, so a changed habit is forgotten.
- A confidence threshold blocks a counter when the pattern is weak.
- A counterattack has a cooldown.
- Some chosen attacks are feints or ordinary swings, so the bot does not answer every habit.
- Reaction delay is never zero.
- The bot can pick a wrong action on purpose at a rate set by difficulty.

Brutal difficulty comes from timing, spacing, and punishment of habits. A larger health bar or a damage multiplier is allowed only when the boss definition lists it. It is not the main way to make a fight hard.

### Boss patterns

A boss profile picks a style: pressure, defense, adaptation, trickery, heavy hits, or battlefield magic. The styles can combine. Each boss still needs a weakness a player can learn.

Phases change the pattern at a health threshold. The player can see the change. Phase 1 is the basic set. Phase 2 adds attacks. Phase 3 raises pressure and changes combinations. The transition itself is not a guaranteed hit.

### Several humans

The host runs the brain. Threat is distance, who is attacking, and who recently hit the boss. Those are visible facts. The boss does not read a player's unpressed inputs or a client-only secret. It switches targets instead of locking one player forever. Area attacks are telegraphed. Clients do not declare that the boss's prediction was correct.

### Tests for this design

NOT RUN. No AI code exists.

- A streak of left dodges raises the left count.
- The punishing attack does not start until the reaction delay ends.
- After enough right dodges, the left prediction is dropped.
- A low confidence score does not force the counter.
- The brain cannot see an input that has not been performed.
- Easy and Expert use the documented delays, not a silent damage bonus.
- A phase change does not deal damage by itself.
- With two players, the boss target comes from observed threat.

The picture below is the shape of the first memory. It is not code in the project.

```csharp
// Proposed record. Not part of the build.
class DodgeMemory
{
    public int Left;
    public int Right;
    public float SecondsSinceUpdate;
}
```

## Bosses and rewards

Confirmed as direction. Not implemented. No boss is in the first milestone.

A boss is an enemy with its own attack list, a telegraph before each dangerous attack, a weakness, and a defeat moment. A phase is a change of pattern after a health threshold. Phase 1 is the basic pattern. Later phases add an ability or change the arena. An attack that cannot be seen in time is not allowed.

The first game holds two boss groups. The adventure path is the smaller story set. The champions named after that set are also in this game, as unlocked bosses. Beating story encounters opens their fights. Their exact unlock order is not final. A locked boss is absent from the fight list. Losing to one does not take away a reward the player already earned. Defeating one grants a free reward tied to that champion. None of this is built yet.

Tracea is not a boss the player kills. On the 17th death to the impossible seventh boss, she saves the player, tries to leave, and then tests whether they can keep up. She wins that test and accepts them. She stays alive and grants further powers as the journey continues. Those powers are free story unlocks for adventure and casual play. Competitive matches keep the standard loadout.

Rewards are data: an id, the condition that grants it, and whether it is cosmetic or a gameplay unlock. Defeating a boss can grant a cosmetic such as a crown, a hat, a winter outfit, dragon-themed armor, or a jester accessory. Those examples are ideas. Gameplay rewards, such as a new ability, are allowed in adventure and in casual arena. Competitive arena uses a standard loadout so a new player is not weaker for having fewer unlocks. A cosmetic never changes reach, damage, or speed.

There are no paid loot boxes, paid chance rewards, or required purchases.

Suggested reward ideas, still unbuilt:

- Goblin King: an oversized goblin crown.
- Mushroom Monarch: a mushroom hat and a mushroom-themed spell.
- Frost Bear: winter clothing and an ice ability.
- Dragon Empress: dragon-inspired armor and a weapon appearance.
- Royal Jester: humorous costumes and odd accessories.

The spell and the ice ability are gameplay unlocks. They stay out of the competitive standard loadout unless the mode rules say otherwise.

## Saves and progression

Confirmed as direction. Not implemented.

A local save stores character profiles, settings, control profiles, completed levels, defeated bosses, and unlocked reward ids. It does not store passwords or account tokens. Cloud save is not included.

The file has a version number. The loader checks types, ranges, and ids it knows. A broken or unknown file is refused and the previous good file is kept when a backup exists. Local files can be edited by the person at the computer. That is not treated as tamper-proof.

If a reward changes competitive play, the host checks the allowed loadout for that mode. The client does not send "I own this ability" as proof.

Progression data lives beside combat. A level script asks "is this reward unlocked?" It does not contain the full unlock list in the combat code.

## Rig families

Confirmed.

Species that stand and walk on two legs can share one humanoid animation family. A human, a goblin, and a bipedal bear are candidates for that family when their skeletons match closely enough.

A four-legged dragon, a snake, and a bird do not share that skeleton. They get their own rig family and their own move and attack clips. Extra bones, such as a tail or wings, belong to the family that has them. Shared clips do not pretend to animate bones they do not have.

This is the same split as cosmetic scale versus the gameplay body. The family decides how the character can move on screen. The body profile still decides the authoritative speed and hitbox.

## Technology direction

The owner's starting stack, recorded here with the verification already in `RESEARCH_POLISH.md`. These pieces have not been tested together. Nothing below is imported. `THIRD_PARTY_ASSETS.md` is the approval list. No third-party resource is APPROVED.

| System | Choice | Status |
| --- | --- | --- |
| Engine | Unity 6000.3.27f1 | Installed at `G:\Unity\Editors\6000.3.27f1`. The project exists in this repository (status updated 2026-10-31). |
| Rendering | URP | URP 17.3.0 is in the project. |
| Movement | Our motor on the built-in character controller. Starter Assets is a reference, not an import. | Starter Assets 2.0 pulls Cinemachine and sample characters. |
| Animation | Unity Animator, then Animation Rigging 1.4.1 | Rigging is a released 6000.3 package. Not installed. |
| Prototype clips | Mixamo, only after the current Adobe terms are copied into the license register | Not cleared for import in this pass. |
| Cloth | Unity Cloth on a few garments | Built in. Later. |
| Hair and tails | A few bones with light motion | Our code. Later. |
| Input | Input System | Input System 1.20.1 is in the project. The prototype uses temporary keys from `NAHPrototype.inputactions`. |
| Placeholder models | A named Kenney or Quaternius pack after its license file is stored | Not downloaded. |
| Materials | Poly Haven or ambientCG, CC0, if the style fits | Not downloaded. |
| Audio | Selected Sonniss GDC sounds under bundle license 2.0 | Not downloaded. |
| Bots on a navmesh | AI Navigation 2.0.16 | Released for 6000.3. Not installed. |
| Online play | Netcode for GameObjects, still marked pre-release at 2.13.3 in the 6000.3 manual | 2.13.3 installed 2026-10-31 for the gray two-player arena. Direct UDP only. |
| Version control | Git | Commits on `main` at `https://github.com/enobot/nah.git`. |

## First milestones

Proposed order. Waiting for approval before code.

1. One character that walks, runs, jumps, lands, and stops cleanly, with acceleration that matches the input. No head bob.
2. That same character attacks, and one simple hair or cloth piece reacts to the stop and the landing.
3. Arena duel: one human, one easy bot, the same actions, health, lives, a defeat, a victory screen, and one cosmetic unlock saved locally.
4. Only after that duel works: dodge-direction memory, a reaction delay, and a test where changing dodge direction beats the prediction.
5. A second weapon style, more personalities, then adventure level 1.

Boss phases, the five level concepts, and a large reward list wait behind that duel. The adaptive dodge is the first prediction. It is not the whole boss system.

## Roadmap

Confirmed order.

1. Project foundation, then one character that looks, walks, runs, jumps, lands, and stops through named input actions.
2. One attack, plus one hair or cloth reaction on that same character.
3. Control profiles: rebind, save, load, and reset.
4. Arena duel: one human, one bot, health, lives, victory, and one local cosmetic unlock.
5. Dodge-direction memory on that bot, with a reaction delay and a failing prediction when the player changes habits.
6. A second weapon style and one class modifier through the same resolver.
7. Two human players, then up to four combatants, including bots. The host decides hits and runs the bot brain.
8. Adventure level 1 and its boss, using the same combat actions.
9. More rig families, weapons, classes, and cosmetic equipment.
10. Parry, dodge, and guard break once basic attacks are readable.
11. Further bosses, phases, levels, grapples, shields, items, and hazards.

## Test expectations for this design

NOT RUN. There is no combat code.

When the prototype exists, the first checks are:

- A horizontal, vertical, and thrust attack each hit only their own hitbox.
- An attack during its own recovery does not start a new hit.
- A block stops a front hit and spends guard stamina.
- A block does not stop a hit from outside the front cone.
- Guard stamina at zero does not block.
- A combo cannot continue after the allowed gap.
- A class modifier changes the attack's effect without changing the weapon's reach.
- A cosmetic scale change does not change the authoritative hitbox.
- A parry succeeds only inside its timing window, including a small attacker against a large weapon.
- The client does not get to declare its own damage or parry success once networking exists.
- Rebinding, profile load, malformed profile rejection, and two local players with isolated bindings behave as described in Controls. Those checks are NOT RUN.
- A bot and a human can both lose a life through the same damage path. NOT RUN.
- A harder bot does not gain undocumented health or damage. NOT RUN.
- A competitive loadout ignores a gameplay unlock that is marked adventure-only. NOT RUN.
- A malformed save does not replace the last good save. NOT RUN.
- Left-dodge memory, reaction delay, and a changed habit clearing that prediction. NOT RUN. The checks are listed under Adaptive combat AI.
