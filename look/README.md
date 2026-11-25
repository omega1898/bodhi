# Bodhi look (`com.bodhi.look`)

The game's palette and motion maths in plain C#, from the art bible (`docs/unity_art_bible.md`). There's no Unity code in here, so it's tested anywhere with the same command as the rules:

```
dotnet test rules_dotnet/Bodhi.Rules.Tests
```

- `Runtime/Palette.cs`: the twelve colours, the single source every material, light, particle and screen reads from. It also has the contrast measure the tests use to keep both characters readable against the dark.
- `Runtime/Motion.cs`: springs (ears, tail, hair, scarf) and the camera's smooth follow, ported from `prototype/web/js/motion.js`. Both move identically at 30, 60 and 120 frames a second.

The Unity scripts that turn these numbers into colours, lights and bone rotations come once the Unity project exists. Add this package the same way as `rules/` (see `rules/README.md`). The same rules apply too: C# 9, .NET Standard 2.1, tests first.
