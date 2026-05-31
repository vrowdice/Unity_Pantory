# 🧭 Pantory

**Pantory** is a fantasy crafting & trading simulation game set in a world where magic and early industrial technology coexist. Players gather resources, craft various tools, and directly sell their creations in the market — shaping the economy with their own hands.

## Gameplay

[![Watch Pantory gameplay on YouTube](https://img.youtube.com/vi/FhVxkCAGbEg/hqdefault.jpg)](https://youtu.be/FhVxkCAGbEg)

[Watch gameplay on YouTube](https://youtu.be/FhVxkCAGbEg?si=UB0Dbl00QhRAHthT)

---

## 🌍 Overview

Pantory is a production and commerce simulation where the fusion of fantasy and modern technology defines the world.

* **Core Loop:** Gather raw materials → Refine/Process → Craft items → Trade in the market.
* **Goal:** Understand production efficiency and market trends to grow your own grand workshop.

---

## ⚙️ Core Game Systems

### 🏭 Building & Grid (Main Scene)

* **Main runner:** `MainRunner` drives the main scene — building/road placement on a tile grid, rotation, and removal (`BuildingData`-based; the old tile "thread" placement flow has been retired).
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

* **`RunnerBase`** — shared runner base (managers, BGM).
* **`MainRunner`** — main/tutorial construction grid and placement.
* **`TutorialRunner`** — extends `MainRunner` for tutorial overrides.
* **`TitleRunner`** — extends `RunnerBase` for title flow.

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
│   ├── Runner/                 # RunnerBase, MainRunner, TutorialRunner, TitleRunner
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

## 📜 License

**Pantory is proprietary software.** All rights are reserved by **Vrowdice**.

| Topic | Policy |
| :--- | :--- |
| **Owner commercial use** | Allowed - Vrowdice may use, modify, and sell Pantory commercially. |
| **Copying / redistribution** | **Not allowed** without prior written permission from Vrowdice. |
| **Forking / mirroring** | **Not allowed** without authorization. |
| **Third-party assets** | Governed by their own licenses - see [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md). |

Full legal terms: [`LICENSE`](LICENSE)

Build/in-game notice: [`Assets/LEGAL/CopyrightNotice.txt`](Assets/LEGAL/CopyrightNotice.txt)

---

*Internal design notes: some identifiers still say "Thread" in enums or legacy strings; production placement is building/grid-based in the main scene.*

---

## README 한국어 요약

**Pantory**는 판타지와 초기 산업 기술이 공존하는 세계를 배경으로 한 제작·거래 시뮬레이션 게임입니다. 자원을 모으고, 도구와 물건을 만들고, 시장에서 직접 판매하며 경제를 바꿔 나갑니다.

### 플레이 영상

[YouTube 플레이 영상](https://youtu.be/FhVxkCAGbEg?si=UB0Dbl00QhRAHthT)에서 게임플레이를 확인할 수 있습니다.

### 개요

- **핵심 루프:** 원자재 수집 → 정제/가공 → 아이템 제작 → 시장 거래
- **목표:** 생산 효율과 시장 흐름을 파악해 자신만의 대형 공방을 키우는 것

### 핵심 게임 시스템

**건설 & 그리드 (메인 씬)**

- `MainRunner`가 타일 그리드 위 건물·도로 배치, 회전, 제거를 담당
- `MainBuildingGridHandler`, `MainBuildingPlacementHandler`, `MainBlueprintHandler`로 그리드·배치·청사진 관리
- `PlacedObjectLayoutDataHandler`, `BlueprintLayoutDataHandler`로 배치/청사진 저장·복원

**건물 & 물류**

- 채굴, 가공, 도로, 하역소 등 다양한 건물 카테고리
- 도로 연결과 단계별 자원 이동 처리

**시장 & 경제**

- 수요·공급 기반 동적 가격
- 기업, 정부 등 `MarketActorData` 참여자
- 일일 수입·비용·유지비 등 재정 시스템 (`FinancesDataHandler`)

**직원 & 성장**

- 고용, 배치, 직원 관리
- 연구, 주문, 뉴스, 정책, 효과, 메인 이벤트 등 장기 시뮬레이션 요소

**자원 & 현지화**

- 금속, 목재, 도구, 무기, 전자제품 등 다양한 자원 데이터
- Unity Localization 기반 다국어 지원 (`Assets/Language`)

### 기술 구조

**매니저:** `GameManager`, `DataManager`, `SaveLoadManager`, `SceneLoadManager`, `UIManager`, `SoundManager`, `PoolingManager`, `VisualManager`

**데이터 핸들러:** `Time`, `Resource`, `MarketActor`, `Finances`, `Employee`, `Building`, `Effect`, `Research`, `Order`, `News`, `Policy`, `MainEvent`, `Player`, `PlacedLayout`, `BlueprintLayout`

**러너:** `RunnerBase`(공통), `MainRunner`(메인 씬), `TutorialRunner`(튜토리얼), `TitleRunner`(타이틀)

**기타:** `Controller`, `Object`, `Common`, `Interface`, `Structure`, `Type`, `String`, `Utile`, `Py`(자동화 스크립트)

### 기술 스택

| 구분 | 기술 |
| :--- | :--- |
| **엔진** | Unity 6 (`6000.x`) |
| **언어** | C# |
| **UI** | TextMesh Pro, Unity UI, DOTween, Evo UI |
| **에셋** | Addressables |
| **도구** | Python 스크립트 (`Assets/Scripts/Py`) |

### 프로젝트 구조

게임 콘텐츠는 주로 `Assets/` 아래에 있습니다.

- `Scripts/` — 게임플레이 C# 코드
- `Datas/` — ScriptableObject 데이터
- `Prefabs/`, `Scenes/`, `Language/`, `Images/`, `Plugins/` 등

### 라이선스

**Pantory는 독점 소프트웨어이며, 모든 권리는 Vrowdice에 있습니다.**

| 항목 | 정책 |
| :--- | :--- |
| **소유자의 상업적 사용** | 허용 |
| **복제 / 재배포** | 사전 서면 허가 없이 금지 |
| **포크 / 미러링** | 허가 없이 금지 |
| **서드파티 에셋** | 각 라이선스에 따름 — [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md) |

전체 법적 조항: [`LICENSE`](LICENSE) (영문 본문 우선)

### 참고

일부 enum·레거시 문자열에는 아직 "Thread"라는 이름이 남아 있지만, 실제 메인 씬 생산 배치는 건물/그리드 기반입니다.
