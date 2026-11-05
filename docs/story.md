# Story: Until We Find Home

## The promise

**The feeling:** "I need to get back to you."

**The shape:** Bodhi's first year. She brings Bodhi home as an eight-week-old puppy. Four months later a flood takes their house and carries them far away. They spend the rest of the year walking home, and Bodhi grows up on the way. The game ends where it began: the house with the green door, a bowl, a towel, and bed.

**The ending:** they get home. She tucks Bodhi in, the way she did the first night, and lies down beside her. Nobody dies; the payoff is survival, trust and reunion (see `docs/design.md`, "do not make the dog's death your default emotional payoff").

**Bookends:** the last chapter repeats the first day's small rituals, a year apart. Two pencil marks on the same doorframe ("8 weeks", "one year"), the same bowl, the same bed.

**Voices:**
- Her lines are first person, past tense, short: "I lit the stove."
- The dog's lines are present-tense sensations in small capitals, with no "I": "Her smell. Rain and soap. That way."
- Written notes appear in a plain typeface.

**Names:** the dog is **Bodhi**, a female goldendoodle (`story_config` in `prototype/web/js/levels.js`, pronouns her/her). She has no name in the text yet; fill it in from `docs/characters.md` if she should have one.

## Chapters

Each chapter names Bodhi's age (`dog_age`), which sets her size, her drawings and how high she jumps (`dog_growth` in `levels.js`).

| # | Chapter | Bodhi | Place | What happens | Quiet scene |
|---|---|---|---|---|---|
| P | Coming Home | 8 weeks | The house with the green door | A film: she brings the puppy home. Then the first day teaches everything: fetch, the whistle, a mark on the doorframe, a meal, a bath, the photograph. At night the puppy cries in her basket; you play her, climbing onto the bed | The puppy asleep on her feet; the basket empty |
| 1 | The Storm | 4 months | The same house, at night | The river floods the house. The stairs go; she is upstairs, the puppy below in rising water. The puppy climbs the furniture; she lowers knotted sheets | A rescue boat at dawn, taking them east to the city |
| 2 | The Last Morning | 5 months | The emergency flat, far from home | The water rises again. She takes the photograph of Bodhi on their sofa and they set out west, home | Walking out of the city at dawn |
| 3 | The Other Side | 6 months | Flooded railway station | The walkway collapses. Power, scent, the glove, the fence | The cabin stove; the glove drying |
| 4 | Visiting Hours | 7 months | Hospital children's wing | Fire doors; the vent only a dog fits; the lift | Painted stars; the toy rabbit |
| 5 | Lessons | 9 months | Primary school | **She chooses to leave**, after other people. The bell, the bridge | The chalkboard: "Northgate. We are alive." |
| 6 | The Crossing | 11 months | Northgate, the river in flood | The river takes the bridge and the dog. **The photograph blows away**: she chooses Bodhi | Dawn; she can't picture the photograph, only the dog |
| E | Home | one year | The road west, and the green door | The house survived. A second mark beside the first, her bowl, a towel, and bed: she tucks Bodhi in and lies down beside her | Both asleep; down the road, another light comes on |

## How the emotional arc builds

1. **Coming Home:** the bond from its first hour. The player learns her habits while she is small enough to fit in one hand.
2. **The Storm:** the first loss: the house, and for a while each other. She is too small to climb out alone, so both of them have to act.
3. **The Last Morning:** far from home. The photograph is all that's left of it, and it shows Bodhi, not the house.
4. **The Other Side, Visiting Hours:** after each separation the dog stays close for a while, driven by the `closeness` number.
5. **Lessons:** a reversal. For the first time she leaves, for other people.
6. **The Crossing:** she can't hold both the photograph and the dog, and chooses the dog.
7. **Home:** the place was home all along, but only because they came back to it together.

## Looking after her

She gets hungry and dirty as the story goes on (`prototype/web/js/care.js`). It never ends the game; a hungry dog is slower and noses the bag, and a muddy one has muddy paws. Feeding her and drying her are the "ordinary affection" the design asks for between frightening moments. Coming Home teaches both; Home repeats them.

## Keepsakes (why players replay)

Each chapter hides three keepsakes, and each tells one small story about someone who cared for someone. About half can only be found as the dog, by sniffing near them. Found keepsakes go into the **memory box** on the title screen. See `docs/art_and_production.md` for the reasoning.

## Play time: where it stands, and the road to five hours

**Measured:** the robot playtest (`node prototype/web/tests/playtest.js`) finishes the whole story in **6.7 minutes of game time**. That is the floor: it knows every answer, never explores, reads nothing and finds 2 of 24 keepsakes.

**Estimated, not measured:** a first-time player, reading, exploring and looking for keepsakes, might take 25 to 45 minutes. This is a guess until people play it (milestone T in `docs/build_plan.md`).

**The target is at least five hours.** That is about seven times more game than exists. Two things get there, and both cost real time:

1. **Deeper chapters.** About 20 to 25 minutes each instead of 3 to 6: two or three separations per chapter, more rooms, side areas with keepsakes, more of the dog's own routes, and care moments.
2. **More chapters,** one per stage of her first year:

| Bodhi | New chapter (planned, not built) | Idea |
|---|---|---|
| 6½ months | The Motorway | A jam of abandoned cars as platforms; a frightened stray Bodhi befriends |
| 8 months | The Orchard | Autumn on an empty farm: food, rest and a farmer's notes; a quiet, care-heavy chapter |
| 10 months | First Snow | A mountain pass; keeping her warm becomes part of looking after her |
| 11½ months | High Farm | The Northgate people and their dogs: stay with them, or go home |

Twelve chapters of 20 to 25 minutes come to roughly four to five hours. Padding with chores or long walks would reach the number faster and lose the players; the measure is whether people choose to keep playing.
