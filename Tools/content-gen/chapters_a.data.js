// Chapters 2-6: still mostly recognizable fantasy, bureaucracy creeping in.
module.exports = [
  {
    n: 2, title: 'The Darkwood Permit Office', theme: 'darkwood',
    blurb: 'An ancient forest. To walk beneath its boughs you will need Form MT-7, Form MT-8, and a pen that works.',
    drops: ['bark_form', 'sap_ink', 'permit_stub'],
    enemies: [
      { id: 'treant_clerk', name: 'Treant Desk Clerk', element: 'nature', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Four hundred years old. Two hundred spent at this desk.',
        actions: [
          { name: 'Branch Stamp', desc: 'A woody wallop.', type: 'damage', amount: 1 },
          { name: 'Regrow Paperwork', desc: 'Converts 3 orbs to Nature.', type: 'convert', count: 3, color: 'nature' } ] },
      { id: 'permit_sprite', name: 'Permit Sprite', element: 'light', hpMul: 0.8, atkMul: 0.9,
        flavor: 'Grants wishes, pending approval. Approval takes 6-8 weeks.',
        actions: [
          { name: 'Pending Review', desc: 'Locks 3 orbs until processed.', type: 'lock', count: 3 },
          { name: 'Glitter Jab', desc: 'Sparkly and rude.', type: 'damage', amount: 1 } ] },
      { id: 'bramble_lawyer', name: 'Bramble Paralegal', element: 'nature', hpMul: 1.1, atkMul: 1.0,
        flavor: 'Bills in thorns. Reads the fine print out loud, slowly.',
        actions: [
          { name: 'Thorny Clause', desc: 'Poisons 2 orbs with legalese.', type: 'poison', count: 2 },
          { name: 'Objection', desc: 'A stinging rebuttal.', type: 'damage', amount: 1 } ] },
      { id: 'owl_auditor', name: 'Night Owl Auditor', element: 'dark', hpMul: 0.9, atkMul: 1.1,
        flavor: 'Who? YOU. That is who. Your receipts, please.',
        actions: [
          { name: 'Late Review', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 },
          { name: 'Talon Deduction', desc: 'Painfully itemized.', type: 'damage', amount: 1 } ] },
      { id: 'stamp_beetle', name: 'Rubber Stamp Beetle', element: 'fire', hpMul: 0.9, atkMul: 1.2,
        flavor: 'DENIED. DENIED. DENIED. It only knows one word.',
        actions: [
          { name: 'DENIED', desc: 'A crushing red stamp.', type: 'bigDamage', amount: 1 } ] },
      { id: 'file_fungus', name: 'Fungal Filing Clerk', element: 'dark', hpMul: 1.0, atkMul: 0.9,
        flavor: 'Grows through the archives. Alphabetized by spore.',
        actions: [
          { name: 'Misfile', desc: 'Poisons 3 orbs. Lost in the system.', type: 'poison', count: 3 },
          { name: 'Cabinet Slam', desc: 'Drawer to the face.', type: 'damage', amount: 1 } ] },
      { id: 'boss_ranger_doreen', name: 'Ranger Supervisor Doreen', element: 'nature', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Protects the forest from you, specifically. Has a whistle and the authority to use it.',
        actions: [
          { name: 'Citation Storm', desc: 'A flurry of fines.', type: 'damage', amount: 1 },
          { name: 'Trail Closure', desc: 'Places a blocker on the board.', type: 'block', count: 1 },
          { name: 'The Whistle', desc: 'A big, shrill hit.', type: 'bigDamage', amount: 1 } ] },
      { id: 'boss_laminator', name: 'The Laminator', element: 'fire', boss: true, hpMul: 11, atkMul: 1.3, countdown: 2,
        flavor: 'Once it laminates you, you are PERMANENT. It considers this a kindness.',
        actions: [
          { name: 'Heat Seal', desc: 'Locks 4 orbs in glossy plastic.', type: 'lock', count: 4 },
          { name: 'Hot Press', desc: 'A scalding flatten.', type: 'bigDamage', amount: 1 },
          { name: 'Fresh Sheet', desc: 'Converts 3 orbs to Fire.', type: 'convert', count: 3, color: 'fire' } ] },
      { id: 'boss_barkimedes', name: 'Barkimedes, Director of Permits', element: 'nature', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'The oldest tree in the wood. Every ring is a fiscal year. He remembers ALL of them.',
        actions: [
          { name: 'Root Canal', desc: 'Massive damage from below.', type: 'bigDamage', amount: 1 },
          { name: 'Archive Everything', desc: 'Locks 5 orbs in living wood.', type: 'lock', count: 5 },
          { name: 'Photosynthesize', desc: 'Heals himself. The sun approves his request.', type: 'heal', amount: 9000 },
          { name: 'Falling Backlog', desc: 'Converts 3 orbs to Nature.', type: 'convert', count: 3, color: 'nature' } ] },
    ],
    miniboss1: 'boss_ranger_doreen', miniboss2: 'boss_laminator', boss: 'boss_barkimedes',
    stageNames: {
      1: 'Welcome to the Waiting Grove', 5: 'Form MT-7 (Lost)', 10: 'RANGER CHECKPOINT',
      12: 'The Sap-Stained Archives', 15: 'An Owl Demands Receipts', 18: 'THE LAMINATION STATION',
      20: 'Hall of Expired Permits', 25: 'BARKIMEDES: FINAL APPROVAL',
    },
    namePool: [
      'Queue Among the Ferns', 'Take a Number, Take a Root', 'The Underbrush Backlog',
      'Sprites With Stamps', 'Thorns and Conditions', 'The Mossy In-Tray', 'Beetle Processing',
      'Canopy of Red Tape', 'The Second Waiting Grove', 'Nocturnal Compliance', 'Fern-Level Bureaucracy',
      'The Photocopse', 'Appeals and Squirrels', 'Deadwood Deadlines', 'The Rustling Registry',
      'Overgrown Overheads', 'Twigs in Triplicate',
    ],
    dialogue: [
      { id: 'ch02_intro', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The Darkwood. Ancient. Mysterious. Fully permitted.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Remember: we need a hiking permit, a camping permit, and a permit permit. I have NONE of them.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'What happens if we just walk in?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The trees file a complaint. And hero... the trees ARE the complaint department.' } ] },
      { id: 'ch02_mini1', lines: [
        { speaker: 'Doreen', portrait: 'enemy_boss_ranger_doreen', side: 'right', text: 'Sir. SIR. This is a protected bureaucracy. Do you have your MT-7?' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We have an MT-6 and a dream.' },
        { speaker: 'Doreen', portrait: 'enemy_boss_ranger_doreen', side: 'right', text: '...I\'m reaching for my whistle now. I want you to know this hurts me more than it hurts you. It won\'t, though.' } ] },
      { id: 'ch02_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'In the heart of the wood stands Barkimedes. His branches hold forty thousand pending applications.' },
        { speaker: 'Barkimedes', portrait: 'enemy_boss_barkimedes', side: 'right', text: 'AH. THE APPLICANTS. I HAVE REVIEWED YOUR FILE. IT IS... INCOMPLETE.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'We attached everything! Even the optional section!' },
        { speaker: 'Barkimedes', portrait: 'enemy_boss_barkimedes', side: 'right', text: 'THE OPTIONAL SECTION IS MANDATORY. IT SAYS SO IN THE OPTIONAL SECTION. PREPARE FOR ARBORTRATION.' } ] },
      { id: 'ch02_boss_after', lines: [
        { speaker: 'Barkimedes', portrait: 'enemy_boss_barkimedes', side: 'right', text: 'APPLICATION... APPROVED... EFFECTIVE... IMMEDIATELY...' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'That\'s all we wanted, big guy.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Onward to Mount Grumble! I hear the dwarves are on strike, so mining permits are — oh no. Oh NO. They\'re free. Something is very wrong.' } ] },
    ],
  },
  {
    n: 3, title: 'Mount Grumble', theme: 'grumble',
    blurb: 'The dwarves are on strike. The mountain is on fire. These facts are related.',
    drops: ['coal_chunk', 'strike_flyer', 'grumble_ore'],
    enemies: [
      { id: 'picket_dwarf', name: 'Picketing Dwarf', element: 'fire', hpMul: 1.0, atkMul: 1.1,
        flavor: 'His sign says "FAIR WAGES OR NO FORGES". His axe agrees.',
        actions: [
          { name: 'Sign Swing', desc: 'The message lands. Hard.', type: 'damage', amount: 1 },
          { name: 'Chant', desc: 'A big rallying blow.', type: 'bigDamage', amount: 1 } ] },
      { id: 'forge_golem', name: 'Unattended Forge Golem', element: 'fire', hpMul: 1.4, atkMul: 0.9,
        flavor: 'Nobody told it the shift ended three weeks ago. It keeps forging.',
        actions: [
          { name: 'Overwork', desc: 'Converts 3 orbs to Fire.', type: 'convert', count: 3, color: 'fire' },
          { name: 'Hammer Time', desc: 'Clang.', type: 'damage', amount: 1 } ] },
      { id: 'coal_bat', name: 'Coal Bat', element: 'dark', hpMul: 0.8, atkMul: 1.0,
        flavor: 'Lives in shaft 7. Extremely sooty. Zero regrets.',
        actions: [
          { name: 'Soot Screech', desc: 'Poisons 2 orbs with coal dust.', type: 'poison', count: 2 },
          { name: 'Dive', desc: 'A flappy ambush.', type: 'damage', amount: 1 } ] },
      { id: 'grumble_goat', name: 'Grumble Goat', element: 'nature', hpMul: 1.1, atkMul: 1.1,
        flavor: 'Eats strike flyers. Sides with no one. Rams everyone.',
        actions: [
          { name: 'Headbutt', desc: 'Nonpartisan violence.', type: 'bigDamage', amount: 1 } ] },
      { id: 'scaffold_spider', name: 'Scaffold Spider', element: 'dark', hpMul: 0.9, atkMul: 0.9,
        flavor: 'Took over the mine scaffolding. Now charges rent.',
        actions: [
          { name: 'Web Toll', desc: 'Locks 3 orbs behind a paywall.', type: 'lock', count: 3 },
          { name: 'Eight-Leg Kick', desc: 'All of them at once.', type: 'damage', amount: 1 } ] },
      { id: 'ash_slime', name: 'Ash Slime', element: 'fire', hpMul: 1.0, atkMul: 0.9,
        flavor: 'Gerald\'s cousin from the volcanic branch office.',
        actions: [
          { name: 'Cinder Bounce', desc: 'Warm and unwelcome.', type: 'damage', amount: 1 },
          { name: 'Smolder', desc: 'Converts 2 orbs to Fire.', type: 'convert', count: 2, color: 'fire' } ] },
      { id: 'boss_flintbeard', name: 'Foreman Flintbeard', element: 'fire', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Caught between the workers and the owner. Chose violence, diplomatically.',
        actions: [
          { name: 'Middle Management Slam', desc: 'He\'s under a lot of pressure.', type: 'bigDamage', amount: 1 },
          { name: 'Safety Meeting', desc: 'Locks 4 orbs. Attendance mandatory.', type: 'lock', count: 4 },
          { name: 'Beard Sparks', desc: 'Converts 3 orbs to Fire.', type: 'convert', count: 3, color: 'fire' } ] },
      { id: 'boss_bertha', name: 'Union-Buster Bertha', element: 'dark', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'A golem built by management, from management, for management.',
        actions: [
          { name: 'Intimidate', desc: 'Binds a party member with a very firm handshake.', type: 'bind', count: 1 },
          { name: 'Cost Cutting', desc: 'A big swing at overhead. You are overhead.', type: 'bigDamage', amount: 1 },
          { name: 'Overtime Clause', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 } ] },
      { id: 'boss_goldtooth', name: 'Magnate Goldtooth', element: 'dark', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'Owns the mine, the mountain, and — he claims — the concept of digging.',
        actions: [
          { name: 'Hostile Takeover', desc: 'Massive damage. It\'s just business.', type: 'bigDamage', amount: 1 },
          { name: 'Asset Freeze', desc: 'Locks 5 orbs in escrow.', type: 'lock', count: 5 },
          { name: 'Golden Parachute', desc: 'Heals himself. He always lands softly.', type: 'heal', amount: 10000 },
          { name: 'Dividend of Doom', desc: 'Converts 3 orbs to Dark.', type: 'convert', count: 3, color: 'dark' } ] },
    ],
    miniboss1: 'boss_flintbeard', miniboss2: 'boss_bertha', boss: 'boss_goldtooth',
    stageNames: {
      1: 'Base Camp Grievances', 5: 'The Picket Switchback', 10: 'FOREMAN ON THE FLOOR',
      12: 'Shaft 7 (Haunted, Sooty)', 15: 'The Goat Incident', 18: 'BERTHA CLOCKS IN',
      20: 'The Executive Elevator', 25: 'GOLDTOOTH: FINAL OFFER',
    },
    namePool: [
      'Switchbacks and Setbacks', 'The Smoldering Break Room', 'Scaffold Rent Is Due',
      'Coal Dust Commute', 'The Second Shift That Never Ends', 'Golem Overtime', 'Strike Fund Scramble',
      'The Grumbling Path', 'Ashfall Alley', 'Cavern of Unpaid Invoices', 'The Foreman\'s Shortcut',
      'Molten Middle Ground', 'Negotiation Ridge', 'The Ore Cart Standoff', 'Peak Discontent',
      'The Slag Heap Summit', 'Lava Level Bureaucracy',
    ],
    dialogue: [
      { id: 'ch03_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Mount Grumble! Famous for ore, beards, and passive aggression. Currently: a labor dispute.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'Whose side are we on?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The side that isn\'t currently swinging at us. This changes moment to moment. Stay flexible!' } ] },
      { id: 'ch03_mini1', lines: [
        { speaker: 'Flintbeard', portrait: 'enemy_boss_flintbeard', side: 'right', text: 'The workers hate me. The owner hates me. My beard is on fire. This is the best day I\'ve had all month.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We can just... go around you?' },
        { speaker: 'Flintbeard', portrait: 'enemy_boss_flintbeard', side: 'right', text: 'And rob me of the one meeting I can WIN? Raise your fists, paper-pusher.' } ] },
      { id: 'ch03_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The summit office. One dwarf. Forty gold teeth. A desk made of other, smaller desks.' },
        { speaker: 'Goldtooth', portrait: 'enemy_boss_goldtooth', side: 'right', text: 'Adventurers! Wonderful. I\'d like to buy you. Not hire — BUY.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We\'re not for sale.' },
        { speaker: 'Goldtooth', portrait: 'enemy_boss_goldtooth', side: 'right', text: 'Everyone says that before the second offer. There is no second offer. FIGHT.' } ] },
      { id: 'ch03_boss_after', lines: [
        { speaker: 'Goldtooth', portrait: 'enemy_boss_goldtooth', side: 'right', text: 'Fine... FINE. The dwarves get their wages... and casual Fridays...' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The strike is over! The forges are singing again! Beautifully! Wait, no, that\'s the fire alarm.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Where next?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Down, hero. Deep down. The Soggy Depths. Bring a towel and INFINITE patience — their call center has a hold queue.' } ] },
    ],
  },
  {
    n: 4, title: 'The Soggy Depths', theme: 'depths',
    blurb: 'An underwater kingdom that outsourced everything to itself. Your call is important to them.',
    drops: ['brine_ticket', 'pearl_button', 'kelp_cable'],
    enemies: [
      { id: 'hold_jelly', name: 'Hold Music Jellyfish', element: 'water', hpMul: 0.9, atkMul: 0.9,
        flavor: 'Its tentacles play smooth jazz. Forever. FOREVER.',
        actions: [
          { name: 'Smooth Jazz', desc: 'Reduces move time by 2 seconds. Please continue to hold.', type: 'timerDown', amount: 2 },
          { name: 'Sting Chord', desc: 'A jazzy zap.', type: 'damage', amount: 1 } ] },
      { id: 'squid_operator', name: 'Switchboard Squid', element: 'water', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Handles eight calls at once. Transfers all of them to the wrong department.',
        actions: [
          { name: 'Transfer', desc: 'Converts 3 orbs to Water.', type: 'convert', count: 3, color: 'water' },
          { name: 'Ink Memo', desc: 'A dark, smudgy blast.', type: 'damage', amount: 1 } ] },
      { id: 'crab_supervisor', name: 'Crab Shift Supervisor', element: 'fire', hpMul: 1.2, atkMul: 1.0,
        flavor: 'Sideways promotion after sideways promotion. Now permanently sideways.',
        actions: [
          { name: 'Pinch Performance', desc: 'Locks 3 orbs in a firm claw.', type: 'lock', count: 3 },
          { name: 'Shell Slam', desc: 'A crusty blow.', type: 'damage', amount: 1 } ] },
      { id: 'phone_eel', name: 'Landline Eel', element: 'light', hpMul: 0.9, atkMul: 1.2,
        flavor: 'Lives in the cables. IS the cables. Your call may be monitored.',
        actions: [
          { name: 'Static Shock', desc: 'ZZZZAP. Bad connection.', type: 'bigDamage', amount: 1 } ] },
      { id: 'complaint_clam', name: 'Complaint Clam', element: 'nature', hpMul: 1.3, atkMul: 0.8,
        flavor: 'Contains ten thousand unread complaints and one (1) pearl of pure grievance.',
        actions: [
          { name: 'Clam Up', desc: 'Places a blocker. Feedback rejected.', type: 'block', count: 1 },
          { name: 'Grievance Pearl', desc: 'Fires a compressed complaint.', type: 'damage', amount: 1 } ] },
      { id: 'bubble_intern', name: 'Bubble Intern', element: 'water', hpMul: 0.7, atkMul: 0.8,
        flavor: 'A bubble with a lanyard. Pops with enthusiasm. Reforms with less.',
        actions: [
          { name: 'Eager Pop', desc: 'A tiny, hopeful attack.', type: 'damage', amount: 1 } ] },
      { id: 'boss_support_shark', name: 'Tier 2 Support Shark', element: 'water', boss: true, hpMul: 10, atkMul: 1.5, countdown: 2,
        flavor: 'You have been escalated. He can smell an unresolved ticket from three miles away.',
        actions: [
          { name: 'Escalation Bite', desc: 'Your ticket has been CHOMPED.', type: 'bigDamage', amount: 1 },
          { name: 'Chum the Queue', desc: 'Converts 3 orbs to Water.', type: 'convert', count: 3, color: 'water' },
          { name: 'Known Issue', desc: 'Locks 3 orbs. A fix is coming in a future release.', type: 'lock', count: 3 } ] },
      { id: 'boss_deep_karen', name: 'The Abyssal Complainant', element: 'dark', boss: true, hpMul: 11, atkMul: 1.4, countdown: 3,
        flavor: 'An elder horror who has asked to speak to every manager in the ocean. All of them resigned.',
        actions: [
          { name: 'Speak To The Manager', desc: 'Binds your leader in a conversation.', type: 'bind', count: 1 },
          { name: 'Review Bomb', desc: 'One star. Massive damage.', type: 'bigDamage', amount: 1 },
          { name: 'Tentacled Testimony', desc: 'Poisons 3 orbs with anecdotes.', type: 'poison', count: 3 } ] },
      { id: 'boss_hold_queen', name: 'The Hold Queen', element: 'water', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'A jellyfish the size of a cathedral. Every soul on hold, ever, drifts in her glow.',
        actions: [
          { name: 'Your Call Is Important', desc: 'Massive damage. It is not important.', type: 'bigDamage', amount: 1 },
          { name: 'Estimated Wait: Eternity', desc: 'Reduces move time by 3 seconds.', type: 'timerDown', amount: 3 },
          { name: 'Tentacle Queue', desc: 'Locks 5 orbs in line.', type: 'lock', count: 5 },
          { name: 'Soothing Chime', desc: 'Heals herself. Thank you for your patience.', type: 'heal', amount: 10000 } ] },
    ],
    miniboss1: 'boss_support_shark', miniboss2: 'boss_deep_karen', boss: 'boss_hold_queen',
    stageNames: {
      1: 'Please Listen Closely (Glub)', 5: 'Menu Option Four Fathoms', 10: 'ESCALATION: SHARK',
      12: 'The Kelp Cable Tangle', 15: 'Complaints Department Reef', 18: 'THE ABYSSAL COMPLAINANT',
      20: 'The Deepest Queue', 25: 'AUDIENCE WITH THE HOLD QUEEN',
    },
    namePool: [
      'Press One For Peril', 'The Jazz Gets Louder', 'Sideways Career Current', 'Ticket Trench',
      'The Unread Inbox Reef', 'Static on Line Two', 'Interns of the Deep', 'Transfer Tide',
      'The Second Menu', 'Grievance Grotto', 'Callback Cavern', 'The Muted Depths',
      'Speakerphone Abyss', 'Currents of Miscommunication', 'The Long Wait Down', 'Brine and Hold',
      'Voicemail Vortex',
    ],
    dialogue: [
      { id: 'ch04_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The Soggy Depths! Underwater kingdom, proud people, and a call center that handles the ENTIRE ocean.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'How do we breathe down here?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'You filled out the Air Request Form in chapter two! See? Bureaucracy SAVES lives. Occasionally. By accident.' } ] },
      { id: 'ch04_mini1', lines: [
        { speaker: 'Support Shark', portrait: 'enemy_boss_support_shark', side: 'right', text: 'Hi, this is Tier 2. I see you\'ve been escalated. I\'m required to ask: have you tried turning your party off and on again?' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'They\'re people.' },
        { speaker: 'Support Shark', portrait: 'enemy_boss_support_shark', side: 'right', text: 'So that\'s a no. Noting non-compliance. Beginning chomp procedure.' } ] },
      { id: 'ch04_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The heart of the Depths. A jellyfish vast as a cathedral, glowing with the patience of the eternally on-hold.' },
        { speaker: 'The Hold Queen', portrait: 'enemy_boss_hold_queen', side: 'right', text: 'YOUR CALL... HAS REACHED... THE FRONT OF THE QUEUE.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'After nine hundred years, someone got through! Hero, ask about our billing issue!' },
        { speaker: 'The Hold Queen', portrait: 'enemy_boss_hold_queen', side: 'right', text: 'TO CONTINUE... PLEASE HOLD... FOREVER.' } ] },
      { id: 'ch04_boss_after', lines: [
        { speaker: 'The Hold Queen', portrait: 'enemy_boss_hold_queen', side: 'right', text: 'YOUR ISSUE... HAS BEEN... RESOLVED... SURVEY TO FOLLOW...' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The hold queue is FREE! Ten thousand mer-people just heard a real voice for the first time! They\'re weeping!' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Five stars. Would battle again.' } ] },
    ],
  },
  {
    n: 5, title: 'Crystal Cavern HR', theme: 'crystalhr',
    blurb: 'A cavern of living gemstones running the kingdom\'s Human Resources. None of them are human. All of them are resources.',
    drops: ['facet_file', 'onboarding_geode', 'morale_shard'],
    enemies: [
      { id: 'gem_recruiter', name: 'Gemstone Recruiter', element: 'light', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Found your resume in a rockslide. Loves your "facets". Wants to "align".',
        actions: [
          { name: 'Cold Outreach', desc: 'A dazzling unsolicited attack.', type: 'damage', amount: 1 },
          { name: 'Pipeline', desc: 'Converts 3 orbs to Light.', type: 'convert', count: 3, color: 'light' } ] },
      { id: 'quartz_screener', name: 'Quartz Phone Screener', element: 'light', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Asks where you see yourself in five geological eras.',
        actions: [
          { name: 'Screening Question', desc: 'Locks 3 orbs pending your answer.', type: 'lock', count: 3 },
          { name: 'Crystal Chirp', desc: 'A pointed follow-up.', type: 'damage', amount: 1 } ] },
      { id: 'onboarding_bat', name: 'Onboarding Bat', element: 'dark', hpMul: 0.8, atkMul: 1.0,
        flavor: 'Delivers your welcome packet at terminal velocity.',
        actions: [
          { name: 'Welcome Packet', desc: 'A heavy folder to the face.', type: 'bigDamage', amount: 1 } ] },
      { id: 'stalag_mite', name: 'Stalag-Mite', element: 'nature', hpMul: 1.1, atkMul: 0.9,
        flavor: 'Technically a pest. Technically also employee of the month.',
        actions: [
          { name: 'Drip Feedback', desc: 'Poisons 2 orbs, one drop at a time.', type: 'poison', count: 2 },
          { name: 'Point Up', desc: 'You walked into it.', type: 'damage', amount: 1 } ] },
      { id: 'morale_moth', name: 'Morale Moth', element: 'fire', hpMul: 0.9, atkMul: 1.1,
        flavor: 'Drawn to burnout. Organizes pizza parties that fix nothing.',
        actions: [
          { name: 'Pizza Party', desc: 'Heals its allies. Morale is now mandatory.', type: 'heal', amount: 3000 },
          { name: 'Wing Dust', desc: 'Inspirational particulates.', type: 'damage', amount: 1 } ] },
      { id: 'crystal_clerk', name: 'Crystalline File Clerk', element: 'water', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Every personnel file, refracted in triplicate.',
        actions: [
          { name: 'Refile', desc: 'Converts 3 orbs to Water.', type: 'convert', count: 3, color: 'water' },
          { name: 'Sharp Corner', desc: 'Mind the edges.', type: 'damage', amount: 1 } ] },
      { id: 'boss_benefits_basilisk', name: 'Benefits Basilisk', element: 'dark', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Look directly at the dental plan and be turned to stone. It\'s a great plan. Nobody has survived reading it.',
        actions: [
          { name: 'Fine Print Gaze', desc: 'Binds a party member who read too closely.', type: 'bind', count: 1 },
          { name: 'Coverage Gap', desc: 'A big hit. Not covered.', type: 'bigDamage', amount: 1 },
          { name: 'Premium Hike', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 } ] },
      { id: 'boss_org_chart', name: 'The Living Org Chart', element: 'light', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'A diagram that achieved sentience and immediately restructured itself.',
        actions: [
          { name: 'Dotted Line', desc: 'You now report to pain.', type: 'damage', amount: 1 },
          { name: 'Reorg', desc: 'Converts 4 orbs to Light. Boxes shift ominously.', type: 'convert', count: 4, color: 'light' },
          { name: 'Circular Reporting', desc: 'A big spinning slam.', type: 'bigDamage', amount: 1 } ] },
      { id: 'boss_vp_vibes', name: 'Vice President of Vibes', element: 'light', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'A colossal geode in a blazer. Radiates synergy. The vibes are, frankly, immaculate and hostile.',
        actions: [
          { name: 'Vibe Check', desc: 'Massive damage to anyone found lacking.', type: 'bigDamage', amount: 1 },
          { name: 'Culture Fit', desc: 'Locks 5 orbs that don\'t match the culture.', type: 'lock', count: 5 },
          { name: 'Wellness Wednesday', desc: 'Heals itself. Self-care.', type: 'heal', amount: 10000 },
          { name: 'Synergy Beam', desc: 'Converts 3 orbs to Light.', type: 'convert', count: 3, color: 'light' } ] },
    ],
    miniboss1: 'boss_benefits_basilisk', miniboss2: 'boss_org_chart', boss: 'boss_vp_vibes',
    stageNames: {
      1: 'Reception (Please Shine In)', 5: 'The Screening Chamber', 10: 'BENEFITS ORIENTATION',
      12: 'Hall of Personnel Facets', 15: 'Mandatory Fun Cavern', 18: 'THE ORG CHART AWAKENS',
      20: 'Performance Crystal Review', 25: 'THE CORNER OFFICE GEODE',
    },
    namePool: [
      'Lanyard Grotto', 'The Onboarding Descent', 'Facets and Feedback', 'Refraction Interview',
      'The Pizza Party Chamber', 'Alignment Alley', 'Core Values Cave', 'The Shimmering Backlog',
      'Stalactite Standup', 'The Long One-on-One', 'Glittering Deliverables', 'The Crystal Cubicles',
      'Circle Back Cavern', 'Touchbase Tunnel', 'The Synergy Seam', 'Deep Dive (Literal)',
      'Quarterly Quartz',
    ],
    dialogue: [
      { id: 'ch05_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Crystal Cavern HR. All hiring for the entire kingdom happens here. The paperwork is beautiful AND sharp.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'Why are we here?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Someone classified you as "contractor". We\'re getting you benefits, hero. Even if we have to fight the dental plan. ESPECIALLY the dental plan.' } ] },
      { id: 'ch05_mini1', lines: [
        { speaker: 'Benefits Basilisk', portrait: 'enemy_boss_benefits_basilisk', side: 'right', text: 'Sssso. You wish to enroll. Have you read the plan documentsss?' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Nobody reads the plan documents.' },
        { speaker: 'Benefits Basilisk', portrait: 'enemy_boss_benefits_basilisk', side: 'right', text: 'CORRECT. Those who read... are STONE. You may yet ssssurvive. Open enrollment... hasss begun.' } ] },
      { id: 'ch05_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The corner office. It has no corners. It is a geode the size of a house, wearing a blazer.' },
        { speaker: 'VP of Vibes', portrait: 'enemy_boss_vp_vibes', side: 'right', text: 'Heyyy, team! Love the energy. Love it. Quick vibe check before I approve your paperwork?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Our vibes are excellent and fully documented!' },
        { speaker: 'VP of Vibes', portrait: 'enemy_boss_vp_vibes', side: 'right', text: 'Mmm. See, I\'m getting more of a "hostile takeover" energy? Which, respect. Let\'s workshop it. WITH VIOLENCE.' } ] },
      { id: 'ch05_boss_after', lines: [
        { speaker: 'VP of Vibes', portrait: 'enemy_boss_vp_vibes', side: 'right', text: 'Okay... wow... big feelings today... approving... everything...' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Full benefits! Retroactive! Hero, you have DENTAL. You can\'t be stopped now.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Onward. I want to use this dental plan on something.' } ] },
    ],
  },
  {
    n: 6, title: 'The Haunted Suburbs', theme: 'suburbs',
    blurb: 'Every house is haunted. Every lawn is perfect. The Homeowners Association demands both.',
    drops: ['ecto_form', 'lawn_trophy', 'casserole_dish'],
    enemies: [
      { id: 'lawn_ghost', name: 'Lawn Care Ghost', element: 'nature', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Died mowing. Continues mowing. The stripes are IMMACULATE.',
        actions: [
          { name: 'Phantom Mower', desc: 'A spectral trim, ankle height.', type: 'damage', amount: 1 },
          { name: 'Grass Guilt', desc: 'Converts 3 orbs to Nature.', type: 'convert', count: 3, color: 'nature' } ] },
      { id: 'hedge_wraith', name: 'Hedge Wraith', element: 'dark', hpMul: 1.1, atkMul: 1.0,
        flavor: 'Trimmed into the shape of regret. Regulation height.',
        actions: [
          { name: 'Topiary Terror', desc: 'Poisons 2 orbs with clippings.', type: 'poison', count: 2 },
          { name: 'Rustle', desc: 'The hedge is CLOSER now.', type: 'damage', amount: 1 } ] },
      { id: 'mailbox_mimic', name: 'Mailbox Mimic', element: 'dark', hpMul: 1.2, atkMul: 1.0,
        flavor: 'Eats letters. Prefers certified mail. The flag goes up when it\'s hungry.',
        actions: [
          { name: 'Certified Chomp', desc: 'Return to sender, violently.', type: 'bigDamage', amount: 1 },
          { name: 'Junk Mail', desc: 'Places a blocker of flyers.', type: 'block', count: 1 } ] },
      { id: 'sprinkler_spirit', name: 'Sprinkler Poltergeist', element: 'water', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Activates at 3 AM, or whenever you walk past. Mostly whenever you walk past.',
        actions: [
          { name: 'Sudden Sprinkle', desc: 'Converts 3 orbs to Water.', type: 'convert', count: 3, color: 'water' },
          { name: 'Pressure Blast', desc: 'FSSST. Right in the face.', type: 'damage', amount: 1 } ] },
      { id: 'gnome_patrol', name: 'Garden Gnome Patrol', element: 'fire', hpMul: 0.8, atkMul: 1.2,
        flavor: 'They see everything. They report everything. Their hats are regulation red.',
        actions: [
          { name: 'Citizen\'s Citation', desc: 'A tiny, furious fine.', type: 'damage', amount: 1 },
          { name: 'Gnome Swarm', desc: 'A big coordinated waddle.', type: 'bigDamage', amount: 1 } ] },
      { id: 'casserole_specter', name: 'Casserole Specter', element: 'light', hpMul: 1.0, atkMul: 0.9,
        flavor: 'Appears uninvited with a dish you must return. The dish binds your soul. And your schedule.',
        actions: [
          { name: 'Unsolicited Casserole', desc: 'Locks 3 orbs under tin foil.', type: 'lock', count: 3 },
          { name: 'Guilt Trip', desc: '"You never called about the dish."', type: 'damage', amount: 1 } ] },
      { id: 'boss_hoa_banshee', name: 'HOA Board Banshee', element: 'dark', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Her wail can be heard at every board meeting. Her wail IS the board meeting.',
        actions: [
          { name: 'Violation Wail', desc: 'A big shriek about your fence height.', type: 'bigDamage', amount: 1 },
          { name: 'Lien Scream', desc: 'Locks 4 orbs against your property.', type: 'lock', count: 4 },
          { name: 'Motion to Haunt', desc: 'Poisons 3 orbs. Seconded.', type: 'poison', count: 3 } ] },
      { id: 'boss_property_value', name: 'The Property Value', element: 'light', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'An abstract concept made manifest. It must go up. It MUST.',
        actions: [
          { name: 'Market Correction', desc: 'A massive adjustment.', type: 'bigDamage', amount: 1 },
          { name: 'Appraisal', desc: 'Converts 4 orbs to Light. Curb appeal rising.', type: 'convert', count: 4, color: 'light' },
          { name: 'Bubble', desc: 'Heals itself unsustainably.', type: 'heal', amount: 6000 } ] },
      { id: 'boss_chairman_polt', name: 'Chairman Poltergeist', element: 'dark', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'Died in 1743. Founded the HOA in 1744. Has never once approved a shed.',
        actions: [
          { name: 'Gavel of the Damned', desc: 'Massive damage. Motion carried.', type: 'bigDamage', amount: 1 },
          { name: 'Eternal Bylaws', desc: 'Locks 5 orbs, per section 12.', type: 'lock', count: 5 },
          { name: 'Quorum of Souls', desc: 'Binds a party member into the board.', type: 'bind', count: 1 },
          { name: 'Table the Motion', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 } ] },
    ],
    miniboss1: 'boss_hoa_banshee', miniboss2: 'boss_property_value', boss: 'boss_chairman_polt',
    stageNames: {
      1: 'Welcome to Wraithwood Estates', 5: 'The Dish You Must Return', 10: 'BOARD MEETING (OPEN SESSION)',
      12: 'Cul-de-Sac of Souls', 15: 'The Gnomes Are Watching', 18: 'THE VALUE MUST RISE',
      20: 'Covenant Court', 25: 'CHAIRMAN POLTERGEIST PRESIDING',
    },
    namePool: [
      'Regulation Lawn Heights', 'The 3 AM Sprinklers', 'Mailbox Flag: Up', 'Hedge Row Hauntings',
      'The Unreturned Dish', 'Driveway Purgatory', 'Phantom Garage Sale', 'The Beige Accord',
      'Fence Height Tribunal', 'The Immaculate Stripes', 'Trash Day of the Dead', 'Spectral Sidewalk Chalk',
      'The Second Casserole', 'Gnome Watch Nightly', 'Bylaw Boulevard', 'The Pale Picket Fence',
      'Homeowners\' Eternal Dues',
    ],
    dialogue: [
      { id: 'ch06_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Wraithwood Estates! Every resident died centuries ago, but the HOA dues are collected MONTHLY.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'What happens if a ghost doesn\'t pay?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'They get haunted. Yes, ghosts haunting ghosts. It\'s haunting all the way down, hero.' } ] },
      { id: 'ch06_mini1', lines: [
        { speaker: 'HOA Banshee', portrait: 'enemy_boss_hoa_banshee', side: 'right', text: 'YOOOOUR FEEEENCE... IS FOUR INCHES... TOO TAAAAALL.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We don\'t have a fence. We don\'t live here.' },
        { speaker: 'HOA Banshee', portrait: 'enemy_boss_hoa_banshee', side: 'right', text: 'NONRESIDENT VIOLATIONS... ARE DOUBLE.' } ] },
      { id: 'ch06_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The community clubhouse. Candles float. The minutes from 1744 are still being read.' },
        { speaker: 'Chairman Poltergeist', portrait: 'enemy_boss_chairman_polt', side: 'right', text: 'NEW BUSINESS: the living have entered the neighborhood. All in favor of destroying them?' },
        { speaker: '', portrait: '', side: 'left', text: 'A thousand spectral hands rise.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Point of order! POINT OF ORDER!! Oh no, he has the gavel—' } ] },
      { id: 'ch06_boss_after', lines: [
        { speaker: 'Chairman Poltergeist', portrait: 'enemy_boss_chairman_polt', side: 'right', text: 'Motion... to adjourn... FOREVER...' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The ghosts are FREE! They\'re painting their doors WHATEVER COLOR THEY WANT! That one\'s doing a mural! It\'s hideous! It\'s BEAUTIFUL!' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Somewhere out there, a shed is finally being built.' } ] },
    ],
  },
];
