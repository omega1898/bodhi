// Purpose: looking after Bodhi. He gets hungry as time passes and dirty in water and mud; he
// can be fed from the food in the bag, and towelled clean. Neither ever ends the game or blocks
// the story: like a real dog, a hungry or muddy Bodhi just behaves differently (slower, nosing
// the bag, muddy paws), and looking after him warms the bond between them. The design asks for
// "ordinary affection: sharing food, drying muddy paws" between the frightening parts.
'use strict';

// How fast his needs change.
const care_rates = {
    minutes_from_fed_to_hungry: 8,   // For a grown dog. A young puppy gets hungry sooner.
    dirt_per_second_in_water: 0.08,  // Wading or swimming: muddy within about ten seconds.
    dirt_per_second_in_mud: 0.2,     // A mud puddle (water marked `mud: true`): much faster.
};
const hungry_from = 0.7;         // From here he slows a little, whines and noses the bag.
const feed_from = 0.25;          // He'll eat once he's at least this hungry.
const offer_food_from = 0.45;    // "Feed" is offered from here, so it doesn't nag after every snack.
const dirty_from = 0.55;         // From here his paws and legs show the mud.
const clean_from = 0.2;          // Towelling him only does something above this.
const largest_food_carried = 9;  // Meals the bag holds.
const hungry_whine_seconds = 30; // How often a hungry dog reminds you.

// What Bodhi is like at the start of a new game: a little hungry, clean, two meals in the bag.
function fresh_care() {
    return { hunger: 0.3, dirt: 0, food: 2 };
}

// Cleans up needs that came from outside the running game (a saved file): anything that isn't
// a number becomes the fresh value, and numbers outside their range are pulled back in. Bad
// input never reaches the game as it is.
function checked_care(care) {
    const fresh = fresh_care();
    const source = care !== null && typeof care === 'object' ? care : {};
    const number_or_fresh = function number_or_fresh(value, fallback) {
        return typeof value === 'number' && Number.isFinite(value) ? value : fallback;
    };
    return {
        hunger: clamp_between(number_or_fresh(source.hunger, fresh.hunger), 0, 1),
        dirt: clamp_between(number_or_fresh(source.dirt, fresh.dirt), 0, 1),
        food: Math.round(clamp_between(number_or_fresh(source.food, fresh.food), 0, largest_food_carried)),
    };
}

// True when he's hungry enough to show it.
function dog_is_hungry(care) {
    return care.hunger >= hungry_from;
}

// How much slower a hungry dog follows: a little, never enough to hold the player up.
function care_speed_scale(care) {
    return dog_is_hungry(care) ? 0.88 : 1;
}

// Moves his needs on by one step of the world. Paused during scripted scenes, so a film
// doesn't make him hungry.
function update_care(delta_seconds) {
    const world = game.world;
    const care = world.care;
    const dog = world.characters.dog;
    if (world.cutscene) {
        return;
    }
    // A puppy (size about 0.5) gets hungry about a quarter faster than a grown dog (size 1).
    const puppy_appetite = 1.5 - 0.5 * clamp_between(dog.size_scale || 1, 0.3, 1);
    care.hunger = clamp_between(care.hunger + delta_seconds / (care_rates.minutes_from_fed_to_hungry * 60) * puppy_appetite, 0, 1);

    // Water and mud: dirtier the longer he's in it. He shakes himself off after climbing out.
    const water = dog.in_water ? water_under(dog) : null;
    if (water) {
        const rate = water.mud ? care_rates.dirt_per_second_in_mud : care_rates.dirt_per_second_in_water;
        care.dirt = clamp_between(care.dirt + rate * delta_seconds, 0, 1);
        dog.shake_timer = 0.8;
    } else if (dog.shake_timer > 0 && dog.on_ground) {
        dog.shake_timer -= delta_seconds;
        if (dog.shake_timer <= 0) {
            shake_off(dog);
        }
    }
    dog.dirt = care.dirt;  // The drawing reads this for the muddy paws.

    // A hungry dog reminds you now and then. The first time in a chapter, he says why.
    world.hungry_whine_timer = (world.hungry_whine_timer === undefined ? 4 : world.hungry_whine_timer) - delta_seconds;
    if (dog_is_hungry(care) && world.hungry_whine_timer <= 0 && !world.reunion) {
        world.hungry_whine_timer = hungry_whine_seconds;
        play_sound('whine', distance_factor(dog), screen_pan(dog));
        if (!world.happened.has('dog_hungry')) {
            queue_lines([care.food > 0 ? '{dog} keeps nosing at the bag. Hungry.' : '{dog} is hungry, and the bag is empty. There must be food somewhere.'], 'her');
        }
        fire_event('dog_hungry');
    }
}

// The pool of water (or mud) a character is standing in, if any.
function water_under(character) {
    const middle = center_x(character);
    for (const water of game.world.water) {
        if (middle > water.x && middle < water.x + water.w) {
            return water;
        }
    }
    return null;
}

// Out of the water, he shakes himself off: a spray of drops both ways, and a little wiggle.
function shake_off(dog) {
    const world = game.world;
    for (let index = 0; index < 22; index++) {
        const side = index % 2 === 0 ? 1 : -1;
        world.particles.push({
            kind: 'drop', x: center_x(dog) + side * dog.width * 0.3, y: dog.y + dog.height * 0.4,
            velocity_x: side * (60 + Math.random() * 120), velocity_y: -60 - Math.random() * 90, gravity: 900,
            life: 0.6, max_life: 0.6, size: 2,
        });
    }
    dog.squash_x = 0.9;
    dog.squash_y = 1.08;
    play_sound('splash', distance_factor(dog) * 0.6 + 0.3, screen_pan(dog));
    fire_event('dog_shook_off');
}

// What looking after him would mean right now, if anything, for the "use" button: feeding if
// he's hungry and there's food, towelling if he's dirty. Only when they're together and she
// isn't busy with the story.
function find_care_prompt(character, dog, together) {
    const care = game.world.care;
    if (!together || character.kind !== 'human' || game.world.reunion || game.world.cutscene) {
        return null;
    }
    if (care.hunger >= offer_food_from && care.food > 0) {
        return { x: center_x(dog), y: dog.y - 26, text: personalize('Feed {dog}'), control: 'act', action: 'feed' };
    }
    if (care.dirt >= dirty_from) {  // Offered once the mud shows.
        return { x: center_x(dog), y: dog.y - 26, text: personalize('Towel {dog} clean'), control: 'act', action: 'clean' };
    }
    return null;
}

// She kneels and puts down a bowl; the dog eats with his head in it, tail going.
// `from_bag` is false for a meal at home, which doesn't use the food in the bag.
function feed_dog(from_bag) {
    const world = game.world;
    const care = world.care;
    const human = world.characters.human;
    const dog = world.characters.dog;
    if (from_bag) {
        care.food = Math.max(0, care.food - 1);
    }
    care.hunger = 0;
    human.kneeling = true;
    human.facing = Math.sign(center_x(dog) - center_x(human)) || human.facing;
    dog.facing = -human.facing;
    world.particles.push({
        kind: 'bowl', x: center_x(dog) + dog.facing * 16 * dog.size_scale, y: dog.y + dog.height,
        velocity_x: 0, velocity_y: 0, gravity: 0, life: 3.4, max_life: 3.4, size: 0.6 + 0.4 * dog.size_scale,
    });
    dog.pose_override = { pose: 'sniff', seconds: 3 };  // Head down in the bowl.
    dog.rest_seconds = 3;
    dog.wag = 1;
    world.bond.closeness = Math.min(1, world.bond.closeness + 0.05);
    world.bond.fear = Math.max(0, world.bond.fear - 0.1);
    world.hungry_whine_timer = hungry_whine_seconds;
    play_sound('pickup', 0.2, 0);
    fire_event('dog_fed');
}

// She kneels and towels the dog down (or bathes him, when `bath` is true, which gets him
// completely clean). The dog leans into it.
function clean_dog(bath) {
    const world = game.world;
    const care = world.care;
    const human = world.characters.human;
    const dog = world.characters.dog;
    care.dirt = bath ? 0 : Math.max(0, care.dirt - 0.75);
    dog.dirt = care.dirt;
    dog.shake_timer = bath ? 0.5 : 0;
    human.kneeling = true;
    human.facing = Math.sign(center_x(dog) - center_x(human)) || human.facing;
    dog.pose_override = { pose: 'lean', seconds: 2 };
    dog.rest_seconds = 2;
    dog.wag = 1;
    for (let index = 0; index < 12; index++) {
        world.particles.push({
            kind: bath ? 'drop' : 'puff', x: center_x(dog) + (Math.random() - 0.5) * dog.width, y: dog.y + Math.random() * dog.height,
            velocity_x: (Math.random() - 0.5) * 40, velocity_y: -20 - Math.random() * 30, gravity: bath ? 700 : 0,
            life: 0.7, max_life: 0.7, size: bath ? 2 : 3,
        });
    }
    world.bond.closeness = Math.min(1, world.bond.closeness + 0.03);
    play_sound(bath ? 'splash' : 'step', 0.3, 0);
    fire_event(bath ? 'dog_bathed' : 'dog_cleaned');
}

// Picks up food found in the world: tins, a bag of kibble. The bag holds only so much.
function pick_up_food(meals) {
    const care = game.world.care;
    const room = largest_food_carried - care.food;
    const taken = Math.max(0, Math.min(room, Math.round(meals)));
    care.food += taken;
    show_toast(taken > 0 ? 'Dog food  \u00b7  ' + care.food + (care.food === 1 ? ' meal' : ' meals') + ' in the bag' : 'The bag is full');
    fire_event('food_found');
}

// A few words for the pause screen and the corner of the screen: how he is right now.
// The corner shows only what needs attention, so a well looked-after dog adds nothing to it.
function care_summary(care, only_needs) {
    const parts = [];
    if (care.hunger >= hungry_from) {
        parts.push('hungry');
    } else if (!only_needs) {
        parts.push(care.hunger < feed_from ? 'fed' : 'getting peckish');
    }
    if (care.dirt >= dirty_from) {
        parts.push('muddy');
    } else if (!only_needs) {
        parts.push(care.dirt < clean_from ? 'clean' : 'a bit grubby');
    }
    if (parts.length === 0) {
        return '';
    }
    const meals = care.food + (care.food === 1 ? ' meal' : ' meals') + ' in the bag';
    return personalize('{dog}: ' + parts.join(', ')) + (only_needs && !dog_is_hungry(care) ? '' : '  \u00b7  ' + meals);
}

// Muddy paws and legs, drawn over the dog's frame (in the frame's own units, feet at 0, 0).
// `feet` are the paw positions the atlas gives for this drawing.
function draw_mud(context, dirt, feet) {
    if (!(dirt > clean_from) || !Array.isArray(feet)) {
        return;
    }
    const strength = clamp_between((dirt - clean_from) / (1 - clean_from), 0, 1);
    for (const foot of feet) {
        if (!Array.isArray(foot) || !Number.isFinite(foot[0]) || !Number.isFinite(foot[1])) {
            continue;
        }
        context.fillStyle = 'rgba(74,52,34,' + (0.7 * strength).toFixed(3) + ')';
        context.beginPath();
        context.ellipse(foot[0] + 0.8, foot[1] - 2, 3.4, 2.6, 0, 0, Math.PI * 2);
        context.fill();
        context.fillStyle = 'rgba(92,66,44,' + (0.45 * strength).toFixed(3) + ')';
        context.beginPath();
        context.ellipse(foot[0] + 0.6, foot[1] - 6, 2.6, 3.4, 0, 0, Math.PI * 2);
        context.fill();
    }
}
