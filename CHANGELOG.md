# Changelog

## 1.0.0 — Initial engine-agnostic release

Extracted the engine-agnostic pieces of `com.crumpetlabs.buttr` 2.1.4 into a
standalone .NET library. See `README.md` for the repository layout.

### Highlights

- Pure C# — zero Unity dependencies.
- Targets `netstandard2.1`; compatible with Unity 6000 and Godot 4 (.NET).
- New `ButtrLog` facade replaces direct `UnityEngine.Debug` calls.
- `CMDArgs.Initialize(IEnumerable<string>)` replaces the Unity runtime-init hook.
- `InjectionProcessor` is now a pure registry + single-instance inject — Unity
  scene-walking helpers live in `Buttr.Unity`.
