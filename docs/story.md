# Story: Until We Find Home

## The promise

**The feeling:** "I need to get back to you."

**The tension:** she is chasing a place from the past (the house in the photograph). The dog keeps pulling her toward life in the present (other people, other dogs, smoke on a hill).

**The ending:** the place was never home. The dog was. Nobody dies; the payoff is survival, trust and reunion (see `docs/design.md`, "do not make the dog's death your default emotional payoff").

**Voices:**
- Her lines are first person, past tense, short: "I lit the stove."
- The dog's lines are present-tense sensations in small capitals, with no "I": "Her smell. Rain and soap. That way."
- Written notes appear in a plain typeface.

**Names:** the dog's name is set once in `prototype/web/js/levels.js` (`story_config`) and is currently **Bodhi**, assumed from the project's name. The dog's pronouns are set in the same place, and she has no name in the text yet. Fill both in from `docs/characters.md`.

## Chapters

| # | Chapter | Place | Separation | She helps the dog by | The dog helps her by | Reunion | Quiet scene |
|---|---|---|---|---|---|---|---|
| P | The Last Morning | Their flat, flooded street | None (tutorial: walk, whistle, pet, fetch) | n/a | n/a | n/a | Walking out of the city at dawn |
| 1 | The Other Side | Flooded railway station, night, rain | Walkway collapses | Restoring power, which opens the luggage-room gate | Finding her glove, barking from a window to show her a gantry | Lever on her side and debris on the dog's, both needed | The cabin stove; the glove drying |
| 2 | Visiting Hours | Hospital children's wing | Fire doors slam; the dog bolts downstairs | Unlocking the basement | Squeezing through a vent only a dog fits through, calling the lift | Crank on her side, emergency cord on the dog's | Painted stars; the dog asleep with a toy rabbit |
| 3 | Lessons | Primary school, grey afternoon | **The dog chooses to leave**, following the scent of other people | Shoving a crate off the roof so the dog can climb a wall | Ringing the school bell, which drops a fire-escape bridge | Latch on her side, desk on the dog's | The chalkboard message: "Northgate. We are alive." |
| 4 | The Crossing | Northgate shelter, river in flood, storm | The river takes the footbridge and the dog | Closing the floodgate so the river drops | Pulling a pin that swings a gangplank across | Winch and post; **the photograph blows away** | Dawn; she can't picture the house, only the dog |
| E | Home | The house in the photograph | None | n/a | n/a | n/a | They walk toward the fire on the hill, toward people |

## How the emotional arc builds

1. **Prologue:** teach the dog's habits. It runs ahead, stops and looks back. It comes to a whistle and leans into her hand.
2. **Chapter 1:** the first separation. Fear is high afterwards, so the dog follows closely, driven by the `closeness` number in code.
3. **Chapter 2:** the dog won't leave her side. A nurse's note mirrors the theme: "I won't leave them alone in the dark."
4. **Chapter 3:** a reversal. For the first time the dog leaves her, for the present (other people). The player feels the pull between the photograph and the chalkboard.
5. **Chapter 4:** the climax. She can't hold both the photograph and the dog, and she chooses the dog. Reaching for the photograph is optional; either way it's gone.
6. **Epilogue:** the house is a ruin with the dog's height marks on the doorframe. The dog looks at the smoke, then back at her, checking she's coming. She follows.

## Keepsakes (why players replay)

Each chapter hides three keepsakes, and each tells one small story about someone who cared for someone. About half can only be found as the dog, by sniffing near them. Found keepsakes go into the **memory box** on the title screen. A missing one shows a hint instead: "Something only Bodhi can find. Try sniffing." See `docs/art_and_production.md` for the reasoning.
