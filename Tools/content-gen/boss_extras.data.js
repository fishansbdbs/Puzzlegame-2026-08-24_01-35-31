// Second-pass boss design: signature behaviors appended to authored boss
// kits, and cameo appearances where a familiar miniboss returns scaled to
// the current chapter ("oh, THIS guy again").
module.exports = {
  // Extra actions appended to the enemy's authored action list.
  actions: {
    // ---- chapter bosses -------------------------------------------------
    boss_barkimedes: [
      { name: 'Fiscal Year End', desc: 'ENRAGES below half strength. The rings tighten.', type: 'enrage', amount: 0, count: 0, color: '' } ],
    boss_goldtooth: [
      { name: 'Market Manipulation', desc: 'Combo shield: only 4+ combos pierce his portfolio.', type: 'comboShield', count: 4 } ],
    boss_hold_queen: [
      { name: 'One With The Queue', desc: 'Absorbs Water damage this rotation. She IS the water.', type: 'absorb', color: 'water' } ],
    boss_vp_vibes: [
      { name: 'Vibe Threshold', desc: 'Combo shield: 3+ combos required. Match the energy.', type: 'comboShield', count: 3 } ],
    boss_chairman_polt: [
      { name: 'Call to Quorum', desc: 'Summons a spectral board member to the fight.', type: 'summon', count: 1 } ],
    boss_grand_gateau: [
      { name: 'Blow Out the Candles', desc: 'TAUNTS: all attacks must target the cake. It insists.', type: 'taunt' } ],
    boss_final_notice: [
      { name: 'Third Attempt', desc: 'ENRAGES below half strength. There is no fourth attempt.', type: 'enrage' } ],
    boss_manager_caldera: [
      { name: 'Geothermal Rewards Program', desc: 'Absorbs Fire damage this rotation. Loyalty pays.', type: 'absorb', color: 'fire' } ],
    boss_lich_clerk: [
      { name: 'Take a Number', desc: 'Combo shield: 3+ combos required to reach the window.', type: 'comboShield', count: 3 } ],
    boss_mall_heart: [
      { name: 'Store Policy', desc: 'Absorbs Dark damage this rotation. No returns.', type: 'absorb', color: 'dark' } ],
    boss_glacier_ceo: [
      { name: 'Cold Snap', desc: 'ENRAGES below half strength. The handshake tightens.', type: 'enrage' } ],
    boss_dead_letter: [
      { name: 'Sealed Court', desc: 'Combo shield: 4+ combos to breach the pigeonholes.', type: 'comboShield', count: 4 } ],
    boss_recurring: [
      { name: 'You Cannot Decline', desc: 'TAUNTS: you cannot target anything else. You are IN the meeting.', type: 'taunt' } ],
    boss_moonlord: [
      { name: 'Dark Side Holdings', desc: 'Absorbs Dark damage this rotation. He owns that too.', type: 'absorb', color: 'dark' } ],
    boss_complaints_prince: [
      { name: 'Backlog Overflow', desc: 'ENRAGES below half strength. Every ticket screams at once.', type: 'enrage' } ],
    boss_membership: [
      { name: 'Clause 4.4', desc: 'Combo shield: 4+ combos, per the agreement you signed.', type: 'comboShield', count: 4 } ],
    boss_undersecretary: [
      { name: 'Delegate Downward', desc: 'Summons a subordinate. There is ALWAYS a subordinate.', type: 'summon', count: 1 } ],
    boss_formula_one: [
      { name: 'Absolute Value', desc: 'Absorbs Light damage this rotation. |damage| becomes healing.', type: 'absorb', color: 'light' } ],
    boss_ceo_everything: [
      { name: 'Fiduciary Rage', desc: 'ENRAGES below half strength. Shareholder value intensifies.', type: 'enrage' },
      { name: 'Executive Session', desc: 'Combo shield: 4+ combos to get on the agenda.', type: 'comboShield', count: 4 } ],

    // ---- minibosses that grow into recurring threats --------------------
    boss_ranger_doreen: [
      { name: 'Jurisdiction', desc: 'TAUNTS: all complaints route through Doreen.', type: 'taunt' } ],
    boss_support_shark: [
      { name: 'Escalation Frenzy', desc: 'ENRAGES below half strength. Smells unresolved tickets.', type: 'enrage' } ],
    boss_kpi_beast: [
      { name: 'Minimum Viable Combo', desc: 'Combo shield: 3+ combos or the metrics reject you.', type: 'comboShield', count: 3 } ],
    boss_gainz_titan: [
      { name: 'Pre-Workout Kicks In', desc: 'ENRAGES below half strength. Do not ask what is in it.', type: 'enrage' } ],
  },

  // Familiar faces return, rescaled to the local chapter baseline.
  // hpMult compensates for the stat gap between their home chapter and the
  // cameo chapter, so they land as a real elite threat, not a pushover.
  cameos: [
    { chapter: 5, stage: 21, enemyId: 'boss_ranger_doreen', hpMult: 2.0 },   // Doreen moonlights as HR security
    { chapter: 8, stage: 21, enemyId: 'boss_slime_dave', hpMult: 4.5 },      // Dave transferred to logistics
    { chapter: 11, stage: 13, enemyId: 'boss_laminator', hpMult: 4.0 },      // mall lamination kiosk
    { chapter: 12, stage: 21, enemyId: 'boss_flintbeard', hpMult: 3.2 },     // selling his forge as a timeshare
    { chapter: 14, stage: 13, enemyId: 'boss_org_chart', hpMult: 2.5 },      // the org chart attends every meeting
    { chapter: 16, stage: 21, enemyId: 'boss_support_shark', hpMult: 3.4 },  // promoted to Tier 9
    { chapter: 18, stage: 13, enemyId: 'boss_registrar', hpMult: 1.7 },      // seconded to Bureaucracy Prime
    { chapter: 19, stage: 21, enemyId: 'boss_presentation', hpMult: 1.6 },   // the slides live in the grid now
    { chapter: 20, stage: 13, enemyId: 'boss_wolf_rep', hpMult: 10.5 },      // Fenris: contractual exhibition match
    { chapter: 20, stage: 21, enemyId: 'boss_lich_clerk', hpMult: 1.9 },     // processing the final paperwork
  ],
};
