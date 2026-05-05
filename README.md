# 🧭 Pantory

**Pantory** is a fantasy crafting & trading simulation game set in a world where magic and early industrial technology coexist. Players gather resources, craft various tools, and directly sell their creations in the market — shaping the economy with their own hands.

---

## 🌍 Overview

Pantory is a production and commerce simulation where the fusion of fantasy and modern technology defines the world.

* **Core Loop:** Gather raw materials → Refine/Process → Craft items → Trade in the market.
* **Goal:** Understand production efficiency and market trends to grow your own grand workshop.

---

## ⚙️ Core Game Systems

### 🏭 Building & Grid (Main Scene)

* **Main runner:** `MainRunner` drives the main scene — building/road placement on a tile grid, rotation, and removal (`BuildingData`-based; the old tile “thread” placement flow has been retired).
* **Handlers:** `MainBuildingGridHandler` (grid, roads, staged resource flow ticks), `MainBuildingPlacementHandler` (install/remove previews), `MainBlueprintHandler` (blueprint layout mode).
* **Layouts:** Save/restore placed buildings and roads via `PlacedObjectLayoutDataHandler`; named blueprint packs via `BlueprintLayoutDataHandler`.

### 🏢 Buildings & Logistics

* **Categories:** Raw extraction, processing plants, roads/distribution, unload stations, and related logistics pieces (see `Assets/Datas/Building` and `Scripts/Data/Building`).
* **Road network:** Connectivity and staged resource propagation are handled from the main grid handler (road vs dual-lane road, forward ticks).

### 📊 Market & Economy

* **Dynamic pricing:** Supply/demand driven behaviour through market actors and resource data.
* **Actors:** Companies, government, and other `MarketActorData` participants.
* **Finances:** Daily flows, fees, and expenses surfaced in UI (e.g. maintenance); logic in `FinancesDataHandler`.

### 👥 Staff & Progression

* **Employees:** Hiring, assignment, and management (`EmployeeData`, `EmployeeDataHandler`).
* **Research:** Unlocks and tech progression (`ResearchData`, `ResearchDataHandler`).
* **Orders:** Contract-style orders from actors (`OrderData`, `OrderDataHandler`).
* **News:** World/events feed (`NewsData`, `NewsDataHandler`).
* **Policies:** Factory/state policies with gameplay modifiers (`PolicyData`, `PolicyDataHandler`).
* **Effects:** Buffs/debuffs and world modifiers (`EffectData`, `EffectDataHandler`).
* **Main events:** Longer-term simulation modules (e.g. war/union/automation hooks under `MainEventDataHandler`).

### 🧩 Resources

* **Data:** Metals, wood, tools, weapons, electronics, and other categories under `Scripts/Data/Resource` and matching `Assets/Datas/Resource` assets.
* **Tracking:** Resource totals and change notifications via `ResourceDataHandler`.

### 🌐 Localization

* **Unity Localization** string tables under `Assets/Language/Table` (e.g. Building, Resource, Order, News, Effect, Common, Tutorial, …) with locale assets at `Assets/Language`.

---

## 🛠️ Technical Architecture

### Managers (`Assets/Scripts/Manager`)

Singleton-style coordinators:

| Manager | Role |
| :--- | :--- |
| **GameManager** | Global game state, camera controller wiring |
| **DataManager** | Central hub; owns all data handlers below |
| **SaveLoadManager** | Persistence (save/load pipeline) |
| **SceneLoadManager** | Scene transitions |
| **UIManager** | UI flow / popups |
| **SoundManager** | Audio |
| **PoolingManager** | Object pools |
| **VisualManager** | Shared visual constants (e.g. placement colours) |

### DataManager handlers (`Assets/Scripts/Manager/DataManager/Handler`)

Functional domains accessed as `DataManager.<Handler>`:

`Time`, `Resource`, `MarketActor`, `Finances`, `Employee`, `Building`, `Effect`, `Research`, `Order`, `News`, `Policy`, `MainEvent`, `Player`, `PlacedLayout`, `BlueprintLayout`.

### Runners (`Assets/Scripts/Runner`)

* **`MainRunner`** — main scene construction grid and UI bridge to `MainCanvas`.
* **`TitleRunner`** — title flow.
* **`RunnerBase`** — shared runner base.

### Supporting layout (`Assets/Scripts`)

* **`Controller/`** — e.g. main camera control.
* **`Object/`** — in-world objects (e.g. tiles).
* **`Common/`**, **`Interface/`**, **`Structure/`**, **`Type/`**, **`String/`**, **`Utile/`** — shared utilities and contracts.
* **`Py/`** — optional Python helpers for asset/automation workflows.

---

## 🎨 Tech Stack

| Category | Technology |
| :--- | :--- |
| **Engine** | Unity 6 (`6000.x` — see `ProjectSettings/ProjectVersion.txt`) |
| **Language** | C# |
| **UI** | TextMesh Pro, Unity UI, DOTween, Evo UI (third-party pack under `Assets/Evo`) |
| **Assets** | Addressables config under `Assets/AddressableAssetsData` |
| **Tools** | Python scripts under `Assets/Scripts/Py` (optional automation) |

---

## 📁 Project structure

High-level layout (game content lives under `Assets/`):

```text
Assets/
├── Scripts/                    # Gameplay C# code
│   ├── Manager/                # GameManager, DataManager, SaveLoad, UI, Sound, …
│   │   └── DataManager/
│   │       └── Handler/        # Per-domain data handlers
│   ├── Runner/                 # MainRunner, TitleRunner, RunnerBase
│   ├── UI/                     # Common, Main, Popup, Title
│   ├── Data/                   # Scriptable data types (Building, Resource, Policy, …)
│   ├── Object/                 # World objects
│   ├── Controller/             # Camera, etc.
│   ├── Common/                 # Shared runtime helpers
│   ├── Interface/, Structure/, Type/, String/, Utile/
│   └── Py/                     # Python automation scripts
├── Datas/                      # ScriptableObject instances (mirrors Scripts/Data domains)
│   ├── Building/
│   ├── Resource/
│   ├── Policy/
│   ├── Research/
│   ├── MarketActor/
│   ├── Order/
│   ├── News/
│   ├── Effect/
│   └── Employee/
├── Prefabs/                    # Effect, Manager, UI prefabs
├── Scenes/
├── Language/                   # Localization tables & locale assets
├── Images/, Fonts/, Audios/, Videos/
├── Resources/
├── Settings/                   # URP / project settings assets
├── Plugins/                    # DOTween and other plugins
├── TextMesh Pro/
├── Evo/                        # Evo UI package
└── AddressableAssetsData/
```

---

*Internal design notes: some identifiers still say “Thread” in enums or legacy strings; production placement is building/grid-based in the main scene.*
