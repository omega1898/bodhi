# A Light in the Window: the Christmas story

> **Status:** approved by the owner on 25 November 2026 (see section 11 for what was decided).
>
> **The words:** every caption, chapter by chapter, is in `docs/christmas_script.md`, with the storytelling methods it uses. The deeper threads added on 25 November 2026 are marked **(added)** below.
>
> **What this is:** the story bible for the Christmas game starring Em and Bodhi. Every chapter's scenery, lighting, props, puzzles and captions are built from it, so when it changes, the chapters change with it. It is a proposal written on 25 November 2026; nothing in the code has been changed yet (section 9 lists what would change). How Em and Bodhi look is designed separately (`docs/characters.md`). This document says what happens to them, not how they are drawn.

**At a glance**
- **Twelve chapters, twelve days:** 14 December to Christmas Day. Every chapter card shows its date, so the countdown to Christmas is always on screen. The chapter select could be an Advent calendar, one door per chapter.
- **Chapter One is free.** One purchase unlocks Chapters Two to Twelve.
- **Length:** about 20 to 25 minutes per chapter for a first-time player (the target in `docs/unity_rebuild_spec.md`, R3.4), so four to five hours in all.
- **The title screen is the ending.** Em and Bodhi building a snowman under the aurora, beside the cabin, the tree and the sled, is exactly the last scene of Chapter Twelve. Players see the happy ending before they start, which is what makes the dark middle bearable.

## 1. The story in one breath

### Logline

> Eleven days before Christmas, Em's puppy Bodhi vanishes into a blizzard after the old deaf dog from down the lane. Em has to search a snowbound valley for them both, then bring them home over the mountains before the biggest storm in fifty years closes every road on Christmas Eve.

### The promise (the feeling)

**"Come home. The light is on."** The flood story's feeling, "I need to get back to you", said the Christmas way. In Hollin, on winter nights, people leave a light in the window for anyone still out on the hill. The whole game is about who keeps a light on for you, and who comes looking.

### Themes
1. **A light in the window.** Someone at home keeps faith while you're lost. Em keeps it for Bodhi, Mina keeps it for Em, and at the end the whole village keeps it for all of them.
2. **Home is whoever comes looking.** The cabin matters because of who is in it.
3. **A puppy learns to look after someone.** Em looks after Bodhi in Chapter One. Bodhi looks after old Biscuit for five days on his own. In the white-out on Christmas Eve, Bodhi leads Em home. The one who was cared for becomes the one who cares.
4. **Kind strangers at Christmas.** A lorry driver, a café owner, a boy with chestnuts, a train guard and an old reindeer herder each help a little. None of them can fix it alone.
5. **The lost are found.** Two lost dogs and two people who won't stop looking. Everyone gets home.

### Why it grips: the question that carries all twelve chapters

**Will Bodhi be home for Christmas?** The date on every chapter card keeps the clock ticking. Under that big question, each part of the story asks a sharper one:

| Part | Chapters | The question | How it ends |
|---|---|---|---|
| Together | 1 | Will this be the perfect first Christmas? | The blizzard. The lead comes back empty, and the whistle gets no answer. **(the paywall)** |
| Apart | 2 to 5 | Will Bodhi find Biscuit? Will Em find Bodhi? | Each chapter brings them closer, a near miss each time: an hour apart, then a street, then the width of a train window |
| Found | 6 | Will they finally meet? | The reunion under the aurora at the midpoint, then a new clock: the valley is cut off and Biscuit can't walk |
| Over the top | 7 to 11 | Can they get Biscuit home over the mountains before the Christmas Eve storm? | Bodhi leads Em down through the white-out, toward a village full of lit windows |
| Home | 12 | What does Christmas look like now? | The snowman from the title screen, and the lamp in the window "for nobody, because everyone was home" |

Each chapter answers the last chapter's question and asks a sharper one. Its closing line is written to make the next chapter impossible not to start.

### The rules of this story
- **Nobody dies.** The dog's death is never the payoff (`docs/design.md`). Biscuit's worst night (Chapter Nine) is the darkest moment, and he recovers.
- **Real peril, broad audience.** Cold, storm, ice, a train on a viaduct, being lost. Nothing gory, no swearing, no villains. The danger comes from winter, not from people.
- **Em and Bodhi are a real woman and her real dog.** Everything about Em stays inside the game world: the cabin, the valley, the dog, the search. She has no invented family, job, past or romance. Christmas here is shared with neighbours, which says nothing about her real life.
- **The happy ending is promised up front** by the title screen, and the store description should say so plainly.

### The flood story: a future game

The twelve-chapter flood story in `docs/story.md` (the green door, the river, the long walk home, Mina's chalk hearts) stays as a possible later game in the same series. Em, Bodhi, Mina, Ama and Biscuit could appear in both, like a cast that returns.

## 2. The world: the Hollin valley

A long glacial valley in the far north, cold enough for snow from December and far enough north for the aurora. It has a vaguely Scottish-Highland feel, with a small reindeer herd on the hill, but all the names are invented.

| Place | What it is | Chapters |
|---|---|---|
| **The cabin** | Em and Bodhi's cabin, the last one above Hollin, under the pines: a creaking porch, a woodpile, a stove, lit windows. The title screen's cabin and clearing. | 1, 2, 11, 12 |
| **Hollin** | A small stone village at the head of the valley, where the road gives up. A church with one bell. Mina's cottage is the first one down the lane from the cabin. | 1, 2, 11, 12 |
| **The timber yard** | At the foot of the lane: stacked logs, netted Christmas trees, one orange yard lamp, and Dougie's green lorry. | 1, 2 |
| **Hollin Water and Kell Bridge** | A long frozen lake in the middle of the valley. At its foot, Kell Bridge: Nell's café and the snowplough depot. | 3 |
| **Strand** | The market town at the bottom of the valley, 25 miles from Hollin: a cobbled square, the Christmas market, the town tree. | 4 |
| **The valley railway** | One small line up the valley. At Christmas it runs the Lantern Train, two red carriages hung with lanterns, over the stone viaduct at Skelder Gorge. | 5 |
| **Tam's reindeer hill** | A herder's farm on the moor above the railway: pens, a barn hung with harness bells, about thirty reindeer. | 6, 7 |
| **The drovers' path** | The old way over the fell from the farms to Hollin, marked by cairns: it passes **Blackwater Tarn** (a high frozen lake), **the bothy** (a stone shelter) and **Windy Gap** (the pass). It comes down through the pines right behind the cabin. | 7 to 11 |

**The custom.** In Hollin, on winter nights, you leave a light in the window "for anyone still out on the hill". It is set up in Chapter One and pays off in every chapter after.

### The history under the snow (added 25 November 2026)

The player only ever glimpses this. It is written down so every chapter tells the same history; none of it is ever explained in one go.

- **The drovers.** For three hundred years, until the railway came, drovers walked cattle and sheep from the high farms over Windy Gap and down past the cabin to Hollin, then on to Strand market. The cairns mark their way. Under the snow the path still smells of them, which is how Bodhi finds it blind in Chapter Eleven.
- **The custom is older than the church.** It started with the drovers' wives, who lit a lamp for men still on the hill. Everyone in Hollin still lights one; almost nobody remembers why. The words you say as you light it are **"A light for the hill."** There is a second half, the answer, that only the old remember: **"And the hill brings them home."** Mina learned it from her gran. Em hears it for the first time in Chapter Two, and in Chapter Twelve everyone says it together.
- **Mercy.** Hollin's church bell, cast in 1868, is called Mercy. It is rung for weddings, for funerals, and "for the hill": in a storm, every quarter hour, so anyone lost up there can walk toward the sound. The church keeps an old storm book of every time it was rung that way. Mina finds it in Chapter Eleven.
- **The Long White.** What old Hollin calls a fifty-year storm. The last was Christmas 1974: eleven days of snow, the valley cut off for three weeks. The radio calls this year's a weather warning; Tam calls it a Long White, and so, by Chapter Eleven, does everyone.
- **Tam and Moss.** In the Long White of 1974, Tam Rennie was twelve. He went back up the hill for a strayed calf and the storm caught him. His father's collie, Moss, found him, lay on him for three nights in the bothy so he wouldn't freeze, then led him down the drovers' path in the dark, nose to the old scent, toward a village where every window had been lit for him. Tam cut her name into the bothy doorframe at a dog's height. He has never told anyone the whole story. He tells Em, in five lines, in Chapter Seven.
- **Biscuit's first home.** Biscuit was born in Tam's barn, the pup of Tam's collie Bess, and given to Ama when she was a young nurse living in Strand. They moved up to Hollin when Mina was small. That is why, deaf and muddled, he walks down the valley in Chapter Three: he isn't lost, not to him; he is going back to where he was young. And it is why he went out into the storm at all: in a Long White an old herd dog goes out to bring the stock in. Tam recognises him in Chapter Six.

### The sayings

Short lines the valley says, which mean more each time they come back.

| Saying | Who says it | Where it lands |
|---|---|---|
| "A light for the hill." / "And the hill brings them home." | Everyone, lighting the lamp; the answer only the old know | Ch1 (Em says the first half lightly), Ch2 (Mina gives the answer), Ch12 (everyone says both) |
| "When the deer turn their tails to the north, bar the door." | Herders | The prologue (Tam watches them turn) |
| "Snow forgets. A nose remembers." | Tam | Ch7 (about Moss), Ch11 (Bodhi, in his own words: "Snow forgets. Noses don't.") |
| "Go now, or you're here till the thaw." | Tam | Ch7 |

## 3. The cast

**Em.** Based on a real person; see `docs/characters.md` for how she looks, and keep giving her a say in how she's shown. In this story she is warm, funny, stubborn and practical. She talks to Bodhi all the time. She never gives up looking, and she never says how frightened she is: you see it in what she does (the hourly whistle from the porch, the empty collar she keeps in her pocket). Nothing about her life outside the valley is mentioned. **(Added.)** She carries one small guilt: after his towel she let his collar out a notch so it wouldn't pinch his fluffed-up curls, and wet curls lie flat. She says it once, at the moment it happens ("One notch."), confesses it to Mina in Chapter Seven, and buckles it "the right notch, this time" in Chapter Six.

**Bodhi.** A male goldendoodle, based on a real dog. Red-apricot curls, a cream beard, a black nose (`docs/characters.md`). In this story he is **about nine months old**: nearly grown, all paws and opinions, seeing his **first snow** and having his **first Christmas**. He stays one age for all twelve days. His collar has a small brass bell and a red bow, as on the title screen, but no name tag yet: it is still in the post, which is why nobody who finds him can ring Em. He grows up over the twelve days, from a puppy who lunges into the dark to a young dog who looks back at Em and waits for her to say "go".

**Mina Osei**, 10 (invented). Lives in the cottage below the cabin. She draws, makes cards and is braver than she looks. She keeps Em's lamp lit for ten nights. Reused from `docs/story.md`. **(Added.)** She is sure she latched the log store the night Biscuit got out. She didn't, and she knows it by morning. Keeping the lamp lit is her penance as much as her promise. She confesses on Tam's phone in Chapter Seven, and Em answers with her own guilt: "Latches slip. So do collars." In Chapter Eleven her guilt turns into action: she finds the church's storm book, has Mercy rung and lights every window in Hollin.

**Ama Osei** (invented). Mina's mother, a nurse at the cottage hospital in Strand. Calm, practical, drives Em down the valley and keeps ringing. Reused from `docs/story.md`.

**Biscuit**, 13 (invented). Mina's dog: grey to the eyebrows, a white muzzle, deaf in one ear, a blue collar ("If found, call Ama"). He barks exactly once, very politely: that means hello. He loves puppies. Reused from `docs/story.md`. **(Added.)** Born in Tam's barn, a herd dog's pup; he still stares up at the hill when weather is coming, though he can't hear it (see "The history under the snow").

**Dougie** (invented). Delivers Christmas trees from the timber yard to Strand market in his green lorry. Never knows he had two stowaways until he sees the poster. Keeps a ham sandwich under his seat.

**Nell** (invented). Runs the steamy café at Kell Bridge. Christmas jumper all December, tinsel on the snowploughs, sausage ends for any dog who needs them.

**Kofi**, 9 (invented). Helps at his mum's chestnut stall in Strand market. Shares his chestnuts with "a ginger one and a grandpa one", and his mum is the one who rings Em.

**Pat** (invented). The guard on the Lantern Train. Pulls the brake on the viaduct without being asked twice.

**Tam Rennie** (invented). An old reindeer herder on the hill above the railway, sixty-four. Gruff, kind, talks to his reindeer more than to people. Gives them his father's sled: the sled on the title screen. **(Added.)** He is the prologue's eyes, the boy Moss brought home in 1974, and the man who bred Biscuit. A young Tam and a collie are in the photo on his wall; the collie is Moss.

**Moss** (invented, 1974; added). Tam's father's collie. Only ever seen in an old photograph and a name cut into the bothy doorframe. Bodhi does on Christmas Eve what she did fifty years before.

**Clementine** (invented). Tam's reindeer calf, with a white face and no sense at all. Bodhi herds her back into her pen and makes a friend for life.

**The radio** (invented voice). Valley Radio's Christmas appeal reads out Em's lost-dog notice every hour from Chapter Three onward: "a ginger curly puppy called Bodhi, and an old grey dog called Biscuit". It also gives the storm warnings.

## 4. The twelve chapters

Each chapter lists its setting (this drives the art), its plot, what the player does (built from the existing mechanics: playing as Em or as Bodhi, switching, care moments, fetch, the whistle, scent trails, push, climb, gate and key puzzles, and film scenes), its props, its emotional beat and its closing pull. Each also has three **keepsakes**, small objects that each tell a story about someone who cared for someone. Mechanics the game doesn't have yet are marked **(new)**; section 9 lists them.

### Chapter One: First Snow (14 December, free)

**Setting.** The cabin above Hollin and its clearing (the title screen's place), the lane down through the pines, and the timber yard. Dawn: blue-white, big slow flakes falling, the first snow lying smooth and untouched. Afternoon: a low peach sun skimming the snow, blue shadows as long as trees, dark by half past three. Evening: inside the cabin, with stove light, the tree, and the lamp in the window. Night: a blizzard roaring out of the north, everything white and sideways, the lantern the only light.

**Plot.** Bodhi wakes Em at six, whining at the window: snow, the first of his life. They spend the day in it: snowball fetch (the snowballs vanish when they land, which baffles him), and the snowman from the title screen. Mina comes up the lane with Biscuit and a card for Bodhi's first Christmas. At the timber yard Dougie is loading his last lorry of trees for Strand market, leaving at first light. Bodhi chooses the most lopsided tree in the yard, and they haul it home on the sled. In the evening Em towels the snow out of his curls, feeds him, decorates the tree (he steals a bauble) and hangs his first stocking. She lights the lamp in the window and tells him about the custom: "Not for us. We're in." **(Added:** before dinner she lets his collar out a notch, because the towel has doubled his curls; at noon Mercy rings in the village; Biscuit stands at the top of the lane staring at the hill; at the door Mina says she is sure she shut the log store.**)** After midnight the blizzard hits, and Mina and Ama hammer on the door: Biscuit got out, and he can't hear them call. Mina begs: Bodhi can smell him. In the white-out Bodhi catches the scent and lunges, and the lead comes back with only his collar on it. Em whistles into the storm.

**The player does.** As Bodhi: the first run in the snow; sniffing out vanished snowballs (scent); fetching the snowman's stick arms; sniffing out the stolen bauble. As Em: throwing snowballs (fetch); the whistle (taught here, so the ending can break it); rolling the snowman's base (push); pulling the sled up the lane (push/pull, with Bodhi tugging the rope); three care moments (towel, dinner, petting him on the bed); hanging baubles and the stocking; lighting the lamp. Then the blizzard search with the lantern, Bodhi pulling on the lead along the scent, and the final whistle.

**Props.** Snowballs; snowman parts (stick arms, carrot nose, pebble eyes, Mina's Santa hat); the wooden sled and rope; Dougie's green lorry with netted trees and a ham sandwich under the seat; the lopsided tree and its stand; fairy lights, tins of baubles, one red glass bauble, tinsel; the stocking with "BODHI" in felt (the D backwards); the window lamp (brass, with a glass chimney); Em's carrying lantern; Bodhi's collar with the brass bell and red bow; his lead, towel, bowl and food; Mina's card; Ama's torch.

**Keepsakes.** Mina's card ("Love from Biscuit (he can't write)"). The stolen bauble, under the sofa (only Bodhi can find it). Em's puppy book, folded at "First winter".

**Emotional beat.** Falling in love with one perfect day. Safety at its highest (tree lit, stocking up, lamp burning, puppy asleep on her feet), and then the floor drops away.

**Closing: the paywall hook.** Two options:

- **Option A: "The whistle in the white" (recommended).** Bodhi lunges after Biscuit's scent and the lead goes light. Em reels it in: his collar, bell and all. The player now controls Em, and the objective reads "Whistle for {dog}." All chapter that button has brought him running. The player presses it.
  > I whistled. / The only bell was the one in my hand. / For the first time, he didn't come.

  Black screen. Then Bodhi's small capitals: *"Her whistle, torn to pieces by the wind." / "The old dog. Thin as a thread. That way." / "Keep going."* Title card: *Chapter Two: The Lamp in the Window.* Then the unlock screen.
- **Option B: "The lamp in the window."** The chapter runs on for one more scene. At two in the morning Ama calls off the search ("We'll freeze, Em. First light."). Em goes home, turns the lamp up, and sits by the window with his empty collar in her lap, below the stocking with his name on it.
  > In Hollin you leave a light in the window for anyone still out on the hill. / I had never needed it to work before.

  Then, far away: *"A light. Very small. Very far. Hers."*

**Why A.** The hook is something the player *does*: they press the whistle themselves, and the game's most reliable promise breaks under their thumb. The question it leaves is the sharpest one possible, "will he come back?", and Bodhi's last line shows he is alive and determined, which keeps it hopeful rather than cruel. It ends on action, at the moment of loss, so the urge to continue is immediate. B is beautiful but quiet, and its image works better as the opening of Chapter Two, which is where it goes.

### Chapter Two: The Lamp in the Window (14 December, night, to dawn)

**Setting.** The pine woods behind Hollin in a full blizzard: drifts to Bodhi's chest, black trunks, snow driving sideways, the roar of wind in the tops. A fallen spruce over a hollow. At the foot of the lane, the timber yard under one orange sodium lamp, Dougie's lorry loaded and tarpaulined, its edges cracking in the wind. Cut in between: the cabin porch and its window lamp. Dawn: the storm gone, the sky blue-grey, every surface new and smooth.

**Plot.** Bodhi follows Biscuit's thread of scent through the storm. The wind keeps tearing it away and he keeps finding it again. He finds the old dog curled in a hollow under a fallen spruce, too cold to stand, and lies along his back until the shaking stops. When the wind drops he coaxes Biscuit downhill toward the only light he can see, the yard lamp, and they crawl under the lorry's tarpaulin among the trees. They share the ham sandwich. Em, sent home by Ama at two, does not go to bed. She turns up the lamp and whistles from the porch every hour. At first light she finds Biscuit's blue collar caught on a fence wire ("If found, call Ama", with Ama standing beside her) and promises Mina: "I'll bring him home in this." **(Added:** at the window Mina gives the answer to the old saying, "And the hill brings them home", which Em has never heard.**)** From under the tarpaulin Bodhi sees a tiny warm light high on the hill, and knows it is hers. But Biscuit can't walk any more, and Bodhi won't leave him. At dawn Dougie climbs into his cab and drives his last load down the valley, with no idea he has passengers.

**The player does.** As Bodhi: a scent trail that drifts with the wind (**new**: sniff again when the wind changes); digging Biscuit out; a **reversed care moment** (**new**: hold to press against Biscuit until he stops shaking); leading Biscuit (**new**: an escort; he follows slowly, and Bodhi must wait and nudge him up); squeezing under the yard fence; climbing a log pile onto the lorry. As Em: the lantern search (calling, whistling); turning up the lamp; the hourly porch whistle (a short film of hours passing); the dawn search; finding the collar on the wire.

**Props.** Fallen spruce and hollow; yard lamp; log pile; the lorry with tarpaulin and netted trees; the ham sandwich; Biscuit's blue collar on a fence wire; Em's lantern; the window lamp; Bodhi's empty collar with its bell, which Em carries in her coat pocket from now until Chapter Six.

**Keepsakes.** A bird feeder in the woods that someone keeps filled (Em). Dougie's lunchbox note in the cab: "Drive slow. Home by Christmas. M x" (Bodhi). A child's mitten on the yard gate, put on the post so its owner would see it (Em).

**Emotional beat.** Bodhi's courage and tenderness: the puppy looks after someone for the first time. Em's helplessness, and her stubbornness.

**Closing.** Em on the porch at dawn, watching the lorry's lights go down the valley road. She doesn't know what she's looking at, but the player does.

### Chapter Three: Kell Bridge (15 December)

**Setting.** Halfway down the valley, where the road crosses the outflow of frozen Hollin Water. Nell's café: steamed-up windows, a neon OPEN sign with one letter out, a string of lights round the door. The snowplough depot: big yellow ploughs with tinsel on their blades. A woodshed, a bus shelter, and the frozen lake stretching away up the valley. Hard frost, a pale lemon sun, breath hanging in the air, dark again by half past three.

**Plot.** The dogs' half comes first. The lorry stops at Kell Bridge to wait for the snowplough, and Dougie goes in for a bacon roll. Bodhi wakes to the smell of bacon and the lake and gets Biscuit down off the lorry before it moves on. Nell sees two dogs at her bins and leaves sausage ends and a bowl of water by the woodshed, but they are too frightened to be caught. Bodhi wants to go up the valley, where home smells drift down the lake on the wind. But Biscuit, deaf and muddled, sets off down the road toward Strand, and Bodhi won't leave him. **(Added:** Biscuit isn't lost, to himself: he is going back to Strand, where he lived when he was young. Ama works it out at Kell Bridge.**)** Then the player switches to Em, an hour later. At the timber yard she found three ginger curls on a tree net and the yard's order book: the last load went to Strand. Before leaving she gave Mina her spare key ("Keep the lamp lit. In case he finds his own way."). Ama drives her down the valley. At Kell Bridge the player, as Em, reads the traces the player just left as Bodhi: two sets of prints, one big and slow, one small and bouncing; a licked-clean bowl; a curl on the woodshed nail; and Nell's story. Em puts the first LOST poster in the café window. His name tag was still in the post, and she knows that's why nobody has rung.

**The player does.** As Bodhi: climbing down from the lorry; sniffing out the sausage ends; hiding in the woodshed (a small space only he fits); sneaking past the plough crew; following Biscuit. As Em: examining the traces (looking, not sniffing); a short talk with Nell (a film moment); putting up the poster; finding where the prints lead. **The twist:** one place, an hour apart. The player always knows more than Em does.

**Props.** The lorry; snowploughs with tinsel; Nell's café, bins, sausage ends and water bowl; the woodshed nail with a curl on it; the order book; prints in the snow (two sizes); the LOST poster (Bodhi in his red bow: "LOST: BODHI, 9 months, ginger curls, very friendly. With an old grey dog, Biscuit."); Ama's car.

**Keepsakes.** Nell's noticeboard ("FOUND: one cat, very rude. Ask inside.") (Em). A plough driver's dog bed under the depot bench, a blanket folded in it (Bodhi). A Christmas card on the café counter, addressed "To the plough crew, for keeping the road open. Love, Class 3" (Em).

**Emotional beat.** Dramatic irony and loyalty. Bodhi turns away from home for his friend.

**Closing.** Em reads the prints, and the small ones keep turning round to face home.

### Chapter Four: Strand (16 December)

**Setting.** The market town at the foot of the valley, at dusk and into night. A cobbled square full of wooden market huts strung with warm-white bulbs. A carousel. A chestnut brazier with sparks going up. The brass band on the town-hall steps. A living nativity with a real donkey. Santa's grotto in a garden shed padded with cotton-wool snow. And the big town tree, dark until six o'clock. Light snow falling through the lights.

**Plot.** The dogs reach Strand at dusk, starving, and the market is a wall of boots and smells. Bodhi learns how a town works: hide under the huts, take only what is dropped, keep Biscuit out of the road. Biscuit wanders toward a bus, and Bodhi barks and shoves him back onto the kerb. Kofi at the chestnut stall shares a paper cone and tells his mum about "a ginger one and a grandpa one". **(Added:** first, Biscuit leads them to a red door on Kirk Wynd, Ama's old house, and sits waiting for it to open. Kofi's family lives there now; that is where Kofi meets them. The door never opens for Biscuit, and from then on Bodhi leads.**)** Em arrives on the evening bus and works the market with posters. At six the whole square counts down for the tree. Em whistles across the square at the very moment the band starts a carol. Under the nativity stable, Bodhi lifts his head at a sliver of something, the donkey brays, and it's gone. They miss each other by one row of huts. Biscuit is fading with cold, so Bodhi finds the warmest place in town: Santa's grotto after closing. They sleep in the cotton snow under the empty chair. Across the square, Em puts up her last poster on the bus shelter, facing the grotto.

**The player does.** As Bodhi: moving unseen through the market (hiding under huts, timing the crowds); sniffing out dropped food; leading Biscuit through gaps (escort); the kerb rescue (a timed shove); pushing a crate to reach the grotto's back window and climbing in. As Em: asking at the stalls; putting up posters; climbing the town-hall steps for a view; the whistle (a scripted near miss); the tree-lighting countdown (a film).

**Props.** Market huts and bulb strings; carousel horses; chestnut brazier and paper cones; brass instruments; the town tree and its star; the nativity stable with straw and a donkey; the grotto shed with cotton-wool snow and a big red chair; posters; the bus shelter; the evening bus; a dropped pretzel.

**Keepsakes.** A wish on the grotto's letter board: "Dear Santa, please can Grandad's dog come home from the vet" (Em). A lost mitten clipped on a market railing at child height (Bodhi). A sixty-year-old photo of the first Strand tree, in the town-hall porch (Em).

**Emotional beat.** Being lonely in a happy crowd. A near miss that hurts, and hope, because people have seen them.

**Closing.** Two halves of one square: the dogs asleep in the grotto, and Em's poster facing them through the falling snow.

### Chapter Five: The Lantern Train (17 December)

**Setting.** The valley railway climbing out of Strand: one track through snowy birch and pine, the stone viaduct over Skelder Gorge with a frozen waterfall hanging below it, and a tiny halt with a hut. The Lantern Train: a small diesel with a wreath on its nose and two red carriages hung with lanterns, every window gold. Blue-violet dusk deepening to starlight. The snow has stopped, and the cold is hard and clear.

**Plot.** At first light the dogs leave Strand up the railway line, because the rails smell of the valley and the valley smells of home. Kofi's mum sees the poster and rings Em: two dogs went up the line. Em catches the Lantern Train and rides at the front beside Pat the guard, watching the track. As Bodhi, the player leads Biscuit onto the viaduct as the light goes. Halfway across, the rails begin to hum. Bodhi gets Biscuit into a refuge recess in the parapet, a niche where track workers stand, and they press against the stone as the train passes a hand's width away, all its lanterns lit. Em, at the front window, sees ginger curls flash in the headlamp, and Bodhi, in the dark, smells her through the passing glass. Pat pulls the brake. The train stops beyond the viaduct and Em runs back along the track with her lantern, but the dogs, terrified, have bolted off the far end into the forest. She finds only the prints in the recess, and then, far down in the trees, she hears one bark.

**The player does.** As Bodhi: walking the line (the snow between the rails is easier going); escorting Biscuit; climbing round a fallen tree on the embankment; the train sequence (nudge Biscuit into the recess before the train arrives, a timed escort with a gentle fail-and-retry). As Em: riding in the cab (looking out, a camera moment); climbing down from the train; running back along the line with the lantern; whistling into the forest, and this time a bark answers.

**Props.** The Lantern Train (diesel, wreath, two carriages, lanterns, headlamp); rails and sleepers in snow; the viaduct with its refuge recesses; the frozen waterfall; the halt hut; Pat's guard lamp; Em's lantern; prints in the recess; a tuft of curls on the stone.

**Keepsakes.** A track worker's tea flask left in the halt hut, labelled "For whoever's on nights" (Em). Bird crumbs scattered on a sleeper (Bodhi). A lantern on the train with a child's tag: "This one's for Mum, at the hospital" (Em).

**Emotional beat.** Terror on the viaduct, then hope. For the first time Em knows he's alive and close.

**Closing.** Em in the recess, and a single bark from the dark.

### Chapter Six: The Reindeer Hill (17 December, night)

**Setting.** The hillside above the line: old pines opening onto a high moor under a clear, freezing sky. **The aurora returns**, green curtains for the first time since the title screen. Tam's farm: wooden pens, a stone farmhouse with one lit window, a barn hung with harness bells, and the herd as dark shapes with frosted breath. Snow that squeaks underfoot.

**Plot.** The dogs climb through the forest in the dark, Biscuit stumbling. Bodhi smells big animals, mossy and warm and not dogs, and follows them to the reindeer barn. Inside, Clementine the calf has nosed the gate open and is wandering toward the moor. Something old wakes up in Bodhi, and he turns her back into her pen. Tam comes out with a torch and finds two dogs and a calf, all safe, and carries Biscuit to his stove. **(Added:** Tam knows him: "You're Bess's." Biscuit was born in this barn, and an old herd dog goes out in a Long White to bring the stock in.**)** Down in the forest Em follows the prints uphill, whistling, until she sees Tam's yard light with the aurora above it. Bodhi hears the whistle, close and real now, and runs. **The reunion** happens in the snow between the pens, with the reindeer watching. The player controls the approach, the kneel and the touch (`docs/design.md`). Em puts his collar back on and the bell rings. In Tam's kitchen, Valley Radio: the road and the railway are closed by drifts, and a bigger storm is coming for Christmas Eve.

**The player does.** As Bodhi: following the herd's scent; **herding Clementine** (**new**: she moves away from him, so he steers her by where he stands, then pulls the gate rope in his teeth); barking to bring Tam. As Em: climbing through the forest with the lantern; whistling; opening the field gate. Then both: the reunion (approach, kneel, contact), and a care moment: buckling his collar back on.

**Props.** The reindeer herd; Clementine (white face); pens and rope-latched gates; the barn and harness bells; Tam's stove, torch and tea; the farmhouse window; Em's lantern; Bodhi's collar, bell and bow; Biscuit's blue collar (Em buckles it on him: "I said I'd bring him home in this"); an old wooden radio.

**Keepsakes.** Tam's herd book, every reindeer named for fifty years (Em). An antler shed in the snow, gnawed by generations of herding dogs (Bodhi). A photo on Tam's wall of a young Tam and a collie in the snow (Em).

**Emotional beat.** Release, the midpoint joy. Then a new clock starts.

**Closing.** Hollin is cut off, Christmas is a week away, and Biscuit can't walk.

### Chapter Seven: Tam's Sled (19 December)

**Setting.** Tam's farm by day: a hard blue sky, snow glare, long blue shadows, the herd grazing lichen on the moor. Tam's workshop with his father's wooden sled hanging on the wall. The drovers' path climbing away from the farm, marked by old cairns. By afternoon a grey lid of cloud sits on the mountains.

**Plot.** A day of ordinary affection after five days of fear. Em towels Bodhi, brushes the ice balls out of his paws and feeds them both, and he sleeps on her feet, waking every hour to check they're still there. Ama rings on Tam's wall phone: Mina has kept Em's lamp lit every night, and won't go to bed until both dogs are home. Tam says the drovers' path goes over the fell to Hollin in two days, three with an old dog: "Go now, or you're here till the thaw." **(Added:** on the phone Mina confesses she didn't latch the log store, and Em tells her about the notch. That evening Tam tells the story of Moss and the Long White of 1974, five lines, the only time he tells it: "Snow forgets, lass. A nose remembers."**)** He takes down his father's sled. Em and Bodhi mend it: Bodhi sniffs out the missing runner pin in the straw, and Em fits it. Bodhi learns "pull" in a small harness, and Biscuit rides on a sheepskin. They say goodbye to Clementine, who tries to come too, and set out at noon. At the first cairn Em looks back, and then the snow begins.

**The player does.** Care moments (towel, grooming ice from his paws, feeding, petting); snowball fetch on the moor (the vanishing snowballs again, now he digs them out like a pro); mending the sled (Bodhi sniffs out the pin, Em fits it); harness practice (**new**: Bodhi pulls the empty sled); the first slope (Em pushes while Bodhi pulls, both at once).

**Props.** The wooden sled (the title screen's sled), a small harness, the runner pin, sheepskin, rope; Tam's wall phone; a brush and towel; the cairns; Tam's map, drawn in pencil on the back of a feed sack; a flask and oatcakes.

**Keepsakes.** Tam's father's initials carved under the sled seat (Em). Clementine's first bell, too small for her now, hanging in the barn (Bodhi). A list in Tam's kitchen: "Hay. Lichen. Ring Jean on Sunday." (Em).

**Emotional beat.** Being together again, and the resolve to get Biscuit home.

**Closing.** Em looks back at Tam waving from the first cairn, and then the farm is gone in the snow.

### Chapter Eight: Blackwater (20 December)

**Setting.** Blackwater Tarn, a high mountain lake frozen from shore to shore. The ice runs in bands, black where it's clear and white where it's snowed over, with reeds frozen through it. On the far shore stands an old fishing hut on stilts with a rope and pulley for its boat. Flat grey-white light, snow haze, the mountain faintly mirrored in the black ice. Toward evening the blue goes deep, and the ice booms and sings.

**Plot.** The path skirts the tarn, but a fallen cornice has buried it, and the only way on is across the ice near the shore. Bodhi loves the ice: he slides, his legs go four ways at once, and he does it again on purpose. Halfway over, a gust snatches the sled with Biscuit in it and skids it out onto thin black ice that creaks. Em is too heavy to follow. As Bodhi, the player crawls out low, takes the sled rope in his teeth and pulls, while Em, from the shore, rigs the hut's rope and pulley to haul both of them in. The ice cracks behind them but holds. That night they shelter in the fishing hut, and Em lights a candle in its window: the custom, even here, where nobody will see it.

**The player does.** As Bodhi: sliding play on the ice (fun, no danger); the belly crawl over thin ice (**new**: slow movement, with the creaking getting louder as he goes); holding the sled rope. As Em: rigging the pulley (a push/turn puzzle with a gate and a crank, the same kind as the existing barrier and crank puzzles); hauling in. Then a care moment: drying his belly by the hut stove.

**Props.** Frozen tarn (black and white ice, reeds); the fishing hut on stilts, its rope and pulley, a tiny stove; the sled; a candle in the hut window.

**Keepsakes.** A fishing log in the hut: "Caught nothing. Saw an otter. Perfect day." (Em). A child's ice skate, hung on a nail for the next one who needs it (Bodhi). A frozen biscuit tin with "FOR THE NEXT ONE" painted on its lid (Em).

**Emotional beat.** Trust under strain. Bodhi does the thing she can't, and she makes it possible.

**Closing.** The candle in the hut window, and the tarn booming all night.

### Chapter Nine: The Bothy (21 to 22 December)

**Setting.** A one-room stone shelter on the open fell: a fireplace, a wooden sleeping platform, a visitors' book on a shelf, a drying line strung across. Outside, the storm: snow banked to the window, wind howling over the roof. Inside: firelight on stone, steam rising from wet gloves. Two nights snowed in.

**Plot.** The first storm pins them in the bothy for two nights. Biscuit stops eating and lies very still, and for the first time Em is afraid for him. She and Bodhi keep him warm, coax water into him and keep the fire alive, and Bodhi lies along Biscuit's back all night, the way he did in the spruce hollow. When the fire runs low, Bodhi goes out into the howling dark to dig the woodpile out of a drift: a short, frightening dog section. To pass the hours Em reads the visitors' book aloud, forty years of strangers stuck in this same room at Christmas. **(Added:** one entry is from 1974, in a twelve-year-old's hand, signed T. R.; low on the doorframe, at a dog's height, is cut the name MOSS. This is Tam's bothy.**)** On the second night Bodhi brings her a present: a stick, from the woodpile. In the morning Biscuit lifts his head, eats from Em's hand, and barks once. Hello.

**The player does.** Care moments: warming Biscuit (the reversed care moment, now with Em's hands too), hand-feeding, towelling, petting Bodhi. Keeping the fire going: as Bodhi, sniffing out and digging up logs in the drift; as Em, carrying them in and shouldering the snow-jammed door open (push). Small comforts: blocking the draught, drying the gloves. Reading the book (the keepsakes). A long night (a film of time passing).

**Props.** The bothy hearth; logs under snow; the visitors' book and a pencil; stub candles; a tin of shortbread left "for whoever needs it"; the drying line; sleeping platform and sleeping bag; Biscuit's sheepskin; Bodhi's stick.

**Keepsakes.** In the visitors' book: "Christmas 1987. Snowed in three days with my brother and a tin of peaches. Best Christmas ever." (Em). "1994. Left the shortbread. You're welcome." (Em). A dog's name scratched low on the doorframe, at a dog's height: "MOSS" (Bodhi).

**Emotional beat.** The darkest and quietest point. Care as love. Bodhi, the puppy who needed looking after, keeps an old dog alive with the warmth of his body. Hope comes back with one polite bark.

**Closing.** Em writes their names in the book under forty years of strangers.

### Chapter Ten: Windy Gap (23 December)

**Setting.** The high pass between two peaks: wind-scoured snow, a cornice curling over the drop, a frozen waterfall like organ pipes, iron marker posts furred with rime ice. A low, bright sun turns everything pink and gold, then the snow goes blue. At the top, the view down into the Hollin valley at dusk.

**Plot.** The climb to the pass is the hardest stretch: Em hauls, Bodhi pulls in harness, and Biscuit rides wrapped in the sheepskin. Below the frozen waterfall the path narrows to a ledge. A gust lifts the sled, and it slides down a gully with Biscuit inside. In Chapter One Bodhi lunged without asking. Now he looks at Em first, and she says "Go". As Bodhi, the player chases the sled down and holds it from sliding further, digging in with the rope in his teeth, then nudges Biscuit onto a safe shelf. As Em, the player climbs round by the frozen waterfall, cutting steps and fixing a rope. They meet at an old sheepfold at the foot of the gully and haul everything back up together. At the top of Windy Gap they see the whole valley, and Hollin at the end of it. The storm has brought the power lines down, and every window is dark.

**The player does.** Pulling together up slopes (Em pushes the sled while Bodhi pulls; both at once). Crossing the ledge (careful, slow movement). The gully rescue, a two-route puzzle in the design's classic shape: Em on the waterfall (climbing, rope); Bodhi holding the sled, then escorting Biscuit; near contact across the gully (she can see him and call to him); a final obstacle where both have to act; the reunion at the sheepfold.

**Props.** The sled, harness and rope; the frozen waterfall (ice columns, cut steps); rime-iced marker posts; the cornice; the sheepfold; Tam's map; Em's lantern.

**Keepsakes.** A memorial bench at the top, cleared of snow by someone this morning: "For Iain, who loved this view" (Em). A dog bowl wedged in a cairn, for passing dogs (Bodhi). Prayer flags someone tied to a marker post, faded to white (Em).

**Emotional beat.** Trust, shown in a single look ("He looked at me. I said go."). Then dread: the lights at home have gone out.

**Closing.** Every window in Hollin is dark, and the worst of the storm is still coming.

### Chapter Eleven: Christmas Eve (24 December)

**Setting.** The descent from Windy Gap into the worst storm in fifty years. A white-out: wind, horizontal snow, no up or down. The lantern blows out, and for a stretch there is **no warm light at all**. Later the storm thins, and Hollin's windows appear below one by one, candles and lamps lit by hand because the power is still out. Highest of all, under the pines, one brighter light: Em's lamp. Then the iced footbridge over the burn's gorge, the pines behind the cabin, the porch, Mina, and midnight.

**Plot.** They start down at noon to beat the storm, and the storm wins. By dark it's a white-out. Em's lantern blows out, and she can't see the path or her own hand. **The roles reverse.** Em holds Bodhi's harness, and Bodhi leads her down, nose to the path, because a path walked for centuries by drovers and sheep still smells of them under the snow. In Hollin, Mina has asked the vicar to ring the church bell every quarter hour "for anyone out on the hill", and she has gone door to door until every house has a candle in the window. **(Added:** the bell is Mercy, and Mina found the custom in the church's old storm book, the last entry written in 1974. Bodhi does what Moss did, on the same path, toward the same lit windows.**)** The bell gives Bodhi a direction. Near the bottom the storm thins, and the village appears below, dozens of small lights, with Em's lamp highest of all. At the iced footbridge they work together for the last time. Down through the pines, Mina, on Em's porch, hears Bodhi's bell before she sees anything. Biscuit barks once. Mina runs.

**The player does.** As Bodhi: **leading Em** (**new**: the reverse of following; she holds on, slows on steep ground, and calls to check he's there, so he barks back to keep her close); following the path's old scent; heading for the church bell (a sound, not a sight). As Em: whistling, so he knows she's still holding on; clearing the footbridge's handrail rope. Both: the footbridge (Bodhi crosses first with the sled rope, Em steadies the sled). Then a film: Mina and Biscuit.

**Props.** The lantern (blown out); harness; marker posts; the church bell (heard, not seen); the village windows, each with a candle or lamp; the iced footbridge and gorge; the pines; the cabin porch and the window lamp; Mina's torch; Biscuit's blue collar; Bodhi's bell.

**Keepsakes.** A candle in the farthest window of the village, an old man's house, lit for them by Mina (Em). Sheep's wool snagged on a post, the old drovers' scent (Bodhi). The vicar's note pinned to the church gate: "Bell every quarter hour until they're down. Volunteers to Mina." (Em).

**Emotional beat.** Bodhi brings her home: the game's series title made real. The village keeps the light for them. Mina's joy.

**Closing.** The lamp still in Em's window, and the bell ringing twelve.

### Chapter Twelve: Christmas Day (25 December)

**Setting.** The cabin on Christmas morning: deep fresh snow, a clean blue sky, sun on everything, icicles on the eaves. Inside: the lopsided tree lit, the stove going, the stocking full. The power comes back at lunchtime. Christmas night: the clearing, the aurora, the lantern on the snow, the sled by the porch: **the title screen, exactly**.

**Plot.** Bodhi wakes Em at six with a cold nose in her ear, as on the first day of snow, but this time from her bed, on her feet. His stocking holds a new ball, the stolen bauble (Em found it under the sofa), and his name tag, which came in the post on Christmas Eve. Em buckles it onto his collar next to the bell. She towels the last of the fell out of his curls and gives him a Christmas dinner of his own. Mina, Ama and Biscuit come up the lane for lunch, Biscuit in his blue collar. Mina gives Bodhi a drawing of two dogs and a lamp in a window. Dougie turns up with a box: a ham sandwich, "for the stowaways". Tam rings to say Clementine is fine and wants to know how the sled is. In the evening, under the aurora, Em and Bodhi build a snowman in the clearing. Mina adds the Santa hat, and the scene settles into the title screen. Em whistles, and of course he comes. Last thing, she lights the lamp in the window anyway. **(Added:** Mina says "A light for the hill", and all of them answer "And the hill brings them home". Then an epilogue: up on the moor, Tam's reindeer lie down facing south.**)**

**The player does.** Opening the stocking; care moments (towel, Christmas dinner, petting); fetch with the new ball; giving presents (Bodhi brings Em his stick from the bothy); Bodhi stealing a sprout from the table and being caught (comedy); building the snowman (push the base, fetch the arms, the hat); the whistle, answered; the last lamp.

**Props.** The stocking (the backwards D); the new ball; the name tag; the bauble; Christmas dinner (turkey, carrot, no gravy); a table, crackers and paper crowns (Bodhi in a paper crown); Mina's drawing; Dougie's box; the phone; snowman parts and the Santa hat; the lamp; the aurora.

**Keepsakes.** Mina's drawing (Em). The bothy stick, under the tree, wrapped by Bodhi in his own way (Bodhi). A Christmas card from Tam with a reindeer hair taped inside, "from Clementine" (Em).

**Emotional beat.** Home. The bookends. Everyone who was lost is found.

**Closing.** The lamp in the window, lit for nobody, because everyone is home.

## 5. Chapter One narration in full

Moved to `docs/christmas_script.md`, section 3, rewritten on 25 November 2026 with the threads added above (the notch, Mercy, Biscuit at the hill, Mina's latch) and a prologue before it (section 2 there). The hook is unchanged: Option A, "The whistle in the white".

## 6. Opening and closing lines, Chapters Two to Twelve

Moved to `docs/christmas_script.md`, section 4, with each chapter's key scenes, and an epilogue after Chapter Twelve.

## 7. Voice guide

**Rules for every line**
1. **At most 90 characters, and most under 60.** One idea per line. Two short lines beat one long one.
2. **Add what the screen can't show:** a smell, a memory, a joke, a hint of what's coming. Never describe the picture.
3. **One concrete thing per line:** an object, a number, a sound. "The stove, ticking" beats "quiet".
4. **Vary the rhythm.** A long line, then a two-word line. The short one is where the feeling lands.
5. **Set up, then pay off.** Reuse the objects: the whistle, the bell, the lamp, the collar, the sandwich, the stick. Section 8 lists them.
6. **No two reunions with the same words.** Each is tied to what just happened.
7. **Only the game world.** Em's cabin, valley, dog and search. Never her job, family, past or love life.
8. **The best details are true ones.** Ask Em how she really calls him, what he really does in snow, what he steals.

**Em**: first person, past tense, telling it afterwards. When she speaks to him it's present tense and in his name ("Not for us, {dog}. We're in."). In warm scenes the humour is deadpan, his dignity against reality. In dark scenes her words get short and plain, and the most frightening line is the calmest. She never names her fear; she says what she did.
- The snowman had a nose for almost four seconds.
- He chose it. He sat down beside it and would not move.
- I cried at a bell.
- I turned the lamp up. Then I turned it up again.
- The map said two days. The map had never pulled a sled.
- We shared the last oatcake. He got the bigger half. He always does.
- The wind took the whistle out of my mouth before it was finished.
- He looked at me. I said go.

**Bodhi**: present tense, fragments, never "I", "me" or "my". Smell first, then sound, then sight. He reads time in smells ("yesterday", "this morning", "days ago"). People and places are smells or roles: "her", "the old dog", "the girl", "the big chair", "the singing ice". He doesn't know words like train, storm or Christmas; he knows what they smell and sound like. Feelings come through his body: fur, ears, tail, shaking. Capitals mark one overwhelming thing per chapter at most. He is a puppy at the start, simple and silly, and grows more purposeful as the days go on.
- It smells of nothing. How does it smell of nothing?
- Pine. Oil. Under the seat, a sandwich. Ham.
- Her light. Up the hill. Very small. Still there.
- Boots. Wheels. Sugar, burning.
- The old dog, shaking. Lie close. Closer.
- Big animals. Moss and breath. Not dogs. Not dogs!
- Ice! Slide. Legs go everywhere. Again!
- Her hand on the harness. Slow. Slow. This way.

**Others**: notes are in the writer's own voice. Mina writes in capitals and kisses. Ama is calm and exact. Tam talks in short dry lines ("Go now, or you're here till the thaw."). Dougie is cheerful ("For the stowaways."). The radio is warm and a little formal. Notes are objects the player finds, never summaries.

**The storyteller** (added): third person, past tense, only in the prologue and the epilogue, and only about Tam and the hill. Plain and old-fashioned, like a story told by a fire: "Fifty years the hill had waited. It had been patient long enough." The ten storytelling methods behind the whole script are in `docs/christmas_script.md`, section 1.

## 8. Setups and payoffs (for whoever builds each chapter)

| Thing | Set up | Pays off |
|---|---|---|
| The whistle | Ch1: "He always came." | Ch1 end: no answer. Ch5: a bark answers. Ch6: he comes. Ch11: Em whistles so he knows she's holding on. Ch12: "Of course he came." |
| The bell on his collar | Ch1: "I heard him before I saw him." | Ch1 end: the bell in her hand. Ch6: "I cried at a bell." Ch11: Mina hears it first. Ch12: the name tag hung beside it |
| The lamp in the window | Ch1: the custom, "Not for us." | Ch2: Em keeps it. Ch3: the key to Mina. Ch8: the hut candle. Ch10: Hollin dark. Ch11: still burning. Ch12: lit for nobody |
| Biscuit's one bark | Ch1: "That is how Biscuit says hello." | Ch9: the bark of recovery. Ch11: he barks once at Mina's |
| Biscuit's blue collar | Ch2: found on the wire; "I'll bring him home in this." | Ch6: buckled on. Ch11 and Ch12: Mina sees him in it |
| Dougie's ham sandwich | Ch1: Bodhi smells it | Ch2: the dogs share it. Ch12: "For the stowaways" |
| The lopsided tree | Ch1: Bodhi chooses it | Ch12: lit, "the best tree in the valley" |
| The stolen bauble | Ch1: stolen | Ch12: in the stocking |
| Vanishing snowballs | Ch1: baffled | Ch7: he digs like a pro. Ch12: the snowman |
| "Here is the place" | Ch1: on her feet in bed | Ch12: the game's last line |
| Lunging versus asking | Ch1: he lunges without a look | Ch10: "He looked at me. I said go." |
| The sled | Ch1: Em's sled; Ch7: Tam's father's sled | Ch12: Tam's sled by the porch, as on the title screen |
| The aurora | The title screen | Ch6: the reunion. Ch12: the last scene |
| The notch (added) | Ch1: "I let his collar out a notch, so it wouldn't pinch." | Ch1 end: "One notch." Ch6: "The right notch, this time", and the puppy book's underlined line. Ch7: confessed to Mina. Ch12: the name tag, "the right notch" |
| Mina's latch (added) | Ch1: "I shut the log store. I'm sure I did." | Ch7: "It was me." / "Latches slip. So do collars." Ch11: she has Mercy rung |
| Mercy, the church bell (added) | Ch1: "rung once, long ago, for the hill" | Ch11: rung every quarter hour; Bodhi walks toward it. Ch11 end: "Mercy rang twelve" |
| The saying's second half (added) | Ch1: "A light for the hill", said lightly | Ch2: Mina gives the answer. Ch12: everyone says both halves |
| Moss and 1974 (added) | The prologue: "a dog called Moss brought him home" | Ch6: the photo. Ch7: Tam's story. Ch9: the book and the doorframe. Ch11: Bodhi on the same path |
| Biscuit's first home (added) | Ch1: Bodhi smells "sheep, a long time ago"; Biscuit stares at the hill | Ch3: "He's going home." Ch4: the red door. Ch6: "You're Bess's." |
| The reindeer (added) | The prologue: tails to the north | The epilogue: lying down, facing south |

## 9. Art direction, chapter by chapter

This follows `docs/unity_art_bible.md`: every scene is cold blue dark with **one warm light**; `warm_amber` is kept for home, firelight and reunions; both characters always carry the `moon_rim` edge light against a `night` backdrop; and everything is in the storybook 3D look of the title screen (the `Bodhi/Lit` shader, soft light wrapping round forms, rounded shapes, fog thickening with distance). Two Christmas rules on top: **Christmas reds and greens live only in props** (baubles, ribbons, the wreath, Bodhi's red bow), never in light; and **fairy lights stay small and mostly warm white**, so the one warm light still leads. Snow needs two new palette colours (`snow_lit`, `snow_shade`); section 10 lists them.

| Ch | Palette mood | Cold key light | The one warm light | Weather and effects |
|---|---|---|---|---|
| 1 | Morning: soft blue-white. Afternoon: peach on snow. Night: grey-white roar | Overcast sky, then moon behind the storm | Cabin windows, then the stove; in the blizzard, Em's lantern only, with the window lamp a speck behind | Big slow flakes, then a horizontal blizzard; breath; footprints |
| 2 | Near-white noise in the woods; blue-grey dawn | Storm-dark fill, no moon | **Em's window lamp**, tiny and amber, high on the hill. The yard's sodium lamp is a duller orange, the "wrong" warm light, because it isn't home | Blizzard, gusts carrying the scent motes sideways |
| 3 | Hard frost: lemon-white sun, long blue shadows | Low, clear winter sun | Nell's café window (amber), with tinsel glinting on the ploughs | Breath, frost sparkle, exhaust steam |
| 4 | The richest chapter: deep blue dusk, warm-white bulbs | Blue dusk from the sky | **The town tree** when it lights at six (before that, the chestnut brazier) | Light snow through the bulbs, brazier sparks |
| 5 | Blue-violet dusk into starlight; the frozen waterfall pale cyan | Starlight and snow-glow | **The Lantern Train**, a moving band of gold, with a white headlamp | Clear, still air; the train's breath of exhaust |
| 6 | Black-green night, aurora curtains, moonlit snow | Moonlight, with a faint green aurora fill | Tam's farmhouse window, then the barn stove; the reunion gets the local warm Volume | Squeaking snow, frosted reindeer breath, a shooting star |
| 7 | Bright blue day, snow glare; a grey lid by afternoon | High clear sky, then flat cloud | Tam's kitchen stove (morning scenes) | Glare, then the first flakes at the cairn |
| 8 | Flat grey-white haze, black ice as the ground plane | Shadowless overcast | The candle in the hut window at dusk | Snow haze, ice cracks, blown spindrift |
| 9 | Inside: amber firelight on stone. Outside the window: storm blue | The blue window, the only cold light inside | **The fire**, the warmest chapter on screen | Steam from the drying line, drifting embers, snow against the glass |
| 10 | Pink-gold sun on rime, then blue snow at dusk | Low raking sun | The low sun itself; at the top, **no warm light below** (Hollin dark, on purpose) | Wind, spindrift off the cornice, ice glitter |
| 11 | White-out: low contrast, no horizon; then night | Diffuse storm white, then dark | **None** while the lantern is out. Then dozens of tiny candles below, and Em's lamp the brightest, highest one; at the end, the porch | Horizontal snow, then thinning flakes; bell rings as visible sound waves in the snow (optional) |
| 12 | Blue-sky morning, sunlit snow; night is the title screen | Sun, then moonlight and aurora | Indoors: the tree and stove. At night: Em's lantern, the cabin window, the tree | Icicles dripping; at night, gentle snow and a shooting star |

## 10. Files that would need changing to build it (none are changed here)

**Story and chapters (rules)**
- `rules/Runtime/Chapters/`: all twelve chapter files rewritten for this story (for example `Chapter01FirstSnow.cs` to `Chapter12ChristmasDay.cs`), with ids, titles, dated eyebrows, places, things, zones, story rules, keepsakes and aftermath lines. `StoryChapters.cs` lists them in order. Bodhi is nine months old in every chapter, with one size.
- `rules/Runtime/ChapterScript.cs`: a marker for where the free part ends (for example `StoryRule.ends_free_part`); a slot for the chapter's date; optionally a separate voice for Em speaking aloud.
- `rules/Runtime/ChapterMemories.cs`: a new table of remembered moments (the empty collar recalled in Ch6, the lamp in Ch11, the stolen bauble and the sandwich in Ch12, "he lunged" in Ch10).
- `rules/Runtime/CompanionBehavior.cs`: **new** follow modes: Biscuit following Bodhi slowly (the escort), and Em following Bodhi in the white-out (Ch11).
- `rules/Runtime/CareMoment.cs`: **new** care kinds: Bodhi warming Biscuit (hold close), hand-feeding, picking ice from paws (an extension of grooming), putting a collar back on.
- `rules/Runtime/Puzzle.cs` and new puzzle data files like `StationPuzzles.cs`: each chapter's main puzzle (the grotto crate, the viaduct recess, Clementine's pen, the pulley, the gully), each proved free of soft locks by `PuzzleSolver`.
- `rules/Runtime/Campaign.cs` (and its save text): the twelve new chapters, the free/paid lock, and a check that a save claiming "unlocked" matches the App Store's record.
- `rules/Runtime/HomeDay.cs`: optional Christmas home tasks (hang a bauble, fill the stocking, build a snowman).
- `rules/Runtime/RailwayChapter.cs`, `RailwaySave.cs`, `StationPuzzles.cs`: built for the flood story's station. Keep them for that future game, or retire them.

**Game logic and scenes**
- `game/Logic/BackStory.cs`: the six opening cards (section 5).
- `game/Logic/Chapters/Stage01...Stage12`: a new layout per chapter (floors, props, snow, staging) and `ChapterStages.cs`.
- `game/Logic/StageLook.cs` and `ChapterStage.cs`: snow and blizzard settings, aurora on or off, ice, the date on the chapter card.
- `game/Logic/BallFlight.cs` and `FetchGame.cs`: **new** snowballs that vanish when they land, for Bodhi to dig out.
- `game/Logic/GameSession.cs`: the paywall at the end of Chapter One, the Advent-calendar chapter select, and Continue on a locked save.
- `game/Logic/SoundSynth.cs` and `MusicSynth.cs`: the collar bell (it must be recognisable, because it carries the plot), wind, the church bell, the brass band, the train.
- `game/Logic/OpeningTitle.cs` and `game/Runtime/TitleScreens.cs`: the subtitle text ("Until We Find Home" today) if the game's name changes.
- `game/Runtime/ChapterDirector.cs`: the end screen ("Em and Bodhi's first year" becomes a Christmas line).
- `game/Runtime/World/`: blizzard strength in `SnowFall.cs`, ice and frozen water, village windows lit by candle, the Lantern Train.
- `game/Runtime/Title/`: the title is already the last scene. Only Tam's sled detail (it already has a sled) and the lamp in the window might need a touch.
- `game/Runtime/` scenes and Unity IAP: buying, restoring and the unlock screen. This must be done on the Mac in Unity.

**Look**
- `look/Runtime/Palette.cs`: two new colours, `snow_lit` and `snow_shade` (blue, never grey), with checks in `PaletteTests`.

**Tests** (`rules_dotnet/Bodhi.Rules.Tests/`)
- `AllChaptersTests.cs`: new chapter ids; the keepsake counts; the check that Bodhi grows every chapter (he doesn't in this story, so it should become "the same size in every chapter"); a new check that every caption is at most 90 characters.
- `ChapterCheckpoints.cs`: every chapter's events.
- `Game/BackStoryTests.cs`: its pinned last card ("He fell asleep before we reached the end of the road.") and the card count.
- `CampaignTests.cs`: the free part stops at its marker, an unlocked year carries on, and a "break it" test where a tampered save claims to be unlocked.
- `RailwayChapterTests.cs`, `StationPuzzleTests.cs`: kept or retired along with the station code.
- `Game/GameSessionTests.cs`: the opening film's lines.

**Docs and art**
- `docs/story.md`: marked as the future flood game, pointing here for the current game.
- `docs/characters.md`: Bodhi's age in this story, and the real details still to collect (Em's real call, his habits in snow, his favourite toy).
- `docs/unity_art_bible.md`: the chapter light table (section 4) replaced with section 9's, the two Christmas colour rules, and the snow colours.
- `docs/unity_rebuild_spec.md`: the chapter list and release scope.
- `docs/architecture.md`: a new decision: the Christmas story is the first game, plus the business model.
- `docs/design.md`: a note that the first mobile release ends its free chapter on a cliffhanger.
- `docs/app_store.md`: the game's name, the in-app purchase and its price, and a description that says the story ends happily.
- `art/`: concept paintings per chapter from section 9; the props in each chapter's list; Clementine, Tam, Mina, Ama, Biscuit, Dougie, Nell, Kofi and Pat as supporting figures.

## 11. Decisions for the owner

**Decided by the owner on 25 November 2026:** build the game from this story; the title is **A Light in the Window** (with "Bodhi" large on the title screen); Chapter One ends on **Hook A, "The whistle in the white"**; the valley has a **Scottish-Highland** feel with a small reindeer herd. Still open: items 3, 4 and 6 to 9 below.


1. **The name.** Suggested: "A Light in the Window" for this game, keeping "Bodhi" large on the title. Is "Until We Find Home" the series name, or the flood game's title?
2. **Hook A or B** for the end of Chapter One. A is recommended.
3. **Bodhi's age.** Nine months gives "first snow, first Christmas". Is that right for the real Bodhi, or should he be older in this story?
4. **Real details.** How does Em really call him: a whistle, a word or a phrase? The hook depends on it. Does the real Bodhi wear a bell? What does he do in snow? Em should read the lines about her.
5. **The setting's flavour.** A Scottish-Highland feel with a reindeer herd (real herds exist there), or something more Nordic? This decides names, signs and the market.
6. **Reusing Mina, Ama and Biscuit.** Recommended, as a neighbour family who return across the series.
7. **Dates on screen and an Advent-calendar chapter select.** Lovely in December. In July it reads as a "Christmas story", like a Christmas film, which is fine as long as the store page says so.
8. **Home days while locked.** Should a player who says "not now" still be able to visit Bodhi on home days? It would be a gentle reason to come back and buy.
9. **Em's winter clothes.** Her design is separate, but the story is set entirely in snow. Does she keep the teal coat and rust scarf (the art bible's palette), with a winter layer added?
