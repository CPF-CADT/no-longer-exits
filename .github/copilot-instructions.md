# Copilot instructions for this repository

Quick summary
- Unity project (Editor 2022.3.47f1, URP). See [ProjectSettings/ProjectVersion.txt](ProjectSettings/ProjectVersion.txt#L1).
- Main gameplay code is under [Assets/scripts](Assets/scripts) (Manager/Controller suffixes).
- Scenes: use [Assets/Scenes/DemoGamePlay.unity](Assets/Scenes/DemoGamePlay.unity#L1) for playtesting.

Quick start (developer / agent)
- Open the project with Unity Hub/Editor 2022.3.x. Project path: repository root.
- To build from CLI (replace `UNITY_EDITOR` with your editor path):
```powershell
$env:UNITY_EDITOR = 'C:\Program Files\Unity\Hub\Editor\2022.3.47f1\Editor\Unity.exe'
& $env:UNITY_EDITOR -batchmode -quit -projectPath "$(Resolve-Path .)" -buildWindowsPlayer "$(Resolve-Path ./Build/MyGame.exe)"
```

Architecture & important patterns (read before editing)
- Managers: central singletons (e.g. `SaveManager.Instance`, `ScrollManager.Instance`). See [Assets/scripts/SaveManager.cs](Assets/scripts/SaveManager.cs#L1).
- Gameplay scripts are component-driven and attach to scene GameObjects (Inventory, NPCs, Doors, Chests). Inspect [Assets/scripts/InventorySystem.cs](Assets/scripts/InventorySystem.cs#L1) and [Assets/scripts/NPCInteract.cs](Assets/scripts/NPCInteract.cs#L1).
- Data is stored as ScriptableObjects for items: [Assets/scripts/ItemData.cs](Assets/scripts/ItemData.cs#L1) — use this pattern for new items.
- Interaction model: raycasts from `Camera.main` + TryGetComponent/GetComponentInParent. Follow existing implementations in `InventorySystem.HandleInteraction()` and `PlayerInteract`.
- Save system: JSON saved to `Application.persistentDataPath/horrorsave.json` and restores position/rotation. See `SaveManager` for respawn and controller enable/disable flow.

Project-specific conventions
- Class name suffixes: `*Manager`, `*Controller`, `*Interact` indicate responsibility and location under `Assets/scripts`.
- Inventory uses a fixed-size `ItemData[]` (5 slots). UI arrays (e.g., slot images) are used directly in code — keep inspector wiring consistent.
- Input: project uses the legacy `Input.GetKey` API in scripts (even though `com.unity.inputsystem` is present in `Packages/manifest.json`). Do not switch input APIs without updating all input code paths.
- Handled components: prefer `TryGetComponent<T>(out var t)` and `GetComponentInParent<T>()` where appropriate (existing code uses both). Mirror these idioms.

Build, test and debug notes
- Unity Editor is primary test/run environment — open scene `DemoGamePlay` for immediate playtesting.
- No CI/test harness included. If adding automated builds, use the Unity CLI pattern above and commit any build scripts under `Tools/` or `.github/workflows` if you add CI.
- To debug runtime code, attach Visual Studio (or Rider) to the Unity Editor process; breakpoints will hit in C# scripts.

Integration and external deps
- Packages are defined in `Packages/manifest.json` — notable ones: `com.unity.render-pipelines.universal`, `com.unity.inputsystem`, `com.unity.textmeshpro`, `com.unity.cinemachine`.
- Despite `inputsystem` being installed, current scripts use legacy input; check both before changing input code.

When making changes (guidelines for AI agents)
- Prefer small, focused edits. Follow naming and singleton patterns.
- When adding new assets, register ScriptableObject items like existing `ItemData` and wire inspector fields manually unless you also add editor scripts.
- Do not change global project settings (render pipeline, package versions) unless you have verified Unity compatibility (this repo expects Unity 2022.3.x LTS).

Files to inspect first (examples)
- [Assets/scripts/SaveManager.cs](Assets/scripts/SaveManager.cs#L1)
- [Assets/scripts/InventorySystem.cs](Assets/scripts/InventorySystem.cs#L1)
- [Assets/scripts/ItemData.cs](Assets/scripts/ItemData.cs#L1)
- [Packages/manifest.json](Packages/manifest.json#L1)

If anything here looks incomplete or you want more detail (for example, wiring an editor script, automated build pipeline, or specific scene flow), tell me which area and I will expand the instructions.
