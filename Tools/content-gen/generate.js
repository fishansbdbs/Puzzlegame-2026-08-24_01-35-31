#!/usr/bin/env node
/*
 * Content generator for PuzzleGame bulk content.
 *
 * Expands hand-authored theme tables (chapters.data.js, banners.data.js,
 * events.data.js) into the validated JSON content schema under
 * Assets/PuzzleGame/Resources/Content/. Everything creative (names, jokes,
 * enemies, dialogue) is authored in the data tables; this script only does
 * stat scaling, wave composition and file plumbing, deterministically
 * (seeded RNG) so re-runs produce identical output.
 *
 * Run:  node Tools/content-gen/generate.js
 * Then validate in-engine with ContentValidator.
 */
const fs = require('fs');
const path = require('path');

const chapters = require('./chapters.data.js');
const banners = require('./banners.data.js');
const events = require('./events.data.js');

const ROOT = path.join(__dirname, '..', '..');
const CONTENT = path.join(ROOT, 'Assets', 'PuzzleGame', 'Resources', 'Content');

function mulberry32(seed) {
  let a = seed >>> 0;
  return function () {
    a |= 0; a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function writeJson(rel, obj) {
  const file = path.join(CONTENT, rel);
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, JSON.stringify(obj, null, 2) + '\n');
  console.log('wrote ' + rel);
}

// ---------------------------------------------------------------- chapters

const baseHp = n => Math.round(3200 * (1 + 0.5 * (n - 1)));
const baseAtk = n => Math.round(550 * (1 + 0.4 * (n - 1)));

function buildEnemy(ch, e) {
  const hp = Math.round(baseHp(ch.n) * (e.hpMul || 1));
  const atk = Math.round(baseAtk(ch.n) * (e.atkMul || 1));
  return {
    id: e.id,
    name: e.name,
    element: e.element,
    boss: !!e.boss,
    hp, atk,
    countdown: e.countdown || (e.boss ? 3 : 3),
    artRef: 'enemy_' + e.id,
    flavor: e.flavor || '',
    actions: e.actions,
  };
}

function stageKind(s) {
  if (s === 25) return 'boss';
  if (s === 10 || s === 18) return 'miniboss';
  if (s === 1 || s === 5 || s === 15) return 'story';
  return 'normal';
}

function waveCountFor(s, rng) {
  if (s <= 3) return 1;
  if (s <= 9) return 1 + (rng() < 0.5 ? 1 : 0);
  if (s <= 17) return 2;
  return 2 + (rng() < 0.45 ? 1 : 0);
}

function buildChapter(ch) {
  const rngName = 'ch' + String(ch.n).padStart(2, '0');
  const minions = ch.enemies.filter(e => !e.boss);
  const mini1 = ch.enemies.find(e => e.id === ch.miniboss1);
  const mini2 = ch.enemies.find(e => e.id === ch.miniboss2);
  const boss = ch.enemies.find(e => e.id === ch.boss);
  if (!mini1 || !mini2 || !boss) throw new Error('chapter ' + ch.n + ': missing boss refs');

  const sceneIds = new Set((ch.dialogue || []).map(d => d.id));
  const names = { ...(ch.stageNames || {}) };
  let poolIdx = 0;
  const pool = ch.namePool || [];
  const usedNames = new Set(Object.values(names));
  function stageName(s) {
    if (names[s]) return names[s];
    while (poolIdx < pool.length) {
      const candidate = pool[poolIdx++];
      if (!usedNames.has(candidate)) { usedNames.add(candidate); return candidate; }
    }
    return 'Deeper Into ' + ch.title + ' (' + s + ')';
  }

  const stages = [];
  for (let s = 1; s <= 25; s++) {
    const rng = mulberry32(ch.n * 7919 + s * 131);
    const kind = stageKind(s);
    const ramp = +(1 + ((s - 1) / 24) * 0.8).toFixed(2);
    const waves = [];
    if (kind === 'miniboss') {
      waves.push({ enemies: pickMinions(minions, rng, 2, ramp) });
      waves.push({ enemies: [{ enemyId: (s === 10 ? mini1 : mini2).id, hpMult: 1, atkMult: 1, countdownOverride: -1 }] });
    } else if (kind === 'boss') {
      waves.push({ enemies: pickMinions(minions, rng, 2, ramp) });
      waves.push({ enemies: [{ enemyId: boss.id, hpMult: 1, atkMult: 1, countdownOverride: -1 }] });
    } else {
      const wc = waveCountFor(s, rng);
      for (let w = 0; w < wc; w++) {
        waves.push({ enemies: pickMinions(minions, rng, 1 + (rng() < 0.55 ? 1 : 0), ramp) });
      }
    }
    const id = rngName + '_st' + String(s).padStart(2, '0');
    const drops = ch.drops || [];
    const rewards = {
      gold: Math.round((300 + s * 28) * (1 + 0.3 * (ch.n - 1))),
      gems: kind === 'boss' ? 30 : kind === 'miniboss' ? 10 : kind === 'story' ? 5 : 0,
      items: drops.length ? [{ id: drops[Math.floor(rng() * drops.length)], count: 1 + Math.floor(s / 10) }] : [],
      firstClearBonus: kind === 'boss' ? '300 Gems' : kind === 'miniboss' ? '100 Gems' : '',
    };
    const stage = {
      id,
      stageNumber: s,
      name: stageName(s),
      kind,
      background: ch.theme + '_' + (s <= 8 ? 'a' : s <= 17 ? 'b' : 'c'),
      waves,
      modifiers: [],
      hpThresholdStar: kind === 'boss' ? 0.4 : 0.5,
      resolutionsStar: 5 + waves.length * 4 + Math.floor(s / 4),
      dialogueBefore: dialogueRef(ch, s, 'before', sceneIds),
      dialogueAfter: dialogueRef(ch, s, 'after', sceneIds),
      rewards,
      staminaCost: 0,
    };
    stages.push(stage);
  }

  return {
    chapterFile: { chapter: { chapterNumber: ch.n, title: ch.title, theme: ch.theme, blurb: ch.blurb, stages } },
    enemyFile: { enemies: ch.enemies.map(e => buildEnemy(ch, e)) },
    dialogueFile: { scenes: ch.dialogue || [] },
  };
}

function dialogueRef(ch, s, phase, sceneIds) {
  const p = 'ch' + String(ch.n).padStart(2, '0');
  const candidates = {
    before: { 1: p + '_intro', 10: p + '_mini1', 15: p + '_mid', 18: p + '_mini2', 25: p + '_boss_before' },
    after: { 25: p + '_boss_after' },
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

// ---------------------------------------------------------------- banners

function buildBanners() {
  const out = { banners: [] };
  const sched = [];
  for (const b of banners.list) {
    out.banners.push({
      id: b.id,
      name: b.name,
      kind: b.kind,
      desc: b.desc,
      packArt: b.packArt || ('pack_' + b.id),
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
      minPoolRarity: 1,
    });
    if (b.window) {
      sched.push({
        id: 'sched_' + b.id, kind: 'banner', targetId: b.id, name: b.name,
        desc: 'Limited banner.', permanent: false,
        startUtc: b.window[0], endUtc: b.window[1], recurrence: 'none', weekday: 0,
      });
    }
  }
  return { bannerFile: out, schedule: sched };
}

// ---------------------------------------------------------------- events

function buildEvents(allEnemies) {
  const out = { events: [] };
  const tables = { tables: [] };
  const sched = [];
  for (const ev of events.list) {
    const rng = mulberry32(ev.id.split('').reduce((a, c) => a * 31 + c.charCodeAt(0) | 0, 17));
    const stages = [];
    const n = ev.stageCount || 6;
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
      stages.push({
        id: ev.id + '_st' + String(s).padStart(2, '0'),
        stageNumber: s,
        name: ev.stageNames && ev.stageNames[s - 1] ? ev.stageNames[s - 1] : ev.name + ' ' + s,
        kind,
        background: ev.theme + '_' + s,
        waves,
        modifiers: [],
        hpThresholdStar: last ? 0.4 : 0.5,
        resolutionsStar: 6 + waves.length * 4 + s,
        dialogueBefore: '', dialogueAfter: '',
        rewards: {
          gold: 400 + s * 120,
          gems: last ? 40 : mid ? 15 : 0,
          items: ev.tokenId ? [{ id: ev.tokenId, count: 8 + s * 3 }] : [],
          firstClearBonus: last && ev.clearTitle ? "Title: '" + ev.clearTitle + "'" : '',
        },
        staminaCost: 0,
      });
    }
    out.events.push({
      id: ev.id, name: ev.name, kind: ev.kind, desc: ev.desc, theme: ev.theme,
      tokenId: ev.tokenId || '', stages, rewardTableId: ev.rewardTableId || '',
    });
    if (ev.rewardTable) {
      tables.tables.push({ id: ev.rewardTableId, entries: ev.rewardTable });
    }
    if (ev.window) {
      sched.push({
        id: 'sched_' + ev.id, kind: ev.kind, targetId: ev.id, name: ev.name,
        desc: ev.desc, permanent: false, startUtc: ev.window[0], endUtc: ev.window[1],
        recurrence: 'none', weekday: 0,
      });
    } else if (ev.recurrence) {
      sched.push({
        id: 'sched_' + ev.id, kind: ev.kind, targetId: ev.id, name: ev.name,
        desc: ev.desc, permanent: false, startUtc: '', endUtc: '',
        recurrence: ev.recurrence, weekday: ev.weekday || 0,
      });
    } else {
      sched.push({
        id: 'sched_' + ev.id, kind: ev.kind, targetId: ev.id, name: ev.name,
        desc: ev.desc, permanent: true, startUtc: '', endUtc: '', recurrence: 'none', weekday: 0,
      });
    }
    // Enemy pool sanity: all ids must exist somewhere.
    for (const id of ev.enemyPool.concat([ev.boss])) {
      if (!allEnemies.has(id)) throw new Error('event ' + ev.id + ': unknown enemy ' + id);
    }
  }
  return { eventFile: out, rewardFile: tables, schedule: sched };
}

function pickId(pool, rng, scale) {
  return { enemyId: pool[Math.floor(rng() * pool.length)], hpMult: scale, atkMult: scale, countdownOverride: -1 };
}

// ---------------------------------------------------------------- main

function collectExistingEnemyIds() {
  const ids = new Set();
  const dir = path.join(CONTENT, 'enemies');
  for (const f of fs.readdirSync(dir)) {
    if (!f.endsWith('.json')) continue;
    const parsed = JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8'));
    for (const e of parsed.enemies || []) ids.add(e.id);
  }
  return ids;
}

for (const ch of chapters.list) {
  const built = buildChapter(ch);
  const nn = String(ch.n).padStart(2, '0');
  writeJson('enemies/chapter_' + nn + '.json', built.enemyFile);
  writeJson('stages/chapter_' + nn + '.json', built.chapterFile);
  if (built.dialogueFile.scenes.length) {
    writeJson('dialogue/chapter_' + nn + '.json', built.dialogueFile);
  }
}

const allEnemies = collectExistingEnemyIds();
const b = buildBanners();
writeJson('banners/generated_banners.json', b.bannerFile);
const e = buildEvents(allEnemies);
writeJson('events/generated_events.json', e.eventFile);
if (e.rewardFile.tables.length) writeJson('rewards/generated_tables.json', e.rewardFile);
writeJson('schedule/generated_schedule.json', { entries: b.schedule.concat(e.schedule) });

console.log('done: ' + chapters.list.length + ' chapters, ' + b.bannerFile.banners.length + ' banners, ' + e.eventFile.events.length + ' events');
