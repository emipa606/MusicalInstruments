# GitHub Copilot Instructions for RimWorld Mod: Musical Instruments (Continued)

## Mod Overview and Purpose
The **Musical Instruments (Continued)** mod is an update to the original **Dog Problems** mod. The primary purpose of this mod is to allow players to craft, use, and play various musical instruments within the colony, enhancing the gameplay experience by providing recreational activities and skill training.

## Key Features and Systems

### Crafting Musical Instruments
- **Research**: Players need to complete specific research projects to unlock new instruments.
- **Crafting Stations**: Small instruments are crafted at the sculptor's bench, while large instruments require construction at designated building sites under the Joy tab.
- **Market and Factions**: Instruments can also be purchased from exotic goods traders or faction bases.

### Playing Musical Instruments
- **Pawn Abilities**: Existing playing requirements are unchanged. Performance quality uses the higher of Artistic or enabled Social skill, so either talent can improve a pawn's music.
- **Music Spots**: Pawns require access to active music spots (like tables or campfires) to play music.
- **Skill Training**: Playing music trains the artistic skill, with different training speeds based on whether the pawn is playing for joy or work.

### Instrument Quality and Performance
- **Performance Skill**: Colony and caravan performances use the same skill-based quality calculation. Existing performance luck still varies the outcome, so higher Artistic or Social skill improves the chance of nice or great music without guaranteeing it. Disabled Social skill provides no bonus, and lower Social skill never reduces an artistic musician's quality.
- **Instrument Quality**: Quality affects the performance quality of the music.
- **Health Points (HP)**: The HP of an instrument affects its longevity and performance quality as it wears down.
- **Materials**: While materials affect the max HP and market price, they do not impact music quality.

## Coding Patterns and Conventions

### General Guidelines
- **Modular Code**: Code is organized into modular components, each responsible for a specific aspect of the mod's functionality.
- **Naming Conventions**: Use descriptive and consistent naming conventions for classes, methods, and variables.
- **Documentation**: Provide clear comments and documentation within the code to explain the purpose and functionality of each component.

### Common C# File Structures
- **Comp Files**: These files define components that are attached to game objects (e.g., `Comp_PlayingMusic.cs`).
- **Patch Files**: These files contain Harmony patches to modify existing RimWorld functionality (e.g., `Caravan_NeedsTracker_GetAvailableJoyKindsFor.cs`).
- **Utility Files**: These files contain utility methods and classes used throughout the mod (e.g., `Utility_MusicalInstruments.cs`).

### Common XML File Structures
- **Defs Files**: These files define the mod's custom game objects, such as instruments, research projects, and job definitions.
- **RulePack Files**: These files define text and naming rules for mod objects, enhancing the game's localization and narrative.

## XML Integration

### Key XML Files and Defs
- **ResearchProjectDefs**: Defines the research projects needed to unlock new instruments (e.g., `ResearchProjects_MusicalInstruments.xml`).
- **ThingDefs**: Defines the custom instruments (e.g., `ThingDef_MusicalInstrument.xml`).
- **JobDefs**: Defines the jobs related to playing musical instruments (e.g., `Jobs_Joy.xml`).
- **RulePackDefs**: Defines text and naming rules for musical instruments (e.g., `RulePacks_Art_DescriptionsMusicalInstruments.xml`).

### Best Practices
- **Consistent Naming**: Ensure that all XML elements and attributes have consistent and descriptive names.
- **Versioning**: Use versioning in XML files to manage changes and ensure compatibility with future updates.
- **Documentation**: Comment within XML files to explain the purpose and functionality of each def.

## Harmony Patching

### Overview
Harmony patching is used to modify existing RimWorld code to integrate mod functionality without altering the original game files.

### Common Patch Types
- **Prefix**: Code executed before the original method.
- **Postfix**: Code executed after the original method.
- **Transpiler**: Code executed to modify the original method's bytecode.

### Best Practices
- **Minimal Changes**: Keep patches minimal to avoid unintended side effects.
- **Compatibility**: Ensure that patches are compatible with future RimWorld updates.
- **Documentation**: Comment within patch files to explain the purpose and functionality of each patch.

## Suggestions for Copilot

### General Recommendations
- **Code Completion**: Use Copilot to complete code snippets and reduce the time spent on repetitive coding tasks.
- **Error Handling**: Assist with generating robust error handling mechanisms to ensure mod stability.
- **Documentation**: Use Copilot to generate clear and concise documentation for code and XML files.

### Specific Recommendations
- **Harmony Patch Generation**: Assist with generating Harmony patches for modifying existing RimWorld functionality.
- **XML Def Generation**: Assist with generating XML def files for custom game objects.
- **Mod Integration**: Assist with integrating new features and systems into the existing mod framework.

By following these guidelines and utilizing GitHub Copilot effectively, you can enhance the development process for the **Musical Instruments (Continued)** mod, ensuring a high-quality and enjoyable modding experience for the RimWorld community.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

