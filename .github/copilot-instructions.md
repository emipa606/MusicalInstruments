# GitHub Copilot Instructions for Musical Instruments (Continued) Mod Development

## Mod Overview and Purpose

The **Musical Instruments (Continued)** mod enhances the RimWorld experience by allowing players to craft and play a variety of musical instruments for recreation and skill development. Originally developed by Dog Problems and now updated, the mod aims to integrate music as both a recreational and skill-enhancing element for pawns, adding depth and enjoyment to gameplay.

## Key Features and Systems

- **Musical Workbench**: Craft small and large instruments. Small instruments are crafted at a sculptor's bench, while large instruments are built as buildings.
- **Instrument Performance**: Instruments play actual music when coupled with the mod by Pos 5, now included.
- **Burn Unwanted Instruments**: Adds a recipe to dispose of unwanted instruments.
- **Performance Quality**: Influenced by the pawn's artistic skill, manipulation, consciousness, instrument quality, and condition.
- **Music Spots**: Designate tables, campfires, or dedicated spots as music gathering points where instruments can be played.
- **Skill Gain**: Playing music develops artistic skills. Work play grants full skill experience, while joy play grants reduced experience.

## Coding Patterns and Conventions

- **Class Organization**: Classes are organized based on their functionality, such as `Comp_PlayingMusic`, `CompMusicalInstrument`, and `PerformanceManager`.
- **Access Modifiers**: Following best practices, classes are marked `internal` when they do not need to be exposed outside the assembly, improving encapsulation.
- **Method Naming**: Uses descriptive method names like `StartPlaying`, `StopPlaying`, `CalculateQuality`, and `ExposeData` to clearly convey functionality.
- **XML Integration**: Integrates data through XML for defining instruments, jobs, and joy kinds to maintain separation between data and logic.

## XML Integration

The mod integrates XML to define game assets such as instruments, job definitions, and joy activities. Ensure XML files are structured properly to avoid data parsing errors. Copilot can assist in writing or converting C# code into XML-compatible structures.

## Harmony Patching

With the removal of the redundant `Harmony.dll`, the mod employs Harmony patches within the `HarmonyPatches` class to intercept and modify base game behavior without directly altering the game's source code. This approach allows compatibility with updates and other mods. When working on patches:
- Use `HarmonyPatch` attributes judiciously.
- Test patches thoroughly to ensure they do not introduce bugs or conflicts with other mods.

## Suggestions for Copilot

- **Method Stubbing**: Leverage Copilot to quickly generate method stubs based on existing patterns. This can accelerate setting up new features or expansions.
- **Refactoring Aid**: Use Copilot to suggest improvements or refactoring of existing code for better readability and performance.
- **XML and C# Integration**: Utilize Copilot to help generate XML code directly from C# requirements, ensuring consistency between logic and data.
- **Error Handling**: Prompt Copilot to include informative error handling that logs useful information for debugging.
- **Suggestions for Harmony Patches**: Request Copilot to assist in creating succinct and effective Harmony patch methods.

Ensure Copilot is used effectively to maintain coding standards and improve development efficiency, while manually verifying generated code for accuracy and logical soundness.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.
