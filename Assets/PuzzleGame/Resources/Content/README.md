# Content bank (draft schemas)

JSON content discovered by folder via `Resources.LoadAll` (see
`Scripts/Content/ContentDb.cs`). One file may hold many entries; adding a
file requires no code change.

| Folder      | Wrapper object        | Notes |
|-------------|-----------------------|-------|
| characters/ | `{ "characters": [] }`| 1–5★ base; every 5★ defines a 6★ `awakened` form (no duplicates required) |
| enemies/    | `{ "enemies": [] }`   | data-driven `actions` (damage/convert/lock/poison/block/bind/timerDown/absorb/comboShield/enrage/heal) |
| stages/     | `{ "chapter": {} }`   | one chapter (≤25 stages) per file; waves reference enemy ids |
| dialogue/   | `{ "scenes": [] }`    | referenced from stages via `dialogueBefore/After` |
| banners/    | `{ "banners": [] }`   | standard/featured/gatherIn/stepUp; rates are DISPLAY copies of core config |
| events/     | `{ "events": [] }`    | eventChapter/materialDungeon/awakeningStage/challengeTower/bossRush |
| rewards/    | `{ "tables": [] }`    | weighted reward tables |
| schedule/   | `{ "entries": [] }`   | rotation entries (permanent, dated, daily, weekly) consumed via IScheduleSource |

Validate with:

```
Unity -batchmode -nographics -quit -projectPath . ^
  -executeMethod PuzzleGame.Presentation.EditorTools.ContentValidator.Run
```

**Contract note:** these are the presentation/content-side draft schemas
mirroring the design spec's data-contract list. Codex owns the
authoritative core schemas; when they land, migrate these files (field
mapping should be near 1:1) rather than forking the vocabulary.
