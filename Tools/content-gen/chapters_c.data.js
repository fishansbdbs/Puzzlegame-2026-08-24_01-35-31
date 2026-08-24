// Chapters 12-16: absurdity fully in charge.
module.exports = [
  {
    n: 12, title: 'Frostbite Fjord Timeshares', theme: 'fjord',
    blurb: 'Ice giants discovered real estate. Attend one (1) presentation and win a free hero-sized ice sculpture. Of you.',
    drops: ['glacier_deed', 'frost_brochure', 'icicle_pen'],
    enemies: [
      { id: 'sales_yeti', name: 'Timeshare Yeti', element: 'water', hpMul: 1.1, atkMul: 1.0,
        flavor: 'Just forty-five minutes of your time. Forty-five ETERNAL minutes.',
        actions: [
          { name: 'The Pitch', desc: 'A big, unskippable presentation.', type: 'bigDamage', amount: 1 },
          { name: 'Limited Offer', desc: 'Reduces move time by 2 seconds. Act NOW.', type: 'timerDown', amount: 2 } ] },
      { id: 'penguin_notary', name: 'Penguin Closing Agent', element: 'water', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Waddles in with the paperwork exactly when you weaken.',
        actions: [
          { name: 'Sign Here', desc: 'Locks 3 orbs under signature tabs.', type: 'lock', count: 3 },
          { name: 'Flipper Slap', desc: 'Formal and firm.', type: 'damage', amount: 1 } ] },
      { id: 'frost_wolf', name: 'Amenities Wolf', element: 'dark', hpMul: 1.0, atkMul: 1.1,
        flavor: 'Guards the pool (frozen), gym (frozen), and sauna (also somehow frozen).',
        actions: [
          { name: 'Amenity Denial', desc: 'Places a POOL CLOSED blocker.', type: 'block', count: 1 },
          { name: 'Cold Bite', desc: 'Refreshingly agonizing.', type: 'damage', amount: 1 } ] },
      { id: 'ice_slime', name: 'Ice Slime (Gerald\'s Ski Trip)', element: 'water', hpMul: 1.0, atkMul: 0.9,
        flavor: 'A slime on vacation. Still finds time to hit quota.',
        actions: [
          { name: 'Chill Bounce', desc: 'Vacation-grade violence.', type: 'damage', amount: 1 },
          { name: 'Slush Splash', desc: 'Converts 3 orbs to Water.', type: 'convert', count: 3, color: 'water' } ] },
      { id: 'aurora_moth', name: 'Aurora Moth', element: 'light', hpMul: 0.8, atkMul: 1.0,
        flavor: 'Drawn to the northern lights. Sells prints of them in the lobby.',
        actions: [
          { name: 'Lightshow', desc: 'Converts 3 orbs to Light.', type: 'convert', count: 3, color: 'light' },
          { name: 'Wing Chill', desc: 'A shimmering swat.', type: 'damage', amount: 1 } ] },
      { id: 'walrus_hoa', name: 'Walrus Owners\' Committee', element: 'dark', hpMul: 1.3, atkMul: 0.9,
        flavor: 'Three walruses in one very large sweater. Votes as a bloc.',
        actions: [
          { name: 'Committee Crush', desc: 'A big triple-weighted vote.', type: 'bigDamage', amount: 1 },
          { name: 'Special Assessment', desc: 'Poisons 3 orbs with fees.', type: 'poison', count: 3 } ] },
      { id: 'boss_presentation', name: 'The 45-Minute Presentation', element: 'light', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'A living slideshow. Slide 1 of ∞. It has animations. ALL of them are spinning.',
        actions: [
          { name: 'Next Slide', desc: 'A big transition wipe.', type: 'bigDamage', amount: 1 },
          { name: 'Q&A Delayed', desc: 'Locks 4 orbs until the end.', type: 'lock', count: 4 },
          { name: 'Bullet Points', desc: 'Converts 3 orbs to Light.', type: 'convert', count: 3, color: 'light' } ] },
      { id: 'boss_closer_kraken', name: 'Karkinos the Closer', element: 'water', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'The deal-closing kraken. Eight arms, eight contracts, zero cooling-off periods.',
        actions: [
          { name: 'Ink the Deal', desc: 'A big eight-pen flourish.', type: 'bigDamage', amount: 1 },
          { name: 'Arm Twist', desc: 'Binds a party member persuasively.', type: 'bind', count: 1 },
          { name: 'Undertow Clause', desc: 'Converts 4 orbs to Water.', type: 'convert', count: 4, color: 'water' } ] },
      { id: 'boss_glacier_ceo', name: 'CEO Permafrost', element: 'water', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'An ice giant in a parka woven from cancelled contracts. His handshake lasts nine minutes and lowers your core temperature.',
        actions: [
          { name: 'Glacial Close', desc: 'Massive damage, delivered slowly and inevitably.', type: 'bigDamage', amount: 1 },
          { name: 'Freeze the Deal', desc: 'Locks 5 orbs in permafrost.', type: 'lock', count: 5 },
          { name: 'Equity Melt', desc: 'Heals himself with your down payment.', type: 'heal', amount: 10000 },
          { name: 'Blizzard of Fees', desc: 'Converts 3 orbs to Water.', type: 'convert', count: 3, color: 'water' } ] },
    ],
    miniboss1: 'boss_presentation', miniboss2: 'boss_closer_kraken', boss: 'boss_glacier_ceo',
    stageNames: {
      1: 'Free Breakfast (A Trap)', 5: 'The Model Unit (Frozen)', 10: 'THE PRESENTATION BEGINS',
      12: 'Amenity Deck C (Closed)', 15: 'The Cooling-Off Period', 18: 'KARKINOS WANTS INK',
      20: 'The Deed Vault', 25: 'CEO PERMAFROST\'S CORNER FLOE',
    },
    namePool: [
      'The Frosted Lobby', 'Brochure Blizzard', 'Sales Floor Floe', 'The Icicle Escrow',
      'Points System Crevasse', 'The Second Pitch', 'Fjord Fine Print', 'The Owners\' Lounge (Members Only)',
      'Maintenance Fee Morraine', 'The Frozen Hot Tub', 'Glacial Common Areas', 'Peak Season Panic',
      'The Referral Bonus Ridge', 'Booking Window Tundra', 'The Perpetual Walkthrough', 'Contract Cove',
      'The Long Handshake',
    ],
    dialogue: [
      { id: 'ch12_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Frostbite Fjord! The ice giants pivoted from pillaging to timeshares. Historians agree this is worse.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'We\'re not buying anything.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Hero, NOBODY plans to buy a timeshare. That\'s the terrifying part. Guard your signature with your LIFE.' } ] },
      { id: 'ch12_mini1', lines: [
        { speaker: 'The Presentation', portrait: 'enemy_boss_presentation', side: 'right', text: 'SLIDE ONE: WELCOME. SLIDE TWO: IMAGINE... OWNING... WINTER.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'How many slides are there?' },
        { speaker: 'The Presentation', portrait: 'enemy_boss_presentation', side: 'right', text: 'SLIDE THREE: DO NOT ASK ABOUT THE SLIDES.' } ] },
      { id: 'ch12_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The corner floe. A desk carved from a glacier. Behind it, a giant whose breath is a cold front.' },
        { speaker: 'Permafrost', portrait: 'enemy_boss_glacier_ceo', side: 'right', text: 'I hear you sat through the ENTIRE presentation and bought NOTHING. Remarkable. I respect it. I also cannot allow it.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Is there a "leave quietly" package?' },
        { speaker: 'Permafrost', portrait: 'enemy_boss_glacier_ceo', side: 'right', text: 'Everything is a package. That one costs a battle. Shall we... close?' } ] },
      { id: 'ch12_boss_after', lines: [
        { speaker: 'Permafrost', portrait: 'enemy_boss_glacier_ceo', side: 'right', text: 'The contracts... void... the fjord... free... my parka... just a regular parka now...' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Four thousand timeshare owners released! The penguins are shredding paperwork! It\'s SNOWING CONTRACTS!' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Prettiest snow I\'ve ever seen.' } ] },
    ],
  },
  {
    n: 13, title: 'The Royal Post Office', theme: 'royalpost',
    blurb: 'Older than the crown itself. The mail moves through rain, snow, dragons, and — slowest of all — internal review.',
    drops: ['first_class_seal', 'wax_blob', 'undeliverable_letter'],
    enemies: [
      { id: 'mail_golem', name: 'First-Class Mail Golem', element: 'nature', hpMul: 1.2, atkMul: 1.0,
        flavor: 'Made entirely of undelivered letters. Some are yours. It will not give them back.',
        actions: [
          { name: 'Letter Storm', desc: 'A big flurry of correspondence.', type: 'bigDamage', amount: 1 },
          { name: 'Postage Due', desc: 'Locks 3 orbs pending stamps.', type: 'lock', count: 3 } ] },
      { id: 'stamp_sprite', name: 'Commemorative Stamp Sprite', element: 'light', hpMul: 0.8, atkMul: 1.0,
        flavor: 'Limited edition. Self-adhesive. Self-important.',
        actions: [
          { name: 'First Day Cover', desc: 'Converts 3 orbs to Light.', type: 'convert', count: 3, color: 'light' },
          { name: 'Lick and Stick', desc: 'Unhygienic but effective.', type: 'damage', amount: 1 } ] },
      { id: 'parcel_hound', name: 'Parcel Hound', element: 'fire', hpMul: 1.0, atkMul: 1.1,
        flavor: 'The eternal war between dog and mail, resolved: the dog IS mail now.',
        actions: [
          { name: 'Fetch (Violent)', desc: 'A big retrieving lunge.', type: 'bigDamage', amount: 1 },
          { name: 'Bark Notice', desc: 'You have been notified.', type: 'damage', amount: 1 } ] },
      { id: 'envelope_wraith', name: 'Envelope Wraith', element: 'dark', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Seals itself around secrets. Yours, preferably.',
        actions: [
          { name: 'Seal Fate', desc: 'Poisons 2 orbs with wax.', type: 'poison', count: 2 },
          { name: 'Paper Wing Slice', desc: 'The sharpest edge known to science.', type: 'damage', amount: 1 } ] },
      { id: 'zip_gremlin', name: 'Zip Code Gremlin', element: 'water', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Transposes two digits and watches empires fall.',
        actions: [
          { name: 'Digit Swap', desc: 'Converts 3 orbs to Water.', type: 'convert', count: 3, color: 'water' },
          { name: 'Gremlin Jab', desc: 'Small hands, big consequences.', type: 'damage', amount: 1 } ] },
      { id: 'registered_owlbear', name: 'Registered Owlbear', element: 'nature', hpMul: 1.3, atkMul: 1.0,
        flavor: 'Requires a signature. Requires it with TALONS.',
        actions: [
          { name: 'Signature Required', desc: 'Binds a party member until they sign.', type: 'bind', count: 1 },
          { name: 'Certified Maul', desc: 'Tracked and traced.', type: 'damage', amount: 1 } ] },
      { id: 'boss_postmaster', name: 'The Postmaster Ancient', element: 'light', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Has personally rejected 4 billion insufficiently-stamped letters. Keeps count. Out loud.',
        actions: [
          { name: 'Insufficient Postage', desc: 'A big RETURN TO SENDER slam.', type: 'bigDamage', amount: 1 },
          { name: 'Sorting Ritual', desc: 'Converts 4 orbs to Light.', type: 'convert', count: 4, color: 'light' },
          { name: 'Hold Mail', desc: 'Locks 4 orbs indefinitely.', type: 'lock', count: 4 } ] },
      { id: 'boss_chain_letter', name: 'The Chain Letter', element: 'dark', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'Forward it to ten friends or suffer misfortune. It has been forwarding ITSELF since the Bronze Age.',
        actions: [
          { name: 'Misfortune Clause', desc: 'Poisons 4 orbs. You didn\'t forward it.', type: 'poison', count: 4 },
          { name: 'Copy of a Copy', desc: 'Heals itself. It multiplies.', type: 'heal', amount: 6000 },
          { name: 'Guilt Post', desc: 'A big passive-aggressive delivery.', type: 'bigDamage', amount: 1 } ] },
      { id: 'boss_dead_letter', name: 'The Dead Letter King', element: 'dark', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'Sovereign of every letter never delivered: confessions, apologies, birthday cards with money still inside.',
        actions: [
          { name: 'Words Unsaid', desc: 'Massive damage. Everything you never mailed.', type: 'bigDamage', amount: 1 },
          { name: 'The Unsent Archive', desc: 'Locks 5 orbs among the lost.', type: 'lock', count: 5 },
          { name: 'Postmark of Doom', desc: 'Converts 3 orbs to Dark.', type: 'convert', count: 3, color: 'dark' },
          { name: 'Reread Old Letters', desc: 'Heals himself with nostalgia.', type: 'heal', amount: 9000 } ] },
    ],
    miniboss1: 'boss_postmaster', miniboss2: 'boss_chain_letter', boss: 'boss_dead_letter',
    stageNames: {
      1: 'The Sorting Hall Eternal', 5: 'Stamp Vault Antechamber', 10: 'THE POSTMASTER\'S COUNT',
      12: 'The Registered Route', 15: 'A Letter Addressed To You', 18: 'DO NOT BREAK THE CHAIN',
      20: 'The Undeliverable Stacks', 25: 'COURT OF THE DEAD LETTER KING',
    },
    namePool: [
      'Conveyor of Consequence', 'The Wax Seal Gallery', 'Priority Peril', 'The Owlbear Route',
      'Misdelivered Meadows', 'The Second Sorting Hall', 'Postage Purgatory', 'The Gremlin Annex',
      'Certified Corridors', 'The Envelope Archives', 'Return Address Unknown', 'The Franking Machine',
      'Parcel Hound Kennels', 'The Overnight Vault', 'Letters to the Void', 'The Philatelist\'s Trap',
      'Outgoing (Never)',
    ],
    dialogue: [
      { id: 'ch13_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The Royal Post Office. They say the mail always gets through. They do not say WHEN.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'Why are we here?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The Dead Letter King has been hoarding every heartfelt letter in history. The kingdom\'s feelings are BACKED UP, hero. It\'s an emotional plumbing emergency.' } ] },
      { id: 'ch13_mini1', lines: [
        { speaker: 'Postmaster', portrait: 'enemy_boss_postmaster', side: 'right', text: 'FOUR BILLION AND TWELVE. That is how many letters I have rejected. Yours would be four billion and THIRTEEN.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We didn\'t bring a letter.' },
        { speaker: 'Postmaster', portrait: 'enemy_boss_postmaster', side: 'right', text: 'THEN I SHALL REJECT YOU DIRECTLY.' } ] },
      { id: 'ch13_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'A throne room of pigeonholes, each one glowing faintly with an unread letter.' },
        { speaker: 'Dead Letter King', portrait: 'enemy_boss_dead_letter', side: 'right', text: 'Every unsaid apology. Every unmailed confession. MINE. They are safest unread. Unread words... cannot wound.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'They can\'t HEAL either, your majesty.' },
        { speaker: 'Dead Letter King', portrait: 'enemy_boss_dead_letter', side: 'right', text: '...deliver THIS, then.' } ] },
      { id: 'ch13_boss_after', lines: [
        { speaker: 'Dead Letter King', portrait: 'enemy_boss_dead_letter', side: 'right', text: 'Very well... release the mail... ALL of it...' },
        { speaker: '', portrait: '', side: 'left', text: 'A century of letters takes flight at once, darkening the sky like starlings. Somewhere, ten thousand grudges end.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Hero. There\'s one for you. It\'s from the slimes. It says "good hustle". I\'m crying.' } ] },
    ],
  },
  {
    n: 14, title: 'The Dungeon of Infinite Meetings', theme: 'meetings',
    blurb: 'An ancient dungeon retrofitted with conference rooms. The minotaur takes minutes. The minutes take hours.',
    drops: ['agenda_scroll', 'action_item', 'whiteboard_shard'],
    enemies: [
      { id: 'agenda_zombie', name: 'Agenda Zombie', element: 'dark', hpMul: 1.0, atkMul: 0.9,
        flavor: 'Shuffles from meeting to meeting. Has not been alive since the kickoff.',
        actions: [
          { name: 'Circle Back', desc: 'It returns. It ALWAYS returns.', type: 'damage', amount: 1 },
          { name: 'Tabled Item', desc: 'Locks 3 orbs for next week.', type: 'lock', count: 3 } ] },
      { id: 'standup_gargoyle', name: 'Daily Standup Gargoyle', element: 'nature', hpMul: 1.2, atkMul: 1.0,
        flavor: 'Stands. Delivers blockers. Has been "almost done" for 400 years.',
        actions: [
          { name: 'Blocker Report', desc: 'Places a blocker. Ironically.', type: 'block', count: 1 },
          { name: 'Status Slam', desc: 'Yesterday: violence. Today: violence. Blockers: you.', type: 'damage', amount: 1 } ] },
      { id: 'calendar_imp', name: 'Double-Booking Imp', element: 'fire', hpMul: 0.8, atkMul: 1.1,
        flavor: 'Schedules two meetings in one slot, then attends neither.',
        actions: [
          { name: 'Conflict Created', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 },
          { name: 'Reminder Ping', desc: 'A sharp notification.', type: 'damage', amount: 1 } ] },
      { id: 'whiteboard_elemental', name: 'Whiteboard Elemental', element: 'light', hpMul: 1.1, atkMul: 1.0,
        flavor: 'Covered in diagrams nobody remembers drawing. DO NOT ERASE burns across its chest.',
        actions: [
          { name: 'Marker Fumes', desc: 'Poisons 3 orbs with dry-erase vapor.', type: 'poison', count: 3 },
          { name: 'Diagram Slam', desc: 'Hit by a flowchart.', type: 'damage', amount: 1 } ] },
      { id: 'action_item_swarm', name: 'Action Item Swarm', element: 'nature', hpMul: 0.9, atkMul: 1.1,
        flavor: 'Each one assigned to "someone". Someone is you. Someone was always you.',
        actions: [
          { name: 'Assigned To You', desc: 'A big pile of sudden responsibility.', type: 'bigDamage', amount: 1 } ] },
      { id: 'projector_specter', name: 'Projector Specter', element: 'dark', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Displays nothing but blue. Demands the correct dongle. There is no correct dongle.',
        actions: [
          { name: 'No Signal', desc: 'Converts 3 orbs to Dark.', type: 'convert', count: 3, color: 'dark' },
          { name: 'Blinding Beam', desc: 'Right in the eyes.', type: 'damage', amount: 1 } ] },
      { id: 'boss_minotaur_pm', name: 'The Minotaur Project Manager', element: 'nature', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Half bull, half burndown chart. His labyrinth is the project plan.',
        actions: [
          { name: 'Scope Charge', desc: 'A big horn-first scope expansion.', type: 'bigDamage', amount: 1 },
          { name: 'Sprint Planning', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 },
          { name: 'Dependency Chain', desc: 'Locks 4 orbs. Blocked by task 12.', type: 'lock', count: 4 } ] },
      { id: 'boss_icebreaker', name: 'The Icebreaker', element: 'water', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'A frost elemental that forces you to share a fun fact about yourself. There is no escape. Fun facts only.',
        actions: [
          { name: 'Fun Fact Demand', desc: 'Binds a party member until they share.', type: 'bind', count: 1 },
          { name: 'Two Truths, One Slam', desc: 'A big deceptive strike.', type: 'bigDamage', amount: 1 },
          { name: 'Cold Open', desc: 'Converts 4 orbs to Water.', type: 'convert', count: 4, color: 'water' } ] },
      { id: 'boss_recurring', name: 'The Recurring Meeting', element: 'dark', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'It has no end date. It has no agenda. Its original purpose died in the Second Age. It occurs FOREVER.',
        actions: [
          { name: 'This Could Have Been An Email', desc: 'Massive existential damage.', type: 'bigDamage', amount: 1 },
          { name: 'Recur', desc: 'Heals itself. Same time next week.', type: 'heal', amount: 10000 },
          { name: 'Overrun', desc: 'Reduces move time by 3 seconds. Just five more minutes.', type: 'timerDown', amount: 3 },
          { name: 'Mandatory Attendance', desc: 'Locks 5 orbs in the invite.', type: 'lock', count: 5 } ] },
    ],
    miniboss1: 'boss_minotaur_pm', miniboss2: 'boss_icebreaker', boss: 'boss_recurring',
    stageNames: {
      1: 'The Kickoff (Doomed)', 5: 'Conference Room B (Occupied)', 10: 'THE MINOTAUR\'S GANTT LABYRINTH',
      12: 'The Breakout Rooms', 15: 'Lunch And Learn (No Lunch)', 18: 'MANDATORY ICEBREAKER',
      20: 'The Retrospective Crypt', 25: 'THE RECURRING MEETING (NO END DATE)',
    },
    namePool: [
      'Hallway of Hard Stops', 'The Agenda Antechamber', 'Sync Room Sorrows', 'The Parking Lot (Ideas)',
      'Stakeholder Stalagmites', 'The Second Kickoff', 'Alignment Chasm', 'The Touchpoint Tunnels',
      'Deliverable Depths', 'The Slide Deck Crypt', 'Offsite (On Site)', 'The Watercooler Wastes',
      'Follow-Up Falls', 'The Pre-Meeting Meeting', 'Consensus Cavern', 'The Post-Mortem Pit',
      'EOD Approaches',
    ],
    dialogue: [
      { id: 'ch14_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The Dungeon of Infinite Meetings. Adventurers enter seeking treasure. They leave with action items.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'What\'s the treasure?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Legend says at the dungeon\'s heart lies... a free afternoon. UNTOUCHED. Imagine it, hero.' } ] },
      { id: 'ch14_mini1', lines: [
        { speaker: 'Minotaur PM', portrait: 'enemy_boss_minotaur_pm', side: 'right', text: 'HALT. You cannot proceed. You are BLOCKED. By task twelve.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'What\'s task twelve?' },
        { speaker: 'Minotaur PM', portrait: 'enemy_boss_minotaur_pm', side: 'right', text: 'DEFEATING ME. The dependency graph is a CIRCLE, tiny adventurer. All graphs are circles down here. CHARGE!' } ] },
      { id: 'ch14_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The deepest chamber. A conference table older than language. At its head: a calendar invite, pulsing.' },
        { speaker: 'The Recurring Meeting', portrait: 'enemy_boss_recurring', side: 'right', text: 'YOU HAVE JOINED. ATTENDANCE: MANDATORY. AGENDA: NONE. END TIME: NONE.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Hero. HERO. Decline the invite. DECLINE THE INVITE.' },
        { speaker: 'The Recurring Meeting', portrait: 'enemy_boss_recurring', side: 'right', text: 'DECLINING... REQUIRES A REASON. IN WRITING. IN BLOOD. IN TRIPLICATE.' } ] },
      { id: 'ch14_boss_after', lines: [
        { speaker: 'The Recurring Meeting', portrait: 'enemy_boss_recurring', side: 'right', text: 'MEETING... CANCELLED... TIME... RETURNED TO SENDER...' },
        { speaker: '', portrait: '', side: 'left', text: 'The dungeon exhales. Somewhere above, ten thousand calendars clear at once. The sound is like rain.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The free afternoon, hero. It\'s REAL. What do we do with it?!' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Absolutely nothing.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'A LEGEND. YOU\'RE A LEGEND.' } ] },
    ],
  },
  {
    n: 15, title: 'The Moon (Rented)', theme: 'moonrent',
    blurb: 'The moon has a landlord. The rabbits who live there would like a word. The word is "unionize".',
    drops: ['moon_lease', 'rabbit_pamphlet', 'crater_chalk'],
    enemies: [
      { id: 'moon_rabbit', name: 'Moon Rabbit Tenant', element: 'light', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Pounds rice cakes by ancient tradition. Pounds landlords by recent necessity.',
        actions: [
          { name: 'Mallet Swing', desc: 'Rice cake technique, combat application.', type: 'damage', amount: 1 },
          { name: 'Tenant Meeting', desc: 'Converts 3 orbs to Light.', type: 'convert', count: 3, color: 'light' } ] },
      { id: 'crater_hermit', name: 'Crater Hermit Crab', element: 'water', hpMul: 1.2, atkMul: 0.9,
        flavor: 'Lives in a crater. The crater is a studio apartment. Rent: astronomical. Literally.',
        actions: [
          { name: 'Shell Sublease', desc: 'Places a blocker. No vacancies.', type: 'block', count: 1 },
          { name: 'Claw Clause', desc: 'Read your lease closer.', type: 'damage', amount: 1 } ] },
      { id: 'dust_bunny', name: 'Lunar Dust Bunny', element: 'dark', hpMul: 0.8, atkMul: 1.0,
        flavor: 'Accumulated since the moon\'s last deep clean, four billion years ago.',
        actions: [
          { name: 'Dust Cloud', desc: 'Poisons 2 orbs with regolith.', type: 'poison', count: 2 },
          { name: 'Fluffy Ambush', desc: 'Soft. Deadly. Soft.', type: 'damage', amount: 1 } ] },
      { id: 'tide_manager', name: 'Tide Middle Manager', element: 'water', hpMul: 1.1, atkMul: 1.0,
        flavor: 'Manages the tides remotely. The oceans have COMPLAINTS.',
        actions: [
          { name: 'Pull Rank', desc: 'A big gravitational power move.', type: 'bigDamage', amount: 1 },
          { name: 'Tidal Memo', desc: 'Converts 2 orbs to Water.', type: 'convert', count: 2, color: 'water' } ] },
      { id: 'cheese_prospector', name: 'Cheese Prospector', element: 'nature', hpMul: 1.0, atkMul: 1.1,
        flavor: 'Still convinced. Forty years of digging. You will not tell him otherwise.',
        actions: [
          { name: 'Pickaxe of Belief', desc: 'Powered by pure conviction.', type: 'damage', amount: 1 },
          { name: 'Core Sample', desc: 'A big exploratory strike.', type: 'bigDamage', amount: 1 } ] },
      { id: 'satellite_pigeon', name: 'Satellite Pigeon', element: 'fire', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Orbits the coop. Delivers eviction notices from very high altitude.',
        actions: [
          { name: 'Notice Drop', desc: 'Terminal velocity paperwork.', type: 'damage', amount: 1 },
          { name: 'Orbital Coo', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 } ] },
      { id: 'boss_property_mgmt', name: 'Lunar Property Management LLC', element: 'dark', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Not a person. Not a monster. An LLC. Somehow, this is worse.',
        actions: [
          { name: 'Ignore Maintenance Request', desc: 'A big hit of neglect.', type: 'bigDamage', amount: 1 },
          { name: 'Deposit Withheld', desc: 'Locks 4 orbs for "cleaning fees".', type: 'lock', count: 4 },
          { name: 'Rent Increase', desc: 'Poisons 3 orbs, effective immediately.', type: 'poison', count: 3 } ] },
      { id: 'boss_man_in_moon', name: 'The Man in the Moon (Retired)', element: 'light', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'Used to run this place. Came out of retirement because the new management is "ruining the vibe".',
        actions: [
          { name: 'Full Beam', desc: 'A big nostalgic glow blast.', type: 'bigDamage', amount: 1 },
          { name: 'Back In My Day', desc: 'Binds a party member in a long story.', type: 'bind', count: 1 },
          { name: 'Waning Patience', desc: 'Converts 4 orbs to Light.', type: 'convert', count: 4, color: 'light' } ] },
      { id: 'boss_moonlord', name: 'The Moonlord (Landlord of the Moon)', element: 'dark', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'Bought the moon in a questionable auction. Charges the tides rent. The rabbits have HAD IT.',
        actions: [
          { name: 'Eviction Eclipse', desc: 'Massive damage. Notice served.', type: 'bigDamage', amount: 1 },
          { name: 'Security Deposit Void', desc: 'Locks 5 orbs. Normal wear and tear DENIED.', type: 'lock', count: 5 },
          { name: 'Collect Rent', desc: 'Heals himself. The tides paid up.', type: 'heal', amount: 10000 },
          { name: 'Dark Side Lease', desc: 'Converts 3 orbs to Dark.', type: 'convert', count: 3, color: 'dark' } ] },
    ],
    miniboss1: 'boss_property_mgmt', miniboss2: 'boss_man_in_moon', boss: 'boss_moonlord',
    stageNames: {
      1: 'Landing Site (No Parking)', 5: 'The Rice Cake Commons', 10: 'THE LLC MANIFESTS',
      12: 'Sea of Tranquility (Disputed)', 15: 'The Rabbits Vote', 18: 'THE MAN IN THE MOON RETURNS',
      20: 'Dark Side Storage Units', 25: 'THE MOONLORD\'S PENTHOUSE CRATER',
    },
    namePool: [
      'Crater Court', 'The Regolith Steps', 'Low Gravity Grievances', 'The Cheese Claims',
      'Tycho Tenant Alley', 'The Second Landing', 'Orbit of Obligations', 'Mare of Maintenance Requests',
      'The Waxing Wing', 'Craterside Commons', 'The Lease Line', 'Apogee Alley',
      'The Waning Wing', 'Luna Laundry Room', 'The Tide Office', 'Eclipse Escrow',
      'One Small Step (Fee Applies)',
    ],
    dialogue: [
      { id: 'ch15_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The moon, hero! We\'re ON THE MOON. Which, it turns out, has been privately owned since a shady auction in the Fourth Age.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'Who buys the MOON?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Someone who charges the OCEAN rent for using gravity. The rabbits called us. It\'s bad.' } ] },
      { id: 'ch15_mini1', lines: [
        { speaker: 'The LLC', portrait: 'enemy_boss_property_mgmt', side: 'right', text: 'THIS ENTITY ACKNOWLEDGES YOUR COMPLAINT. THIS ENTITY WILL RESPOND WITHIN 6-8 BUSINESS EONS.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'How do you fight an LLC?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Aim for the registered agent!' } ] },
      { id: 'ch15_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The penthouse crater. A throne of security deposits. The Moonlord counts tide-rent by candlelight, though he owns the biggest light in the night sky.' },
        { speaker: 'The Moonlord', portrait: 'enemy_boss_moonlord', side: 'right', text: 'Ah. The rabbits\' little "advocates". The moon is MINE. I have a deed. It\'s laminated.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Laminated by WHOM? We know The Laminator personally, and he keeps RECORDS.' },
        { speaker: 'The Moonlord', portrait: 'enemy_boss_moonlord', side: 'right', text: '...EVICTION PROCEEDINGS BEGIN NOW.' } ] },
      { id: 'ch15_boss_after', lines: [
        { speaker: 'The Moonlord', portrait: 'enemy_boss_moonlord', side: 'right', text: 'Fine... FINE... the moon goes... to the rabbits...' },
        { speaker: '', portrait: '', side: 'left', text: 'The rabbits establish the Lunar Cooperative. First act: rice cakes for all. Second act: the tides ride free, forever.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Best view I\'ve ever had of home.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Don\'t get cozy. Our next stop is... hmm. The travel form just says "HELL (CUSTOMER SERVICE DIVISION)". Neat!' } ] },
    ],
  },
  {
    n: 16, title: 'Customer Service Hell', theme: 'servicehell',
    blurb: 'Actual hell. The demons wear headsets now. Torment is ticket-based. Your case number is your soul.',
    drops: ['ticket_stub_infernal', 'brimstone_headset', 'escalation_sigil'],
    enemies: [
      { id: 'headset_imp', name: 'Headset Imp', element: 'fire', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Thank you for calling the Abyss. This call may be recorded for eternal purposes.',
        actions: [
          { name: 'Scripted Response', desc: 'A damage template, personalized with your name.', type: 'damage', amount: 1 },
          { name: 'Hold Transfer', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 } ] },
      { id: 'ticket_hydra', name: 'Ticket Hydra', element: 'dark', hpMul: 1.3, atkMul: 1.0,
        flavor: 'Close one ticket, two more open. This is not a metaphor. It is a hydra.',
        actions: [
          { name: 'Duplicate Issue', desc: 'A big two-headed strike.', type: 'bigDamage', amount: 1 },
          { name: 'Reopen', desc: 'Heals itself. The issue persists.', type: 'heal', amount: 4000 } ] },
      { id: 'survey_succubus', name: 'Survey Fiend', element: 'dark', hpMul: 0.9, atkMul: 1.1,
        flavor: 'On a scale of 1 to 10, how likely are you to recommend eternal damnation to a friend?',
        actions: [
          { name: 'NPS Drain', desc: 'Poisons 3 orbs with feedback requests.', type: 'poison', count: 3 },
          { name: 'Follow-Up', desc: 'Just checking in. Violently.', type: 'damage', amount: 1 } ] },
      { id: 'chatbot_golem', name: 'Chatbot Golem (Legacy)', element: 'water', hpMul: 1.2, atkMul: 0.9,
        flavor: 'I\'m sorry, I didn\'t understand that. I\'m sorry, I didn\'t understand that. I\'m sorry—',
        actions: [
          { name: 'Did Not Understand', desc: 'Converts 3 orbs to Water. Rephrasing...', type: 'convert', count: 3, color: 'water' },
          { name: 'Canned Reply', desc: 'CLANG. Have you tried our FAQ?', type: 'damage', amount: 1 } ] },
      { id: 'refund_wraith', name: 'Refund Denial Wraith', element: 'dark', hpMul: 1.0, atkMul: 1.1,
        flavor: 'Your refund has been processed. It has been processed into the void.',
        actions: [
          { name: 'Store Credit Curse', desc: 'Locks 3 orbs as store credit.', type: 'lock', count: 3 },
          { name: 'Policy Citation', desc: 'Section 4: no.', type: 'damage', amount: 1 } ] },
      { id: 'warranty_demon', name: 'Extended Warranty Demon', element: 'fire', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Has been trying to reach you about your soul\'s extended warranty.',
        actions: [
          { name: 'Robocall', desc: 'A big automated blast.', type: 'bigDamage', amount: 1 },
          { name: 'Coverage Pitch', desc: 'Converts 2 orbs to Fire.', type: 'convert', count: 2, color: 'fire' } ] },
      { id: 'boss_tier9_shark', name: 'Tier 9 Support Archfiend', element: 'fire', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Nine escalations deep. Wields the Forbidden Knowledge: the supervisor override code.',
        actions: [
          { name: 'Override Code', desc: 'A big forbidden keystroke.', type: 'bigDamage', amount: 1 },
          { name: 'Ticket Storm', desc: 'Locks 4 orbs in the queue.', type: 'lock', count: 4 },
          { name: 'Brimstone Hold Music', desc: 'Converts 3 orbs to Fire.', type: 'convert', count: 3, color: 'fire' } ] },
      { id: 'boss_kpi_beast', name: 'The KPI Beast', element: 'light', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'Feeds on metrics. Average handle time, first call resolution, and your dwindling hope.',
        actions: [
          { name: 'Metric Maul', desc: 'A big data-driven strike.', type: 'bigDamage', amount: 1 },
          { name: 'Dashboard Glare', desc: 'Converts 4 orbs to Light. The numbers are RED.', type: 'convert', count: 4, color: 'light' },
          { name: 'Handle Time', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 } ] },
      { id: 'boss_complaints_prince', name: 'The Prince of Unresolved Tickets', element: 'dark', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'Third in line to the infernal throne. Rules a backlog stretching past the heat death of patience.',
        actions: [
          { name: 'Backlog Avalanche', desc: 'Massive damage. Every unresolved case at once.', type: 'bigDamage', amount: 1 },
          { name: 'Eternal Hold', desc: 'Reduces move time by 3 seconds.', type: 'timerDown', amount: 3 },
          { name: 'Case Closed (Lie)', desc: 'Heals himself. The case was NOT closed.', type: 'heal', amount: 10000 },
          { name: 'Soul Ticket', desc: 'Binds a party member to a case number.', type: 'bind', count: 1 },
          { name: 'Infernal Merge', desc: 'Converts 3 orbs to Dark.', type: 'convert', count: 3, color: 'dark' } ] },
    ],
    miniboss1: 'boss_tier9_shark', miniboss2: 'boss_kpi_beast', boss: 'boss_complaints_prince',
    stageNames: {
      1: 'Abandon All Hope (Press 1)', 5: 'The Chatbot Graveyard', 10: 'ESCALATION: TIER 9',
      12: 'The Refund River (Dry)', 15: 'Survey Purgatory', 18: 'THE KPI BEAST FEEDS',
      20: 'Backlog Bluffs', 25: 'THE PRINCE OF UNRESOLVED TICKETS',
    },
    namePool: [
      'Queue of the Damned', 'The Scripted Circles', 'IVR Inferno', 'The Muted Masses',
      'Callback Chasm', 'The Second Ring', 'Brimstone Break Room', 'The Knowledge Base (On Fire)',
      'Warranty Wastes', 'The Transfer Tunnels', 'Case Number Catacombs', 'First Call Damnation',
      'The Deflection Fields', 'Hold Music Hollow', 'The Churn Pits', 'Escalation Escarpment',
      'The Final FAQ',
    ],
    dialogue: [
      { id: 'ch16_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'So. Hell restructured. The pitchforks are headsets now. Torment is self-service, with a 2% resolution rate.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'Why are WE here?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The kingdom\'s complaints all route here now. Including ours. Hero... we\'re here about the birthday box. It\'s TICKET TIME.' } ] },
      { id: 'ch16_mini1', lines: [
        { speaker: 'Tier 9 Archfiend', portrait: 'enemy_boss_tier9_shark', side: 'right', text: 'You\'ve reached Tier 9. Congratulations. Only three mortals have ever made it this far. Two hung up.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'And the third?' },
        { speaker: 'Tier 9 Archfiend', portrait: 'enemy_boss_tier9_shark', side: 'right', text: 'You\'re speaking to him. I never got MY refund either. Now I ANSWER the phones. Don\'t become me, hero. FIGHT.' } ] },
      { id: 'ch16_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The Backlog Throne. A prince of darkness, buried to the shoulders in tickets, each one softly screaming.' },
        { speaker: 'The Prince', portrait: 'enemy_boss_complaints_prince', side: 'right', text: 'CASE 1: A KNIGHT, WRONGED. CASE 2: A WIDOW, OVERCHARGED. CASE 3,000,000: A FAIRY... AWAITING A BIRTHDAY BOX.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'THAT\'S MY TICKET! IT\'S REAL! IT\'S BEEN OPEN FOR NINE YEARS!' },
        { speaker: 'The Prince', portrait: 'enemy_boss_complaints_prince', side: 'right', text: 'AND IT SHALL REMAIN OPEN. FOREVER. AN OPEN TICKET IS AN OPEN WOUND. AND I AM ITS KING.' } ] },
      { id: 'ch16_boss_after', lines: [
        { speaker: 'The Prince', portrait: 'enemy_boss_complaints_prince', side: 'right', text: 'RESOLVING... ALL... TICKETS... satisfaction... guaranteed...' },
        { speaker: '', portrait: '', side: 'left', text: 'Three million cases close at once. The sound is indescribable. It is the sound of everyone, everywhere, finally getting through.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Ticket closed, hero. All of them. Even mine. ESPECIALLY mine.' } ] },
    ],
  },
];
