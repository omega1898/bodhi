# Story: Until We Find Home

## The promise

**The feeling:** "I need to get back to you."

**The shape:** Bodhi's first year. She brings Bodhi home as an eight-week-old puppy. Four months later a flood takes their house and carries them far away. They spend the rest of the year walking home, and Bodhi grows up on the way. The game ends where it began: the house with the green door, a bowl, a towel, and bed.

**The ending:** they get home. She tucks Bodhi in, the way she did the first night, and lies down beside him. Nobody dies; the payoff is survival, trust and reunion (see `docs/design.md`, "do not make the dog's death your default emotional payoff").

**Bookends:** the last chapter repeats the first day's small rituals, a year apart. Two pencil marks on the same doorframe ("8 weeks", "one year"), the same bowl, the same bed.

**Voices:**
- Her lines are first person, past tense, short: "I lit the stove."
- The dog's lines are present-tense sensations in small capitals, with no "I": "Her smell. Rain and soap. That way."
- Written notes appear in a plain typeface.

**Names:** the dog is **Bodhi**, a male goldendoodle (`story_config` in `prototype/web/js/levels.js`, pronouns him/his). She is **Em** (`her_name`, filled into `{her}`). The story is told in her voice, so her name only appears when someone speaks to her, as Ama does at High Farm, and on the year-end screen.

## Back story

Told on New Game, before Coming Home starts, as caption cards in her voice (`game/Logic/BackStory.cs`). A tap moves on; holding a finger down skips them all. Each card names a picture for the scene to show behind it.

| # | Card | Picture |
|---|---|---|
| 1 | Our town sat low beside a wide, slow river. | `river` |
| 2 | My house was the one with the green door. | `green_door` |
| 3 | I had wanted a dog for as long as I could remember. | `empty_bowl` |
| 4 | Then a photograph came. A small, curly face. | `photo` |
| 5 | I already had his name: {dog}. | `photo` |
| 6 | On the first of May, I drove out to fetch him. | `road` |
| 7 | He fell asleep before we reached the end of the road. | `asleep` |

It stays with the game world: the river, the house, the wanting, the drive. Nothing about her own life beyond that, since she is based on a real person (`docs/characters.md`).

Before the title, the start-up animation (`game/Logic/OpeningTitle.cs`): a dusk sky over the house with the green door, a trail of paw prints, Bodhi trotting in to sit by the door, the door opening on her kneeling, the hug, then the name and "Until We Find Home".

## The thread: Mina, Biscuit and the hearts

The journey home is driven by a second, smaller story the player pieces together: a lost dog.

**Who they are** (invented supporting characters, not real people):
- **Mina Osei**, 10, lives two doors down, behind the blue door.
- **Ama Osei**, her mother: the night nurse who stayed with the four patients who couldn't be moved.
- **Biscuit**, their dog: 13, grey with a white muzzle, deaf in one ear, a blue collar. He loves puppies.

**What happened to them:** in the flood, Mina and Ama were rescued but Biscuit wasn't. Mina went looking for her mother, and on the way she drew chalk hearts on the road so Biscuit could follow her. Biscuit did follow, by smell, always a few days behind. And Bodhi follows Biscuit's smell, so the player is always one step behind both of them.

**Why it works:**
- It gives every chapter a question beyond "will they reunite?": is Biscuit alive, and will they catch him?
- It mirrors the main story: another person separated from her dog.
- It pays off the theme: Bodhi pulls her toward other people (`docs/design.md`).
- The darkest moment (the empty collar in the snow) comes just before the best one: Biscuit found, alive, and run straight into Mina's arms.

| Chapter | What the player learns |
|---|---|
| Coming Home | The old dog next door, over the fence. A welcome card: "From Mina (10) and Biscuit (13). He LOVES puppies." |
| The Storm | Biscuit howling next door. In the rescue boat, Mina calling his name over the water. He isn't with them |
| The Last Morning | Mina's poster: LOST IN THE FLOOD: BISCUIT. Her mother is a nurse at St Agnes, west |
| The Other Side | A chalk heart with an arrow. Grey hairs on the bench: "Biscuit waited here." Bodhi smells him: days ago, going west |
| The Motorway | A heart on every car door. Mina's note under a wiper: "I drew hearts so he can follow me." His scent under a car |
| Visiting Hours | Biscuit visited the children's ward on Thursdays. Ama's letter to Mina: gone north to Northgate |
| The Orchard | The farmer's note: an old grey dog slept in the barn, "looking for someone". The hollow he left in the hay, still warm |
| Lessons | Bodhi bolts because he smells him. In the register: "MINA was here. BISCUIT, GO NORTH." |
| First Snow | His collar, caught on a fence wire in the snow. "If found, call Ama." No one to call |
| The Crossing | Ribbons tied at a dog's height, a ribbon every hundred steps |
| High Farm | Mina asks, "Have you seen Biscuit?" Bodhi smells him on the wind, trapped in a byre on the hill. Freed, he runs to Mina |
| Home | Two doors down, a light comes on behind the blue door. Biscuit barks once. Bodhi answers |

## Chapters

Each chapter names Bodhi's age (`dog_age`), which sets his size, his drawings and how high he jumps (`dog_growth` in `levels.js`). He is bigger in every chapter than the one before; a test checks it.

| # | Chapter | Bodhi | Place | What happens |
|---|---|---|---|---|
| P | Coming Home | 8 weeks | The house with the green door | A film: she brings the puppy home. The first day teaches fetch, the whistle, the doorframe, a meal, a bath, the photograph; at night you play the puppy, climbing onto her bed |
| 1 | The Storm | 4 months | The same house, at night | The flood. The stairs go; the puppy climbs out of rising water; knotted sheets; a rescue boat takes them east |
| 2 | The Last Morning | 5 months | The emergency flat, far from home | Mina's poster. They set out west |
| 3 | The Other Side | 6 months | Flooded railway station | The walkway collapses; power, scent, the glove, the fence |
| 4 | The Motorway | 6½ months | Abandoned cars on a flooded motorway | The bridge deck drops; a ladder; Bodhi swims; a chained barrier |
| 5 | Visiting Hours | 7 months | Hospital children's wing | Fire doors; the vent; the lift; Ama's letter |
| 6 | The Orchard | 8 months | A farm gone wild | Bodhi under the locked gate; the key; the hayloft; a rusted bolt |
| 7 | Lessons | 9 months | Primary school | Bodhi leaves her, after Biscuit's scent; the bell, the bridge |
| 8 | First Snow | 10 months | The pass over the moor | The snow edge gives way; the hut, the stove, a hurdle; the collar; a frozen gate |
| 9 | The Crossing | 11 months | Northgate, the river in flood | The river takes the bridge and the dog; the photograph blows away |
| 10 | High Farm | 11½ months | The fire on the hill | Mina; Biscuit found under a fallen beam; the two of them together again |
| E | Home | one year | The road west, and the green door | A second mark beside the first, his bowl, a towel, and bed: she tucks Bodhi in and lies down beside him |

## Looking after him

He gets hungry and dirty as the story goes on (`prototype/web/js/care.js`). It never ends the game; a hungry dog is slower and noses the bag, and a muddy one has muddy paws. Feeding him and drying him are the "ordinary affection" the design asks for between frightening moments. Coming Home teaches both; Home repeats them.

### The bond across the year

Separate from how he feels in the moment (which starts fresh each chapter), Em and Bodhi's bond grows slowly over the whole year (`rules/Runtime/BondJourney.cs`). Every ordinary kindness adds a little: a meal, a towel or bath, brushing, a cuddle, a game of fetch, a walk, practising a trick. Finishing a chapter, finding each other again and finding a keepsake add more. The same kindness on the same day counts in full a couple of times, then less, then not at all until tomorrow, so the bond can't be rushed by repeating one thing.

The bond has five levels, each with a trick he becomes ready to learn:

| Level | Name | Trick |
|---|---|---|
| 1 | Getting to know you | Sit |
| 2 | Friends | High five |
| 3 | Best friends | Spin |
| 4 | Family | Bow |
| 5 | Inseparable | Hug |

A whole year of story alone takes them to about Family; looking after him at home is what makes them Inseparable. The numbers live in one table in the code so they can be tuned in playtests.

### Home days

Between and after chapters there is always something small to do at home (`rules/Runtime/HomeDay.cs`). Each day offers three tasks, the same three for everyone on that date: brush him, give him his dinner, a walk round the block, dry him after the garden, a cuddle, find the hidden ball as Bodhi, play fetch until he catches five in a row, practise a trick he knows, or teach him a new one once he's ready. Each grows the bond. Doing all three on days in a row builds a streak; missing a day only starts the streak again, and never takes anything away. The fetch game keeps his best run of catches, ever and today.

## Keepsakes (why players replay)

Each chapter hides three keepsakes, and each tells one small story about someone who cared for someone. About half can only be found as the dog, by sniffing near them. Found keepsakes go into the **memory box** on the title screen. See `docs/art_and_production.md` for the reasoning.

## Play time: where it stands, and the road to five hours

**Measured:** the robot playtest (`node prototype/web/tests/playtest.js`) finishes all twelve chapters in **10.8 minutes of game time**. That is the floor: it knows every answer, never explores, reads nothing and finds 2 of 37 keepsakes.

**Estimated, not measured:** a first-time player, reading, exploring and looking for keepsakes, might take 45 to 75 minutes. This is a guess until people play it (milestone T in `docs/build_plan.md`).

**The target is at least five hours,** about five times more game than exists. All twelve chapters of Bodhi's first year are now built, so the rest has to come from deeper chapters, about 25 minutes each instead of 4 to 7:
- a second separation in each chapter;
- more rooms, and side routes with keepsakes;
- more of the dog's own routes;
- care moments (feeding, drying, resting).

A later step could add the journey of Mina and Biscuit themselves, told in a few short chapters played as Biscuit. Padding with chores or long walks would reach the number faster and lose the players; the measure is whether people choose to keep playing.
