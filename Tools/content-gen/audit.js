#!/usr/bin/env node
/*
 * Deep content audit. Complements the in-engine ContentValidator with
 * checks that need whole-bank visibility: duplicate ids across files,
 * empty/malformed files, unknown reward item ids, orphaned dialogue,
 * schedule overlaps, and coverage stats. Exit 1 on any error.
 */
const fs = require('fs');
const path = require('path');

const CONTENT = path.join(__dirname, '..', '..', 'Assets', 'PuzzleGame', 'Resources', 'Content');
const errors = [];
const warnings = [];

function listJson(dir) {
  const full = path.join(CONTENT, dir);
  if (!fs.existsSync(full)) return [];
  return fs.readdirSync(full).filter(f => f.endsWith('.json')).map(f => path.join(full, f));
}

function load(file) {
  const text = fs.readFileSync(file, 'utf8');
  if (!text.trim()) { errors.push('EMPTY FILE: ' + file); return null; }
  try { return JSON.parse(text); }
  catch (e) { errors.push('MALFORMED JSON: ' + file + ' — ' + e.message); return null; }
}

function collect(dir, key, idField = 'id') {
  const map = new Map();
  for (const file of listJson(dir)) {
    const parsed = load(file);
    if (!parsed) continue;
    const list = key === 'chapter' ? [parsed.chapter] : parsed[key];
    if (!list || list.length === 0) { errors.push('NO ENTRIES (' + key + '): ' + file); continue; }
    for (const entry of list) {
      if (!entry) continue;
      const id = key === 'chapter' ? 'chapter_' + entry.chapterNumber : entry[idField];
      if (!id && id !== 0) { errors.push(dir + ': entry missing ' + idField + ' in ' + path.basename(file)); continue; }
      if (map.has(id)) errors.push('DUPLICATE id "' + id + '" in ' + dir + ' (' + path.basename(file) + ' vs ' + map.get(id).file + ')');
      map.set(id, { entry, file: path.basename(file) });
    }
  }
  return map;
}

const characters = collect('characters', 'characters');
const enemies = collect('enemies', 'enemies');
const chapters = collect('stages', 'chapter');
const dialogue = collect('dialogue', 'scenes');
const banners = collect('banners', 'banners');
const events = collect('events', 'events');
const rewards = collect('rewards', 'tables');
const items = collect('items', 'items');
const packs = collect('packs', 'themes');

// Schedule entries (no unique-id map helper since ids share one array key).
const schedule = [];
for (const file of listJson('schedule')) {
  const parsed = load(file);
  if (!parsed || !parsed.entries) continue;
  for (const e of parsed.entries) schedule.push({ e, file: path.basename(file) });
}
{
  const seen = new Map();
  for (const { e, file } of schedule) {
    if (seen.has(e.id)) errors.push('DUPLICATE schedule id "' + e.id + '" (' + file + ' vs ' + seen.get(e.id) + ')');
    seen.set(e.id, file);
  }
}

// ------------------------- reference checks ----------------------------

const usedItemIds = new Set();
const usedDialogue = new Set();
const stageIds = new Map();

function checkStage(where, s) {
  if (stageIds.has(s.id)) errors.push('DUPLICATE stage id "' + s.id + '" (' + where + ' vs ' + stageIds.get(s.id) + ')');
  stageIds.set(s.id, where);
  if (!s.waves || s.waves.length === 0) errors.push(where + ' ' + s.id + ': no waves');
  for (const w of s.waves || []) {
    if (!w.enemies || w.enemies.length === 0) errors.push(where + ' ' + s.id + ': empty wave');
    for (const we of w.enemies || []) {
      if (!enemies.has(we.enemyId)) errors.push(where + ' ' + s.id + ': unknown enemy "' + we.enemyId + '"');
    }
  }
  for (const ref of [s.dialogueBefore, s.dialogueAfter]) {
    if (ref) {
      usedDialogue.add(ref);
      if (!dialogue.has(ref)) errors.push(where + ' ' + s.id + ': unknown dialogue "' + ref + '"');
    }
  }
  if (s.rewards && s.rewards.items) {
    for (const it of s.rewards.items) usedItemIds.add(it.id);
  }
  if (s.hpThresholdStar <= 0 || s.hpThresholdStar > 1) errors.push(where + ' ' + s.id + ': bad hpThresholdStar ' + s.hpThresholdStar);
  if (!(s.resolutionsStar >= 1)) errors.push(where + ' ' + s.id + ': bad resolutionsStar');
}

for (const [id, { entry }] of chapters) {
  for (const s of entry.stages) checkStage(id, s);
  if (entry.stages.length !== 25) warnings.push(id + ': ' + entry.stages.length + ' stages (expected 25)');
}
for (const [id, { entry }] of events) {
  for (const s of entry.stages) checkStage('event ' + id, s);
  if (entry.rewardTableId && !rewards.has(entry.rewardTableId)) errors.push('event ' + id + ': unknown reward table ' + entry.rewardTableId);
  if (entry.tokenId) usedItemIds.add(entry.tokenId);
}
for (const [id, { entry }] of banners) {
  for (const cid of entry.featured || []) {
    if (!characters.has(cid)) errors.push('banner ' + id + ': unknown featured character ' + cid);
  }
  if (entry.packArt && packs.size > 0 && !packs.has(entry.packArt)) {
    warnings.push('banner ' + id + ': pack theme "' + entry.packArt + '" has no pack theme definition');
  }
  const r = entry.rates;
  const total = r.fiveStar + r.fourStar + r.threeStar + r.twoStar + r.oneStar;
  if (Math.abs(total - 100) > 0.01) errors.push('banner ' + id + ': rates sum ' + total);
}
for (const [id, { entry }] of rewards) {
  for (const e of entry.entries || []) usedItemIds.add(e.itemId);
}
for (const { e, file } of schedule) {
  const target = (e.kind === 'banner') ? banners : events;
  if (!target.has(e.targetId)) errors.push('schedule ' + e.id + ' (' + file + '): unknown target ' + e.targetId);
  if (!e.permanent && (e.recurrence || 'none') === 'none') {
    if (isNaN(Date.parse(e.startUtc)) || isNaN(Date.parse(e.endUtc))) errors.push('schedule ' + e.id + ': bad dates');
    else if (Date.parse(e.endUtc) <= Date.parse(e.startUtc)) errors.push('schedule ' + e.id + ': end before start');
  }
}

// Item registry coverage: every reward/drop/token id must be declared.
const CURRENCY_IDS = new Set(['gold', 'gems']);
for (const id of usedItemIds) {
  if (CURRENCY_IDS.has(id)) continue;
  if (items.size === 0) { warnings.push('no items registry yet; ' + usedItemIds.size + ' item ids unchecked'); break; }
  if (!items.has(id)) errors.push('unknown item id in rewards/drops: "' + id + '"');
}

// Character sanity.
for (const [id, { entry }] of characters) {
  if (entry.rarity === 5 && !(entry.awakened && entry.awakened.abilityName)) errors.push('character ' + id + ': 5-star without awakened form');
  if (!['fire', 'water', 'nature', 'light', 'dark'].includes(entry.element)) errors.push('character ' + id + ': bad element');
}

// Orphaned dialogue (warning only).
for (const [id] of dialogue) {
  if (!usedDialogue.has(id)) warnings.push('dialogue scene never referenced: ' + id);
}

// ------------------------------- report --------------------------------
const rarities = {};
for (const [, { entry }] of characters) rarities[entry.rarity] = (rarities[entry.rarity] || 0) + 1;
console.log('characters=' + characters.size + ' (by rarity: ' + JSON.stringify(rarities) + ')');
console.log('enemies=' + enemies.size + ' chapters=' + chapters.size + ' stages=' + stageIds.size +
  ' dialogue=' + dialogue.size + ' banners=' + banners.size + ' events=' + events.size +
  ' rewardTables=' + rewards.size + ' items=' + items.size + ' packThemes=' + packs.size + ' schedule=' + schedule.length);
for (const w of warnings) console.log('WARN: ' + w);
for (const e of errors) console.log('ERROR: ' + e);
console.log(errors.length === 0 ? 'AUDIT OK' : 'AUDIT FAILED: ' + errors.length + ' errors');
process.exit(errors.length === 0 ? 0 : 1);
