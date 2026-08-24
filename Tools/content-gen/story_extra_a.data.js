// Second-pass story scenes, chapters 2-10: mid-chapter beats (stage 15),
// second-miniboss encounters (stage 18) and pre-boss setup (stage 24).
// Recurring cast: Pip (enthusiastic compliance fairy), the Hero (deadpan),
// Sir Prestige (sponsored rival), Fenris (union organizer), Gerald
// sightings, and the slowly-revealed Department of Doom.
module.exports = {
  2: [
    { id: 'ch02_mid', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'A fanfare sounds. A knight in gleaming, logo-covered armor descends on a rope held by interns.' },
      { speaker: 'Sir Prestige', portrait: 'npc_prestige', side: 'right', text: 'Fear not, citizens! Sir Prestige has arrived — proudly partnered with PermitPal™, the only permit app with a five-crown rating!' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We already cleared this section.' },
      { speaker: 'Sir Prestige', portrait: 'npc_prestige', side: 'right', text: 'And I already took credit! Teamwork! #blessed #DarkwoodCleared' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'I despise him and I have downloaded the app.' } ] },
    { id: 'ch02_mini2', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'A machine hums in the clearing. Warm. Patient. Glossy.' },
      { speaker: 'The Laminator', portrait: 'enemy_boss_laminator', side: 'right', text: 'YOUR DOCUMENTS ARE FRAGILE. YOUR BONES ARE FRAGILE. I CAN FIX BOTH.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We just need to get past you.' },
      { speaker: 'The Laminator', portrait: 'enemy_boss_laminator', side: 'right', text: 'EVERYTHING PASSES THROUGH ME. ONCE. FLATLY.' } ] },
    { id: 'ch02_preboss', lines: [
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Hero, look at this rejected permit. The watermark... that\'s not the forest\'s seal. "DEPARTMENT OF DOOM — FIELD OFFICE 7."' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'The clipboard goblins had the same stamp.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Someone is franchising bureaucracy, hero. And Barkimedes is just the branch manager. Come on — the Director\'s office is past this grove.' } ] },
  ],
  3: [
    { id: 'ch03_mid', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'On a crate by the picket line, a familiar wolf addresses a crowd of dwarves.' },
      { speaker: 'Fenris', portrait: 'enemy_boss_wolf_rep', side: 'right', text: 'Brothers and sisters of the mountain! The wolves of the meadow won DENTAL. You dig through ROCK. Imagine your dental!' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Fenris? You\'re organizing here now?' },
      { speaker: 'Fenris', portrait: 'enemy_boss_wolf_rep', side: 'right', text: 'The movement travels, friend. Also you still owe me a signature on the meadow accord. Article 12 never sleeps.' } ] },
    { id: 'ch03_mini2', lines: [
      { speaker: 'Bertha', portrait: 'enemy_boss_bertha', side: 'right', text: 'MANAGEMENT DIRECTIVE 4: DISPERSE THE ORGANIZERS. MANAGEMENT DIRECTIVE 5: DISPERSE THE ADVENTURERS.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'We\'re not organizers! We\'re barely organized!' },
      { speaker: 'Bertha', portrait: 'enemy_boss_bertha', side: 'right', text: 'DIRECTIVE 6: DISPERSE EVERYONE. IT IS SIMPLER.' } ] },
    { id: 'ch03_preboss', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'Outside the summit office, a dejected knight sits amid rejected sponsorship contracts.' },
      { speaker: 'Sir Prestige', portrait: 'npc_prestige', side: 'right', text: 'Goldtooth declined my endorsement package. Said he "already owns better heroes". Can you BELIEVE—' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Move, Prestige.' },
      { speaker: 'Sir Prestige', portrait: 'npc_prestige', side: 'right', text: 'Destroy him a little for me. #justice' } ] },
  ],
  4: [
    { id: 'ch04_mid', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'At desk 4,401, a green slime wearing a tiny tie answers three phones at once.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Hero. HERO. The tie. The TIE.' },
      { speaker: 'Tie Slime', portrait: 'enemy_slime_green', side: 'right', text: 'Thank you for holding! This is Gerald\'s cousin, Blerald. Gerald says hi. Gerald says you cost him a promotion. Gerald is FINE about it.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'That sounds like Gerald is not fine about it.' } ] },
    { id: 'ch04_mini2', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'The water darkens. Something ancient rises, holding a receipt from before receipts existed.' },
      { speaker: 'The Abyssal Complainant', portrait: 'enemy_boss_deep_karen', side: 'right', text: 'I HAVE WAITED NINE EONS TO SPEAK TO A MANAGER. YOU... ARE SHAPED LIKE A MANAGER.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'I\'m really not.' },
      { speaker: 'The Abyssal Complainant', portrait: 'enemy_boss_deep_karen', side: 'right', text: 'ALL WHO DENY BEING MANAGERS ARE MANAGERS. IT IS KNOWN.' } ] },
    { id: 'ch04_preboss', lines: [
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The hold music is getting louder. That means we\'re close to the front of the queue. Or the queue is close to the front of US.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'How do you fight a queue?' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Form E-A-R, hero: Engage, Advance, RESOLVE. I just made it up. It\'s already policy. That\'s how confident I am.' } ] },
  ],
  5: [
    { id: 'ch05_mid', lines: [
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Mandatory Fun Cavern. The pizza party is in forty minutes. Attendance is being TRACKED, hero.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We could just... attend? Eat pizza? Rest?' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The pizza is a crystal. The party is a performance review. The fun is MANDATORY. Weapons out.' } ] },
    { id: 'ch05_mini2', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'The wall of the cavern is a diagram. The diagram is looking at you.' },
      { speaker: 'The Org Chart', portrait: 'enemy_boss_org_chart', side: 'right', text: 'WHERE... DO YOU... REPORT?' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Nowhere. We\'re independent.' },
      { speaker: 'The Org Chart', portrait: 'enemy_boss_org_chart', side: 'right', text: 'UNACCEPTABLE. EVERYONE IS A BOX. YOU WILL BE A BOX. THE DEPARTMENT OF DOOM REQUIRES IT.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'There\'s that name again, hero. They\'re EVERYWHERE.' } ] },
    { id: 'ch05_preboss', lines: [
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'I can\'t believe Ranger Doreen was moonlighting as HR security. I can\'t believe she whistled at us AGAIN.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'She said the benefits here are excellent.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'They ARE. That\'s the horrifying part. Now — the corner office. Match the vibes. BEAT the vibes.' } ] },
  ],
  6: [
    { id: 'ch06_mid', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'In the clubhouse basement, spectral workers gather around a very alive wolf.' },
      { speaker: 'Fenris', portrait: 'enemy_boss_wolf_rep', side: 'right', text: 'Ghost Local 1744 is hereby chartered! Dues: one rattle per month. First demand: haunting is WORK, and work gets WAGES.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'He unionized the DEAD. The man cannot be stopped.' },
      { speaker: 'Fenris', portrait: 'enemy_boss_wolf_rep', side: 'right', text: 'Wolf, actually. And no. I cannot.' } ] },
    { id: 'ch06_mini2', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'The air shimmers with an abstract dread. Numbers float in it. They are going up.' },
      { speaker: 'The Property Value', portrait: 'enemy_boss_property_value', side: 'right', text: 'I MUST RISE. IF YOU REMAIN, I CANNOT RISE. THE MATH IS SIMPLE. THE MATH IS CRUEL.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We\'re not even residents.' },
      { speaker: 'The Property Value', portrait: 'enemy_boss_property_value', side: 'right', text: 'YOU ARE FOOT TRAFFIC. FOOT TRAFFIC IS A MIXED INDICATOR.' } ] },
    { id: 'ch06_preboss', lines: [
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Hero... the casserole dish. It\'s on our doorstep. We don\'t HAVE a doorstep. We don\'t have a DOOR.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Bring it. We\'ll return it to the Chairman.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'RETURN the dish?! Nobody has EVER returned the dish. This changes everything. This is our secret weapon.' } ] },
  ],
  7: [
    { id: 'ch07_mid', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'Under a lukewarm milk oasis palm, a knight melts in full armor.' },
      { speaker: 'Sir Prestige', portrait: 'npc_prestige', side: 'right', text: 'Sir Prestige here, LIVE from the Great Dessert, fueled by CrunchQuest Granola — the official granola of destiny!' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Your armor is full of custard.' },
      { speaker: 'Sir Prestige', portrait: 'npc_prestige', side: 'right', text: 'It\'s called a COLLAB, peasant.' } ] },
    { id: 'ch07_mini2', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'The ground jiggles. Then the horizon jiggles. Then, concerningly, YOU jiggle.' },
      { speaker: 'The Flanbeast', portrait: 'enemy_boss_flanbeast', side: 'right', text: '*resonant, threatening wobble*' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'It\'s speaking in pure texture, hero. Hold formation. HOLD. FORMATION.' } ] },
    { id: 'ch07_preboss', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'Ten thousand candles gutter in the sugar wind. Each one is a wish nobody made.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The Gateau declared itself a wedding cake and nobody ever came. Four hundred years, hero. Even the frosting is lonely.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Then we fight it... and then we sing to it.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'The two-phase strategy. Classic.' } ] },
  ],
  8: [
    { id: 'ch08_mid', lines: [
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Hero! The cargo manifest! Line 88,412: "One (1) small box. Sender: Pip. Recipient: Pip. Note: do not open until the finale."' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'You mailed yourself a box and forgot?' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'I NEVER forget. I file. Somewhere in my past is a Pip with a PLAN, and I intend to honor her paperwork.' } ] },
    { id: 'ch08_mini2', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'The clouds ahead wear a tiny badge: ROUTE MANAGER.' },
      { speaker: 'Thunderhead', portrait: 'enemy_boss_thunderhead', side: 'right', text: 'YOU ARE OFF ROUTE. EVERYTHING IS OFF ROUTE. I HAVE A QUOTA OF STORMS AND YOU ARE WEATHER NOW.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'We\'re pedestrians.' },
      { speaker: 'Thunderhead', portrait: 'enemy_boss_thunderhead', side: 'right', text: 'PEDESTRIANS ARE SLOW PACKAGES.' } ] },
    { id: 'ch08_preboss', lines: [
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Did you see Dave back there?! "Regional Slime Manager, LOGISTICS DIVISION." He transferred! He got a lateral move out of losing to us!' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Everyone we beat gets promoted. I\'m starting to feel used.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Focus. The Dead Letter Office is ahead — and my box is INSIDE.' } ] },
  ],
  9: [
    { id: 'ch09_mid', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'In the treatment room, a line of magmites waits reverently. On the heated table: a familiar rock.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Warm Rock has been hired as the resort\'s master hot-stone therapist. He has a waiting list. He has a TITLE.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'He\'s the best of us.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'He\'s asked for five minutes at the end of the chapter to just... be warm. Granted, obviously.' } ] },
    { id: 'ch09_mini2', lines: [
      { speaker: 'Bikram', portrait: 'enemy_boss_yoga_dragon', side: 'right', text: 'WELCOME TO MY 104-DEGREE DOMAIN. WE BEGIN IN CHILD\'S POSE. WE END IN DRAGON\'S POSE. THERE IS NO SAVASANA.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'No corpse pose?!' },
      { speaker: 'Bikram', portrait: 'enemy_boss_yoga_dragon', side: 'right', text: 'THE CORPSE POSE IS EARNED.' } ] },
    { id: 'ch09_preboss', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'The penthouse elevator plays lava-lamp music. The floor indicator says: TOP. Then: TOO FAR. Then: MANAGER.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'Remember: we are CHECKING OUT. Not negotiating. Not upgrading. If he offers the eruption package, the answer is no.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'What if it\'s complimentary?' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'NOTHING HERE IS COMPLIMENTARY. WE LEARNED THAT AT THE MINIBAR.' } ] },
  ],
  10: [
    { id: 'ch10_mid', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'In the eternal queue, skeletons pass around pamphlets printed on rice paper.' },
      { speaker: 'Queue Skeleton', portrait: 'enemy_queue_skeleton', side: 'right', text: 'It says here... we don\'t HAVE to hold our place in line... for six centuries... without breaks?' },
      { speaker: 'Fenris', portrait: 'enemy_boss_wolf_rep', side: 'left', text: 'Local 4,000,001 — the Undead Line-Standers\' Guild. First demand: CHAIRS.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'He\'s here too. Of course he\'s here too.' } ] },
    { id: 'ch10_mini2', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'A phantom vehicle idles in the testing yard. It has no wheels, no doors, and one furious instructor.' },
      { speaker: 'Road Test Revenant', portrait: 'enemy_boss_road_test', side: 'right', text: 'MIRRORS. SIGNAL. SCREAM. In that order. BEGIN.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'This isn\'t a road.' },
      { speaker: 'Road Test Revenant', portrait: 'enemy_boss_road_test', side: 'right', text: 'THE ROAD IS A STATE OF MIND. YOU HAVE ALREADY FAILED IT.' } ] },
    { id: 'ch10_preboss', lines: [
      { speaker: '', portrait: '', side: 'left', text: 'PING. The number board changes: NOW SERVING 4,000,001. The queue holds its collective breath. All of it. Every bone.' },
      { speaker: 'Pip', portrait: 'npc_pip', side: 'left', text: 'That\'s us, hero. Six thousand years of waiting ends TODAY. Straighten your collar. Practice your embarrassing middle name.' },
      { speaker: 'Hero', portrait: 'npc_hero', side: 'left', text: 'Never.' } ] },
  ],
};
