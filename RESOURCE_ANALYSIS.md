# 자원(Resource) 상황 분석 및 설명

## 📊 ResourceType 매핑
- **0**: raw (원자재)
- **1**: metal (금속)
- **4**: weapon (무기)
- **5**: Essentials (생필품)
- **7**: component (부품)
- **8**: vehicle (차량)

---

## 🌲 원자재 (Raw) - 7개

### 기본 정보
- **타입**: raw (0)
- **특징**: 모든 제작의 기초, requirements 없음 (채집 가능)
- **초기 재고**: iron_ore (2000), wood_log (2000), coal (1500), oil (1500), copper_ore (1000), aluminum_ore (500), gold_ore (500)

### 자원 목록

#### 1. **Iron Ore** (철광석)
- **ID**: `iron_ore`
- **기본 가격**: 15
- **시장 민감도**: 0.15 (낮음)
- **평균 회귀 강도**: 0.7
- **변동성 배율**: 0.8
- **희귀도**: 0.8 (흔함)
- **부족 가중치**: 0.4
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **다음 단계**: iron_ingot
- **초기 재고**: 2000
- **설명**: The foundation of industry. The primary material for steel and machine parts.

#### 2. **Wood Log** (원목)
- **ID**: `wood_log`
- **기본 가격**: 10
- **시장 민감도**: 0.15 (낮음)
- **평균 회귀 강도**: 0.7
- **변동성 배율**: 0.8
- **희귀도**: 0.9 (매우 흔함)
- **부족 가중치**: 0.3
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **다음 단계**: fine_wood
- **초기 재고**: 2000
- **설명**: Raw timber harvested from forests. The basic building material before processing.

#### 3. **Coal** (석탄)
- **ID**: `coal`
- **기본 가격**: 12
- **시장 민감도**: 0.18 (낮음)
- **평균 회귀 강도**: 0.6
- **변동성 배율**: 0.9
- **희귀도**: 0.85 (흔함)
- **부족 가중치**: 0.4
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **다음 단계**: 없음 (최종 자원)
- **초기 재고**: 1500
- **설명**: Black combustible rock formed from ancient plant remains. Essential for fuel and steel production.

#### 4. **Copper Ore** (구리 광석)
- **ID**: `copper_ore`
- **기본 가격**: 25
- **시장 민감도**: 0.22 (보통)
- **평균 회귀 강도**: 0.5
- **변동성 배율**: 1.0
- **희귀도**: 0.7 (흔함)
- **부족 가중치**: 0.5
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **다음 단계**: copper_ingot
- **초기 재고**: 1000
- **설명**: Reddish-brown ore with excellent electrical conductivity. Essential for wires and modern machinery.

#### 5. **Aluminum Ore** (알루미늄 광석)
- **ID**: `aluminum_ore`
- **기본 가격**: 40
- **시장 민감도**: 0.25 (보통)
- **평균 회귀 강도**: 0.5
- **변동성 배율**: 1.1
- **희귀도**: 0.65 (흔함)
- **부족 가중치**: 0.6
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **다음 단계**: aluminum_ingot
- **초기 재고**: 500
- **설명**: Lightweight aluminum-bearing ore. Valued in modern construction for its strength-to-weight ratio.

#### 6. **Gold Ore** (금 광석)
- **ID**: `gold_ore`
- **기본 가격**: 120
- **시장 민감도**: 0.3 (높음)
- **평균 회귀 강도**: 0.4
- **변동성 배율**: 1.2
- **희귀도**: 0.6 (보통)
- **부족 가중치**: 0.8
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **다음 단계**: pure_gold
- **초기 재고**: 500
- **설명**: Heavy ore with gleaming precious metal veins. Valuable raw material ready for smelting.

#### 7. **Oil** (원유)
- **ID**: `oil`
- **기본 가격**: 20
- **시장 민감도**: 0.4 (매우 높음)
- **평균 회귀 강도**: 0.3
- **변동성 배율**: 1.5
- **희귀도**: 0.7 (흔함)
- **부족 가중치**: 0.9
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **다음 단계**: 없음 (최종 자원)
- **초기 재고**: 1500
- **설명**: Black gold. Material for synthetic textiles, plastics, and fuel for tanks and aircraft.

---

## 🔩 금속 (Metal) - 5개

### 기본 정보
- **타입**: metal (1)
- **특징**: 원자재(raw)를 제련하여 생산, 대부분 requirements 필요
- **초기 재고**: 0 (제작 필요)

### 자원 목록

#### 1. **Iron Ingot** (철괴)
- **ID**: `iron_ingot`
- **기본 가격**: 60
- **시장 민감도**: 0.18 (낮음)
- **평균 회귀 강도**: 0.55
- **희귀도**: 0.6 (보통)
- **부족 가중치**: 0.55
- **요구사항**: iron_ore × 3
- **다음 단계**: steel_ingot
- **초기 재고**: 0
- **설명**: A solid bar of smelted iron, forged from raw ore. Versatile and durable, ready for crafting.

#### 2. **Steel Ingot** (강철괴)
- **ID**: `steel_ingot`
- **기본 가격**: 170
- **시장 민감도**: 0.2 (보통)
- **평균 회귀 강도**: 0.6
- **희귀도**: 0.5 (보통)
- **부족 가중치**: 0.6
- **요구사항**: iron_ingot × 2
- **다음 단계**: 없음 (최종 자원)
- **초기 재고**: 0
- **설명**: High-quality steel forged from refined iron through advanced smelting. Stronger and more durable than pure iron.

#### 3. **Copper Ingot** (구리괴)
- **ID**: `copper_ingot`
- **기본 가격**: 100
- **시장 민감도**: 0.2 (보통)
- **평균 회귀 강도**: 0.58
- **희귀도**: 0.7 (흔함)
- **부족 가중치**: 0.5
- **요구사항**: copper_ore × 3
- **다음 단계**: 없음 (최종 자원)
- **초기 재고**: 0
- **설명**: Refined copper bar with excellent electrical conductivity. The backbone of modern electrical systems.

#### 4. **Aluminum Ingot** (알루미늄괴)
- **ID**: `aluminum_ingot`
- **기본 가격**: 120
- **시장 민감도**: 0.19 (낮음)
- **평균 회귀 강도**: 0.6
- **희귀도**: 0.65 (흔함)
- **부족 가중치**: 0.55
- **요구사항**: aluminum_ore × 3
- **다음 단계**: 없음 (최종 자원)
- **초기 재고**: 0
- **설명**: Lightweight aluminum bar with exceptional strength. Used in aircraft, vehicles, and modern structures.

#### 5. **Pure Gold** (순금)
- **ID**: `pure_gold`
- **기본 가격**: 400
- **시장 민감도**: 0.2 (보통)
- **평균 회귀 강도**: 0.6
- **희귀도**: 0.4 (드묾)
- **부족 가중치**: 0.65
- **요구사항**: gold_ore × 3
- **다음 단계**: 없음 (최종 자원)
- **초기 재고**: 0
- **설명**: Refined gold of the highest purity, gleaming with warm radiance. A symbol of wealth and craftsmanship excellence.

---

## 🏠 생필품 (Essentials) - 3개

### 기본 정보
- **타입**: Essentials (5)
- **특징**: 일반 시민의 주요 소비재, 일상 생활 필수품
- **초기 재고**: simple_tools (1000), fabric (500), 나머지 0

### 자원 목록

#### 1. **Fabric** (직물)
- **ID**: `fabric`
- **기본 가격**: 20
- **시장 민감도**: 0.19 (낮음)
- **평균 회귀 강도**: 0.6
- **변동성 배율**: 1.0
- **희귀도**: 0.7 (흔함)
- **부족 가중치**: 0.5
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: wood_log × 2
- **초기 재고**: 500
- **설명**: Woven textile material made from natural or synthetic fibers. Essential for clothing, upholstery, and various household items.

#### 2. **Simple Tools** (간단한 도구)
- **ID**: `simple_tools`
- **기본 가격**: 110
- **시장 민감도**: 0.2 (보통)
- **평균 회귀 강도**: 0.6
- **변동성 배율**: 1.0
- **희귀도**: 0.7 (흔함)
- **부족 가중치**: 0.5
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: iron_ingot × 1, wood_log × 1
- **초기 재고**: 1000
- **설명**: A basic household tool set including hammer, saw, and wrench. Essential for everyday repairs and furniture assembly. A consumer staple.

#### 3. **Basic Clothing** (기본 의류)
- **ID**: `basic_clothing`
- **기본 가격**: 100
- **시장 민감도**: 0.21 (보통)
- **평균 회귀 강도**: 0.59
- **변동성 배율**: 1.0
- **희귀도**: 0.7 (흔함)
- **부족 가중치**: 0.55
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: fabric × 3
- **초기 재고**: 0
- **설명**: Simple, durable clothing made from basic fabric. Practical and comfortable for everyday wear.

#### 4. **Basic Furniture** (기본 가구)
- **ID**: `basic_furniture`
- **기본 가격**: 240
- **시장 민감도**: 0.2 (보통)
- **평균 회귀 강도**: 0.58
- **변동성 배율**: 1.0
- **희귀도**: 0.65 (흔함)
- **부족 가중치**: 0.6
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: fine_wood × 4, fabric × 2
- **초기 재고**: 0
- **설명**: Simple, functional furniture crafted from fine wood. Practical and affordable, suitable for everyday use.

---

## ⚙️ 부품 (Component) - 6개

### 기본 정보
- **타입**: component (7)
- **특징**: 기계 및 차량 제작에 필수, 중간 제작 자원
- **초기 재고**: 0 (제작 필요)

### 자원 목록

#### 1. **Fine Wood** (정제 목재)
- **ID**: `fine_wood`
- **기본 가격**: 30
- **시장 민감도**: 0.2 (보통)
- **평균 회귀 강도**: 0.55
- **변동성 배율**: 1.0
- **희귀도**: 0.5 (보통)
- **부족 가중치**: 0.8 (높음)
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: wood_log × 2
- **다음 단계**: premium_wood
- **초기 재고**: 0
- **설명**: High-quality wood with a smooth finish and beautiful grain. Selected and perfected through meticulous processing.

#### 2. **Machine Parts** (기계 부품)
- **ID**: `machine_parts`
- **기본 가격**: 150
- **시장 민감도**: 0.2 (보통)
- **평균 회귀 강도**: 0.6
- **변동성 배율**: 1.0
- **희귀도**: 0.7 (흔함)
- **부족 가중치**: 0.5
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: iron_ingot × 2
- **초기 재고**: 0
- **설명**: Essential mechanical components including bolts, gears, and structural elements. The foundation of all machinery, construction, and industrial production.

#### 3. **Electronic Components** (전자 부품)
- **ID**: `electronic_components`
- **기본 가격**: 250
- **시장 민감도**: 0.22 (보통)
- **평균 회귀 강도**: 0.6
- **변동성 배율**: 1.0
- **희귀도**: 0.6 (보통)
- **부족 가중치**: 0.55
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: copper_ingot × 1, aluminum_ingot × 1
- **초기 재고**: 0
- **설명**: Advanced electronic components including circuits, wiring, and control systems. Essential for modern machinery, vehicles, and sophisticated devices. Uses aluminum for lightweight, heat-resistant casings.

#### 4. **Precision Tools** (정밀 도구)
- **ID**: `precision_tools`
- **기본 가격**: 500
- **시장 민감도**: 0.25 (높음)
- **평균 회귀 강도**: 0.6
- **변동성 배율**: 1.0
- **희귀도**: 0.4 (드묾)
- **부족 가중치**: 0.7
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: steel_ingot × 1, machine_parts × 1
- **초기 재고**: 0
- **설명**: Professional-grade steel tools including power drills and precision wrenches. Essential for factory maintenance and machinery production.

#### 5. **Engine** (엔진)
- **ID**: `engine`
- **기본 가격**: 2500
- **시장 민감도**: 0.24 (높음)
- **평균 회귀 강도**: 0.65
- **변동성 배율**: 1.0
- **희귀도**: 0.3 (드묾)
- **부족 가중치**: 0.8
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: machine_parts × 1, electronic_components × 1, steel_ingot × 1
- **초기 재고**: 0
- **설명**: A high-performance internal combustion engine. The heart of vehicles, tanks, and aircraft. Requires advanced manufacturing capabilities and is a critical strategic resource during wartime.

---

## ⚔️ 무기 (Weapon) - 3개

### 기본 정보
- **타입**: weapon (4)
- **특징**: 고가 자원, 전쟁 시 수요 급증, 높은 희귀도
- **초기 재고**: 0 (제작 필요)

### 자원 목록

#### 1. **Small Arms** (소형 무기)
- **ID**: `small_arms`
- **기본 가격**: 1000
- **시장 민감도**: 0.3 (높음)
- **평균 회귀 강도**: 0.65
- **변동성 배율**: 1.0
- **희귀도**: 0.35 (드묾)
- **부족 가중치**: 0.8
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: steel_ingot × 2, wood_log × 1, machine_parts × 1
- **초기 재고**: 0
- **설명**: Basic infantry weapons including rifles, pistols, and combat knives. Essential equipment for conscripted soldiers. Demand explodes at the start of war. Priced affordably for mass production.

#### 2. **Munitions** (탄약)
- **ID**: `munitions`
- **기본 가격**: 300
- **시장 민감도**: 0.5 (매우 높음)
- **평균 회귀 강도**: 0.6
- **변동성 배율**: 1.2
- **희귀도**: 0.5 (보통)
- **부족 가중치**: 0.8
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: steel_ingot × 1, coal × 1, oil × 1
- **초기 재고**: 0
- **설명**: The fuel of war. Bullets, shells, and explosives consumed continuously as long as conflict persists. Demand becomes nearly infinite during wartime.

#### 3. **Heavy Arms** (중화기)
- **ID**: `heavy_arms`
- **기본 가격**: 15000
- **시장 민감도**: 0.4 (매우 높음)
- **평균 회귀 강도**: 0.7
- **변동성 배율**: 1.0
- **희귀도**: 0.2 (매우 드묾)
- **부족 가중치**: 0.9
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: steel_ingot × 10, machine_parts × 5
- **초기 재고**: 0
- **설명**: High-value strategic weapons. Field artillery and anti-aircraft guns that can change the course of battle. Extremely high profit margin for prepared manufacturers.

---

## 🚗 차량 (Vehicle) - 3개

### 기본 정보
- **타입**: vehicle (8)
- **특징**: 최고가 자원, 복잡한 제작 체인, 전쟁 시 수요 급증
- **초기 재고**: 0 (제작 필요)

### 자원 목록

#### 1. **Car** (자동차)
- **ID**: `car`
- **기본 가격**: 6000
- **시장 민감도**: 0.25 (높음)
- **평균 회귀 강도**: 0.65
- **변동성 배율**: 1.0
- **희귀도**: 0.3 (드묾)
- **부족 가중치**: 0.85
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: 
  - engine × 1
  - steel_ingot × 2
  - electronic_components × 2
  - oil × 2
  - aluminum_ingot × 1
- **초기 재고**: 0
- **설명**: The product of modern engineering. A luxury item for the wealthy during peaceful times. Uses lightweight aluminum for improved fuel efficiency. Demand plummets during war as civilians become impoverished.

#### 2. **Battle Tank** (전차)
- **ID**: `tank`
- **기본 가격**: 25000
- **시장 민감도**: 0.35 (매우 높음)
- **평균 회귀 강도**: 0.72
- **변동성 배율**: 1.0
- **희귀도**: 0.15 (매우 드묾)
- **부족 가중치**: 1.0 (최대)
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: 
  - engine × 1
  - heavy_arms × 1
  - steel_ingot × 20
  - machine_parts × 5
- **초기 재고**: 0
- **설명**: The king of ground warfare. A combination of steel and heavy artillery. Consumes massive amounts of steel and heavy arms during wartime production.

#### 3. **Fighter Jet** (전투기)
- **ID**: `airplane`
- **기본 가격**: 12000
- **시장 민감도**: 0.3 (높음)
- **평균 회귀 강도**: 0.7
- **변동성 배율**: 1.0
- **희귀도**: 0.2 (매우 드묾)
- **부족 가중치**: 0.95
- **가격 저항선**: 3.0x
- **최대 가격 배율**: 10.0x
- **요구사항**: 
  - engine × 2
  - aluminum_ingot × 10
  - electronic_components × 5
  - small_arms × 1
- **초기 재고**: 0
- **설명**: The ruler of the skies. Requires aluminum and advanced electronic equipment. The pinnacle of precision industry.

---

## 📊 종합 분석

### 자원 분류

#### 1. **원자재 (Raw)** - 7개
- **특징**: 채집 가능, requirements 없음
- **가격 범위**: 10~120 (낮음~중간)
- **초기 재고**: iron_ore (2000), wood_log (2000), coal (1500), oil (1500), copper_ore (1000), aluminum_ore (500), gold_ore (500)
- **주요 자원**: iron_ore, wood_log, coal, oil

#### 2. **1차 가공품 (Metal)** - 5개
- **특징**: 원자재를 제련하여 생산
- **가격 범위**: 60~400 (중간)
- **초기 재고**: 0
- **주요 자원**: iron_ingot, steel_ingot, copper_ingot, aluminum_ingot, pure_gold

#### 3. **생필품 (Essentials)** - 4개
- **특징**: 일반 시민의 주요 소비재
- **가격 범위**: 20~240 (낮음~중간)
- **초기 재고**: simple_tools (1000), fabric (500)
- **주요 자원**: fabric, simple_tools, basic_clothing, basic_furniture

#### 4. **부품 (Component)** - 5개
- **특징**: 기계 및 차량 제작에 필수, 중간 제작 자원
- **가격 범위**: 30~2500 (중간~고가)
- **초기 재고**: 0
- **주요 자원**: fine_wood, machine_parts, electronic_components, precision_tools, engine

#### 5. **무기 (Weapon)** - 3개
- **특징**: 전쟁 시 수요 급증, 높은 희귀도
- **가격 범위**: 300~15000 (중간~고가)
- **초기 재고**: 0
- **주요 자원**: small_arms, munitions, heavy_arms

#### 6. **차량 (Vehicle)** - 3개
- **특징**: 최고가 자원, 복잡한 제작 체인
- **가격 범위**: 6000~25000 (고가)
- **초기 재고**: 0
- **주요 자원**: car, tank, airplane

---

## 🔗 제작 체인 (Production Chain)

### 철 계열
```
iron_ore (15) 
  → iron_ingot (60) [iron_ore × 3]
    → steel_ingot (170) [iron_ingot × 2]
      → machine_parts (150) [iron_ingot × 2]
        → [다양한 제작품에 사용]
```

### 나무 계열
```
wood_log (10)
  → fine_wood (30) [wood_log × 2]
    → [가구 제작에 사용]
```

### 전자 계열
```
copper_ore (25)
  → copper_ingot (100) [copper_ore × 3]
    → electronic_components (250) [copper_ingot × 2]
      → [차량 및 고급 기계에 사용]
```

### 무기 제작 체인
```
iron_ore → iron_ingot → steel_ingot
  ↓
machine_parts [iron_ingot × 2]
  ↓
small_arms (2500) [steel_ingot × 2, wood_log × 1, machine_parts × 1]
heavy_arms (15000) [steel_ingot × 10, machine_parts × 5]
munitions (300) [steel_ingot × 1, coal × 1, oil × 1]
```

### 차량 제작 체인
```
[민수용 - 평화 시기]
engine (2500) + steel_ingot × 2 + electronic_components × 2 + oil × 2
  → Car (6000)

[군수용 - 지상전]
engine (2500) + heavy_arms (15000) + steel_ingot × 20 + machine_parts × 5
  → Battle Tank (25000)

[군수용 - 공중전]
engine × 2 (5000) + aluminum_ingot × 10 + electronic_components × 5 + small_arms (2500)
  → Fighter Jet (12000)
```

---

## 💰 가격 분석

### 가격대별 분류

#### **저가 자원** (10~100)
- 원자재: wood_log (10), coal (12), iron_ore (15), oil (20), copper_ore (25)
- 생필품: fabric (20), basic_clothing (100)
- 부품: fine_wood (30)
- 무기 소모품: munitions (300)

#### **중가 자원** (100~1000)
- 원자재: aluminum_ore (40), gold_ore (120)
- 1차 가공품: iron_ingot (60), copper_ingot (100), aluminum_ingot (120), steel_ingot (170), pure_gold (400)
- 생필품: simple_tools (110), basic_furniture (240)
- 부품: machine_parts (150), electronic_components (250), precision_tools (500)
- 무기: small_arms (1000)

#### **고가 자원** (1000~10000)
- 부품: engine (2500)
- 무기: heavy_arms (15000)
- 차량: car (6000), airplane (12000), tank (25000)

---

## 📈 시장 특성 분석

### 시장 민감도 (Market Sensitivity)
- **낮음 (0.15~0.19)**: 원자재 (iron_ore, wood_log, coal, fabric), 기본 가공품
- **보통 (0.2~0.25)**: 1차 가공품, 기본 제작품, 부품
- **높음 (0.3~0.35)**: 무기, 차량
- **매우 높음 (0.4~0.5)**: oil, heavy_arms, munitions

### 평균 회귀 강도 (Mean Reversion Strength)
- **낮음 (0.3~0.5)**: oil, gold_ore, copper_ore, aluminum_ore
- **보통 (0.55~0.65)**: 대부분의 제작품
- **높음 (0.7~0.72)**: 원자재 (iron_ore, wood_log), 최고급 무기, 차량

### 희귀도 (Rarity)
- **매우 흔함 (0.8~0.9)**: wood_log, iron_ore, coal
- **흔함 (0.6~0.8)**: fabric, copper_ingot, machine_parts, simple_tools, basic_clothing
- **보통 (0.4~0.6)**: iron_ingot, fine_wood, electronic_components, munitions
- **드묾 (0.2~0.4)**: precision_tools, small_arms, car, airplane
- **매우 드묾 (0.15~0.2)**: tank, heavy_arms

### 부족 가중치 (Scarcity Weight)
- **낮음 (0.3~0.5)**: 원자재, 기본 제작품, machine_parts
- **보통 (0.55~0.7)**: 대부분의 제작품, precision_tools
- **높음 (0.75~0.9)**: 고급 무기, 차량, oil
- **최대 (0.95~1.0)**: fine_wood, airplane, tank

---

## ⚠️ 특이사항 및 잠재적 문제

### 1. **초기 재고 균형**
- **초기 재고 있음**: iron_ore (2000), wood_log (2000), coal (1500), oil (1500), copper_ore (1000), aluminum_ore (500), gold_ore (500), fabric (500), simple_tools (1000)
- **초기 재고 없음**: 나머지 모든 자원 (0)
- **영향**: 게임 시작 시 원자재와 기본 생필품만 사용 가능, 제작 체인 활성화 필요

### 2. **가격 격차**
- **최저가**: wood_log (10)
- **최고가**: tank (25000)
- **격차**: 약 2,500배
- **영향**: 저가 자원은 변동성이 낮고, 고가 자원은 변동성이 높음

### 3. **제작 체인 복잡도**
- **단순 (1단계)**: 원자재 → 1차 가공품
- **중간 (2단계)**: 원자재 → 1차 가공품 → 2차 제작품
- **복잡 (3단계 이상)**: 원자재 → 1차 가공품 → 2차 제작품 → 완제품
- **가장 복잡**: tank (4개 requirements, 중간 단계 포함)

### 4. **전쟁 시 수요 급증 자원**
- **무기**: small_arms, munitions, heavy_arms
- **차량**: tank, airplane
- **부품**: engine, machine_parts, electronic_components
- **원자재**: iron_ore, steel_ingot, oil (무기/차량 제작에 필수)

---

## 🎯 시장 동향 예측

### 안정적 수요 자원 (일반 시민 소비)
- **basic_furniture**: 가구 수요 안정
- **basic_clothing**: 의류 수요 안정
- **simple_tools**: 도구 수요 안정
- **fabric**: 직물 수요 안정

### 변동성 높은 자원 (전쟁/이벤트 영향)
- **weapon**: 전쟁 발발 시 수요 급증
- **vehicle**: 전쟁 발발 시 수요 급증 (tank, airplane), 수요 감소 (car)
- **oil**: 전쟁 발발 시 수요 급증 (전략 자원)

### 공급망 의존성
- **고위험**: tank, airplane (복잡한 제작 체인, 하나라도 부족하면 생산 불가)
- **중위험**: heavy_arms, engine (여러 부품 필요)
- **저위험**: basic_furniture, basic_clothing, simple_tools (단순한 제작 체인)

---

## 📋 자원 총계

- **총 자원 수**: 27개
- **원자재 (Raw)**: 7개
- **금속 (Metal)**: 5개
- **생필품 (Essentials)**: 4개
- **부품 (Component)**: 5개
- **무기 (Weapon)**: 3개
- **차량 (Vehicle)**: 3개

---

## 🔄 제작 체인 요약

### 주요 제작 경로

1. **철 → 강철 → 부품 → 무기/차량**
   - iron_ore → iron_ingot → steel_ingot → machine_parts → small_arms/heavy_arms/tank

2. **나무 → 목재 → 가구**
   - wood_log → fine_wood → basic_furniture

3. **직물 → 의류**
   - wood_log → fabric → basic_clothing

4. **구리/알루미늄 → 전자 부품 → 차량**
   - copper_ore → copper_ingot → electronic_components → car/airplane
   - aluminum_ore → aluminum_ingot → electronic_components (평시 수요 확보)

5. **알루미늄 → 전투기**
   - aluminum_ore → aluminum_ingot → airplane

6. **복합 제작 → 차량**
   - engine + steel_ingot × 2 + electronic_components × 2 + oil × 2 + aluminum_ingot × 1 → car (평시 Aluminum 수요)
   - engine + heavy_arms + steel_ingot × 20 + machine_parts × 5 → tank
   - engine × 2 + aluminum_ingot × 10 + electronic_components × 5 + small_arms → airplane

---

이 분석 문서는 시장 시뮬레이션의 기초가 되는 자원 시스템의 전체적인 구조를 파악하는 데 도움이 됩니다.
