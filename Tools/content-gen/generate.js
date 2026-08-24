#!/usr/bin/env node
/*
 * Content generator for PuzzleGame bulk content (second pass).
 *
 * Expands hand-authored theme tables into the validated JSON content
 * schema under Assets/PuzzleGame/Resources/Content/. Everything creative
 * (names, jokes, enemies, dialogue) is authored in the data tables; this
 * script does stat scaling, wave composition, mechanical identity,
 * milestones and plumbing — deterministically (seeded RNG), so re-runs
 * produce identical output.
 *
 * Safety: only writes chapter_02..chapter_20 and *generated* files. It
 * refuses duplicate ids, unknown references, and insufficient name pools
 * instead of emitting broken or placeholder output.
 *
 * Run:  node Tools/content-gen/generate.js
 * Then: node Tools/content-gen/audit.js  + in-engine ContentValidator.
 */
const fs = require('fs');
const path = require('path');

const chapters = require('./chapters.data.js');
const banners = require('./banners.data.js');
const events = require('./events.data.js');
const bossExtras = require('./boss_extras.data.js');
const storyExtra = require('./story_extra.data.js');

const ROOT = path.join(__dirname, '..', '..');
const CONTENT = path.join(ROOT, 'Assets', 'PuzzleGame', 'Resources', 'Content');

function fail(msg) {
  console.error('GENERATOR ERROR: ' + msg);
  process.exit(1);
}

function mulberry32(seed) {
  let a = seed >>> 0;
  return function () {
    a |= 0; a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const ALLOWED_WRITE = /^(enemies\/chapter_(0[2-9]|1[0-9]|20)\.json|stages\/chapter_(0[2-9]|1[0-9]|20)\.json|dialogue\/chapter_(0[2-9]|1[0-9]|20)\.json|dialogue\/generated_.*\.json|banners\/generated_.*\.json|events\/generated_.*\.json|rewards\/generated_.*\.json|schedule\/generated_.*\.json)$/;

function writeJson(rel, obj) {
  if (!ALLOWED_WRITE.test(rel)) fail('refusing to write outside the generated set: ' + rel);
  const file = path.join(CONTENT, rel);
  fs.mkdirSync(path.dirname(file), { recursive: true });
  const text = JSON.stringify(obj, null, 2) + '\n';
  if (text.length < 40) fail('refusing to write near-empty file: ' + rel);
  fs.writeFileSync(file, text);
  console.log('wrote ' + rel);
}

// Global id registries so the generator itself refuses duplicates.
const seenEnemyIds = new Set();
const seenStageIds = new Set();
const seenSceneIds = new Set();

function registerId(set, id, what) {
  if (set.has(id)) fail('duplicate ' + what + ' id: ' + id);
  set.add(id);
}

// Pre-register hand-authored (non-generated) content so cross-file
// duplicates are caught and cameo references can be verified.
function preloadHandAuthored() {
  const handEnemyFiles = ['chapter01_meadow.json', 'event_bog.json', 'shared_dungeons.json'];
  for (const f of handEnemyFiles) {
    const parsed = JSON.parse(fs.readFileSync(path.join(CONTENT, 'enemies', f), 'utf8'));
    for (const e of parsed.enemies) registerId(seenEnemyIds, e.id, 'enemy');
  }
  const ch1 = JSON.parse(fs.readFileSync(path.join(CONTENT, 'stages', 'chapter_01.json'), 'utf8'));
  for (const s of ch1.chapter.stages) registerId(seenStageIds, s.id, 'stage');
  for (const f of ['chapter_01.json', 'event_bog.json']) {
    const file = path.join(CONTENT, 'dialogue', f);
    if (!fs.existsSync(file)) continue;
    const parsed = JSON.parse(fs.readFileSync(file, 'utf8'));
    for (const s of parsed.scenes) registerId(seenSceneIds, s.id, 'dialogue scene');
  }
}

// ------------------------------------------------------------ mechanics

// Per-chapter mechanical identity: modifier pool + note + weighting.
// Modifiers are data-driven hooks interpreted by core (see the validator's
// allowlist and docs/INTEGRATION-presentation.md).
const MECHANICS = {
  2: { note: 'Lock pressure: permits hold your orbs hostage.', pool: ['start_locks:2', 'start_locks:3'] },
  3: { note: 'Fire conversion pressure and heavy single hits.', pool: ['element_bonus:fire', 'start_blockers:1'] },
  4: { note: 'Timer pressure: hold music eats your move time.', pool: ['move_time_minus:1', 'move_time_minus:2'] },
  5: { note: 'Binds and screening locks.', pool: ['start_locks:2', 'healing_reduced'] },
  6: { note: 'Poison boards: spores and grievances everywhere.', pool: ['start_poison:2', 'start_poison:3'] },
  7: { note: 'Enemy healing pressure: desserts self-frost.', pool: ['healing_reduced', 'start_poison:2'] },
  8: { note: 'Timer + lock combo: deliveries wait for no one.', pool: ['move_time_minus:1', 'start_locks:2'] },
  9: { note: 'Fire conversion and enemy self-healing.', pool: ['element_bonus:water', 'start_locks:2'] },
  10: { note: 'Queue mechanics: locks, blockers and slow dread.', pool: ['start_locks:3', 'start_blockers:1'] },
  11: { note: 'Mixed disruption: retail is chaos.', pool: ['start_poison:2', 'move_time_minus:1'] },
  12: { note: 'Frozen orbs and closing windows.', pool: ['start_locks:3', 'move_time_minus:1'] },
  13: { note: 'Binds: signatures required mid-battle.', pool: ['start_locks:2', 'healing_reduced'] },
  14: { note: 'Meetings drain your movement time.', pool: ['move_time_minus:2', 'start_blockers:1'] },
  15: { note: 'Blockers: property disputes on the board.', pool: ['start_blockers:1', 'start_blockers:2'] },
  16: { note: 'Hold music: timer and poison pressure.', pool: ['move_time_minus:2', 'start_poison:2'] },
  17: { note: 'Combo discipline: sentinels demand form.', pool: ['combo_shield:3', 'combo_shield:4'] },
  18: { note: 'Full bureaucratic suite: locks and binds.', pool: ['start_locks:3', 'start_blockers:1'] },
  19: { note: 'Conversion chaos: the grid recalculates.', pool: ['combo_shield:4', 'start_locks:2'] },
  20: { note: 'Everything at once. The final review holds nothing back.', pool: ['combo_shield:4', 'move_time_minus:2', 'start_locks:3'] },
};

// Star-goal tuning by chapter: forgiving early, meaningful mid, tight late.
function starTuning(n, rng) {
  let hp, resSlack;
  if (n <= 3) { hp = 0.3; resSlack = 6; }
  else if (n <= 8) { hp = 0.4 + (rng() < 0.5 ? 0.05 : 0); resSlack = 4; }
  else if (n <= 14) { hp = 0.5; resSlack = 2; }
  else { hp = 0.55 + (rng() < 0.4 ? 0.05 : 0); resSlack = 1; }
  return { hp: +hp.toFixed(2), resSlack };
}

const ELITE_STAGES = [7, 13, 21];

// ------------------------------------------------------------- chapters

const baseHp = n => Math.round(3200 * (1 + 0.5 * (n - 1)));
const baseAtk = n => Math.round(550 * (1 + 0.4 * (n - 1)));

function buildEnemy(ch, e) {
  const hp = Math.round(baseHp(ch.n) * (e.hpMul || 1));
  const atk = Math.round(baseAtk(ch.n) * (e.atkMul || 1));
  const actions = e.actions.slice();
  const extras = bossExtras.actions[e.id];
  if (extras) actions.push(...extras);
  return {
    id: e.id,
    name: e.name,
    element: e.element,
    boss: !!e.boss,
    hp, atk,
    countdown: e.countdown || 3,
    artRef: 'enemy_' + e.id,
    flavor: e.flavor || '',
    actions,
  };
}

function stageKind(s) {
  if (s === 25) return 'boss';
  if (s === 10 || s === 18) return 'miniboss';
  if (s === 1 || s === 5 || s === 15 || s === 24) return 'story';
  return 'normal';
}

function waveCountFor(s, rng) {
  if (s <= 3) return 1;
  if (s <= 9) return 1 + (rng() < 0.5 ? 1 : 0);
  if (s <= 17) return 2;
  return 2 + (rng() < 0.45 ? 1 : 0);
}

function buildChapter(ch) {
  const prefix = 'ch' + String(ch.n).padStart(2, '0');
  const minions = ch.enemies.filter(e => !e.boss);
  const mini1 = ch.enemies.find(e => e.id === ch.miniboss1);
  const mini2 = ch.enemies.find(e => e.id === ch.miniboss2);
  const boss = ch.enemies.find(e => e.id === ch.boss);
  if (!mini1 || !mini2 || !boss) fail('chapter ' + ch.n + ': missing boss refs');
  for (const e of ch.enemies) registerId(seenEnemyIds, e.id, 'enemy');

  // Merge authored dialogue: base scenes from the chapter table plus the
  // second-pass story scenes (mid/mini2/preboss and any additions).
  const scenes = (ch.dialogue || []).concat(storyExtra[ch.n] || []);
  const sceneIds = new Set();
  for (const scene of scenes) {
    registerId(seenSceneIds, scene.id, 'dialogue scene');
    sceneIds.add(scene.id);
    if (!scene.lines || scene.lines.length === 0) fail('empty dialogue scene ' + scene.id);
  }

  const names = { ...(ch.stageNames || {}) };
  const pool = (ch.namePool || []).slice();
  const keyed = Object.keys(names).length;
  if (keyed + pool.length < 25) {
    fail('chapter ' + ch.n + ': name pool too small (' + keyed + ' keyed + ' + pool.length + ' pooled < 25)');
  }
  let poolIdx = 0;
  const usedNames = new Set(Object.values(names));
  function stageName(s) {
    if (names[s]) return names[s];
    while (poolIdx < pool.length) {
      const candidate = pool[poolIdx++];
      if (!usedNames.has(candidate)) { usedNames.add(candidate); return candidate; }
    }
    fail('chapter ' + ch.n + ': ran out of stage names at stage ' + s);
  }

  const mech = MECHANICS[ch.n] || { note: '', pool: [] };
  const cameos = (bossExtras.cameos || []).filter(c => c.chapter === ch.n);
  for (const cameo of cameos) {
    if (!seenEnemyIds.has(cameo.enemyId)) fail('chapter ' + ch.n + ': cameo references unknown enemy ' + cameo.enemyId);
  }

  const stages = [];
  for (let s = 1; s <= 25; s++) {
    const rng = mulberry32(ch.n * 7919 + s * 131);
    const kind = stageKind(s);
    const elite = ELITE_STAGES.includes(s);
    const ramp = +(1 + ((s - 1) / 24) * 0.8).toFixed(2);
    const waves = [];
    if (kind === 'miniboss') {
      waves.push({ enemies: pickMinions(minions, rng, 2, ramp) });
      waves.push({ enemies: [{ enemyId: (s === 10 ? mini1 : mini2).id, hpMult: 1, atkMult: 1, countdownOverride: -1 }] });
    } else if (kind === 'boss') {
      // Late chapters get a longer gauntlet before the boss.
      waves.push({ enemies: pickMinions(minions, rng, 2, ramp) });
      if (ch.n >= 10) waves.push({ enemies: pickMinions(minions, rng, 2, ramp) });
      waves.push({ enemies: [{ enemyId: boss.id, hpMult: 1, atkMult: 1, countdownOverride: -1 }] });
    } else if (elite) {
      // Elite: two hardened minions with faster countdowns — pressure over HP inflation.
      const picks = pickMinions(minions, rng, 2, +(ramp * 1.1).toFixed(2));
      for (const p of picks) {
        const def = minions.find(m => m.id === p.enemyId);
        p.countdownOverride = Math.max(2, (def.countdown || 3) - 1);
      }
      waves.push({ enemies: picks });
    } else {
      const wc = waveCountFor(s, rng);
      for (let w = 0; w < wc; w++) {
        waves.push({ enemies: pickMinions(minions, rng, 1 + (rng() < 0.55 ? 1 : 0), ramp) });
      }
    }
    // Authored cameo: a familiar miniboss returns as the final wave.
    const cameo = cameos.find(c => c.stage === s);
    if (cameo) {
      waves.push({ enemies: [{ enemyId: cameo.enemyId, hpMult: cameo.hpMult, atkMult: cameo.atkMult || cameo.hpMult, countdownOverride: -1 }] });
    }

    // Mechanical identity: modifiers ramp up through the chapter.
    const modifiers = [];
    if (elite) modifiers.push('elite');
    if (mech.pool.length > 0) {
      const chance = kind === 'boss' ? 0.9 : kind === 'miniboss' ? 0.7 : elite ? 0.8 : (s >= 12 ? 0.4 : s >= 6 ? 0.2 : 0);
      if (rng() < chance) modifiers.push(mech.pool[Math.floor(rng() * mech.pool.length)]);
    }

    const tuning = starTuning(ch.n, rng);
    const drops = ch.drops || [];
    const gemBase = kind === 'boss' ? 30 : kind === 'miniboss' ? 10 : kind === 'story' ? 5 : elite ? 5 : 0;
    const stage = {
      id: prefix + '_st' + String(s).padStart(2, '0'),
      stageNumber: s,
      name: stageName(s),
      kind,
      background: ch.theme + '_' + (s <= 8 ? 'a' : s <= 17 ? 'b' : 'c'),
      waves,
      modifiers,
      hpThresholdStar: kind === 'boss' ? Math.max(0.25, +(tuning.hp - 0.1).toFixed(2)) : tuning.hp,
      resolutionsStar: 5 + waves.length * 4 + Math.floor(s / 4) + tuning.resSlack + (cameo ? 3 : 0),
      dialogueBefore: dialogueRef(prefix, s, 'before', sceneIds),
      dialogueAfter: dialogueRef(prefix, s, 'after', sceneIds),
      rewards: {
        gold: Math.round((300 + s * 28) * (1 + 0.3 * (ch.n - 1)) * (elite ? 1.5 : 1)),
        gems: gemBase,
        items: drops.length ? [{ id: drops[Math.floor(rng() * drops.length)], count: (1 + Math.floor(s / 10)) * (elite ? 2 : 1) }] : [],
        firstClearBonus: kind === 'boss' ? '300 Gems' : kind === 'miniboss' ? '100 Gems' : elite ? '50 Gems' : '',
      },
      staminaCost: 0,
    };
    registerId(seenStageIds, stage.id, 'stage');
    stages.push(stage);
  }

  // Chapter star milestones (max 75 stars).
  const gemScale = 1 + 0.1 * (ch.n - 1);
  const milestones = [
    { stars: 25, gems: Math.round(50 * gemScale), items: [] },
    { stars: 50, gems: Math.round(100 * gemScale), items: ch.drops ? [{ id: ch.drops[0], count: 5 }] : [] },
    { stars: 75, gems: Math.round(200 * gemScale), items: [{ id: 'radiant_core', count: ch.n >= 10 ? 3 : 2 }] },
  ];

  return {
    chapterFile: {
      chapter: {
        chapterNumber: ch.n, title: ch.title, theme: ch.theme, blurb: ch.blurb,
        mechanicNote: mech.note, starMilestones: milestones, stages,
      },
    },
    enemyFile: { enemies: ch.enemies.map(e => buildEnemy(ch, e)) },
    dialogueFile: { scenes },
  };
}

function dialogueRef(prefix, s, phase, sceneIds) {
  const candidates = {
    before: {
      1: prefix + '_intro', 10: prefix + '_mini1', 15: prefix + '_mid',
      18: prefix + '_mini2', 24: prefix + '_preboss', 25: prefix + '_boss_before',
    },
    after: { 25: prefix + '_boss_after' },
  };
  const id = candidates[phase][s];
  return id && sceneIds.has(id) ? id : '';
}

function pickMinions(minions, rng, count, ramp) {
  const picks = [];
  for (let i = 0; i < count; i++) {
    const e = minions[Math.floor(rng() * minions.length)];
    picks.push({ enemyId: e.id, hpMult: ramp, atkMult: ramp, countdownOverride: -1 });
  }
  return picks;
}

// -------------------------------------------------------------- banners

// Pack theme routing for generated banners (defined in Content/packs).
const PACK_MAP = {
  fac_guild_gains: 'pack_fire', fac_order_moist: 'pack_water', fac_root_authority: 'pack_nature',
  fac_bright_bureau: 'pack_light', fac_nap_dynasty: 'pack_dark',
  seasonal_spooky: 'pack_dark', seasonal_harvest: 'pack_nature',
  seasonal_midwinter: 'pack_light', seasonal_lunar_new_year: 'pack_comedy',
  stepup_object_lesson: 'pack_comedy', stepup_union_drive: 'pack_comedy', stepup_royal_treatment: 'pack_anniversary',
  newplayer_first_steps: 'pack_beginner', revival_hall_of_legends: 'pack_revival', villain_appreciation: 'pack_villain',
};

function packFor(b) {
  if (b.packArt) return b.packArt;
  if (PACK_MAP[b.id]) return PACK_MAP[b.id];
  if (b.kind === 'gatherIn') return 'pack_gatherin';
  if (b.kind === 'stepUp') return 'pack_standard';
  return 'pack_standard';
}

function buildBanners() {
  const out = { banners: [] };
  const sched = [];
  const ids = new Set();
  for (const b of banners.list) {
    if (ids.has(b.id)) fail('duplicate banner id ' + b.id);
    ids.add(b.id);
    out.banners.push({
      id: b.id,
      name: b.name,
      kind: b.kind,
      desc: b.desc,
      packArt: packFor(b),
      featured: b.featured || [],
      singleCost: b.singleCost || 150,
      multiCost: b.multiCost || 1500,
      rates: Object.assign(
        { fiveStar: 4, fourStar: 16, threeStar: 40, twoStar: 25, oneStar: 15, featuredShareOfFiveStar: b.featured && b.featured.length ? 50 : 0 },
        b.rates || {}),
      guarantee: b.guarantee || '10-pulls always contain at least one 4★.',
      steps: b.steps || [],
      rotationIndex: b.rotationIndex || 0,
      poolTags: b.poolTags || [],
      minPoolRarity: b.minPoolRarity || 1,
    });
    if (b.window) {
      sched.push({
        id: 'sched_' + b.id, kind: 'banner', targetId: b.id, name: b.name,
        desc: 'Limited banner.', permanent: false,
        startUtc: b.window[0], endUtc: b.window[1], recurrence: 'none', weekday: 0,
      });
    } else if (b.permanent) {
      sched.push({
        id: 'sched_' + b.id, kind: 'banner', targetId: b.id, name: b.name,
        desc: 'Always available.', permanent: true, startUtc: '', endUtc: '', recurrence: 'none', weekday: 0,
      });
    }
  }
  return { bannerFile: out, schedule: sched };
}

// --------------------------------------------------------------- events

function buildEvents() {
  const out = { events: [] };
  const tables = { tables: [] };
  const sched = [];
  const dialogueScenes = [];
  const ids = new Set();
  for (const ev of events.list) {
    if (ids.has(ev.id)) fail('duplicate event id ' + ev.id);
    ids.add(ev.id);
    const rng = mulberry32(ev.id.split('').reduce((a, c) => (a * 31 + c.charCodeAt(0)) | 0, 17));
    const stages = [];
    const n = ev.stageCount || 6;
    const evSceneIds = new Set();
    for (const scene of ev.dialogue || []) {
      registerId(seenSceneIds, scene.id, 'dialogue scene');
      evSceneIds.add(scene.id);
      dialogueScenes.push(scene);
    }
    for (let s = 1; s <= n; s++) {
      const last = s === n;
      const mid = !last && s === Math.ceil(n / 2);
      const kind = last ? 'boss' : mid ? 'miniboss' : 'normal';
      const pool = ev.enemyPool.filter(id => id !== ev.boss);
      const scale = +(ev.scale * (1 + (s - 1) * 0.12)).toFixed(2);
      const waves = [];
      if (last) {
        waves.push({ enemies: [pickId(pool, rng, scale)] });
        waves.push({ enemies: [{ enemyId: ev.boss, hpMult: ev.bossScale || ev.scale, atkMult: ev.scale, countdownOverride: -1 }] });
      } else {
        waves.push({ enemies: [pickId(pool, rng, scale)].concat(rng() < 0.5 ? [pickId(pool, rng, scale)] : []) });
        if (rng() < 0.5) waves.push({ enemies: [pickId(pool, rng, scale)] });
      }
      const dlgBefore = (ev.stageDialogue && ev.stageDialogue[s] && ev.stageDialogue[s].before) || '';
      const dlgAfter = (ev.stageDialogue && ev.stageDialogue[s] && ev.stageDialogue[s].after) || '';
      for (const reference of [dlgBefore, dlgAfter]) {
        if (reference && !evSceneIds.has(reference)) fail('event ' + ev.id + ': unknown dialogue ref ' + reference);
      }
      const stage = {
        id: ev.id + '_st' + String(s).padStart(2, '0'),
        stageNumber: s,
        name: ev.stageNames && ev.stageNames[s - 1] ? ev.stageNames[s - 1] : fail('event ' + ev.id + ': missing stage name ' + s),
        kind,
        background: ev.theme + '_' + s,
        waves,
        modifiers: (ev.modifiers && ev.modifiers[s]) || [],
        hpThresholdStar: last ? 0.4 : 0.5,
        resolutionsStar: 6 + waves.length * 4 + s,
        dialogueBefore: dlgBefore, dialogueAfter: dlgAfter,
        rewards: {
          gold: 400 + s * 120,
          gems: last ? 40 : mid ? 15 : 0,
          items: ev.tokenId ? [{ id: ev.tokenId, count: 8 + s * 3 }] : [],
          firstClearBonus: last && ev.clearTitle ? "Title: '" + ev.clearTitle + "'" : '',
        },
        staminaCost: 0,
      };
      registerId(seenStageIds, stage.id, 'stage');
      stages.push(stage);
    }
    out.events.push({
      id: ev.id, name: ev.name, kind: ev.kind, desc: ev.desc, theme: ev.theme,
      tokenId: ev.tokenId || '', stages, rewardTableId: ev.rewardTableId || '',
    });
    if (ev.rewardTable) {
      tables.tables.push({ id: ev.rewardTableId, entries: ev.rewardTable });
    }
    if (ev.window) {
      sched.push({ id: 'sched_' + ev.id, kind: ev.kind, targetId: ev.id, name: ev.name, desc: ev.desc, permanent: false, startUtc: ev.window[0], endUtc: ev.window[1], recurrence: 'none', weekday: 0 });
    } else if (ev.recurrence) {
      sched.push({ id: 'sched_' + ev.id, kind: ev.kind, targetId: ev.id, name: ev.name, desc: ev.desc, permanent: false, startUtc: '', endUtc: '', recurrence: ev.recurrence, weekday: ev.weekday || 0 });
    } else {
      sched.push({ id: 'sched_' + ev.id, kind: ev.kind, targetId: ev.id, name: ev.name, desc: ev.desc, permanent: true, startUtc: '', endUtc: '', recurrence: 'none', weekday: 0 });
    }
    for (const id of ev.enemyPool.concat([ev.boss])) {
      if (!seenEnemyIds.has(id)) fail('event ' + ev.id + ': unknown enemy ' + id);
    }
  }
  return { eventFile: out, rewardFile: tables, schedule: sched, dialogueScenes };
}

function pickId(pool, rng, scale) {
  return { enemyId: pool[Math.floor(rng() * pool.length)], hpMult: scale, atkMult: scale, countdownOverride: -1 };
}

// ----------------------------------------------------------------- main

preloadHandAuthored();

for (const ch of chapters.list) {
  const built = buildChapter(ch);
  const nn = String(ch.n).padStart(2, '0');
  writeJson('enemies/chapter_' + nn + '.json', built.enemyFile);
  writeJson('stages/chapter_' + nn + '.json', built.chapterFile);
  if (built.dialogueFile.scenes.length) {
    writeJson('dialogue/chapter_' + nn + '.json', built.dialogueFile);
  }
}

const b = buildBanners();
writeJson('banners/generated_banners.json', b.bannerFile);
const e = buildEvents();
writeJson('events/generated_events.json', e.eventFile);
if (e.rewardFile.tables.length) writeJson('rewards/generated_tables.json', e.rewardFile);
if (e.dialogueScenes.length) writeJson('dialogue/generated_events.json', { scenes: e.dialogueScenes });
writeJson('schedule/generated_schedule.json', { entries: b.schedule.concat(e.schedule) });

console.log('done: ' + chapters.list.length + ' chapters, ' + b.bannerFile.banners.length +
  ' banners, ' + e.eventFile.events.length + ' events, ' +
  seenStageIds.size + ' total stages, ' + seenSceneIds.size + ' total scenes');
