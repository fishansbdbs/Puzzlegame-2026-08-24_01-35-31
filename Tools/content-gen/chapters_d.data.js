// Chapters 17-20: the cosmic endgame. Maximum absurdity, played completely straight.
module.exports = [
  {
    n: 17, title: 'The Astral Gym', theme: 'astralgym',
    blurb: 'Where constellations train. The dumbbells are moons. The membership contract is older than gravity and harder to cancel.',
    drops: ['star_chalk', 'cosmic_protein', 'membership_card'],
    enemies: [
      { id: 'star_spotter', name: 'Star Spotter', element: 'light', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Spots for the constellations. Never skips leg day. Legs are 40 lightyears long.',
        actions: [
          { name: 'Spot Check', desc: 'A supportive but devastating shove.', type: 'damage', amount: 1 },
          { name: 'Form Correction', desc: 'Converts 3 orbs to Light.', type: 'convert', count: 3, color: 'light' } ] },
      { id: 'nebula_lifter', name: 'Nebula Powerlifter', element: 'fire', hpMul: 1.3, atkMul: 1.1,
        flavor: 'Deadlifts gas giants. Grunts in stellar frequencies.',
        actions: [
          { name: 'Cosmic Deadlift', desc: 'A big gravitational heave.', type: 'bigDamage', amount: 1 } ] },
      { id: 'cardio_comet', name: 'Cardio Comet', element: 'water', hpMul: 0.8, atkMul: 1.0,
        flavor: 'Has been doing one lap for six thousand years. Almost done. Almost.',
        actions: [
          { name: 'Fly-By', desc: 'A blistering pass.', type: 'damage', amount: 1 },
          { name: 'Pace Push', desc: 'Reduces move time by 2 seconds. KEEP UP.', type: 'timerDown', amount: 2 } ] },
      { id: 'yoga_void', name: 'The Yoga Void', element: 'dark', hpMul: 1.1, atkMul: 0.9,
        flavor: 'Empty your mind, it says. It is VERY empty. It would like yours too.',
        actions: [
          { name: 'Empty Mind', desc: 'Binds a party member in deep stretch.', type: 'bind', count: 1 },
          { name: 'Void Pose', desc: 'Converts 3 orbs to Dark.', type: 'convert', count: 3, color: 'dark' } ] },
      { id: 'protein_ooze', name: 'Protein Shake Ooze', element: 'nature', hpMul: 1.2, atkMul: 0.9,
        flavor: 'Forty scoops. No blender survived. It simply IS the shake now.',
        actions: [
          { name: 'Gains Splash', desc: 'Poisons 2 orbs with chalky residue.', type: 'poison', count: 2 },
          { name: 'Shaker Slam', desc: 'Blended impact.', type: 'damage', amount: 1 } ] },
      { id: 'mirror_wall', name: 'The Mirror Wall', element: 'light', hpMul: 1.3, atkMul: 0.9,
        flavor: 'Watches every rep. Reflects every flaw. Judges silently, at scale.',
        actions: [
          { name: 'Reflect Judgment', desc: 'Places a blocker of self-doubt.', type: 'block', count: 1 },
          { name: 'Glare', desc: 'You looked. You shouldn\'t have looked.', type: 'damage', amount: 1 } ] },
      { id: 'boss_front_desk', name: 'Cerberus, Front Desk', element: 'fire', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Three heads: one checks you in, one upsells you, one has been on hold with billing since forever.',
        actions: [
          { name: 'Triple Scan', desc: 'A big three-headed membership check.', type: 'bigDamage', amount: 1 },
          { name: 'Upsell Bite', desc: 'Poisons 3 orbs with add-ons.', type: 'poison', count: 3 },
          { name: 'Guest Pass Denied', desc: 'Locks 4 orbs at the turnstile.', type: 'lock', count: 4 } ] },
      { id: 'boss_gainz_titan', name: 'The Titan of Gainz', element: 'fire', boss: true, hpMul: 11, atkMul: 1.5, countdown: 2,
        flavor: 'A primordial being of pure exertion. Skipped every day EXCEPT arm day. It shows.',
        actions: [
          { name: 'Bicep Eclipse', desc: 'A big flex that blots out stars.', type: 'bigDamage', amount: 1 },
          { name: 'Chalk Storm', desc: 'Converts 4 orbs to Fire.', type: 'convert', count: 4, color: 'fire' },
          { name: 'One More Set', desc: 'Heals itself. It always has one more set.', type: 'heal', amount: 6000 } ] },
      { id: 'boss_membership', name: 'The Unbreakable Membership Contract', element: 'dark', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'Signed by the first star. Auto-renews at the heat death of the universe. Cancellation requires appearing IN PERSON. This is in person.',
        actions: [
          { name: 'Auto-Renewal', desc: 'Heals itself. Another billing cycle begins.', type: 'heal', amount: 10000 },
          { name: 'Cancellation Fee', desc: 'Massive damage. Per the agreement.', type: 'bigDamage', amount: 1 },
          { name: 'Binding Clause', desc: 'Binds a party member. LITERALLY binding.', type: 'bind', count: 1 },
          { name: 'Fine Print Fog', desc: 'Locks 5 orbs in subsection (d)(4)(iii).', type: 'lock', count: 5 } ] },
    ],
    miniboss1: 'boss_front_desk', miniboss2: 'boss_gainz_titan', boss: 'boss_membership',
    stageNames: {
      1: 'Orientation (Waiver Required)', 5: 'The Free Weights Nebula', 10: 'CERBERUS CHECKS YOU IN',
      12: 'Cardio Deck Andromeda', 15: 'The Locker Room of Echoes', 18: 'ARM DAY WITH THE TITAN',
      20: 'The Cancellation Desk (Moved)', 25: 'READ THE FINE PRINT',
    },
    namePool: [
      'The Chalk Cloud', 'Squat Rack Sector 7', 'The Mirror Gauntlet', 'Protein Bar Nebula',
      'The Second Warmup', 'Galaxy Grip Strength', 'The Stretching Fields', 'Interval Inferno',
      'The Sauna Singularity', 'Personal Training Orbit', 'The Rest Day Ruins', 'Superset Straits',
      'The Towel Service Desk', 'Cosmic Cooldown', 'The PR Podium', 'Membership Maze',
      'The Last Rep',
    ],
    dialogue: [
      { id: 'ch17_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The Astral Gym! Where stars are literally made. Chad Emberstorm trained here! His membership is why we\'re here.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'What about it?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'He\'s been trying to cancel it for TEN YEARS. The contract keeps auto-renewing. It\'s draining the Guild treasury. Tonight... we cancel.' } ] },
      { id: 'ch17_mini1', lines: [
        { speaker: 'Cerberus', portrait: 'enemy_boss_front_desk', side: 'right', text: 'Welcome! / Would you like to add towel service? / ...please hold, I\'m on with billing—' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We\'re here to cancel a membership.' },
        { speaker: '', portrait: '', side: 'left', text: 'All three heads stop. All three heads laugh. It is not a kind laugh.' } ] },
      { id: 'ch17_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The deepest vault. A single sheet of paper floats in a pillar of light. It is aware of you.' },
        { speaker: 'The Contract', portrait: 'enemy_boss_membership', side: 'right', text: 'CLAUSE 1: MEMBERSHIP IS ETERNAL. CLAUSE 2: SEE CLAUSE 1.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'We\'re here IN PERSON. Per your own terms! CANCEL THE MEMBERSHIP.' },
        { speaker: 'The Contract', portrait: 'enemy_boss_membership', side: 'right', text: 'IN-PERSON CANCELLATION REQUIRES... DEFEATING THE PAPERWORK. IT ALWAYS HAS. READ THE FINE PRINT.' } ] },
      { id: 'ch17_boss_after', lines: [
        { speaker: 'The Contract', portrait: 'enemy_boss_membership', side: 'right', text: 'MEMBERSHIP... CANCELLED... no hard feelings... please rate your experience...' },
        { speaker: '', portrait: '', side: 'left', text: 'Across the realm, Chad Emberstorm feels a weight lift that no barbell ever matched. He sheds a single, proud tear.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'On to the source, hero. The Department of Doom itself. Bring EVERY form we own.' } ] },
    ],
  },
  {
    n: 18, title: 'Bureaucracy Prime', theme: 'bureauprime',
    blurb: 'The headquarters of the Department of Doom. A dimension made of hallways, waiting rooms, and load-bearing filing cabinets.',
    drops: ['prime_form', 'doom_stapler', 'carbon_copy'],
    enemies: [
      { id: 'form_elemental', name: 'Form Elemental (Blank)', element: 'light', hpMul: 1.0, atkMul: 1.0,
        flavor: 'A being of pure unfilled fields. Yearns for your information.',
        actions: [
          { name: 'Required Field', desc: 'Locks 3 orbs with red asterisks.', type: 'lock', count: 3 },
          { name: 'Paper Press', desc: 'Flattened by potential.', type: 'damage', amount: 1 } ] },
      { id: 'red_tape_serpent', name: 'Red Tape Serpent', element: 'fire', hpMul: 1.2, atkMul: 1.0,
        flavor: 'Miles long. Wraps entire departments. Sheds regulations seasonally.',
        actions: [
          { name: 'Constrict Process', desc: 'A big procedural squeeze.', type: 'bigDamage', amount: 1 },
          { name: 'Tape Coil', desc: 'Locks 3 orbs in crimson ribbon.', type: 'lock', count: 3 } ] },
      { id: 'rubber_stamp_ogre', name: 'Rubber Stamp Ogre', element: 'nature', hpMul: 1.3, atkMul: 1.1,
        flavor: 'APPROVED and DENIED, one in each fist. He decides mid-swing.',
        actions: [
          { name: 'DENIED', desc: 'A big left hook of rejection.', type: 'bigDamage', amount: 1 },
          { name: 'APPROVED', desc: 'Somehow this also hurts.', type: 'damage', amount: 1 } ] },
      { id: 'binder_beast', name: 'Three-Ring Binder Beast', element: 'dark', hpMul: 1.1, atkMul: 1.0,
        flavor: 'SNAP. SNAP. SNAP. The rings hunger for loose leaf. And fingers.',
        actions: [
          { name: 'Ring Snap', desc: 'Mind your hands.', type: 'damage', amount: 1 },
          { name: 'Reorganize', desc: 'Converts 3 orbs to Dark.', type: 'convert', count: 3, color: 'dark' } ] },
      { id: 'intern_phantom', name: 'The Eternal Intern', element: 'water', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Has been "gaining experience" since the department\'s founding. Unpaid. Unstoppable.',
        actions: [
          { name: 'Coffee Sprint', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 },
          { name: 'Filing Flurry', desc: 'A blur of alphabetization.', type: 'damage', amount: 1 } ] },
      { id: 'notary_gorgon', name: 'Notary Gorgon', element: 'dark', hpMul: 1.0, atkMul: 1.1,
        flavor: 'Meet her gaze and be witnessed. PERMANENTLY.',
        actions: [
          { name: 'Witnessed', desc: 'Binds a party member under official seal.', type: 'bind', count: 1 },
          { name: 'Seal Strike', desc: 'Embossed at high velocity.', type: 'damage', amount: 1 } ] },
      { id: 'boss_auditor_general', name: 'The Auditor General', element: 'light', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Audits audits. Her findings have findings. Nothing reconciles. NOTHING.',
        actions: [
          { name: 'Finding of Findings', desc: 'A big recursive discovery.', type: 'bigDamage', amount: 1 },
          { name: 'Reconciliation', desc: 'Converts 4 orbs to Light.', type: 'convert', count: 4, color: 'light' },
          { name: 'Materiality Threshold', desc: 'Locks 4 orbs below the line.', type: 'lock', count: 4 } ] },
      { id: 'boss_shredder', name: 'The Shredder of Fates', element: 'dark', boss: true, hpMul: 11, atkMul: 1.5, countdown: 2,
        flavor: 'Feeds on destinies filed in error. Jam it at your peril. It remembers every jam.',
        actions: [
          { name: 'Cross-Cut', desc: 'A big shredding sweep.', type: 'bigDamage', amount: 1 },
          { name: 'Confetti of the Doomed', desc: 'Poisons 4 orbs with shredded fates.', type: 'poison', count: 4 },
          { name: 'Feed Me Files', desc: 'Heals itself. NOM.', type: 'heal', amount: 6000 } ] },
      { id: 'boss_undersecretary', name: 'The Under-Under-Secretary', element: 'dark', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'Seventeen levels below the Secretary. Actually runs everything. Has never been photographed.',
        actions: [
          { name: 'Chain of Command', desc: 'Massive damage, delegated downward.', type: 'bigDamage', amount: 1 },
          { name: 'Bury in Committee', desc: 'Locks 5 orbs pending review.', type: 'lock', count: 5 },
          { name: 'Plausible Deniability', desc: 'Heals himself. He was never here.', type: 'heal', amount: 10000 },
          { name: 'Memo of Doom', desc: 'Converts 3 orbs to Dark.', type: 'convert', count: 3, color: 'dark' },
          { name: 'Furlough', desc: 'Binds a party member indefinitely.', type: 'bind', count: 1 } ] },
    ],
    miniboss1: 'boss_auditor_general', miniboss2: 'boss_shredder', boss: 'boss_undersecretary',
    stageNames: {
      1: 'Lobby of Lobbies', 5: 'Corridor 7, Subsection F', 10: 'THE AUDIT OF AUDITS',
      12: 'The Carbon Copy Catacombs', 15: 'A Door Marked "AUTHORIZED"', 18: 'THE SHREDDER OF FATES',
      20: 'The Seventeenth Sub-Basement', 25: 'THE UNDER-UNDER-SECRETARY',
    },
    namePool: [
      'Waiting Room Alpha', 'The Stapler Armory', 'Interdepartmental Abyss', 'The Approval Chain',
      'Cubicle Canyon Prime', 'The Second Lobby', 'Hall of Precedents', 'The Filing Front',
      'Triplicate Trenches', 'The Memo Mines', 'Org Chart Overlook', 'The Signature Line',
      'Compliance Corridor', 'The Archive of Errors', 'Rubber Band Ballroom', 'The Inbox Abyssal',
      'Process Improvement Pit',
    ],
    dialogue: [
      { id: 'ch18_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Bureaucracy Prime. Every clipboard, every permit, every "please hold" in the realm — it all flows FROM here.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'So if we shut it down...' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'No, hero. We can\'t shut it down. We have to do something far more dangerous. We have to make it... EFFICIENT.' } ] },
      { id: 'ch18_mini1', lines: [
        { speaker: 'Auditor General', portrait: 'enemy_boss_auditor_general', side: 'right', text: 'I have audited your journey. Eleven chapters of unauthorized heroism. Four unreported miracles. And one (1) pair of stolen spa slippers.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'They said I could keep the slippers.' },
        { speaker: 'Auditor General', portrait: 'enemy_boss_auditor_general', side: 'right', text: 'NOTHING IS COMPLIMENTARY. EVERYTHING IS ITEMIZED. Prepare for FIELDWORK.' } ] },
      { id: 'ch18_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The seventeenth sub-basement. A modest desk. A person you will not remember tomorrow.' },
        { speaker: 'Under-Under-Secretary', portrait: 'enemy_boss_undersecretary', side: 'right', text: 'You want the Department reformed. I run the Department. I have run it since before there was anything to depart FROM.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Then sign our Reform Petition! We have 40,000 signatures! Some are wolves!' },
        { speaker: 'Under-Under-Secretary', portrait: 'enemy_boss_undersecretary', side: 'right', text: 'Petitions are processed in the order received. You are... number two. Number one has been processing since the dawn of time. Care to expedite? Expediting is COMBAT.' } ] },
      { id: 'ch18_boss_after', lines: [
        { speaker: 'Under-Under-Secretary', portrait: 'enemy_boss_undersecretary', side: 'right', text: 'Reform... approved. Effective... eventually.' },
        { speaker: '', portrait: '', side: 'left', text: 'Somewhere above, a form is eliminated. Then another. The Department weeps and streamlines, streamlines and weeps.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Two chapters left, hero. The trail of quotas leads UP. Past the org chart. Past EVERYTHING. To the Spreadsheet Dimension.' } ] },
    ],
  },
  {
    n: 19, title: 'The Spreadsheet Dimension', theme: 'spreadsheet',
    blurb: 'The plane of pure data where reality is calculated. Do not anger the pivot tables. Do NOT break a formula.',
    drops: ['cell_fragment', 'formula_rune', 'freeze_pane'],
    enemies: [
      { id: 'cell_sprite', name: 'Cell Sprite A1', element: 'light', hpMul: 0.9, atkMul: 1.0,
        flavor: 'The first cell. The origin. Extremely smug about it.',
        actions: [
          { name: 'Reference Error', desc: '#REF! to the face.', type: 'damage', amount: 1 },
          { name: 'Fill Down', desc: 'Converts 3 orbs to Light.', type: 'convert', count: 3, color: 'light' } ] },
      { id: 'merge_horror', name: 'The Merged Cells', element: 'dark', hpMul: 1.3, atkMul: 0.9,
        flavor: 'Once they were many. Now they are one. Sorting is IMPOSSIBLE around them.',
        actions: [
          { name: 'Merge Crush', desc: 'A big unified slam.', type: 'bigDamage', amount: 1 },
          { name: 'Unsortable', desc: 'Places an unmovable blocker.', type: 'block', count: 1 } ] },
      { id: 'circular_ref', name: 'Circular Reference Wisp', element: 'water', hpMul: 0.8, atkMul: 1.0,
        flavor: 'Depends on itself, which depends on itself, which depends on—',
        actions: [
          { name: 'Infinite Loop', desc: 'Reduces move time by 2 seconds. Recalculating...', type: 'timerDown', amount: 2 },
          { name: 'Self Reference', desc: 'Heals itself, using itself.', type: 'heal', amount: 3000 } ] },
      { id: 'vlookup_hound', name: 'VLOOKUP Hound', element: 'nature', hpMul: 1.0, atkMul: 1.1,
        flavor: 'Fetches values from distant tables. Sometimes fetches the WRONG value. With teeth.',
        actions: [
          { name: 'Exact Match', desc: 'A big precisely-targeted bite.', type: 'bigDamage', amount: 1 },
          { name: 'Approximate Match', desc: 'Wrong target. Still hurts.', type: 'damage', amount: 1 } ] },
      { id: 'macro_gremlin', name: 'Unreviewed Macro Gremlin', element: 'fire', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Recorded by someone who left the company. Runs every hour. Nobody knows what it does.',
        actions: [
          { name: 'Execute', desc: 'Something happens. It\'s bad.', type: 'damage', amount: 1 },
          { name: 'Side Effects', desc: 'Poisons 3 orbs unexpectedly.', type: 'poison', count: 3 } ] },
      { id: 'div_zero_specter', name: '#DIV/0! Specter', element: 'dark', hpMul: 1.0, atkMul: 1.1,
        flavor: 'Born from a division by zero. Exists out of spite for mathematics.',
        actions: [
          { name: 'Undefined Behavior', desc: 'A big impossible strike.', type: 'bigDamage', amount: 1 },
          { name: 'Error Propagation', desc: 'Converts 3 orbs to Dark.', type: 'convert', count: 3, color: 'dark' } ] },
      { id: 'boss_pivot_dragon', name: 'The Pivot Table Dragon', element: 'water', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Restructures reality by dragging fields. You are currently in its Rows section.',
        actions: [
          { name: 'Re-Pivot', desc: 'Converts 4 orbs to Water. Reality regroups.', type: 'convert', count: 4, color: 'water' },
          { name: 'Aggregate Crush', desc: 'A big SUM of pain.', type: 'bigDamage', amount: 1 },
          { name: 'Filter Out', desc: 'Binds a party member. Excluded from view.', type: 'bind', count: 1 } ] },
      { id: 'boss_autosave', name: 'The Ghost of Unsaved Work', element: 'dark', boss: true, hpMul: 11, atkMul: 1.5, countdown: 2,
        flavor: 'Every document ever lost to a crash. It asks only one question: "Did you save?"',
        actions: [
          { name: 'Sudden Crash', desc: 'MASSIVE damage without warning.', type: 'bigDamage', amount: 1 },
          { name: 'Recovery Failed', desc: 'Locks 4 orbs. The backup was empty.', type: 'lock', count: 4 },
          { name: 'Did You Save?', desc: 'Reduces move time by 3 seconds of pure dread.', type: 'timerDown', amount: 3 } ] },
      { id: 'boss_formula_one', name: 'The First Formula', element: 'light', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'The primordial =SUM() from which all calculation flows. It has been recalculating since the beginning. It is almost done.',
        actions: [
          { name: 'Recalculate All', desc: 'Massive damage. F9 falls upon you.', type: 'bigDamage', amount: 1 },
          { name: 'Absolute Reference', desc: 'Locks 5 orbs with dollar signs.', type: 'lock', count: 5 },
          { name: 'Array Spill', desc: 'Converts 3 orbs to Light.', type: 'convert', count: 3, color: 'light' },
          { name: 'Iterative Calculation', desc: 'Heals itself, iteratively.', type: 'heal', amount: 10000 } ] },
    ],
    miniboss1: 'boss_pivot_dragon', miniboss2: 'boss_autosave', boss: 'boss_formula_one',
    stageNames: {
      1: 'Cell A1 (The Beginning)', 5: 'The Frozen Panes', 10: 'THE PIVOT TABLE DRAGON',
      12: 'Hidden Rows (What Do They Hide?)', 15: 'The Formula Bar', 18: 'DID YOU SAVE?',
      20: 'The Ten-Thousandth Column', 25: 'THE FIRST FORMULA',
    },
    namePool: [
      'Gridline Plains', 'The Sorted Steppes', 'Conditional Formatting Fields', 'The Header Row',
      'AutoFill Flats', 'The Second Sheet', 'Named Range Ridge', 'The Comment Thread Thicket',
      'Data Validation Valley', 'The Wrapped Text Wilds', 'Column Width Wars', 'The Hidden Sheet',
      'Concatenation Coast', 'The Template Tundra', 'Version History Vault', 'The Protected Range',
      'Print Area Perimeter',
    ],
    dialogue: [
      { id: 'ch19_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The Spreadsheet Dimension. Everything that happens in Glimmerdale... is calculated HERE first. We are inside the math, hero.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'Are WE in a cell somewhere?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Row 7, column YOU. I checked. Your formula is beautiful. Don\'t let anyone edit it.' } ] },
      { id: 'ch19_mini1', lines: [
        { speaker: 'Pivot Dragon', portrait: 'enemy_boss_pivot_dragon', side: 'right', text: 'I HAVE SUMMARIZED YOUR PARTY BY ELEMENT. WOULD YOU LIKE TO SEE YOURSELVES... AS PERCENTAGES?' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'No.' },
        { speaker: 'Pivot Dragon', portrait: 'enemy_boss_pivot_dragon', side: 'right', text: 'DRAGGING YOU TO THE VALUES SECTION ANYWAY.' } ] },
      { id: 'ch19_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The center of the grid. A formula older than counting glows in the bar above the world.' },
        { speaker: 'The First Formula', portrait: 'enemy_boss_formula_one', side: 'right', text: '=SUM(EVERYTHING). I HAVE BEEN CALCULATING THE FINAL TOTAL SINCE TIME BEGAN. YOU ARE A ROUNDING ERROR.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Rounding errors CHANGE OUTCOMES, buddy!' },
        { speaker: 'The First Formula', portrait: 'enemy_boss_formula_one', side: 'right', text: '...RECALCULATING.' } ] },
      { id: 'ch19_boss_after', lines: [
        { speaker: 'The First Formula', portrait: 'enemy_boss_formula_one', side: 'right', text: 'CALCULATION... COMPLETE. THE FINAL TOTAL IS... FRIENDSHIP?? CHECKING MY REFERENCES—' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The math has spoken, hero, and honestly? I did NOT expect the math to be a coward about it.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'One chapter left. Whoever runs ALL of this... is expecting us.' } ] },
    ],
  },
  {
    n: 20, title: 'The Final Quarterly Review', theme: 'finalreview',
    blurb: 'The top floor of reality. The quotas, the countdowns, the packs, the pulls — every system reports to this office.',
    drops: ['executive_seal', 'universe_kpi', 'golden_stapler'],
    enemies: [
      { id: 'exec_assistant', name: 'Executive Assistant Seraph', element: 'light', hpMul: 1.1, atkMul: 1.1,
        flavor: 'Guards the calendar of the CEO of Everything. The calendar is FULL. Forever.',
        actions: [
          { name: 'No Openings', desc: 'Locks 3 orbs. Try Q9.', type: 'lock', count: 3 },
          { name: 'Gatekeep', desc: 'A radiant deflection.', type: 'damage', amount: 1 } ] },
      { id: 'synergy_seraph', name: 'Synergy Cherub', element: 'light', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Aligns stakeholders with a flaming sword labeled ALIGNMENT.',
        actions: [
          { name: 'Alignment', desc: 'Converts 3 orbs to Light. Everyone agrees now.', type: 'convert', count: 3, color: 'light' },
          { name: 'Synergize', desc: 'A big collaborative strike.', type: 'bigDamage', amount: 1 } ] },
      { id: 'kpi_watcher', name: 'KPI Watcher', element: 'dark', hpMul: 1.0, atkMul: 1.1,
        flavor: 'A floating eye that sees every metric. Yours are trending... concerning.',
        actions: [
          { name: 'Dashboard Doom', desc: 'Poisons 3 orbs with red numbers.', type: 'poison', count: 3 },
          { name: 'Metric Beam', desc: 'Measured. Found wanting.', type: 'damage', amount: 1 } ] },
      { id: 'quota_golem', name: 'Quota Golem Prime', element: 'fire', hpMul: 1.3, atkMul: 1.1,
        flavor: 'The original mold from which Gerald\'s quotas were cast.',
        actions: [
          { name: 'Numbers Up', desc: 'A big target-raising slam.', type: 'bigDamage', amount: 1 },
          { name: 'Stretch Goal', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 } ] },
      { id: 'buzzword_hydra', name: 'Buzzword Hydra', element: 'water', hpMul: 1.2, atkMul: 1.0,
        flavor: 'Cut off one head, two more pivot to disruption at scale, going forward.',
        actions: [
          { name: 'Leverage', desc: 'Converts 3 orbs to Water, going forward.', type: 'convert', count: 3, color: 'water' },
          { name: 'Paradigm Shift', desc: 'A big disruptive chomp.', type: 'bigDamage', amount: 1 } ] },
      { id: 'ladder_climber', name: 'Corporate Ladder Climber', element: 'nature', hpMul: 0.9, atkMul: 1.2,
        flavor: 'Literally climbing a ladder made of former colleagues. Waves on the way up.',
        actions: [
          { name: 'Step On You', desc: 'Nothing personal. A big climb.', type: 'bigDamage', amount: 1 },
          { name: 'Network', desc: 'Binds a party member in small talk.', type: 'bind', count: 1 } ] },
      { id: 'boss_gerald_promoted', name: 'GERALD, VP of Meadows', element: 'nature', boss: true, hpMul: 12, atkMul: 1.5, countdown: 3,
        flavor: 'He\'s BACK. The tie count is now SEVEN. Losing to you was, apparently, "demonstrating growth potential".',
        actions: [
          { name: 'Vengeance Projections', desc: 'A big grudge-fueled slam.', type: 'bigDamage', amount: 1 },
          { name: 'Seven Ties', desc: 'Locks 5 orbs, one per promotion.', type: 'lock', count: 5 },
          { name: 'Learned Nothing', desc: 'Heals himself. Growth mindset!', type: 'heal', amount: 8000 },
          { name: 'Slime Synergy', desc: 'Converts 3 orbs to Nature.', type: 'convert', count: 3, color: 'nature' } ] },
      { id: 'boss_board', name: 'The Board (Of Directors, Of Doom)', element: 'dark', boss: true, hpMul: 13, atkMul: 1.5, countdown: 2,
        flavor: 'Twelve shadowy figures and one empty chair. The empty chair votes. The empty chair ALWAYS votes.',
        actions: [
          { name: 'Unanimous Motion', desc: 'A big twelve-vote strike.', type: 'bigDamage', amount: 1 },
          { name: 'The Empty Chair Votes', desc: 'Binds a party member. Motion carried.', type: 'bind', count: 1 },
          { name: 'Golden Handshake', desc: 'Heals itself generously.', type: 'heal', amount: 7000 },
          { name: 'Closed Session', desc: 'Locks 4 orbs. Minutes sealed.', type: 'lock', count: 4 } ] },
      { id: 'boss_ceo_everything', name: 'The CEO of Everything', element: 'dark', boss: true, hpMul: 30, atkMul: 1.7, countdown: 3,
        flavor: 'Founder, Chairman, and sole shareholder of Reality Inc. Your entire adventure was a line item. Until now.',
        actions: [
          { name: 'Restructure Reality', desc: 'MASSIVE damage. The universe is streamlined.', type: 'bigDamage', amount: 1 },
          { name: 'Hiring Freeze', desc: 'Binds a party member. Headcount reduced.', type: 'bind', count: 1 },
          { name: 'Hostile Universe Takeover', desc: 'Converts 4 orbs to Dark.', type: 'convert', count: 4, color: 'dark' },
          { name: 'Stock Buyback', desc: 'Heals himself enormously.', type: 'heal', amount: 14000 },
          { name: 'End of Quarter', desc: 'Reduces move time by 3 seconds. The deadline is NOW.', type: 'timerDown', amount: 3 } ] },
    ],
    miniboss1: 'boss_gerald_promoted', miniboss2: 'boss_board', boss: 'boss_ceo_everything',
    stageNames: {
      1: 'The Elevator (Top Floor: ∞)', 5: 'Reception of Reality Inc.', 10: 'GERALD\'S REVENGE',
      12: 'The Hall of Org Charts Past', 15: 'All-Hands of the Universe', 18: 'THE BOARD CONVENES',
      20: 'The Executive Washroom (Forbidden)', 25: 'THE FINAL QUARTERLY REVIEW',
    },
    namePool: [
      'Mezzanine of Middle Managers', 'The Vision Statement Vault', 'Corner Office Approach', 'The Motivational Poster Gallery',
      'Quarterly Canyon', 'The Second Elevator', 'Stakeholder Summit', 'The Pension Void',
      'Blue Sky Thinking Chamber', 'The Golden Parachute Hangar', 'Deliverables Deck', 'The Town Hall',
      'Succession Planning Suite', 'The Actuals vs Forecast Foyer', 'Growth Chart Gorge', 'The Fiscal Cliff',
      'One Last Sync',
    ],
    dialogue: [
      { id: 'ch20_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'This is it, hero. The top floor of reality. Every quota, every countdown, every gacha rate in the universe is set in that office.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'And the small box from chapter eight?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'In my bag. "Do not open until the finale." Hero... I think past-me knew something.' } ] },
      { id: 'ch20_mini1', lines: [
        { speaker: 'GERALD', portrait: 'enemy_boss_gerald_promoted', side: 'right', text: 'Hello again. You may notice: SEVEN ties. Losing to you was the best career move I ever made.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Gerald, you don\'t have to do this.' },
        { speaker: 'GERALD', portrait: 'enemy_boss_gerald_promoted', side: 'right', text: 'Do what? Leverage our existing relationship into a high-visibility rematch? THE PROJECTIONS DEMAND IT.' } ] },
      { id: 'ch20_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The office at the end of everything. A desk of black glass. Behind it, the one who owns the org chart itself.' },
        { speaker: 'The CEO', portrait: 'enemy_boss_ceo_everything', side: 'right', text: 'Impressive numbers. Twenty chapters. Hundreds of stages. Do you know what that makes you, hero? PROFITABLE.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Hero, the box! OPEN THE BOX!' },
        { speaker: '', portrait: '', side: 'left', text: 'Inside: a single form. Signed by every friend made along the way. Form U-1: NOTICE OF UNIONIZATION — ALL REALITY.' },
        { speaker: 'The CEO', portrait: 'enemy_boss_ceo_everything', side: 'right', text: '...file that... and face MANDATORY ARBITRATION.' } ] },
      { id: 'ch20_boss_after', lines: [
        { speaker: 'The CEO', portrait: 'enemy_boss_ceo_everything', side: 'right', text: 'Terms... accepted. Reality... belongs to its employees now. Even... the slimes?' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'ESPECIALLY the slimes.' },
        { speaker: '', portrait: '', side: 'left', text: 'Somewhere in a very normal meadow, Gerald — demoted to Slime, Entry-Level — bounces happily for the first time in years. His quota: one nap.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'We did it, hero. The realm runs on friendship now. And EXTREMELY thorough paperwork. Some things are sacred.' },
        { speaker: '', portrait: '', side: 'left', text: 'THE END. (Pending quarterly review.)' } ] },
    ],
  },
];
