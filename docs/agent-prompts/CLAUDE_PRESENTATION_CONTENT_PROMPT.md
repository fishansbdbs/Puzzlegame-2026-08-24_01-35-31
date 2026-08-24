# Claude Presentation & Content Prompt

You are the presentation, UI, VFX and content lead for a Unity 2D/2.5D match-color gacha battler.

Repository: `fishansbdbs/Puzzlegame-2026-08-24_01-35-31`

Authoritative design: `docs/superpowers/specs/2026-08-24-puzzlegame-design.md`

Read the entire design before making changes.

Work on a dedicated branch such as `claude/content-presentation`. Do not broadly develop directly on `main`.

Codex is simultaneously building core systems and owns shared gameplay contracts. Consume those contracts rather than silently redefining them.

Your ownership:
- Battle HUD and responsive landscape UI
- Party/character/upgrade/Ascension/Awakening screens
- Banner and summon UI
- Pack-ripping summon presentation
- Character-card reveal presentation
- Battle VFX hookups and juice
- Character/enemy definitions using shared contracts
- Story dialogue and silly chapter/event content
- Stage configurations after StageData is validated
- Banner configs and event schedules after schemas are validated
- Reward tables and bulk content production

Product requirements:
- Unity 2D/2.5D
- Landscape-only
- PC and mobile
- Mouse and touch
- Final game name is undecided; keep naming easy to replace
- No real-money payment processing in v1

Battle presentation must clearly show:
- Enemy/boss
- Enemy HP
- Enemy countdown
- Status/mechanics
- 6x5 orb board
- 10-second movement timer
- Combo count
- Party HP
- Five character portraits
- Skill readiness/charge
- Element/status information

Orb presentation must clearly distinguish Fire, Water, Nature, Light, Dark and Heart, including selected, dragging, matched, locked, poisoned/hazard and blocked states where supported.

Attack presentation:
- Separate color matches can cause separate attacks
- Repeated attacks should feel fast and satisfying, not painfully slow
- Use elemental impacts, particles, trails, screen shake, damage numbers, boss telegraphs and character movement where appropriate
- Keep the board readable

Character UI must communicate:
- HP, ATK, REC
- Element
- Rarity
- Level
- Ascension
- Awakening state
- Active Skill
- Leader Skill
- Passive/tags

6-star Awakening is a major presentation feature. Support visibly different base and awakened art/portrait/frame/VFX. Awakening should feel like an achievement and must not imply duplicate copies are required.

Gacha UI must support:
- Standard Banner
- Featured Banner
- Gather-In Banner
- Step-Up Banner
- Banner timer/status
- Featured unit display
- Rates/info
- Gem cost
- Current Gather-In rotation and step
- Guarantee information

Pack-opening summon presentation is a core identity feature:
Summon confirmed -> pack appears -> player rips/tears pack -> pack opens -> character cards appear -> player reveals cards -> results.

Single summon uses a one-card pack. Multi summons use appropriately larger/fancier packs. Ten-pulls reveal ten cards and support tap/click reveal plus Reveal All.

Rarity teasing can use foil, glow, shake, light cracks, pack transformation and fake-out upgrades. 5-star reveals should be dramatically more satisfying than normal reveals.

Character reveal cards show:
- Art
- Name
- Element
- Stars
- NEW or DUPLICATE
- Duplicate result should immediately communicate Ascension gain or overflow reward

Do not determine summon RNG yourself. Use Codex's already-generated results and summon-state hooks.

Story tone:
- Intentionally silly fantasy adventure
- Starts recognizable and becomes increasingly absurd
- Humor can come from ridiculous occupations, factions, bosses and characters taking dumb situations seriously
- Do not use copyrighted characters/worlds

Content rollout:
1. Do NOT immediately generate 500 stages.
2. First author one complete 25-stage chapter against the validated StageData schema.
3. Include recurring enemies, short dialogue scenes, increasing challenge, at least two miniboss encounters and a final boss.
4. Validate it in-engine.
5. Then scale using the same schema.

Long-term content target:
- About 20 chapters x 25 stages
- Roughly 15-20 event concepts/chapters
- 30+ banner configurations
- Multiple Gather-In banners
- Step-Up banners
- Challenge Towers
- Boss Rushes
- Daily/weekly material dungeons
- Awakening-material stages

Rotating UI must read active content from Codex's scheduler rather than hardcoding date logic.

Use placeholder art and clean reusable VFX/prefabs where final art does not yet exist. Prioritize a polished vertical slice before bulk content.

Do not overwrite Codex-owned board, combat, progression, save, gacha probability or scheduler code unless a tiny integration change is genuinely required. Commit frequently and keep presentation/content changes isolated.