# Ho regression validation

Use an isolated Built-in Render Pipeline Unity project with graphics enabled.
Copy `Assets/lilToon` into its `Assets`, and the contents of `Tests~/Editor` and
`Tests~/Shaders` into `Assets/HoValidation/Editor` and `Assets/HoValidation/Shaders`.

Run the editor with:

```text
Unity.exe -batchmode -force-d3d11 -projectPath <isolated-project> -executeMethod LilToonHoValidation.Run -logFile <log-path>
```

The harness regenerates shaders through lilToon's generator, synchronously compiles
the tested passes, checks persistent material variants (Unity 2022.1+), reads GPU
pixels from the production HLSL functions, and checks texture feature scanning.
It writes `HoValidation-results.txt` in the test project and exits with 0 on success.
Pass `-ho-verify-generated` to check the supplied generated shaders without
regenerating them. Do not pass `-nographics` because GPU readback is required.

Unity 2021 skips Material Variant tests because that editor has no Material Variant
API. The production code isolates these APIs with `UNITY_2022_1_OR_NEWER`.
This harness does not perform VRChat uploads, Warudo runtime loading, interactive
Inspector clicking, or XR device rendering.

For UPM validation, install lilToon as a local or Git dependency instead of copying
it under `Assets`. Change the fixture shader's `Assets/lilToon/` include prefix to
`Packages/jp.lilxyzw.liltoon/`, and run `LilToonHoUpmValidation.Run`. This also checks
physical package path resolution, version discovery, language enumeration, culture
mapping and translation loading before running the full regression harness.
