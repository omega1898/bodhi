# Walkthrough of `core/`

Purpose: a guided reading of every file in `core/`, in an order where each file only uses ideas already explained. Each C++ term is defined in **bold** the first time it appears. Keep the real file open next to this page and read the two together.

Reading order:

1. `core/CMakeLists.txt`: how the code is built
2. `chapter_beat.hpp` / `.cpp`: the list of story beats and the legal order between them
3. `bond_state.hpp` / `.cpp`: the dog's feelings
4. `event_bus.hpp` / `.cpp`: how "something happened" messages travel
5. `telemetry_log.hpp` / `.cpp`: the playtest log
6. `railway_chapter.hpp` / `.cpp`: chapter one, built from the four pieces above
7. `tests/test_harness.hpp`, `tests/test_main.cpp`: the testing tool
8. The five `tests/test_*.cpp` files
9. `tools/chapter_demo.cpp`: a playable run with no graphics

Before you start, two ideas that every file relies on:

- A **header file** (`.hpp`) *declares* things: it says "a function called `beat_name` exists, it takes a beat and gives back text". A **source file** (`.cpp`) *defines* them: it holds the actual steps. Other files read only the header, so they can use the function without seeing how it works.
- A **compiler** (here `g++` or `clang++`) turns `.cpp` files into machine code. A **linker** then joins the pieces into one program.

---

## 1. `core/CMakeLists.txt`

This isn't C++. It's a recipe for **CMake**, a tool that writes the build instructions the compiler follows. **Ninja** (the `-G Ninja` in the commands) is the tool that actually runs them, quickly.

| Lines | What it does | Why |
|---|---|---|
| 1–6 | Comments (`#` in CMake) with the three build commands. | So nobody has to guess how to build. |
| 7–8 | Requires CMake 3.20 or newer and names the project. `LANGUAGES CXX` means C++ only. | Older CMake lacks features used below. |
| 10–12 | Use C++17 exactly: required, and with no compiler-specific extras (`EXTENSIONS OFF`). | Unreal's compiler on the Mac must accept the same code. Extras that work on Linux might fail there. |
| 14–21 | `add_library(bodhi_core …)` builds the five `.cpp` files into one **library**, a bundle of compiled code that other programs reuse. `target_include_directories(… PUBLIC include)` tells everyone who uses the library to look in `core/include/` for headers. | Tests, the demo and later Unreal all reuse the same compiled rules. |
| 23–31 | On compilers other than Microsoft's (`MSVC`): `-fno-exceptions -fno-rtti` turns off two C++ features (see CLAUDE.md, rule 2). `-Wall -Wextra -Wpedantic -Wshadow` turns on many warnings, and `-Werror` makes each warning stop the build. | Unreal builds without exceptions and RTTI, so code that needs them fails here, early, in the cloud. A warning that stops the build can't be ignored. |
| 33–39 | An optional switch, `BODHI_SANITIZE`. When it's on, the program is built with **AddressSanitizer** (catches reads or writes to memory the program doesn't own) and **UndefinedBehaviorSanitizer** (catches operations C++ gives no meaning to, such as some integer overflows). `-g` keeps names and line numbers for error reports. | Memory bugs often look fine in tests and crash later on a phone. Sanitizers make them fail loudly now. |
| 41–52 | `enable_testing()` switches on **CTest**, CMake's test runner. `add_executable(core_tests …)` builds a program from the test files. `target_link_libraries(… PRIVATE bodhi_core)` connects it to the library. `add_test` registers it with CTest. | `ctest` runs it and reports pass or fail from its **exit code** (0 means success, anything else means failure). |
| 54–56 | The same for the demo. Its test passes only if the demo reaches the last beat. | The demo doubles as a full end-to-end test. |

---

## 2. `include/bodhi/chapter_beat.hpp`

```cpp
// chapter_beat.hpp
// Purpose: ...
```
Everything after `//` on a line is a **comment**: notes for people, which the compiler ignores.

```cpp
#pragma once
```
**`#pragma once`**: "include this file at most once per `.cpp` file". Lines starting with `#` are handled by the **preprocessor**, a step that runs before compiling and pastes text together. Without this line, a file that pulls in this header twice (directly and through another header) would declare everything twice, and the build fails.

```cpp
#include <cstddef>
```
**`#include`**: paste the contents of another file here. Angle brackets `<…>` mean a file from the **standard library** (the code that comes with every C++ compiler). Quotes `"…"` mean a file from this project. `<cstddef>` provides `std::size_t` (explained below).

```cpp
namespace bodhi {
...
}  // namespace bodhi
```
**namespace**: a named container for names. Everything inside is really called `bodhi::beat_name`, `bodhi::chapter_beat` and so on. This stops our names clashing with Unreal's or the standard library's. The standard library lives in the namespace `std`, which is why you see `std::string`. The `::` operator means "the name on the right, inside the container on the left".

```cpp
enum class chapter_beat {
    together,
    separation,
    ...
    aftermath,
};
```
**`enum class`**: a new type whose only values are the names listed. A **type** says what kind of value something is: a whole number (`int`), a decimal number (`double`), true/false (`bool`). A variable of type `chapter_beat` holds one of the eight beats. You write the values as `chapter_beat::together`.

`class` is what makes it "scoped". It forces the full name `chapter_beat::together`, and it stops a beat being silently used as a plain number. (Behind the scenes each value *is* a number, starting at 0. `static_cast` can force one out, which the tests do on purpose.)

```cpp
constexpr std::size_t chapter_beat_count = 8;
```
- **`std::size_t`**: a whole-number type that can't be negative and is large enough to count anything in memory. It's the standard type for sizes and positions in lists.
- **`constexpr`**: the value is fixed when the program is compiled and can never change. That lets it set the size of an array (see `railway_chapter.hpp`).
- Weak spot: if someone adds a ninth beat and forgets to update this, the array is one slot short. `beat_to_index` (section 6) stops that from reading outside memory, but the count would still be wrong.

```cpp
const char* beat_name(chapter_beat beat);
bool is_allowed_transition(chapter_beat from_beat, chapter_beat to_beat);
```
Two **function declarations**. A **function** is a named list of steps. It takes **parameters** (inputs, in the brackets) and gives back a **return value** (its type is written first).

- **`const`**: "can't be changed through this name". It's used throughout the code to promise "I only read this".
- **`const char*`**: a **pointer** (the `*`) to a run of `char` (single characters) that can't be changed. A pointer is a memory address, "the text is stored *there*". This is the oldest, simplest way to pass fixed text in C++, and it's used here for text written directly in the code (a **string literal** such as `"together"`).

The `;` with no body means "declared here, defined elsewhere".

## 3. `src/chapter_beat.cpp`

```cpp
#include "bodhi/chapter_beat.hpp"
```
The source file includes its own header first. If the header forgot an `#include` it needs, this file fails to compile and shows the mistake straight away.

**`beat_name` (lines 7–20)**: a **`switch`** compares one value against several `case`s and jumps to the match. Each `case` **returns** (hands back) a fixed piece of text. Because each case returns, no `break` is needed.

Line 19, `return "unknown";`, runs only when the value matches no real beat. That can happen if someone forces a number such as 99 into the type. Without this line the function would end without returning anything, which is **undefined behavior**: C++ promises nothing about what happens next, and the program may crash or print garbage. (`-Wall` would also warn about it, and `-Werror` would stop the build.)

**`is_allowed_transition` (lines 22–46)**:
- **`struct`**: a type that groups several named values (**members**) into one. Here a `beat_step` holds a `from_beat` and a `to_beat`. The struct is declared inside the function, so only this function can see it.
- `static const beat_step allowed_steps[] = { … };` is an **array**: a fixed-length row of values of one type, one after another in memory. The `[]` lets the compiler count the entries. **`static`** inside a function means "build this once and keep it", instead of rebuilding it on every call. Each `{a, b}` fills one `beat_step` in member order. This is called **brace initialization**.
- `for (const beat_step& step : allowed_steps)` is a **range-based for loop**: "for each `step` in `allowed_steps`". The **`&`** makes `step` a **reference**: another name for the existing value, not a copy. `const &` means "look at the original, don't copy it, don't change it". Copying would be wasted work, and for big values (long text) that waste adds up.
- `==` compares, `&&` means "and". If a matching row is found, the function returns `true`. Otherwise it returns `false`.

Why a table instead of `if` statements: a table is data. A designer can read it, a test can check it, and anything **not** listed is refused by default. This is the "allow list" pattern from security: say what's allowed, not what's forbidden.

---

## 4. `include/bodhi/bond_state.hpp`

```cpp
#include <string>
```
This brings in **`std::string`**, a piece of text that manages its own memory. It grows when you add to it and tidies up after itself. That's safer than raw `const char*`, which can't tell you how long it is and can't grow.

```cpp
struct bond_state {
    double closeness_need = 0.2;
    double fear = 0.0;
    double trust = 0.8;
};
```
Three decimal numbers (`double`), each with a **default member value**. Every new `bond_state` starts as "a calm dog that wants to stay a little close and trusts the human a lot".

```cpp
void apply_bond_event(bond_state& bond, const std::string& event_name);
```
- `void` means the function returns nothing.
- `bond_state& bond` is a reference *without* `const`, so the function **changes the caller's own** `bond_state`. That's how it "returns" its result.
- `const std::string& event_name` is read-only and not copied.

## 5. `src/bond_state.cpp`

- `<algorithm>` provides `std::clamp` and `std::max`. `<cmath>` provides `std::isnan`.
- `namespace {` with **no name** (line 9) is an **anonymous namespace**. Names inside it are visible only in this one `.cpp` file. They're private helpers that can't clash with a helper of the same name elsewhere.
- **`bond_rule` and `bond_rules[]` (lines 12–28)**: each row is "when this event happens, add these amounts to each feeling, and make closeness need at least this much". Positive numbers raise a feeling and negative numbers lower it. All the tuning sits in one table.
- **`keep_in_unit_range` (lines 32–37)**: `std::isnan` checks for **NaN** ("not a number"), a special `double` value produced by maths such as 0 ÷ 0. Any sum that touches NaN is also NaN, so one bad value would spread to every feeling forever. This function turns NaN into 0. `std::clamp(value, 0.0, 1.0)` then limits the value to between 0 and 1.
- **`apply_bond_event` (lines 41–52)**: it walks the table. `continue` skips to the next row when the name doesn't match. (`!=` means "not equal". Comparing a `std::string` with a `const char*` compares the text.) On a match it adds each change, clamps, and `return`s, so at most one rule applies. For closeness, `std::max(a, b)` picks the larger of the two, which enforces the "at least" floor. If no row matches, nothing changes: unknown events are ignored, not errors.

---

## 6. `include/bodhi/event_bus.hpp`

The idea first: an **event bus** is a noticeboard. One part of the game posts "lever_pulled". Anyone who signed up for that name gets told. The lever doesn't need to know who's listening. This is called the **publish/subscribe** (or *observer*) pattern. It keeps parts of the game separate, so you can add a reaction without editing the lever.

Includes:
- **`<functional>`** provides **`std::function`**.
- **`<vector>`** provides **`std::vector`**: a list that can grow, like a row of boxes you can add to. `std::vector<int>` is a list of whole numbers. The type inside `<…>` says what the list holds.
- **`<unordered_map>`** provides `std::unordered_map`: a lookup table from a **key** to a **value**, like a dictionary from word to definition.

```cpp
struct game_event {
    std::string name;
};
```
For now an event is only a name. Wrapping it in a struct means fields such as a position or a strength can be added later without changing every function that passes events around.

```cpp
class event_bus {
public:
    ...
private:
    ...
};
```
**`class`**: like a struct, but it pairs data with **member functions** (functions that belong to the object and work on its data). The only real difference from `struct` in C++ is the default visibility. Members of a class are hidden unless marked otherwise. This codebase uses `struct` for "plain bundle of data" and `class` for "object with rules that protect its data".
- **`public:`** is everything other code may use.
- **`private:`** is everything only the class's own functions may touch. Outside code can't, for example, reset `dropped_events` to zero to hide a loop.

```cpp
using listener = std::function<void(const game_event&)>;
```
- **`using X = Y;`** gives a type a shorter name (a **type alias**).
- **`std::function<void(const game_event&)>`**: a box that can hold *any* callable thing (a function, or a lambda; see section 9) that takes a `const game_event&` and returns nothing. That's how the bus can store reactions without knowing what they are.

The member functions:
- `subscribe(name, on_event)`: "call `on_event` when an event with this name is published".
- `subscribe_to_all(on_event)`: "call it for every event". These run first, so the log records the cause before its effects.
- `publish(event)`: deliver an event. It returns `false` if the event was dropped by the loop guard.
- `int dropped_event_count() const;`: the **`const` after the brackets** means this member function promises not to change the object. You can call it on a `const event_bus`.

The private data:
- `static constexpr int maximum_publish_depth = 16;`: **`static`** on a class member means one shared value for the class, not one per object.
- `std::unordered_map<std::string, std::vector<listener>> listeners_by_event_name;`: event name → list of listeners for that name.
- `std::vector<listener> listeners_for_all_events;`
- Two counters, both starting at 0.

## 7. `src/event_bus.cpp`

- `<utility>` provides **`std::move`**.
- `void event_bus::subscribe(...)` uses `event_bus::` in front of the name, meaning "this is the definition of the `subscribe` that belongs to `event_bus`".
- **Line 10**: `listeners_by_event_name[event_name]` looks up the list for this name. If none exists, the `[]` creates an empty one. `.push_back(x)` adds `x` to the end of a vector. `std::move(on_event)` says "I'm finished with `on_event`, take its contents instead of copying them", which avoids a needless copy.
- **`publish`, lines 21–24, the loop guard**: `current_publish_depth` counts how many `publish` calls are running inside each other right now. If listeners keep publishing (A causes B causes A …), the depth reaches 16, and the event is counted and dropped. Without this, the calls would nest until the program ran out of **stack** (the memory used to track function calls in progress) and crashed.
- **Lines 28–33, copy before you loop**: `const std::vector<listener> general_listeners = listeners_for_all_events;` makes a *copy* of the list. Why: a listener might call `subscribe` while we're looping. Adding to a vector can move its contents somewhere else in memory, and a loop still walking the old spot would read freed memory. That's a classic crash bug, called "iterator invalidation". The test "A listener that subscribes more listeners while running" exists to catch it.
  - **`auto`** lets the compiler work out the type (here it's long and unhelpful to read).
  - `.find(event.name)` searches the map. It returns an **iterator** (a position marker in the map), or the special `.end()` position when the name isn't there.
  - `found->second`: `->` reaches through the marker to the entry it points at. Each map entry is a pair: `first` is the key (the name) and `second` is the value (the list).
- **Line 36**: copy the event too, in case a listener changes the original while the others are still using it.
- **Lines 38–46**: depth up, run the general listeners, then the named ones, depth down, return `true`. `on_event(event_copy)` *calls* the stored function.

---

## 8. `include/bodhi/telemetry_log.hpp` and `src/telemetry_log.cpp`

**Telemetry** is data a program records about itself. Here it's a timed diary of the chapter, used in playtests to measure things like "how long did players stay at the fence".

Header:
- `maximum_time_seconds = 1.0e7`: written as `1.0e7`, which means 1.0 × 10⁷ = 10,000,000 seconds, about 115 days. A larger time is treated as broken.
- `struct telemetry_entry`: one diary line with a time, a `kind` (category) and a `detail`.
- `explicit telemetry_log(std::size_t entry_limit = 100000);` is a **constructor**: a special member function with the class's own name that runs automatically when an object is created, to set it up. `= 100000` is a **default argument**: `telemetry_log log;` gets 100,000, and `telemetry_log log(3);` gets 3. **`explicit`** stops C++ silently turning a plain number into a whole log where a log was expected, which would hide mistakes.
- `const std::vector<telemetry_entry>& entries() const;` returns a **const reference** to the private list. Callers can read every entry without a copy, but can't add, remove or edit any.
- `escape_json_string` is declared outside the class because it's a general tool, not part of one log.

Source:
- **Line 10**: `: maximum_entries(entry_limit) {}` is a **member initializer list**. It sets members before the constructor's body `{}` runs, and the body is empty.
- **`record`, lines 12–26**: two checks, then a store.
  1. The time must be usable. `std::isfinite` is false for NaN and for infinity. The time must also be ≥ 0, ≤ the maximum, and not earlier than the last entry. `||` means "or". `recorded_entries.empty() || …` reads "either there's no previous entry, or the new time isn't earlier than the last one". `.back()` is the last entry. C++ checks the left side of `||` first and skips the right side if the left is true, so `.back()` is never called on an empty list (which would be undefined behavior).
  2. The log mustn't be full. That's the memory cap: a runaway loop can't fill a phone's memory.

  Failures add to a counter and return `false`, with no crash. Success uses `push_back({time_seconds, kind, detail})`, which builds a `telemetry_entry` from braces.
- **`to_json_lines`, lines 40–56**: **JSON** is a common text format for data (`{"key":"value"}`). **JSON Lines** means one JSON object per line. `char time_text[32];` is a fixed 32-character buffer. `std::snprintf` writes formatted text into it (`%.3f` means a decimal number with 3 places). The **`n`** means it never writes more than `sizeof(time_text)` characters, so the buffer can't overflow. `+=` adds text to the end of a string. `\"` inside a string literal is a literal quote mark, and `\n` is a newline. (The backslash starts an **escape sequence**.)
- **`escape_json_string`, lines 58–82**: this is the **injection** defence. Suppose an event name is `x","kind":"admin`. Pasted raw into `"detail":"…"`, its quote mark would close the string early, and `"kind":"admin"` would become a real, fake field. The function walks every byte and rewrites the dangerous ones: `"` becomes `\"`, `\` becomes `\\`, and newline, carriage return and tab become `\n`, `\r`, `\t`. Any other invisible control byte (value below `0x20`, which is 32 written in **hexadecimal**, base 16) becomes `\u00XX`.
  - `.reserve(text.size())` sets aside memory up front, so the string doesn't have to grow again and again.
  - **`static_cast<unsigned char>(text_byte)`**: `static_cast<T>(x)` converts `x` to type `T` on purpose and visibly. On many machines `char` can be negative (bytes 128–255 read as −128 to −1). Without the conversion, those bytes would compare as "below 0x20" and be wrongly escaped.
  - Known gap (also in `docs/architecture.md`): invalid UTF-8 text is passed through unchanged.

---

## 9. `include/bodhi/railway_chapter.hpp`

This is the one object Unreal will talk to. It's the **trust boundary**: everything entering through its public functions is treated as possibly wrong or hostile.

- `<array>` provides `std::array<T, N>`, a fixed-size array of `N` values of type `T` that knows its own size. It's safer than a bare `T name[N]`.
- `#include "bodhi/…"`: the four pieces this chapter is built from.
- `enum class character { human, dog };` says which character the player controls.
- Three free functions (not members): `character_name`, `is_valid_event_name`, `is_internal_event_name`. They're public so tests can check them directly.
- `railway_chapter();` declares the constructor.
- **Lines 37–38**: `railway_chapter(const railway_chapter&) = delete;` and `railway_chapter& operator=(const railway_chapter&) = delete;` The first is the **copy constructor** (it makes a new object as a copy). The second is **copy assignment** (`a = b`; `operator=` is how C++ names the function behind `=`). **`= delete`** removes them, so any attempt to copy fails to compile. Why: the listeners stored in the bus remember `this` (see below), which is the address of the original object. A copy's listeners would still change the *original*, a silent and confusing bug. Banning copies turns that bug into a compile error.
- Public functions: two that take input (`report_world_event`, `request_character_switch`) and several read-only "getters" marked `const`.
- Private: helper functions, then the object's data. The bus, log and bond are kept *inside* the chapter (this is called **composition**: building an object out of other objects). `std::array<double, chapter_beat_count> finished_seconds_by_beat{};` holds one running total per beat. The empty `{}` sets every slot to 0.0.

## 10. `src/railway_chapter.cpp`

**Lines 9–62, private tables and helpers (anonymous namespace):**
- `beat_triggers[]`: "in beat X, event Y moves the story to beat Z". Note that `pair_met_at_fence` works from either section.
- `route_links[]`: an action on one route *causes* an effect on the other. The human restoring power opens the dog's gate, and the dog's bark reveals the human's route.
- `const char* const human_final_action = …;` has two `const`s: the text can't change (first `const`), and the pointer can't be aimed at different text (second `const`).
- `internal_event_names[]`: the effect events. Only the core may announce them.
- `beat_to_index`: converts a beat into an array position, but first checks it's `< chapter_beat_count`. It returns success as a `bool` and writes the answer into a reference parameter (`std::size_t& index`). Without the check, `static_cast<chapter_beat>(99)` would read memory past the end of the array.

**Lines 66–91, the free functions:**
- `character_name` uses the **ternary operator** `condition ? a : b`, meaning "if condition then a, else b".
- `is_valid_event_name` checks 1 to 64 characters, and only `a–z`, `0–9` and `_`. Characters are compared like numbers: `'a'`…`'z'` is a continuous range. Single quotes `'a'` mean one character, and double quotes `"a"` mean text. Another allow list: uppercase letters, quotes, spaces and anything unexpected are refused.
- `is_internal_event_name` walks the internal list.

**Lines 93–96, the constructor:** it connects the rules to the bus, then logs the starting beat at time 0.

**Lines 98–117, `wire_event_rules`, where lambdas appear:**
```cpp
bus.subscribe_to_all([this](const game_event& event) {
    log.record(latest_time_seconds, "world_event", event.name);
    apply_bond_event(dog_bond, event.name);
    advance_story(event.name);
});
```
A **lambda** is a function written in place, with no name. Its parts:
- `[ … ]` is the **capture list**: values from the surrounding code the lambda may use later. **`this`** is a pointer every member function has to "the object I belong to". Capturing it lets the lambda reach `log`, `dog_bond` and `advance_story` of *this* chapter. (It's also why copying is banned; see section 9.)
- `(const game_event& event)` are the parameters, as for any function.
- `{ … }` is the body.

The lambda is stored inside a `std::function` in the bus and runs on every event. That gives this order: log it, update the dog's feelings, check the story.

The route-link loop (lines 107–112) captures `[this, effect_event]`. `effect_event` is captured **by value** (copied into the lambda) as a `std::string`. Why a copy: the loop variable `link` disappears after each turn of the loop, and the lambda runs much later. The lambda's body publishes the effect: `bus.publish({effect_event})` builds a `game_event` from braces.

Lines 115–116: the two final-obstacle actions call `handle_final_action(true)` for the human and `(false)` for the dog.

**Lines 119–134, `report_world_event`, the front door.** It runs three checks in order, and each refusal is counted and logged.
1. Bad name: refused. `.substr(0, 64)` takes the first 64 characters, so a 100,000-character name can't flood the log.
2. Internal name from outside: refused. This stops a buggy or tampered Blueprint from announcing `shared_route_opened` and skipping the final obstacle.
3. Bad time (NaN, too large, or backwards): refused.

Only then is the event published. **`!`** means "not".

**Lines 136–147, `request_character_switch`:** time check, then "only during the final obstacle", then swap characters.

**Lines 149–157, `accept_time`:** the same idea as the log's time check. On success it moves the chapter clock forward.

**Lines 159–162, `refuse_input`:** add 1 to the counter and log `reason:detail`. (`+` joins strings.)

**Lines 164–173, `advance_story`:** it finds the *first* trigger that matches the current beat and event, enters that beat, and **returns immediately**. The comment explains why. If the loop kept going after `beat` changed, a later row could match the *new* beat with the same event and skip two beats at once.

**Lines 175–200, `enter_beat`:**
1. A second check against `is_allowed_transition` (**defence in depth**: two independent locks, so one mistake in a table isn't enough to break the story).
2. Add the time spent in the beat that's ending to its total.
3. Switch to the new beat, note when it started, and log it.
4. Decide control: the dog in `dog_section`, the player's choice in `near_contact` and `final_obstacle`, otherwise the human. Here `switch` uses `break` to leave, and two `case`s that share one `break` both do nothing. **`default:`** catches every other value.

**Lines 202–208, `take_control_of`:** it logs only real changes, so the log has no noise.

**Lines 210–222, `handle_final_action`:** this does nothing outside the final obstacle, so a debris clear earlier in the chapter doesn't count later. It marks the action done. When both are done, the core *itself* publishes `shared_route_opened`, which moves the story to the reunion through `beat_triggers`. Because it's published straight to the bus, it skips the front-door refusal of internal names. That's on purpose: only the core can reach the bus.

**Lines 224–254, getters and `seconds_spent_in`:** the getters return the value, or a const reference. `seconds_spent_in` refuses a bad beat value, takes the stored total, and adds the time spent so far if that beat is the current one.

**A worked example, tracing `report_world_event("maintenance_power_restored", 140.0)`:**
1. The name is valid, not internal, and the time 140 ≥ 75 is accepted.
2. `publish("maintenance_power_restored")`, depth 1. The all-events lambda logs it, the bond is unchanged (no rule), and the story is unchanged (no trigger from `human_section` for this name).
3. Then the named route-link lambda runs: `publish("dog_gate_opened")`, depth 2. It's logged, and `advance_story` finds `human_section → dog_section`. `enter_beat` logs the beat and hands control to the dog.

The demo's log shows exactly these lines at 140.000.

---

## 11. `tests/test_harness.hpp` and `tests/test_main.cpp`

- **`inline int failure_count = 0;`**: an **`inline` variable** (a C++17 feature) can be defined in a header that many `.cpp` files include and still be one single shared variable. Without `inline`, every file that includes the header would define its own variable with the same name, and the linker would refuse the duplicates.
- **`#define CHECK(condition) …`** is a **macro**: the preprocessor pastes this text wherever `CHECK(…)` is written, before compiling. A normal function can't do what this one does:
  - `__FILE__` and `__LINE__` are filled in with the file and line *where CHECK was written*.
  - `#condition` turns the code you wrote into text, so the failure message shows `bond.fear > 0.5` itself.
  - `do { … } while (0)` runs the body exactly once. It's a standard trick that makes the macro behave like a single statement, even right after an `if` with no braces. The `\` at each line end continues the macro onto the next line.

  CLAUDE.md allows macros only when justified, and the comment above it gives the reason.
- The five `void run_…_tests();` declarations let `test_main.cpp` call functions defined in other files.
- **`test_main.cpp`**: `int main()` is where every C++ program starts. It runs each group, prints `N checks, M failed` with `std::printf` (`%d` means "a whole number goes here"), and returns 0 only if nothing failed. That exit code is how CTest knows the result. The current result is `73 checks, 0 failed`.

## 12. The test files

Each test sits in its own `{ … }` block. A block is a **scope**: variables made inside it are destroyed at the closing brace, so every test starts with a fresh object. `using bodhi::bond_state;` at the top lets the file write `bond_state` without the `bodhi::` prefix.

- **`test_chapter_beat.cpp`**: the designed steps are allowed. Shortcuts, backward steps and "stay in place" are refused. `std::strcmp(a, b) == 0` (from `<cstring>`) compares two `const char*` texts and returns 0 when they're equal. `static_cast<chapter_beat>(99)` forces a fake beat to prove `beat_name` returns `"unknown"`.
- **`test_bond_state.cpp`**: the collapse raises fear and closeness, and the reunion forces closeness to at least 0.9. **Break it**: 1,000 repeats each way, and the values stay within 0–1. Unknown names change nothing.
- **`test_event_bus.cpp`**: here lambdas capture **by reference**: `[&lever_count]` lets the lambda change the test's own counter. (Without `&`, it would change a private copy.) `const game_event&` with no parameter name means "I receive an event but don't use it". The tests check: delivery only to matching names; publishing with no listener is fine; the order "all" then "named"; subscribing during a publish doesn't crash (run the sanitizer build to prove no hidden memory error); and **break it**: ping → pong → ping … is stopped, with exactly one event dropped at depth 16.
- **`test_telemetry_log.cpp`**: `<limits>` provides `std::numeric_limits<double>::quiet_NaN()` and `::infinity()`, special values built on purpose to attack the time check. It tests normal entries, five kinds of broken time, the size cap (3 kept, 7 dropped), each escape rule, and **break it**: the injection string ends up harmless inside `"detail"`. The expected output is hard to read because each `\` must itself be escaped in C++ source. `\\\"` in the code means the two characters `\"` in the output.
- **`test_railway_chapter.cpp`**: a helper in an anonymous namespace plays up to the final obstacle. The parameter `railway_chapter& chapter` is a reference, because copying is deleted (and a copy would be pointless anyway). The main test walks the full designed path and checks beat, control, feelings and the 45 seconds at the fence. The **break it** tests cover: an event too early does nothing; announcing `shared_route_opened` from outside is refused; one route's action alone (even twice) isn't enough; an early `debris_cleared` doesn't count later; switching outside the final obstacle is refused; five bad inputs (empty name, uppercase, injection, 100,000 characters, backwards time) are refused and counted; and a fake beat can't read outside the array.

## 13. `tools/chapter_demo.cpp`

A small program that plays a scripted chapter. `struct scripted_event` is declared inside `main`, and `const scripted_event script[]` is the script. One entry, `"switch_character"`, isn't a world event: the loop spots it with `std::strcmp` and calls `request_character_switch` instead.

At the end:
- `std::fputs(text.c_str(), stdout)` prints the JSON log to **standard output**. `.c_str()` hands a `std::string` to an older function that expects a `const char*`.
- `std::fprintf(stderr, …)` prints a one-line summary to **standard error**, a second output channel, so the log stays clean to save or analyse (`%s` means "text goes here", and `%.2f` is a decimal with 2 places).
- The return code is 0 only if the chapter reached `aftermath`, which is what CTest checks.

Real output today ends with: `final beat: aftermath | fence seconds: 45.0 | fear 0.00 closeness 0.70 trust 1.00`. Closeness is 0.70, not 0.9 or more, because `rested_together` (−0.2) comes after the reunion.

Try it yourself:
```
cmake -S core -B core/build -G Ninja && cmake --build core/build
./core/build/chapter_demo
```
You should see 29 JSON lines, starting with `{"time_seconds":0.000,"kind":"beat_entered","detail":"together"}`, plus the summary line. The summary may appear first: standard error is printed straight away, while standard output is held back and printed in batches.

---

## 14. Check yourself

Answer these out loud or on paper before opening the answers.

1. What's the difference between a `.hpp` and a `.cpp` file, and what would go wrong without `#pragma once`?
2. Why is `chapter_beat` an `enum class` rather than plain numbers 0–7?
3. In `void apply_bond_event(bond_state& bond, const std::string& event_name)`, what does each `&` do, and why is only one of them `const`?
4. What's the difference between a `struct` and a `class` in C++, and how does this codebase choose between them?
5. Why does `event_bus::publish` copy the listener lists before looping over them?
6. What stops ping → pong → ping … from crashing the game, and what number does `dropped_event_count()` show afterwards?
7. In `[this, effect_event](const game_event&) { bus.publish({effect_event}); }`, name the three parts of the lambda, and explain why `effect_event` is copied rather than referenced.
8. Why is copying a `railway_chapter` deleted?
9. List, in order, the three checks `report_world_event` makes, and give one attack each one stops.
10. A detail of `x","kind":"admin` is logged. Without `escape_json_string`, what would the JSON line look like and why is that dangerous?

<details>
<summary>Answers (open after you've tried)</summary>

1. A header *declares* (names and shapes, so other files can use them). A source file *defines* (the actual steps). Without `#pragma once`, a header included twice into one `.cpp` (directly and through another header) would declare everything twice. That's a compile error for types such as `struct bond_state`.

2. `enum class` gives the values names (`chapter_beat::reunion`), keeps those names inside the type so they don't clash, and refuses silent conversion to and from numbers. A mistake like passing a time where a beat was expected fails to compile. A beat can only be forced from a number on purpose, with `static_cast`.

3. Both `&` make references: the function works on the caller's original, with no copy. `bond` isn't `const` because the function's whole job is to change the dog's feelings. `event_name` is `const` because the function only reads it, and the compiler then enforces that promise.

4. The only language difference is the default visibility: struct members are `public` unless marked otherwise, and class members are `private`. This codebase uses `struct` for plain bundles of data (`bond_state`, `game_event`, `telemetry_entry`) and `class` for objects whose data must be protected by rules (`event_bus`, `telemetry_log`, `railway_chapter`).

5. A listener may call `subscribe` while the loop runs. Adding to a vector can move its contents to new memory, so a loop still walking the old memory would read freed memory (undefined behavior, often a crash). Looping over a copy makes that impossible. New listeners start with the next publish.

6. `current_publish_depth` counts nested `publish` calls. When it reaches `maximum_publish_depth` (16), the next publish is refused and counted instead of nesting until the stack runs out. The chain stops there and unwinds, so `dropped_event_count()` is 1.

7. Capture list `[this, effect_event]`, parameters `(const game_event&)`, body `{ bus.publish({effect_event}); }`. `effect_event` is a local variable inside the loop and is destroyed when that turn of the loop ends. The lambda runs much later, when the trigger event happens. A reference would then point at destroyed memory, so the lambda keeps its own copy.

8. The lambdas inside the bus captured `this`, the address of the original chapter. A copy would carry those same lambdas, so events published on the copy would change the *original's* log, bond and beat. Deleting the copy constructor and copy assignment turns that silent bug into a compile error.

9. (1) Valid name: 1–64 lowercase letters, digits or underscores. This stops oversized names flooding memory or the log, and quote-based injection strings. (2) Not an internal name. This stops a buggy or tampered engine call announcing `shared_route_opened` to skip the final obstacle. (3) A usable time: finite, not backwards, at most 10⁷ seconds. This stops NaN or backwards clocks corrupting durations such as "seconds at the fence".

10. It would become `{"time_seconds":1.000,"kind":"world_event","detail":"x","kind":"admin"}`. The quote mark closes the `detail` string early, and a second, fake `kind` field appears. Many JSON readers keep the *last* duplicate key, so analysis would see `kind = admin`. That's an injection: outside text changing the structure of trusted data. Escaping turns the quote into `\"`, so the whole text stays inside `detail`.

</details>
