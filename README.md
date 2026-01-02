# No Longer Exist

## Project Overview
**No Longer Exist** is a 3D first-person horror puzzle game developed using Unity.  
The project focuses on environmental exploration, inventory management, puzzle solving, and survival mechanics within a haunted setting.

---

## Game Overview (Story)
The main character, **Neari**, wakes up in an unfamiliar place with no memory of how she arrived.  
She must survive a haunted village by exploring the environment, avoiding hostile ghosts, and searching for the **Egress Door** in order to escape.

---

## Gameplay Overview

### Gameplay Flow
- Player starts in an unknown area of the ghost village
- Explores the environment to discover clues and items
- Avoids or escapes ghosts while progressing through the map
- Solves environmental and logic-based puzzles to unlock new paths
- Reaches the **Egress Door** to complete the game

Players explore a dark, haunted village to uncover hidden paths and restricted areas.

---

## Gameplay & Core Mechanics

### Controls Reference

| Action | Key |
| --- | --- |
| **Move** | W, A, S, D, & Mouse |
| **Interact** | E |
| **Read Item** | F |
| **Unequip** | Q |
| **Inventory** | Tab |
| **Hotbar** | 1–6 / Mouse Scroll |

---

### Interaction System
- Raycast-based interaction system
- Player interactions are detected using camera raycasts
- Triggers are used only for scene transitions and scripted events

---

## Win / Lose Conditions

### Win Condition
- The player successfully finds and enters the **Egress Door**

### Lose Condition
- If a ghost catches the player, the game reloads the previous save state

---
## Build & Demo

### Game Build (Windows)

The compiled Windows build is provided in the `Build/` directory.
Players can launch the game directly by double-clicking the `No Longer Exists.exe` file.

For convenience, the same build is also available for download via MediaFire:
https://www.mediafire.com/file/efg8s58htjz89eo/NoLongerExists.zip/file



### Demo Video
A gameplay demonstration video is available at:
https://www.youtube.com/watch?v=-l3gTQDtOV0


## Project Structure

### Scenes

```text
Assets/Scenes/
├── Startmenu        # Main menu scene
├── Game             # Core gameplay scene
├── Quick            # Fast Gameplay
└── Ending           # Game ending scene

```
## Project Folder Structure

```text
Assets/
├── 2D/                     # 2D UI, Puzzles, and scripts
├── Audio/                  # Sound effects and ambient tracks
├── Editor/                 # Custom editor tooling
├── packages/         # Third-party environment and controller packs
├── Items/                  # ScriptableObject item definitions
├── missions/               # MissionData ScriptableObjects
├── GamePlayPrefabs/                # Core gameplay and environment prefabs
├── Resources/              # Runtime registries (e.g., ItemRegistry)
├── Scenes/                 # Game scenes (Startmenu, Game, Demo)
├── Scripts/                # Core gameplay logic (see breakdown below)
│   ├── Enviroment/         # World interaction (Doors, Chests, Flashlight)
│   ├── GameManager/        # High-level systems (Missions, Dialogue)
│   ├── Interaction/        # Raycasting and interaction hints
│   ├── Inventory/          # Item management and UI
│   ├── Npc/                # AI roaming and interactions
│   ├── Puzzle/             # Puzzle logic and management
│   └── Save/               # JSON persistence system
├── sotries/       # Story UI images
├── TextMesh Pro/           # UI Text assets
└── Uploaded Prefab/        # Development prefabs

```

### Core Scripts (`Assets/Scripts`)

#### Inventory & Item System (`Scripts/Inventory`)

* `InventorySystem.cs`: Main inventory logic and input handling.
* `InventorySlotUI.cs`: Inventory UI slot rendering.
* `ItemData.cs`: ScriptableObject definition for items.
* `ItemDatabase.cs`: Central item registration system.
* `ItemPickup.cs`: Item pickup interaction.
* `ItemReadable.cs`: Readable item behavior.
* `ScrollManager.cs`: Hotbar scroll and item switching.

#### Save System (`Scripts/Save`)

* `SaveManager.cs`: Global save/load controller (Singleton).
* `ISaveable.cs`: Interface for saveable objects.
* `SaveLoadUI.cs`: Save/load menu UI.
* `SaveStation.cs`: In-world save point.
* `SaveObjectState.cs`: Persistent object state handler.

#### Interaction System (`Scripts/Interaction`)

* `PlayerInteract.cs`: Player raycast interaction logic.
* `NPCInteract.cs`: NPC interaction handling.
* `WorldInteractable.cs`: Base interactable component.

#### Missions & Puzzle System  (`Scripts/GameManager`)

* `MissionManager.cs`: Mission progression and tracking.
* `StonePuzzle.cs`: Environmental puzzle logic.
* `Assets/missions/`: Mission data and configurations.
* `Assets/2D/scripts/`: 2D puzzle mechanics.

---

### Resources & Registries

* `Assets/Resources/ItemRegistry.asset`: Runtime item registry.

### Editor Tools

* `ItemRegistryBuilder.cs`: Builds item registry automatically.
* `AssignPersistentIDs.cs`: Assigns unique IDs for the save system.
* `DeleteSaveMenu.cs`: Editor utility for save cleanup.

### Prefabs & Assets

* `Assets/prefabs/`: General environment and object prefabs.
---

## Gameplay Architecture

* **Manager-based Architecture**: Key systems use the **Singleton pattern** for global access (e.g., `SaveManager`, `ScrollManager`).
* **Component-driven Design**: Modular logic where inventory, NPCs, and puzzles are attached as independent components.
* **Item System**: Data-driven items defined via `ScriptableObjects` and registered in a central database.
* **Interaction**: Raycasting from `Camera.main` combined with `TryGetComponent` for efficient object detection.
* **Input**: Utilizes the legacy `UnityEngine.Input` system.

---

## Save System

* **Format**: JSON
* **Storage Path**: `Application.persistentDataPath/horrorsave.json`
* **Data Persistence**:
* Player position and rotation.
* Current inventory contents.
* States of all objects implementing `ISaveable`.


