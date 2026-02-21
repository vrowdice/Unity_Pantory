# 🧭 Pantory

**Pantory** is a fantasy crafting & trading simulation game set in a world where magic and early industrial technology coexist. Players gather resources, craft various tools, and directly sell their creations in the market — shaping the economy with their own hands.

---

## 🌍 Overview
Pantory is a production and commerce simulation where the fusion of fantasy and modern technology defines the world.
* **Core Loop:** Gather raw materials → Refine/Process → Craft Items → Trade in the Market.
* **Goal:** Understand production efficiency and market trends to grow your own grand workshop.

---

## ⚙️ Core Game Systems

### 🏭 Thread System (Production Threads)
* **Main Runner:** The primary interface for managing active production threads.
* **Tracking:** Each thread utilizes a unique ID and category for resource consumption/production calculations.
* **Interactivity:** Toggle between Placement and Removal modes for thread optimization.

### 🏢 Building System (Logistics & Design)
* **Design Runner:** A specialized UI for designing complex production chains.
* **Rotational Placement:** 4-way (90-degree) rotation for optimized layouts.
* **Building Categories:**
  * 🏭 **Raw Material Factory**: Basic extraction.
  * 🔧 **Processing Building**: Component manufacturing.
  * 🛣️ **Road/Distribution**: Logistics networking.
  * 📦 **Unload Station**: Logistics endpoints.

### 📊 Market & Economy
* **Dynamic Price System**: Prices fluctuate based on real-time supply and demand.
* **Market Actors**: NPC "Companies" act as market participants, driving liquidity.
* **Financial Logic**: 5% transaction fees, trust/reputation systems, and daily asset management.

### 🧩 Resource & Logistics
* **Resource Types**: Metal, Wood, Tool, Weapon, etc.
* **Trend Analysis**: Tracks up to 60 historical data points for price forecasting.
* **Road Network Logic**: Validates connectivity from Unload Stations and handles circular dependencies in production.

---

## 🛠️ Technical Architecture

### Manager System
Centralized control via a high-level Manager/Handler pattern:
* **GameManager**: Global state and UI Canvas control.
* **DataManager**: Central hub for all specialized Data Handlers.
* **SaveLoadManager**: Persistent JSON/Binary data handling.
* **PoolingManager**: Object pooling for optimized performance.

### Runner & Handler Logic
The game separates logic through "Runners" (States) and "Handlers" (Functional logic):
* **DesignRunnerGridHandler**: Manages building grid coordinates.
* **DesignRunnerRoadHandler**: Calculates resource propagation across the road network.
* **DesignRunnerCalculationHandler**: Real-time production chain math.

---

## 🎨 Tech Stack
| Category | Technology |
| :--- | :--- |
| **Engine** | Unity 2022.x+ |
| **Language** | C# (97.8%), Python (Automation) |
| **Graphics** | ShaderLab, HLSL |
| **UI** | TextMesh Pro, Unity Canvas, DOTween |
| **Tools** | Python scripts for automated asset generation |

---

## 📁 Project Structure
```text
Assets/
├── Scripts/
│   ├── Manager/              # Global Singletons (Game, Data, Sound)
│   ├── Runner/               # Screen Controllers (Main, Design)
│   ├── Data/                 # ScriptableObjects & Data Structures
│   ├── UI/                   # View logic & UI Panels
│   └── Py/                   # Python scripts for asset automation
├── Assets/
│   ├── Sprites/              # 2D Assets
│   ├── Prefabs/              # Construction & Thread objects
│   └── Images/               # Generated icons
└── ...
