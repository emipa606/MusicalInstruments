# GitHub Copilot Instructions for RimWorld Mod: Musical Instruments

## Mod Overview and Purpose

The Musical Instruments mod enriches RimWorld gameplay by introducing musical elements to the environment. This mod allows pawns to perform music, enhancing their mood and providing entertainment for their community. By integrating musical instruments into the game, pawns can gain joy and skill development while fostering a more dynamic community atmosphere.

## Key Features and Systems

- **Musical Instruments**: Adds various musical instruments that pawns can play, each with unique characteristics.
- **Performance Management**: A system that handles musical performances, affecting pawn joy and community mood.
- **Job System Integration**: Custom job definitions and drivers allow pawns to engage in musical activities.
- **Joy Mechanism**: Implements an incidental joy kind specific to music, enhancing the entertainment value of instruments.
- **Harmony Patches**: Custom patches to adjust and improve base game functionalities (such as joy interactions).
- **Dynamic Spot Management**: Manage spots where music can be played, impacting where and when performances occur.

## Coding Patterns and Conventions

- **Class Naming**: Classes are named using PascalCase (e.g., `CompMusicalInstrument`, `PerformanceManager`).
- **Method Conventions**: Method names are also in PascalCase and should be descriptive of their function (e.g., `StartPlaying`, `TryFindInstrumentToPlay`).
- **Encapsulation**: Internal methods are used to maintain the integrity of the mod's logic, providing a clear structure (e.g., `JobDriver_MusicPlayBase`).
- **Code Comments**: Important logic segments are adequately commented, especially in abstract and base classes, to clarify functionality.

## XML Integration

While specifics on XML files weren't provided, traditional RimWorld mods use XML for defining items, jobs, and other game elements. Ensure that XML files are properly integrated in the mod structure to define:

- **Items**: XML to define the musical instruments, describing their properties and stats.
- **Jobs and Joy**: XML to link job definitions and joy categories to musical activities.

Ensure all XML files are linked to the C# code via appropriate Defs references.

## Harmony Patching

- **Patch Implementation**: Utilize `HarmonyPatches` to integrate changes into the base game smoothly without overwriting core files.
- **Targeted Patches**: Implement targeted patches such as `PatchTrySatisfyJoyNeed` and `PatchGetAvailableJoyKindsFor` to extend and modify base gameplay functionality relevant to joy and music.
- **Method Overriding**: Use method overriding to control the output and enhance the systems without disturbing existing processes.

## Suggestions for Copilot

To maximize the efficiency and utility of GitHub Copilot for this project:

- **Contextual Prompts**: Focus prompts on enhancing performance management, such as suggesting algorithms for calculating performance quality or managing dynamic music spots.
- **Method Definitions**: Use Copilot to autocomplete method stubs especially for new functionality, like additional joy conditions or new musical effects.
- **XML Handling**: Leverage Copilot to outline and complete XML definition files for adding new instruments or job definitions.
- **Patch Enhancement**: Automate repetitive patching tasks by guiding Copilot to propose template patches with common patterns like prefix, postfix, and transpiler hooks tailored for music functionality.

These guidelines should help leverage GitHub Copilot effectively to develop and maintain the Musical Instruments mod, enhancing gameplay and extending content without sacrificing performance or compatibility.
