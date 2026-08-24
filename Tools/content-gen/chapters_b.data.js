// Chapters 7-11: the world gets noticeably weirder.
module.exports = [
  {
    n: 7, title: 'The Great Dessert', theme: 'dessert',
    blurb: 'A vast expanse of dunes. The dunes are sugar. The cacti are churros. The heat is oven-quality.',
    drops: ['sugar_crystal', 'frosting_vial', 'sprinkle_pouch'],
    enemies: [
      { id: 'rogue_scone', name: 'Rogue Scone', element: 'fire', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Went stale. Went feral. Crumbles for no one.',
        actions: [
          { name: 'Crumb Burst', desc: 'Dry, devastating shrapnel.', type: 'damage', amount: 1 },
          { name: 'Stale Stare', desc: 'A big dense hit.', type: 'bigDamage', amount: 1 } ] },
      { id: 'custard_elemental', name: 'Custard Elemental', element: 'light', hpMul: 1.2, atkMul: 0.9,
        flavor: 'Wobbles with ancient power. Sets when threatened.',
        actions: [
          { name: 'Wobble Wave', desc: 'Converts 3 orbs to Light.', type: 'convert', count: 3, color: 'light' },
          { name: 'Scald', desc: 'It\'s hotter in the middle.', type: 'damage', amount: 1 } ] },
      { id: 'churro_cactus', name: 'Churro Cactus', element: 'nature', hpMul: 1.1, atkMul: 1.0,
        flavor: 'Do not hug. Delicious, but do not hug.',
        actions: [
          { name: 'Cinnamon Spines', desc: 'Poisons 2 orbs with sugar dust.', type: 'poison', count: 2 },
          { name: 'Snap Off', desc: 'A crunchy jab.', type: 'damage', amount: 1 } ] },
      { id: 'gelato_golem', name: 'Gelato Golem', element: 'water', hpMul: 1.3, atkMul: 0.9,
        flavor: 'Slowly melting. Extremely calm about it.',
        actions: [
          { name: 'Brain Freeze', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 },
          { name: 'Scoop Slam', desc: 'Two scoops of impact.', type: 'damage', amount: 1 } ] },
      { id: 'biscotti_bandit', name: 'Biscotti Bandit', element: 'dark', hpMul: 0.9, atkMul: 1.2,
        flavor: 'Twice-baked. Twice as bitter. Robs coffee carts.',
        actions: [
          { name: 'Dunk', desc: 'A big dip attack.', type: 'bigDamage', amount: 1 } ] },
      { id: 'meringue_wisp', name: 'Meringue Wisp', element: 'light', hpMul: 0.8, atkMul: 0.9,
        flavor: 'Light as air. Judgmental as a pastry chef.',
        actions: [
          { name: 'Stiff Peaks', desc: 'Places a fluffy blocker.', type: 'block', count: 1 },
          { name: 'Whip', desc: 'Folded into your face.', type: 'damage', amount: 1 } ] },
      { id: 'boss_tiramisu', name: 'Tiramisu the Unrisen', element: 'dark', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'A layered horror. Espresso-soaked. Pick-me-up? It will pick YOU up.',
        actions: [
          { name: 'Espresso Shot', desc: 'A jittery, devastating blast.', type: 'bigDamage', amount: 1 },
          { name: 'Cocoa Cloud', desc: 'Poisons 3 orbs with fine powder.', type: 'poison', count: 3 },
          { name: 'Layer Up', desc: 'Heals itself. Another layer forms.', type: 'heal', amount: 5000 } ] },
      { id: 'boss_flanbeast', name: 'The Flanbeast', element: 'light', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'It jiggles with malice. Caramel drips like the tears of its enemies.',
        actions: [
          { name: 'Caramel Flood', desc: 'Converts 4 orbs to Light.', type: 'convert', count: 4, color: 'light' },
          { name: 'Jiggle Quake', desc: 'A big resonant wobble.', type: 'bigDamage', amount: 1 },
          { name: 'Sticky Situation', desc: 'Locks 3 orbs in caramel.', type: 'lock', count: 3 } ] },
      { id: 'boss_grand_gateau', name: 'The Grand Gateau', element: 'fire', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'A wedding cake the size of a fortress. Nobody got married. It simply DECLARED itself.',
        actions: [
          { name: 'Tier Drop', desc: 'A floor of cake falls on you.', type: 'bigDamage', amount: 1 },
          { name: 'Fondant Prison', desc: 'Locks 5 orbs in smooth icing.', type: 'lock', count: 5 },
          { name: 'Candle Barrage', desc: 'Converts 3 orbs to Fire. Make a wish.', type: 'convert', count: 3, color: 'fire' },
          { name: 'Self-Frosting', desc: 'Heals itself. Another rosette appears.', type: 'heal', amount: 9000 } ] },
    ],
    miniboss1: 'boss_tiramisu', miniboss2: 'boss_flanbeast', boss: 'boss_grand_gateau',
    stageNames: {
      1: 'Dunes of Powdered Sugar', 5: 'The Churro Grove', 10: 'TIRAMISU RISES (FINALLY)',
      12: 'Caramel Quicksand', 15: 'Oasis of Lukewarm Milk', 18: 'THE FLANBEAST JIGGLES',
      20: 'Valley of Expired Dates', 25: 'THE GRAND GATEAU',
    },
    namePool: [
      'Sprinkle Storm Rising', 'The Crumbling Path', 'Biscotti Ambush Gulch', 'Meringue Mirage',
      'Frosting Flats', 'The Gelato Glacier', 'Sugar High Plateau', 'The Proofing Chamber',
      'Custard Canyon', 'Stale Winds', 'The Second Dessert', 'Ganache Gorge',
      'Rolling Pin Ridge', 'The Doughy Depths', 'Torte Territory', 'Cannoli Corridor',
      'The Bundt Badlands',
    ],
    dialogue: [
      { id: 'ch07_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The Great Dessert! A wizard misspelled "desert" on an official map in 1502 and the terrain COMPLIED.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'That\'s not how maps work.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'That\'s not how maps USED to work. He was a very senior wizard. Hydrate and watch for scones.' } ] },
      { id: 'ch07_mini1', lines: [
        { speaker: 'Tiramisu', portrait: 'enemy_boss_tiramisu', side: 'right', text: 'They called me "unrisen". Said I was "just a trifle". NOW WHO IS LAYERED BEYOND COMPREHENSION?' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Still you. That\'s still... that\'s what tiramisu is.' },
        { speaker: 'Tiramisu', portrait: 'enemy_boss_tiramisu', side: 'right', text: 'FLATTERY. DETECTED. DEPLOYING ESPRESSO.' } ] },
      { id: 'ch07_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'It blots out the sun. Seven tiers. Ten thousand candles. All of them lit.' },
        { speaker: 'The Grand Gateau', portrait: 'enemy_boss_grand_gateau', side: 'right', text: 'WHO APPROACHES THE CAKE?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Um. Party of six? We don\'t have a reservation. Or a wedding.' },
        { speaker: 'The Grand Gateau', portrait: 'enemy_boss_grand_gateau', side: 'right', text: 'THEN YOU SHALL BE THE FILLING.' } ] },
      { id: 'ch07_boss_after', lines: [
        { speaker: 'The Grand Gateau', portrait: 'enemy_boss_grand_gateau', side: 'right', text: 'I only wanted... someone... to celebrate...' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Oh no. Oh no, now I feel terrible. HAPPY BIRTHDAY, GATEAU. Everyone sing. SING, HERO.' },
        { speaker: '', portrait: '', side: 'left', text: 'The party sings. The cake weeps buttercream. Somewhere, a wizard fixes a typo, four hundred years too late.' } ] },
    ],
  },
  {
    n: 8, title: 'Cloud Nine Logistics', theme: 'cloudnine',
    blurb: 'The sky kingdom runs every delivery in the realm. Your package is out for delivery. It has been for nine years.',
    drops: ['cloud_label', 'storm_tape', 'lost_parcel'],
    enemies: [
      { id: 'parcel_pigeon', name: 'Parcel Pigeon', element: 'light', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Delivers with pinpoint inaccuracy. Your neighbor has your sword now.',
        actions: [
          { name: 'Misdelivery', desc: 'Converts 3 orbs to Light. Wrong address.', type: 'convert', count: 3, color: 'light' },
          { name: 'Peck of Peril', desc: 'Signature required.', type: 'damage', amount: 1 } ] },
      { id: 'storm_courier', name: 'Storm Courier', element: 'water', hpMul: 1.0, atkMul: 1.1,
        flavor: 'Neither rain nor MORE rain stays this courier. There is only rain.',
        actions: [
          { name: 'Express Bolt', desc: 'Overnight shipping, to your face.', type: 'bigDamage', amount: 1 },
          { name: 'Drizzle Route', desc: 'Converts 2 orbs to Water.', type: 'convert', count: 2, color: 'water' } ] },
      { id: 'bubble_wrap_slime', name: 'Bubble Wrap Slime', element: 'water', hpMul: 1.3, atkMul: 0.8,
        flavor: 'Pop it and it only gets angrier. And more popped.',
        actions: [
          { name: 'Pop Pop Pop', desc: 'Deeply satisfying damage.', type: 'damage', amount: 1 },
          { name: 'Cushioning', desc: 'Places a protective blocker.', type: 'block', count: 1 } ] },
      { id: 'lost_package', name: 'The Lost Package', element: 'dark', hpMul: 1.1, atkMul: 1.0,
        flavor: 'Nine years in transit. It has seen things. It has BECOME things.',
        actions: [
          { name: 'Return to Sender', desc: 'A haunted big hit.', type: 'bigDamage', amount: 1 },
          { name: 'Damaged in Transit', desc: 'Poisons 2 orbs.', type: 'poison', count: 2 } ] },
      { id: 'tape_gull', name: 'Packing Tape Gull', element: 'nature', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Steals tape. Nests in tape. IS mostly tape at this point.',
        actions: [
          { name: 'Tape Tangle', desc: 'Locks 3 orbs in industrial adhesive.', type: 'lock', count: 3 },
          { name: 'Squawk Strike', desc: 'MINE. MINE. MINE.', type: 'damage', amount: 1 } ] },
      { id: 'drone_cherub', name: 'Delivery Cherub (Beta)', element: 'fire', hpMul: 0.8, atkMul: 1.2,
        flavor: 'The future of delivery. Crashes into the present constantly.',
        actions: [
          { name: 'Crash Landing', desc: 'A big uncontrolled descent.', type: 'bigDamage', amount: 1 } ] },
      { id: 'boss_sorting_sphinx', name: 'The Sorting Sphinx', element: 'light', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Answer her riddle or your package goes to the wrong dimension. The riddle is a zip code.',
        actions: [
          { name: 'Riddle of Routing', desc: 'Locks 4 orbs pending correct postage.', type: 'lock', count: 4 },
          { name: 'Claw Cancellation', desc: 'A big stamping strike.', type: 'bigDamage', amount: 1 },
          { name: 'Address Unknown', desc: 'Converts 3 orbs to Light.', type: 'convert', count: 3, color: 'light' } ] },
      { id: 'boss_thunderhead', name: 'Thunderhead, Route Manager', element: 'water', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'A storm cloud with a delivery quota. Rains on schedule. The schedule is "always".',
        actions: [
          { name: 'Route Storm', desc: 'A big electrified downpour.', type: 'bigDamage', amount: 1 },
          { name: 'Weather Delay', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 },
          { name: 'Flood the Zone', desc: 'Converts 4 orbs to Water.', type: 'convert', count: 4, color: 'water' } ] },
      { id: 'boss_final_notice', name: 'FINAL NOTICE, The Undeliverable', element: 'dark', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'Every failed delivery slip ever written, fused into one entity. It knows you weren\'t home. You were home.',
        actions: [
          { name: 'We Missed You', desc: 'Massive damage. You were home. IT KNOWS.', type: 'bigDamage', amount: 1 },
          { name: 'Pickup Window', desc: 'Reduces move time by 3 seconds. 2-4pm only.', type: 'timerDown', amount: 3 },
          { name: 'Slip Storm', desc: 'Locks 5 orbs under paper slips.', type: 'lock', count: 5 },
          { name: 'Redelivery Loop', desc: 'Heals itself. Attempt 3 of ∞.', type: 'heal', amount: 9000 } ] },
    ],
    miniboss1: 'boss_sorting_sphinx', miniboss2: 'boss_thunderhead', boss: 'boss_final_notice',
    stageNames: {
      1: 'Departures (Delayed)', 5: 'The Pigeon Loft Hub', 10: 'RIDDLES AT THE SORTING FACILITY',
      12: 'Conveyor Belt Skyway', 15: 'The Nine-Year Package', 18: 'THUNDERHEAD\'S ROUTE',
      20: 'Terminal Velocity Terminal', 25: 'FINAL NOTICE',
    },
    namePool: [
      'Baggage Claim Nimbus', 'The Fragile Stack', 'Tape Gull Territory', 'Priority Lane Peril',
      'The Mislabeled Mile', 'Cumulus Customs', 'Drone Testing Grounds', 'The Second Sorting',
      'Handle With Fury', 'Lost and Found and Lost', 'The Overnight Layover', 'Postage Due Pass',
      'Cirrus Circulation', 'The Damaged Goods Gate', 'Signature Required Ridge', 'Air Mail Alley',
      'The Backlogged Skies',
    ],
    dialogue: [
      { id: 'ch08_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Cloud Nine Logistics! Every delivery in the realm routes through this sky-hub. Including, nine years ago, my birthday present.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'Is that why we\'re here?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'We\'re here to save the kingdom\'s supply lines. But if we HAPPEN to find a small box addressed to Pip... we take it.' } ] },
      { id: 'ch08_mini1', lines: [
        { speaker: 'Sorting Sphinx', portrait: 'enemy_boss_sorting_sphinx', side: 'right', text: 'ANSWER MY RIDDLE OR BE MISROUTED: What has four digits, defines your fate, and nobody remembers theirs?' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'A zip code.' },
        { speaker: 'Sorting Sphinx', portrait: 'enemy_boss_sorting_sphinx', side: 'right', text: 'CORRECT. UNFORTUNATELY I AM STILL CONTRACTUALLY REQUIRED TO FIGHT YOU.' } ] },
      { id: 'ch08_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The Dead Letter Office. Paper slips swirl like snow. Something vast breathes beneath them.' },
        { speaker: 'FINAL NOTICE', portrait: 'enemy_boss_final_notice', side: 'right', text: 'WE ATTEMPTED DELIVERY. YOU. WERE. HOME.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'I heard the knock! I ran to the door in eight seconds!' },
        { speaker: 'FINAL NOTICE', portrait: 'enemy_boss_final_notice', side: 'right', text: 'EIGHT SECONDS IS AN ETERNITY. PREPARE FOR REDELIVERY.' } ] },
      { id: 'ch08_boss_after', lines: [
        { speaker: 'FINAL NOTICE', portrait: 'enemy_boss_final_notice', side: 'right', text: 'Package... released... all packages... released...' },
        { speaker: '', portrait: '', side: 'left', text: 'Nine years of parcels rain gently from the sky. Somewhere, everyone\'s missing left glove comes home.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'MY BIRTHDAY PRESENT! It\'s... a smaller box. Addressed to me. From me. "Do not open until the finale." PIP, YOU ABSOLUTE MYSTERY.' } ] },
    ],
  },
  {
    n: 9, title: 'The Volcano Spa & Resort', theme: 'volcanospa',
    blurb: 'Five stars. Zero survivors of the deep-tissue massage. The lava pool is "invigorating".',
    drops: ['spa_voucher', 'pumice_stone', 'cucumber_slice'],
    enemies: [
      { id: 'masseuse_magmite', name: 'Deep-Tissue Magmite', element: 'fire', hpMul: 1.0, atkMul: 1.1,
        flavor: 'Finds knots you didn\'t know you had. Creates several new ones.',
        actions: [
          { name: 'Deep Tissue', desc: 'A BIG therapeutic slam.', type: 'bigDamage', amount: 1 },
          { name: 'Hot Stone', desc: 'The stones are lava. They were always lava.', type: 'damage', amount: 1 } ] },
      { id: 'towel_wraith', name: 'Towel Wraith', element: 'light', hpMul: 0.9, atkMul: 0.9,
        flavor: 'Folded into a swan. The swan remembers being a towel. It is furious.',
        actions: [
          { name: 'Swan Snap', desc: 'A whip-crack of terrycloth.', type: 'damage', amount: 1 },
          { name: 'Fold', desc: 'Locks 3 orbs into decorative shapes.', type: 'lock', count: 3 } ] },
      { id: 'sauna_imp', name: 'Sauna Imp', element: 'fire', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Keeps adding coals. "It\'s good for you," it hisses. "Sweat out the cowardice."',
        actions: [
          { name: 'More Coals', desc: 'Converts 3 orbs to Fire.', type: 'convert', count: 3, color: 'fire' },
          { name: 'Steam Slap', desc: 'Open-palm humidity.', type: 'damage', amount: 1 } ] },
      { id: 'cucumber_horror', name: 'The Cucumber Horror', element: 'nature', hpMul: 1.2, atkMul: 0.9,
        flavor: 'Two slices covered its eyes. Now it has neither eyes nor mercy.',
        actions: [
          { name: 'Blind Fury', desc: 'Poisons 2 orbs with spa water.', type: 'poison', count: 2 },
          { name: 'Crisp Strike', desc: 'Refreshingly painful.', type: 'damage', amount: 1 } ] },
      { id: 'mud_mask_golem', name: 'Mud Mask Golem', element: 'nature', hpMul: 1.4, atkMul: 0.8,
        flavor: 'Exfoliates enemies. Violently. Your pores will thank it, eventually.',
        actions: [
          { name: 'Exfoliate', desc: 'Removes several layers. Of you.', type: 'damage', amount: 1 },
          { name: 'Clay Wall', desc: 'Places a mineral-rich blocker.', type: 'block', count: 1 } ] },
      { id: 'zen_salamander', name: 'Zen Salamander', element: 'fire', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Achieved inner peace. Guards it jealously with outer violence.',
        actions: [
          { name: 'Mindful Strike', desc: 'Fully present damage.', type: 'damage', amount: 1 },
          { name: 'Breathing Exercise', desc: 'Heals itself. In through the nose...', type: 'heal', amount: 3000 } ] },
      { id: 'boss_concierge', name: 'The Obsidian Concierge', element: 'dark', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Anything you need, any hour. What you need, it has decided, is to never leave.',
        actions: [
          { name: 'Turndown Service', desc: 'A big tucking-in you cannot escape.', type: 'bigDamage', amount: 1 },
          { name: 'Do Not Disturb', desc: 'Locks 4 orbs behind the door hanger.', type: 'lock', count: 4 },
          { name: 'Minibar Charges', desc: 'Poisons 3 orbs. You WILL pay for that water.', type: 'poison', count: 3 } ] },
      { id: 'boss_yoga_dragon', name: 'Bikram the Yoga Dragon', element: 'fire', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'The hot yoga instructor. The room is hot because he is IN it.',
        actions: [
          { name: 'Downward Dragon', desc: 'A big, extremely aligned slam.', type: 'bigDamage', amount: 1 },
          { name: 'Hold the Pose', desc: 'Binds a party member mid-stretch.', type: 'bind', count: 1 },
          { name: 'Breath of Fire', desc: 'Converts 4 orbs to Fire. Literally.', type: 'convert', count: 4, color: 'fire' } ] },
      { id: 'boss_manager_caldera', name: 'General Manager Caldera', element: 'fire', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'The volcano itself, in a name-tagged blazer. Checkout time is never.',
        actions: [
          { name: 'Eruption Package', desc: 'The premium experience. Massive damage.', type: 'bigDamage', amount: 1 },
          { name: 'All-Inclusive', desc: 'Locks 5 orbs. Everything is included. Nothing may leave.', type: 'lock', count: 5 },
          { name: 'Complimentary Upgrade', desc: 'Heals himself. Suite deal.', type: 'heal', amount: 10000 },
          { name: 'Lava Robes', desc: 'Converts 3 orbs to Fire. So warm. TOO warm.', type: 'convert', count: 3, color: 'fire' } ] },
    ],
    miniboss1: 'boss_concierge', miniboss2: 'boss_yoga_dragon', boss: 'boss_manager_caldera',
    stageNames: {
      1: 'Check-In (Mandatory)', 5: 'The Towel Swan Gallery', 10: 'CONCIERGE ATTENDS TO YOU',
      12: 'The Lava Lap Pool', 15: 'Couples Massage (Violent)', 18: 'HOT YOGA WITH BIKRAM',
      20: 'The Infinity Edge (Of Doom)', 25: 'CHECKOUT WITH MANAGER CALDERA',
    },
    namePool: [
      'Lobby of Lingering Guests', 'The Steam Room Gauntlet', 'Pumice Path', 'The Robe Warmers',
      'Cabana Row Chaos', 'The Tepid Plunge', 'Aromatherapy Ambush', 'The Second Sauna',
      'Wellness Wing West', 'The Cucumber Water Station', 'Magma Manicures', 'The Loyalty Program',
      'Poolside Reckoning', 'The Meditation Pit', 'Spa Day Eternal', 'The Volcanic Facial',
      'Minibar of the Ancients',
    ],
    dialogue: [
      { id: 'ch09_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The Volcano Spa & Resort! Five stars! The reviews say "transformative", "life-changing", and "please send help, this is not a review".' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'Why does the kingdom need us HERE?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Nobody who checks in ever checks out. The spa says that\'s "guest retention". The families say it\'s "kidnapping". Semantics! Onward!' } ] },
      { id: 'ch09_mini1', lines: [
        { speaker: 'Concierge', portrait: 'enemy_boss_concierge', side: 'right', text: 'Welcome. Your suite is ready. Your FOREVER suite.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We\'re just passing through.' },
        { speaker: 'Concierge', portrait: 'enemy_boss_concierge', side: 'right', text: 'Nobody... passes through. There is a package for that, and you cannot afford it.' } ] },
      { id: 'ch09_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The penthouse. The floor is warm. The floor is TOO warm. The floor is the boss.' },
        { speaker: 'Caldera', portrait: 'enemy_boss_manager_caldera', side: 'right', text: 'Checking out early incurs a fee. The fee is combat. We take violence and all major currencies.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'What if we leave a good review?' },
        { speaker: 'Caldera', portrait: 'enemy_boss_manager_caldera', side: 'right', text: 'Five stars is the MINIMUM. It is also insufficient. ERUPTION PACKAGE, ACTIVATED.' } ] },
      { id: 'ch09_boss_after', lines: [
        { speaker: 'Caldera', portrait: 'enemy_boss_manager_caldera', side: 'right', text: 'Very well... checkout... approved... minibar charges... waived...' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The guests are free! Look at them stampede toward the exit in their robes! MAJESTIC.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'I kept the slippers.' } ] },
    ],
  },
  {
    n: 10, title: 'The Necropolis DMV', theme: 'necrodmv',
    blurb: 'Where the dead go to renew their licenses. The line has not moved since the Third Age.',
    drops: ['bone_ticket', 'expired_license', 'soul_stamp'],
    enemies: [
      { id: 'queue_skeleton', name: 'Queue Skeleton', element: 'dark', hpMul: 1.0, atkMul: 1.0,
        flavor: 'Died in line. Stayed in line. Will defend its place in line.',
        actions: [
          { name: 'Hold My Spot', desc: 'A bony defense of position.', type: 'damage', amount: 1 },
          { name: 'Rattle Forward', desc: 'The line moves one inch. A big hit celebrates.', type: 'bigDamage', amount: 1 } ] },
      { id: 'ticket_ghoul', name: 'Take-a-Number Ghoul', element: 'dark', hpMul: 0.9, atkMul: 1.0,
        flavor: 'Dispenses tickets. Current number: 4. Now serving: ∞.',
        actions: [
          { name: 'Number Dispense', desc: 'Locks 3 orbs behind ticket 8,412.', type: 'lock', count: 3 },
          { name: 'Paper Swipe', desc: 'A shuffling strike.', type: 'damage', amount: 1 } ] },
      { id: 'photo_wisp', name: 'License Photo Wisp', element: 'light', hpMul: 0.8, atkMul: 1.1,
        flavor: 'Takes your photo at the worst possible moment. Every moment is the worst possible moment.',
        actions: [
          { name: 'Flash', desc: 'A blinding candid.', type: 'bigDamage', amount: 1 },
          { name: 'Retake Denied', desc: 'Converts 2 orbs to Light.', type: 'convert', count: 2, color: 'light' } ] },
      { id: 'form_mummy', name: 'Form-Wrapped Mummy', element: 'nature', hpMul: 1.3, atkMul: 0.9,
        flavor: 'Wrapped not in bandages, but in forms. Every form filled out incorrectly.',
        actions: [
          { name: 'Red Tape Wrap', desc: 'Locks 3 orbs in triplicate.', type: 'lock', count: 3 },
          { name: 'Papyrus Cut', desc: 'The most ancient of paper cuts.', type: 'damage', amount: 1 } ] },
      { id: 'vision_test_beholder', name: 'Vision Test Beholder', element: 'water', hpMul: 1.1, atkMul: 1.0,
        flavor: 'Reads the bottom row of every chart. You cannot. It judges you.',
        actions: [
          { name: 'Eye Exam', desc: 'Poisons 2 orbs. Read the bottom row. READ IT.', type: 'poison', count: 2 },
          { name: 'Glare', desc: 'Eleven eyes of disappointment.', type: 'damage', amount: 1 } ] },
      { id: 'counter_wight', name: 'Window 4 Wight', element: 'dark', hpMul: 1.0, atkMul: 1.1,
        flavor: 'Closes its window the moment you reach it. Has done so for six centuries.',
        actions: [
          { name: 'Window Closed', desc: 'Places a NEXT WINDOW blocker.', type: 'block', count: 1 },
          { name: 'Stamp of Doom', desc: 'DENIED, with force.', type: 'damage', amount: 1 } ] },
      { id: 'boss_registrar', name: 'The Registrar of Souls', element: 'dark', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Knows your legal name. All of them. Even the embarrassing middle one.',
        actions: [
          { name: 'Name Check', desc: 'A big, humiliating roll call.', type: 'bigDamage', amount: 1 },
          { name: 'Records Hold', desc: 'Locks 4 orbs pending verification.', type: 'lock', count: 4 },
          { name: 'Archive Dust', desc: 'Poisons 3 orbs.', type: 'poison', count: 3 } ] },
      { id: 'boss_road_test', name: 'The Road Test Revenant', element: 'fire', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'Failed its own road test in life. Now it IS the road test.',
        actions: [
          { name: 'Parallel Park', desc: 'A crushing maneuver between two objects. You are the objects.', type: 'bigDamage', amount: 1 },
          { name: 'Check Your Mirrors', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 },
          { name: 'Sudden Stop', desc: 'Binds a party member at a phantom stop sign.', type: 'bind', count: 1 } ] },
      { id: 'boss_lich_clerk', name: 'The Lich Clerk', element: 'dark', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'His phylactery is a laminated ID badge. He cannot be destroyed, only transferred to another branch.',
        actions: [
          { name: 'NEXT.', desc: 'Massive damage. It is finally your turn. You are not ready.', type: 'bigDamage', amount: 1 },
          { name: 'System Down', desc: 'Locks 5 orbs. The system is down. The system is ALWAYS down.', type: 'lock', count: 5 },
          { name: 'Lunch Break', desc: 'Heals himself. Back in 30 minutes (eternal).', type: 'heal', amount: 10000 },
          { name: 'Expired Stamp', desc: 'Converts 3 orbs to Dark.', type: 'convert', count: 3, color: 'dark' },
          { name: 'Come Back Tomorrow', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 } ] },
    ],
    miniboss1: 'boss_registrar', miniboss2: 'boss_road_test', boss: 'boss_lich_clerk',
    stageNames: {
      1: 'Take a Number (4,000,000)', 5: 'The Eternal Queue, Section B', 10: 'RECORDS: THE REGISTRAR',
      12: 'Photo Booth of the Damned', 15: 'The Line Moves (A Miracle)', 18: 'ROAD TEST: THE REVENANT',
      20: 'Window 4 Is Closing', 25: 'THE LICH CLERK WILL SEE YOU',
    },
    namePool: [
      'Velvet Rope Purgatory', 'The Clipboard Catacombs', 'Form 66-Undead', 'Waiting Room of Woe',
      'The Second Queue', 'Renewal Wing Ruins', 'Proof of Unlife Required', 'The Stapler Crypt',
      'Ossuary of Old Applications', 'Vision Test Vault', 'The Pen Chained to Nothing', 'Hall of Expired Souls',
      'Learner\'s Permit Limbo', 'The Numbered Dead', 'Bureau of Bones', 'Registry Rigor Mortis',
      'The Final Window',
    ],
    dialogue: [
      { id: 'ch10_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The Necropolis DMV. Halfway point of our journey, hero. The dead need licenses to haunt, and the line predates several gods.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'What do WE need?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'A Class-H Heroism License. Ours expired in chapter three. We\'ve been adventuring ILLEGALLY, hero. I haven\'t slept.' } ] },
      { id: 'ch10_mini1', lines: [
        { speaker: 'Registrar', portrait: 'enemy_boss_registrar', side: 'right', text: 'NAME?' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'You have it on file. You just said you have everything on file.' },
        { speaker: 'Registrar', portrait: 'enemy_boss_registrar', side: 'right', text: 'THE FILE MUST BE CONFIRMED. VERBALLY. WITH YOUR EMBARRASSING MIDDLE NAME.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: '...draw your weapon.' } ] },
      { id: 'ch10_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'Window 1. The number board flickers: NOW SERVING 4,000,001. It is your number. It has been 6,000 years.' },
        { speaker: 'The Lich Clerk', portrait: 'enemy_boss_lich_clerk', side: 'right', text: 'NEXT.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'That\'s us! THAT\'S US! Hero, I\'ve never been so happy and so terrified!' },
        { speaker: 'The Lich Clerk', portrait: 'enemy_boss_lich_clerk', side: 'right', text: 'YOUR FORMS ARE... adequate. UNFORTUNATELY, THE SYSTEM IS DOWN. COMBAT IS THE BACKUP SYSTEM.' } ] },
      { id: 'ch10_boss_after', lines: [
        { speaker: 'The Lich Clerk', portrait: 'enemy_boss_lich_clerk', side: 'right', text: 'Processed... approved... laminated...' },
        { speaker: '', portrait: '', side: 'left', text: 'A fresh Class-H Heroism License materializes. The photo is terrible. It is perfect.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'WE\'RE LEGAL! Hero, we\'re LEGAL! ...why is the license already expiring in chapter twenty?' } ] },
    ],
  },
  {
    n: 11, title: 'The Enchanted Mall', theme: 'mall',
    blurb: 'A thousand-year-old shopping labyrinth. The food court is cursed. The pretzels are ALSO cursed, but worth it.',
    drops: ['gift_card', 'pretzel_salt', 'kiosk_key'],
    enemies: [
      { id: 'kiosk_goblin', name: 'Kiosk Goblin', element: 'fire', hpMul: 0.9, atkMul: 1.1,
        flavor: 'HEY. HEY. Can it ask you a question? It\'s about your phone case. HEY.',
        actions: [
          { name: 'Aggressive Sample', desc: 'You WILL try the lotion.', type: 'damage', amount: 1 },
          { name: 'Sales Pitch', desc: 'A big, unstoppable offer.', type: 'bigDamage', amount: 1 } ] },
      { id: 'escalator_serpent', name: 'Escalator Serpent', element: 'dark', hpMul: 1.2, atkMul: 1.0,
        flavor: 'An endless metal serpent. Always going the wrong direction.',
        actions: [
          { name: 'Wrong Way', desc: 'Reduces move time by 2 seconds.', type: 'timerDown', amount: 2 },
          { name: 'Step Chomp', desc: 'Mind the gap. The gap has teeth.', type: 'damage', amount: 1 } ] },
      { id: 'perfume_banshee', name: 'Perfume Banshee', element: 'light', hpMul: 0.9, atkMul: 1.0,
        flavor: 'One spritz can be smelled across three timelines.',
        actions: [
          { name: 'Spritz Assault', desc: 'Poisons 3 orbs with Eau de Doom.', type: 'poison', count: 3 },
          { name: 'Sample Scream', desc: 'A fragrant shriek.', type: 'damage', amount: 1 } ] },
      { id: 'pretzel_elemental', name: 'Cursed Pretzel Elemental', element: 'nature', hpMul: 1.1, atkMul: 1.0,
        flavor: 'Twisted beyond redemption. Salted beyond reason. Delicious beyond dispute.',
        actions: [
          { name: 'Salt Storm', desc: 'Converts 3 orbs to Nature.', type: 'convert', count: 3, color: 'nature' },
          { name: 'Knot Slam', desc: 'A doughy haymaker.', type: 'damage', amount: 1 } ] },
      { id: 'mannequin_stalker', name: 'Mannequin (Do Not Turn Around)', element: 'dark', hpMul: 1.0, atkMul: 1.2,
        flavor: 'It was in the window. Now it is not in the window.',
        actions: [
          { name: 'It Moved', desc: 'A big hit from exactly behind you.', type: 'bigDamage', amount: 1 } ] },
      { id: 'fountain_nymph', name: 'Wishing Fountain Nymph', element: 'water', hpMul: 1.0, atkMul: 0.9,
        flavor: 'Every coin is a wish she must process. She is nine million wishes behind.',
        actions: [
          { name: 'Coin Flick', desc: 'Exact change, high velocity.', type: 'damage', amount: 1 },
          { name: 'Wish Backlog', desc: 'Locks 3 orbs pending wish review.', type: 'lock', count: 3 } ] },
      { id: 'boss_food_court', name: 'The Food Court Sovereign', element: 'fire', boss: true, hpMul: 10, atkMul: 1.4, countdown: 3,
        flavor: 'Rules seventeen competing cuisines from a throne of trays. The samples are tribute.',
        actions: [
          { name: 'Tray Avalanche', desc: 'A big cascade of lightly-used trays.', type: 'bigDamage', amount: 1 },
          { name: 'Grease Fire', desc: 'Converts 3 orbs to Fire.', type: 'convert', count: 3, color: 'fire' },
          { name: 'Free Sample', desc: 'Poisons 3 orbs. It was NOT free.', type: 'poison', count: 3 } ] },
      { id: 'boss_lost_child', name: 'The Eternally Lost Child', element: 'light', boss: true, hpMul: 11, atkMul: 1.4, countdown: 2,
        flavor: 'Announced over the intercom in 1387. Never claimed. Now it claims OTHERS.',
        actions: [
          { name: 'Intercom Wail', desc: 'A big echoing announcement.', type: 'bigDamage', amount: 1 },
          { name: 'Hold Hands', desc: 'Binds a party member. For safety.', type: 'bind', count: 1 },
          { name: 'Balloon Release', desc: 'Converts 4 orbs to Light.', type: 'convert', count: 4, color: 'light' } ] },
      { id: 'boss_mall_heart', name: 'The Anchor Store', element: 'dark', boss: true, hpMul: 26, atkMul: 1.6, countdown: 3,
        flavor: 'The ancient store at the mall\'s heart. Nobody remembers what it sells. It sells NOTHING. It BUYS.',
        actions: [
          { name: 'Everything Must Go', desc: 'Massive damage. Including you.', type: 'bigDamage', amount: 1 },
          { name: 'Store Credit Only', desc: 'Locks 5 orbs. No refunds.', type: 'lock', count: 5 },
          { name: 'Doorbuster', desc: 'Reduces move time by 3 seconds.', type: 'timerDown', amount: 3 },
          { name: 'Clearance Rack', desc: 'Heals itself with unsold inventory.', type: 'heal', amount: 9000 } ] },
    ],
    miniboss1: 'boss_food_court', miniboss2: 'boss_lost_child', boss: 'boss_mall_heart',
    stageNames: {
      1: 'Mall Entrance (North of Nothing)', 5: 'Kiosk Alley', 10: 'THE FOOD COURT SOVEREIGN',
      12: 'The Fountain of Backlogged Wishes', 15: 'You Are Here (You Are Not)', 18: 'PAGING: THE LOST CHILD',
      20: 'The Dead Wing', 25: 'THE ANCHOR STORE OPENS',
    },
    namePool: [
      'Directory of Lies', 'The Scented Gauntlet', 'Escalator to Elsewhere', 'The Pretzel Shrine',
      'Mannequin Row', 'The Second Food Court', 'Parking Structure Z', 'The Gift Wrap Dungeon',
      'Window Shopping Horrors', 'The Sample Gauntlet', 'Atrium of Echoes', 'Seasonal Aisle (Wrong Season)',
      'The Loitering Grounds', 'Fitting Room Labyrinth', 'The Muzak Chamber', 'Customer Service (Abandoned)',
      'The Anchor\'s Shadow',
    ],
    dialogue: [
      { id: 'ch11_intro', lines: [
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The Enchanted Mall! Built by wizards, abandoned by wizards, haunted by retail.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'right', text: 'What\'s our objective?' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The Anchor Store at the center has been... acquiring people. Also I need socks. Two birds, hero.' } ] },
      { id: 'ch11_mini1', lines: [
        { speaker: 'Food Court Sovereign', portrait: 'enemy_boss_food_court', side: 'right', text: 'YOU APPROACH THE COURT. ALL WHO EAT HERE SERVE HERE. THOSE ARE THE TERMS OF THE SAMPLE.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Hero... I ate the sample. I ate FOUR samples.' },
        { speaker: 'Food Court Sovereign', portrait: 'enemy_boss_food_court', side: 'right', text: 'FOUR?! THE DEBT IS ENORMOUS. TO ARMS.' } ] },
      { id: 'ch11_boss_before', lines: [
        { speaker: '', portrait: '', side: 'left', text: 'The mall\'s heart. A storefront with no sign, no windows, and doors that open like a yawn.' },
        { speaker: 'The Anchor Store', portrait: 'enemy_boss_mall_heart', side: 'right', text: 'WELCOME. EVERYTHING MUST GO. YOU... ARE EVERYTHING.' },
        { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We\'re just here for the missing people. And socks.' },
        { speaker: 'The Anchor Store', portrait: 'enemy_boss_mall_heart', side: 'right', text: 'THE MISSING PEOPLE ARE IN AISLE NEVER. THE SOCKS ARE EXCELLENT. PREPARE FOR ACQUISITION.' } ] },
      { id: 'ch11_boss_after', lines: [
        { speaker: 'The Anchor Store', portrait: 'enemy_boss_mall_heart', side: 'right', text: 'GOING... OUT OF... BUSINESS...' },
        { speaker: '', portrait: '', side: 'left', text: 'The doors exhale four hundred missing shoppers, sixty mall-walkers mid-lap, and one wizard who just wanted a smoothie.' },
        { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Everyone\'s free! And hero... the socks. They ARE excellent.' } ] },
    ],
  },
];
