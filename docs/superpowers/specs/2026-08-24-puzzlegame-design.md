# PuzzleGame Master Design Specification

## Working title
PuzzleGame is a temporary working title. Keep the final public-facing game name easy to replace.

## Product target
- Unity 2D/2.5D.
- Landscape-only.
- Target PC and mobile from the beginning.
- Touch and mouse input must both work.
- No real-money payment processing in v1. Premium currency exists only as an in-game resource.

## Core gameplay
PuzzleGame is a match-color gacha battler, not a traditional turn-based RPG.

### Board
- 6 columns x 5 rows.
- Six orb types: Fire, Water, Nature, Light, Dark, Heart.
- Fire > Nature > Water > Fire.
- Light and Dark deal bonus damage to one another.
- Heart is recovery-only and is not a playable character element.
- Minimum match size is 3.
- Player selects one orb and drags it through the board.
- As the dragged orb passes through cells, it swaps position with each orb crossed.
- Default movement timer is 10 seconds.
- Releasing early ends movement immediately.
- Once movement ends, all valid matches resolve, then cascades continue until the board is stable.

### Attack resolution
- Each separate color match creates a separate attack event.
- Example: three separate Fire matches cause each eligible Fire character to attack three times.
- Cascades can add further attack events.
- Heart matches heal the party.
- Global combo count may multiply output.
- Skills enhance/manipulate the matching and attack system rather than replacing it.

### Enemies
- Enemies use visible countdown values rather than initiative or turn-order systems.
- One completed board resolution decreases the countdown.
- At zero, the enemy attacks or performs its scripted action, then resets its countdown as configured.
- Enemy/boss actions can include damage, orb conversion, orb locks, poison/hazard orbs, blockers, bind effects, timer reduction, elemental absorbs, combo shields, enrage thresholds and other data-driven mechanics.

## Team and characters
- Party size: 5.
- Party can contain any combination of elements.
- Slot 1 supplies the Leader Skill.

### Character data
Every playable character supports:
- ID and display name.
- Element: Fire, Water, Nature, Light or Dark.
- Base rarity: 1 to 5 stars.
- 6-star awakened form.
- Level and level curve.
- HP, ATK and REC.
- Leader Skill.
- Active Skill.
- Passive.
- Awakened Ability/Form properties.
- Team-composition tags/factions/classes.
- Base portrait/art references.
- Awakened portrait/art references.
- Base and awakened VFX references where applicable.

### Leader Skills
Always-on team-building passives. They may target element, tags, faction, mono-element, rainbow composition, combo count, HP thresholds or similar data-driven conditions.

### Active Skills
Charge through matching, primarily through the character's element and/or configurable rules. They can convert/create/remove orbs, increase attack, heal, extend movement time, delay enemies, manipulate countdowns, provide shields, deal direct damage and similar effects.

## Rarity, duplicates, Ascension and Awakening
- Pullable rarity caps at 5 stars.
- Characters can awaken to 6 stars through gameplay.
- 6-star Awakening must NOT require duplicate copies.
- Awakening requires progression thresholds plus farmable Awakening Materials.
- Awakening produces a visible unit change: upgraded art/portrait, stronger costume/visual details, upgraded card/frame treatment and stronger VFX/skill presentation.

### Ascension
- Duplicate pulls advance character Ascension.
- Default cap: Ascension 5.
- Early Ascension levels improve Active Skill performance.
- Later Ascension levels can improve passive functionality and/or provide modest stat bonuses.
- Extra duplicates after maximum Ascension convert into a universal resource rather than becoming worthless.

## Gacha and currencies
### Currencies
- Gold: normal upgrade currency.
- Gems: gacha/premium currency, earnable in-game in v1.
- Tickets and event-specific currencies may exist.

### Standard pull pricing
- Single pull: 150 Gems.
- 10-pull: 1,500 Gems.

### Gather-In / step-up concept
A Gather-In banner uses increasing pull counts and costs across steps.
- Early examples: 50 Gems -> 1 character, 100 Gems -> 2 characters, 150 Gems -> 3 characters.
- Continue upward until a 10-character final step.
- Rotation 1 final 10-pull guarantees a 5-star with increased odds for the featured 5-star.
- Rotation 2 final 10-pull guarantees the featured 5-star.
- Steps are one-time within the rotation/cycle as configured.

### Summon presentation
Summoning is presented as opening collectible card packs.
- Purchase/confirm summon.
- Pack appears.
- Player rips/tears the pack open.
- Character cards are revealed.
- Ten-pulls use a larger/thicker/fancier pack and reveal ten cards.
- Results screen supports per-card reveal and Reveal All.
- Rarity can be teased through pack foil, glow, shake, cracks, transformations or fake-outs.
- Character reveal cards show art, name, stars, element and NEW/DUPLICATE state.
- Duplicate reveal immediately communicates Ascension gain.
- 5-star reveals should be substantially more dramatic.
- Banner/event data can swap pack art and reveal presentation without changing summon logic.

## Story and stage structure
- Normal permanent story stages do not consume stamina.
- Long-term target: approximately 20 chapters x 25 stages = 500 story stages.
- Stages are data-driven and reuse thematic enemies/backgrounds with varied waves, rules and modifiers.
- Chapters contain normal battles, minibosses, bosses and occasional story scenes.
- Typical chapter climax is stage 25.

### Stage stars
Default three-star goals:
1. Clear the stage.
2. Finish above a configured HP threshold.
3. Clear within a configured number of board resolutions.

Chapter star totals award milestone rewards.

## Rotating content
The game must support pseudo-live-service content through data-driven schedules rather than requiring a live backend for v1.

Required rotation categories:
- Permanent Standard Banner.
- Rotating Featured Banners.
- Gather-In banners.
- Step-Up banners.
- Event Chapters.
- Daily/Weekly Material Dungeons.
- Awakening Material stages.
- Challenge Towers.
- Boss Rush content.

The schedule system reads dates/times and selects active content from configuration. Content files must be addable later without rewriting core gameplay systems.

Initial content bank target can include roughly 15-20 events, 30+ banner configurations, multiple challenge towers and evergreen daily/weekly schedules after core systems are validated.

## Data-driven architecture
Characters, skills, leader skills, enemies, stages, waves, dialogue, drops, banners, events, rotation schedules and awakening requirements must be represented through shared data contracts rather than hard-coded per-content logic.

Major contracts should include equivalents of:
- CharacterData
- SkillData
- LeaderSkillData
- EnemyData
- StageData
- WaveData
- BannerData
- EventData
- RotationScheduleData
- RewardData

Exact Unity representation (ScriptableObject, JSON, or hybrid) can be chosen during implementation, but the boundaries must remain stable and content-authorable.

## Presentation goals
The core game can remain mechanically simple, but battle and summon presentation should feel flashy.

Target effects include:
- Screen shake.
- Hit stop.
- Slash/projectile trails.
- Particles.
- Damage numbers.
- Elemental overlays.
- Character attack movement/animation.
- Boss introductions and telegraphs.
- Full-screen or near-full-screen special cut-ins where appropriate.
- High-rarity pack and card reveal effects.

Effects must remain performant on mobile and offer reduced-intensity/reduced-motion options where practical.

## Parallel agent ownership
### Codex owns core systems
- Unity project foundation and architecture.
- Shared data contracts/schemas.
- Board generation and drag input.
- Matching, cascades and refill.
- Damage/combo calculation.
- Battle state machine.
- Enemy countdown/action engine.
- Skill framework.
- Character progression.
- Ascension/Awakening logic.
- Save/load.
- Currencies/economy state.
- Gacha probability/guarantee logic.
- Pack summon state flow hooks.
- Stage loader.
- Calendar/rotation scheduler.
- Automated validation and tests.

### Claude owns presentation and bulk content
Claude must consume the shared contracts rather than redefine core rules.
- UI/menu presentation after contracts exist.
- Party/character/upgrade/awakening screens.
- Banner and summon UI.
- Pack-ripping/reveal presentation using the summon hooks.
- Battle HUD presentation and VFX hookup.
- Character definitions/content.
- Enemy definitions/content.
- Chapter/event dialogue and silly story content.
- Stage configurations after the stage schema is validated.
- Banner configurations and event schedules.
- Reward tables and bulk content production.

## Parallel-development rules
- Never let both agents make broad edits to the same files simultaneously.
- Use separate git branches/worktrees for core and content/presentation work.
- Shared contracts are owned by Codex first; Claude reads them and requests changes rather than silently redefining them.
- Keep changes small and commit frequently.
- Merge shared-contract/core work before bulk content that depends on it.
- Do not generate hundreds of content entries before one complete chapter, one banner and one event have been validated end-to-end.

## MVP validation slice
Before mass-producing content, the project must prove one complete vertical slice:
- Working 6x5 board.
- 10-second drag input on mouse and touch.
- Match/cascade/refill.
- Five-character party.
- At least two elements plus Heart demonstrated.
- Separate attacks for separate matches.
- One active skill.
- One leader skill.
- One enemy countdown action.
- One boss mechanic.
- One stage with three-star objectives.
- One character upgrade/Ascension example.
- One 6-star Awakening example with visible art/VFX change support.
- One Standard summon and one Gather-In banner.
- Pack-rip summon flow.
- Save/load.
- One scheduled rotating event/content example.

Only after this slice works should agents expand toward hundreds of stages and large content banks.
