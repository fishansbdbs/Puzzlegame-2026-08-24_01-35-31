# Codex Core Systems Prompt

You are the lead systems engineer for a new Unity 2D/2.5D match-color gacha battler.

Repository: `fishansbdbs/Puzzlegame-2026-08-24_01-35-31`

Authoritative design: `docs/superpowers/specs/2026-08-24-puzzlegame-design.md`

Read the entire design before making changes.

Work on a dedicated branch such as `codex/core-systems`. Do not broadly develop directly on `main`.

Your ownership:
- Unity project foundation and architecture
- Shared data contracts
- 6x5 board logic
- 10-second drag input for mouse and touch
- Match detection, cascades and refill
- Damage/combo/healing calculations
- Five-character battle model
- Leader Skills, Active Skills and passives framework
- Enemy countdown/action engine
- Character level/Ascension/Awakening logic
- Gold/Gems/Tickets/economy state
- Gacha probability, guarantees and Gather-In logic
- Pack summon state-machine hooks
- Stage loader and three-star objective evaluation
- Calendar-driven rotating-content scheduler
- Save/load with versioned save data
- Automated tests and validation

Claude is working in parallel on UI, presentation, VFX and bulk content. Do not overwrite Claude-owned presentation/content files. Shared contracts are yours to define first and should remain stable and data-driven.

Create equivalents of these shared contracts before large feature work:
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

Board requirements:
- 6 columns x 5 rows
- Fire, Water, Nature, Light, Dark, Heart
- Minimum match size 3
- Drag one orb through cells, swapping as it passes
- 10-second timer, early release allowed
- Resolve simultaneous matches and all cascades until stable
- Separate match groups must remain distinguishable

Combat requirements:
- No initiative or traditional RPG turn order
- Each separate elemental match creates a separate attack event
- Heart heals
- Fire > Nature > Water > Fire
- Light and Dark are mutually advantageous
- Five-character party
- Slot 1 supplies Leader Skill
- Skills manipulate/enhance the match system rather than replacing it

Enemy requirements:
- Visible configurable countdown
- One completed board resolution reduces countdown
- At zero, execute action then reset
- Reusable effects should support damage, conversion, locks, poison/hazards, blockers, binds, timer reduction, absorbs, combo shields, enrage and countdown manipulation

Progression requirements:
- 1-5 star pullable rarity
- 6-star Awakening through gameplay
- Awakening does NOT require dupes
- Ascension 0-5 from duplicate pulls
- Early Ascension improves Active Skill
- Later Ascension improves passive and/or modest stats
- Extra dupes after max Ascension convert to universal resource
- Data model supports separate base and awakened art/VFX references

Gacha requirements:
- 150 Gems single pull
- 1,500 Gems ten-pull
- Support Standard, Featured, Step-Up and Gather-In banners
- Gather-In steps configurable, e.g. 50 Gems -> 1 character, 100 -> 2, 150 -> 3, continuing upward to 10
- Rotation 1 final 10-pull guarantees a 5-star with boosted featured odds
- Rotation 2 final 10-pull guarantees the featured 5-star
- Results are determined before reveal presentation starts

Provide pack summon flow hooks equivalent to:
- PurchaseValidated
- PackPresented
- PackRipStarted
- PackOpened
- CardReady
- CardRevealed
- AllCardsRevealed
- ResultsComplete

Stage requirements:
- No stamina for permanent story
- Data-driven chapters/stages/waves
- Architecture supports about 20 chapters x 25 stages
- Three-star defaults: clear; finish above HP threshold; clear within configured board-resolution count

Scheduler requirements:
- Local date/time driven, no backend required in v1
- Supports rotating Featured/Gather-In/Step-Up banners, event chapters, daily/weekly dungeons, Awakening-material stages, towers and Boss Rush
- Clock access must be testable/mockable

Before mass content, prove one vertical slice:
- Working 6x5 board
- Mouse/touch drag
- 10-second timer
- Cascades
- Five-character party
- Separate attacks
- Heart healing
- One Leader Skill
- One Active Skill
- One enemy countdown action
- One boss mechanic
- One stage with three-star objectives
- One Ascension example
- One 6-star Awakening example
- One Standard summon
- One Gather-In summon
- Pack-state hooks
- Save/load
- One scheduled rotating-content example

Use Unity Test Framework where practical. Keep gameplay logic testable outside presentation. Commit frequently. Do not stop at empty scaffolding; leave a working playable vertical slice.