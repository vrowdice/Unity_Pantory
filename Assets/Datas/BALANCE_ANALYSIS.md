# 게임 경제 벨런싱 분석

직원 급여·고용비용과 자원 baseValue를 기준으로, 플레이어가 게임을 이어나갈 수 있는 수준으로 정리한 분석입니다.

---

## 1. 현재 수치 요약

### 1.1 재정 기본값
| 항목 | 값 | 비고 |
|------|-----|------|
| 초기 자금 (initialCredit) | 10,000 | `InitialFinancesData.asset` |
| 급여 지급 주기 | **일 단위** | FinancesDataHandler – 매일 차감 |
| 일일 신용 변화 | `자원 수입 − 급여 − 유지비 − 이자` | 자원 = 판매−구매 등 |

### 1.2 직원 비용 (일 단위 기준) — 재평가 반영

| 직종 | baseSalary | hiringCost | firingCost | 역할 개요 |
|------|------------|------------|------------|-----------|
| Worker | 8 | 100 | 10 | 기본 작업 |
| Technician | 12 | 100 | 10 | 조립·제작 |
| Researcher | 22 | 100 | 10 | 연구·레시피 |
| Manager | 34 | 100 | 10 | 사기·피로 관리 |

- **급여 레벨 배율** (InitialEmployeeData): 0.5 / 0.75 / **1.0** / 1.25 / 1.5 (기본 = 보통 1.0)
- **인당 1명, 보통 급여일 때 일급여 합계**: 8+12+22+34 = **76/일** (재평가로 초반 부담 완화)
- **전원 채용 비용**: 4×100 = **400** (초기 자금의 4%)

### 1.3 자원 baseValue (카테고리별)

#### Raw (원자재) — 가격 조정 반영
| id | displayName | baseValue |
|----|-------------|-----------|
| wood_log | Raw Wood | 16 |
| coal | Coal | 18 |
| iron_ore | Iron Ore | 24 |
| oil | Oil | 28 |
| copper_ore | Copper Ore | 36 |
| aluminum_ore | Aluminum Ore | 56 |
| gold_ore | Gold Ore | 150 |

#### Metal (금속)
| id | displayName | baseValue |
|----|-------------|-----------|
| iron_ingot | Iron Ingot | 72 |
| copper_ingot | Copper Ingot | 118 |
| steel_ingot | Steel Ingot | 200 |
| aluminum_ingot | Aluminum Ingot | 142 |
| pure_gold | Pure Gold | 480 |

#### Essentials (필수품)
| id | displayName | baseValue |
|----|-------------|-----------|
| basic_clothing | Basic Clothing | 128 |
| simple_tools | Simple Tools | 140 |
| basic_furniture | Basic Furniture | 300 |

#### Component (부품)
| id | displayName | baseValue |
|----|-------------|-----------|
| fine_wood | Fine Wood | 40 |
| fabric | Fabric | 28 |
| machine_parts | Machine Parts | 180 |
| electronic_components | Electronic Components | 300 |
| premium_wood | Premium Wood | 580 |
| precision_tools | Precision Tools | 580 |
| engine | Engine | 2,800 |

#### Luxuries (사치품)
| id | displayName | baseValue |
|----|-------------|-----------|
| premium_clothing | Premium Clothing | 1,380 |
| premium_furniture | Premium Furniture | 4,000 |

#### Weapon (무기)
| id | displayName | baseValue |
|----|-------------|-----------|
| munitions | Munitions | 350 |
| small_arms | Small Arms | 1,150 |
| heavy_arms | Heavy Arms | 16,500 |

#### Vehicle (차량)
| id | displayName | baseValue |
|----|-------------|-----------|
| car | Car | 6,600 |
| airplane | Fighter Jet | 13,200 |
| tank | Battle Tank | 27,500 |

### 1.4 건물 (건설비·유지비·필요 인원) — 재평가 반영

건물은 배치된 수만큼 **일일 유지비**가 재정에서 차감됩니다. (FinancesDataHandler – `CalculateTotalMaintenanceCostOfAllPlaced`)

| id | displayName | buildCost | maintenanceCost | requiredEmployees | 비고 |
|----|-------------|-----------|-----------------|-------------------|------|
| road | Road | 10 | 1 | 0 | 기본 해금 |
| load | Load | 100 | 8 | 2 | 기본 해금 |
| unload | Unload | 100 | 8 | 2 | 기본 해금 |
| logging_camp | Logging Camp | 140 | 6 | 5 | 기본 해금, 원목 생산 |
| sawmill | Sawmill | 180 | 8 | 4 | 기본 해금 |
| light_factory | Light Factory | 240 | 10 | 6 | 기본 해금, 가구·의류·도구 |
| chemical_plant | Chemical Plant | 250 | 13 | 4 | 연구 해금 |
| component_plant | Component Factory | 280 | 12 | 5 | 연구 해금 |
| smelter | Smelter | 200 | 10 | 4 | 연구 해금 |
| mine | Mine | 500 | 22 | 8 | 연구 해금 |
| oil_pump | Oil Pump | 600 | 30 | 3 | 연구 해금 |
| engine_plant | Engine Factory | 450 | 20 | 5 | 연구 해금 |
| weapon_plant | Military Factory | 450 | 20 | 6 | 연구 해금 |
| heavy_factory | Heavy Factory | 500 | 22 | 6 | 연구 해금 |
| vehicle_plant | Vehicle Assembly Plant | 800 | 35 | 12 | 연구 해금 |

- **유지비 합계 예시 (초반 최소)**: Road×N(1/개) + Load(8) + Unload(8) + Sawmill(8) + Light Factory(10) = **34 + 도로 수**/일 (재평가로 인하).
- **전체 가동 시**: 위 표 건물 전부 배치 시 유지비만 **약 245/일** (도로 제외).

---

## 2. 벨런싱 관점 정리

### 2.1 직원 vs 초기 자금 vs 건물 유지비
- 초기 10,000으로 전원(4명) 채용 시 400 지출 → **9,600 잔여**.
- 보통 급여만 유지해도 **일 76** 지출(재평가 반영). 여기에 **건물 유지비**가 더해짐 (예: 초반 최소 라인만 써도 Load+Unload+Sawmill+Light Factory만 해도 **34/일**).
- **일일 고정 지출 예시**: 급여 76 + 유지비 34 = **110/일** → 9,600 ÷ 110 ≈ **87일분** (이자·자원 구매 제외).
- **정리**: 초기 자금 대비 채용비는 부담이 크지 않음. **급여 + 배치한 건물 유지비**를 합친 “일일 고정비”를 상회하는 수입(자원 판매 등)이 나와야 플레이가 이어짐.

### 2.2 자원 가격대 구간
- **저가 (10~50)**: Raw 대부분, fine_wood, fabric  
  → 초반 생산·판매로 일일 수입을 만드는 구간.
- **중가 (100~500)**: Metal 일부, Essentials 전부, Component 일부  
  → 안정기에 자주 거래할 구간.
- **고가 (1,000~2,500)**: small_arms, premium_clothing, engine  
  → 수입이 쌓인 뒤 목표로 할 구간.
- **초고가 (6,000~25,000)**: car, airplane, tank, heavy_arms  
  → 장기 목표, 한 번에 큰 수입/지출이 나는 구간.

### 2.3 “이어나가기”를 위한 체크포인트
1. **초반 (1~2주차)**  
   - 저가 Raw/간단 제작품만으로 **일일 수입 > (급여 76 + 배치 건물 유지비)** 가 나오는지.  
   - wood_log baseValue 16, initialAmount 5000 등은 “초반 버티기”용으로 적절한지.  
   - 초반에 배치 가능한 건물만 써도 유지비가 **약 34**이므로, **일일 수입 110 이상**을 목표로 잡기 좋음(재평가 반영).
2. **중반**  
   - Essentials/Component 판매로 **급여 + 유지비 + 약간의 여유**가 나오는지.  
   - Smelter, Component Factory, Chemical Plant 등 추가 시 유지비가 10~15씩 늘어나므로, 수입이 그만큼 커졌는지 확인.
3. **후반**  
   - Vehicle/Weapon/Luxuries는 “목표 구매” 또는 “대량 판매”로 자금을 크게 움직이도록 설계되어 있는지.  
   - Mine(25), Oil Pump(35), Vehicle Plant(40) 등 고유지비 건물은 수익이 확실할 때 추가하는 것이 안전.

---

## 3. 건물 생산 기반 유지 가능성 평가

현재 로직 기준으로, **자원을 건물(스레드)로만 생산할 때** 일일 고정비(급여 + 유지비)를 상회하는 수입이 나오는지 정리한 평가입니다.

### 3.1 생산 규칙 요약

- **진행 방식**: 매일 각 배치 스레드마다 `진행도 += (현재 인원 / 필요 인원) × 품질 효율`. 진행도 ≥ 1이 되면 1배치 생산(입력 차감, 출력 증가) 후 진행도 차감.
- **실질 생산량**: 필요 인원을 100% 채우면 **스레드당 1배치/일**, 인원이 부족하면 그 비율만큼 감소(예: 50% 채우면 2일에 1배치).
- **1배치**: 해당 스레드의 레시피 1회분(출력 자원 수만큼 생산, 입력 자원 수만큼 소모). 거래 시 수수료 5% 적용(판매 ≈ 기준가×0.95, 구매 ≈ 기준가×1.05).

따라서 “유지”를 위해서는 **스레드에서 나오는 순 수입(판매 수입 − 구매 비용)이 일일 급여 + 일일 유지비를 넘어야** 합니다.

### 3.2 초반 구간 (기본 해금 건물만)

| 생산품 | 출력 기준가 | 1배치 입력 비용(대략) | 1배치 순이익(기준가 기준) | 해당 건물 유지비 |
|--------|-------------|------------------------|----------------------------|------------------|
| wood_log | 16 | 0 | ≈16 | Logging 6 |
| fine_wood | 40 | wood 2×16 = 32 | ≈8 | Sawmill 8 |
| fabric (Sawmill) | 28 | wood 2×16 = 32 | ≈−4 | Sawmill 8 |
| basic_clothing | 128 | fabric 3×28 = 84 | ≈44 | Light 10 |
| simple_tools | 140 | fine_wood 1×40 + iron_ingot 1×72 = 112 | ≈28 | Light 10 |
| basic_furniture | 300 | fine_wood 4×40 + iron_ingot 2×72 = 304 | ≈−4 | Light 10 |

- 원목(wood_log)만 팔면 1배치당 약 16 수입(가격 조정 후), 로깅 캠프 유지비 6을 넘기지만 **급여 76을 상쇄하려면 하루에 원목 약 5배치 이상**이 필요합니다(재평가 반영).
- 필요 인원: Load(2) + Unload(2) + Logging(5) = **9명**. 직원 4명이면 효율 약 4/9 → **약 2.25일에 1배치** 수준이라, 원목만으로는 수입이 급여+유지비를 감당하기 어렵습니다.
- fine_wood, basic_clothing, simple_tools는 배치당 이익이 있지만, 같은 인원/스레드 구조에서 **일일 배치 수**가 적으면 총 수입이 110(급여 76 + 유지비 34)에 도달하기 힘듭니다.

**결론 (초반)**: 기본 해금 건물만으로는, **직원 4명 + 최소 스레드 1개** 구성에서 건물 생산만으로 일일 고정비를 지속적으로 상회하기 **어렵습니다**. 초기 자금(10,000)으로 버티는 동안 직원 증원·고부가가치 라인 추가(또는 시장/미션 수입)가 필요합니다.

### 3.3 중·후반 구간 (연구 해금 건물)

- **Chemical Plant**: oil 1 → fabric 1. fabric 기준가 20, oil 20 → 입력·출력 가격이 비슷해 **마진이 작음**. 유지비 15를 넘으려면 fabric 가격이 오르거나 다른 라인과 연계해야 함.
- **Smelter / Component / Engine / Weapon / Vehicle**: 중간재·완제품이라 **1배치당 이익이 크지만**, 필요 인원(4~12명)과 유지비(12~40)가 큽니다. **인원을 채우고** 해당 라인을 안정 가동하면 유지비 상회는 가능한 구조입니다.
- **Mine / Oil Pump**: 원자재 직접 생산, 유지비 25/35로 높음. 생산량(배치/일)과 시장 가격이 유지비를 넘을 수준이어야 수지가 맞습니다.

**결론 (중·후반)**: 연구로 해금하는 고부가가치 건물은 **인원을 채운 뒤** 가동하면 유지 가능성이 있으나, **초반에 인원·자금이 부족한 상태**에서는 건물 생산만으로는 유지가 잘 되지 않는 구조입니다.

### 3.4 종합 평가

| 항목 | 판정 | 비고 |
|------|------|------|
| 초반(기본 건물만) | **유지 개선** | 재평가로 급여+유지비 ≈**110**. 자원 가격·유지비 인하로 달성 난이도 완화. 여전히 직원 증원·고부가 라인 또는 시장/미션 보완 권장. |
| 중반(연구 해금 + 인원 충당) | **유지 가능** | Essentials·Component 등으로 스레드당 이익을 올리고 인원을 채우면 수지 맞출 여지 있음. |
| 후반(고가 차량·무기) | **유지 가능** | 1배치 단가가 커서, 가동만 되면 유지비 회수 가능. 다만 초기 설비·인원 투자 부담 있음. |

**권장 조정 방향 (유지 용이화)**  
- 초반이 너무 빡빡하면: **원목·fine_wood·basic_clothing 등 초반 생산품 baseValue 소폭 상향**, 또는 **Logging/Sawmill/Light Factory 필요 인원(requiredEmployees) 소폭 하향**, 또는 **초반 건물 유지비 소폭 하향**.  
- “건물로만 버티기”를 의도하지 않았다면: **시장 거래·미션 보상·초기 자원(initialAmount)** 으로 초반 수입을 보강하는 설계가 필요합니다.

---

## 4. 조정 시 권장 사항 (요약)

- **직원**
  - 재평가 반영 후 **baseSalary 합 76/일**.  
    추가로 완화하려면 Worker/Technician을 더 낮추고, 난이도를 올리려면 전원 또는 Manager/Researcher 위주로 소폭 상향 검토.
  - `hiringCost` 100은 초기 자금 10,000 대비 적당한 수준으로 유지해도 됨.

- **자원**
  - **Raw**: 초반 생존용이므로 baseValue를 크게 올리지 않는 편이 안전. (올리면 초반 수입은 좋아지지만 난이도 하락.)
  - **Essentials / Component**: 플레이어가 “이걸 만들면 수입이 난다”고 느끼는 구간이므로,  
    일일 급여 76의 2~5일치(152~380) 정도에 걸친 제품이 있으면 목표 설정이 쉬움.
  - **Vehicle/Weapon**: 단가가 크므로 “가격만 조정”보다는 **생산 난이도·레시피·수요(시장/미션)** 와 함께 보는 것이 좋음.

- **초기 자금**
  - 10,000이면 “전원 채용 + 수일치 급여”까지는 여유 있음.  
  - 초반 튜토리얼/난이도를 더 쉽게 하려면 `initialCredit` 소폭 상향,  
    더 타이트하게 하려면 7,000~8,000대로 낮추는 식으로 조정 가능.

- **건물**
  - 초반 필수 라인만 써도 유지비 **약 34/일**(재평가 반영)이므로,  
    일일 수입이 **급여(76) + 유지비(34)** ≈ 110을 넘도록 Raw/Essentials 판매가 나오는지가 핵심.
  - 고유지비 건물(Oil Pump 30, Vehicle Plant 35, Mine 22)은 **buildCost**도 크므로,  
    해당 라인 수익이 유지비를 상회한 뒤 추가하는 흐름이 자연스러움.
  - Road(1/개)는 개수만큼 누적되므로, 도로를 많이 깔수록 유지비가 소폭 증가함.

---

## 5. 수치만 빠르게 참고할 때

- **일일 급여 (보통 기준, 재평가 반영)**: Worker 8, Technician 12, Researcher 22, Manager 34 → **합 76**.
- **채용비**: 100/명, **해고비**: 10/명.
- **초기 자금**: 10,000.
- **자원 (baseValue, 가격 조정 반영)**: Raw 16~150, Metal 72~480, Essentials 128~300, Component 28~2,800, Luxuries 1,380~4,000, Weapon 350~16,500, Vehicle 6,600~27,500.
- **건물 유지비 (재평가 반영)**: Road 1, Load/Unload 8, Logging 6, Sawmill 8, Light 10, Chemical 13, Component 12, Smelter 10, Mine 22, Oil 30, Engine/Military 20, Heavy 22, Vehicle 35. (전부 배치 시 약 **245/일** + 도로.)

이 문서는 `Assets/Datas/Employee/*.asset`, `Assets/Datas/Resource/**/*.asset`, `Assets/Datas/Building/*.asset`, `Assets/Datas/Order/*.asset`, `Assets/Datas/Research/*.asset`, `InitialFinancesData.asset`, `InitialOrderData.asset`, `InitialResearchData.asset`, 그리고 급여·유지비·재정·오더·연구 로직(FinancesDataHandler, EmployeeDataHandler, ResourceDataHandler, ThreadPlacement, OrderDataHandler, ResearchDataHandler)을 기준으로 작성되었습니다.  
실제 난이도는 **레시피, 생산 속도, 시장 변동, 유지비, 이자, 오더 발생/납기, 연구 RP/비용** 등과 함께 조정하는 것을 권장합니다.

---

## 6. 전체 벨런스 재평가 및 보고

**기준일**: 가격·급여·유지비 조정 반영 후.  
**목적**: 현재 수치로 플레이가 이어질 수 있는지, 구간별로 요약·판정·리스크를 정리합니다.

---

### 6.1 현재 벨런스 스냅샷

| 구분 | 항목 | 값 |
|------|------|-----|
| **재정** | 초기 자금 | 10,000 |
| | 채용 후 가용(전원 4명) | 9,600 |
| **일일 고정비** | 급여(보통, 4명) | 76 |
| | 초반 최소 건물 유지비(Load+Unload+Sawmill+Light) | 34 |
| | **합계(초반 최소)** | **110/일** |
| | 전체 건물 가동 시 유지비(도로 제외) | 약 245 |
| **버티기** | 수입 0 가정 시 9,600으로 버틸 수 있는 일수 | 약 **87일** |

---

### 6.2 구간별 벨런스 판정

| 구간 | 일일 고정비(대략) | 목표 일일 수입 | 판정 | 비고 |
|------|-------------------|----------------|------|------|
| **초반** | 76+34=**110** | ≥110 | **도전적** | 스레드 1개·인원 4명만으로는 1배치/일 미만. 원목·간단 제작품만으로 110 달성 어려움. 직원 증원·고부가 라인·시장/미션 보완 권장. |
| **중반** | 76+50~80 | ≥130~160 | **가능** | Essentials·Component 생산, 인원 보강 시 수지 맞출 여지 있음. |
| **후반** | 76+200~245 | ≥280~320 | **가능** | Vehicle/Weapon/Luxuries 1배치 단가가 커서, 가동만 되면 유지비 회수 가능. |

---

### 6.3 생산 경제 요약 (1배치당, 기준가·수수료 반영)

- **원목**: 수입 ≈16, 비용 0, 유지비 6 → **스레드당 순기여 +10**. 110 상회하려면 원목만으로 **약 7배치/일** 필요(인원 9명 풀가동 시 이론상 1배치/일 수준).
- **basic_clothing**: 순이익 ≈44, 유지비 10 → **스레드당 순기여 +34**. 110 상회에 **약 3.2배치/일** 필요.
- **simple_tools**: 순이익 ≈28, 유지비 10 → **스레드당 순기여 +18**. 110 상회에 **약 6배치/일** 필요.

→ 초반에는 **한 스레드로는 110 달성 불가**. **직원 수 증가**로 동시 가동 스레드 수를 늘리거나, **시장 판매·미션·초기 재고**로 수입을 보완하는 설계가 필요합니다.

---

### 6.4 리스크 요인

- **마이너스 이자**: 채권(initialCredit 음수) 시 `negativeInterestRate`(0.5%)만큼 일일 이자 부담 → 빚이면 고정비가 더 커짐.
- **가격 변동**: 자원은 `currentValue`가 변동하므로, 기준가 대비 판매 단가 하락 시 마진 감소.
- **인원 부족**: 필요 인원 미달 시 배치/일이 줄어들어 같은 스레드로는 수입이 더 적어짐.

---

### 6.5 종합 결론 및 권장

| 항목 | 내용 |
|------|------|
| **전체 판정** | 가격·급여·유지비 조정으로 **초반 목표 일일 고정비 110**으로 낮아졌고, 중·후반은 인원·라인 확대 시 **유지 가능**한 구조. |
| **초반** | **건물 생산만**으로 110을 채우기는 **어렵다**. 초기 자금 9,600으로 **약 87일** 버티는 동안 **직원 증원**, **고부가 라인 추가**, **시장/미션/초기 재고** 중 하나 이상이 있으면 플레이가 이어지기 수월함. |
| **추가 완화 옵션** | 초반을 더 쉽게 하려면: (1) Worker/Technician baseSalary 추가 인하, (2) Logging/Sawmill/Light requiredEmployees 소폭 인하, (3) initialCredit 소폭 상향, (4) wood_log·fine_wood 등 초반 baseValue 소폭 상향. |
| **추가 강화 옵션** | 난이도를 올리려면: (1) 급여·유지비 소폭 상향, (2) initialCredit 인하, (3) 초반 자원 baseValue 인하. |

이 보고는 위 1~5절 수치와 동일한 에셋·로직을 기준으로 한 **재평가 요약**입니다.

---

## 7. 오더(의뢰) 벨런스 평가 및 보고

**연계**: 일일 고정비(급여+유지비) **110**을 상회하려면 건물 생산만으로는 부족하므로, **오더 완료 보상**이 초반 수입 보강 수단으로 중요합니다.  
**기준**: `Assets/Datas/Order/*.asset`, `OrderDataHandler.cs`, `InitialOrderData.asset`.

---

### 7.1 오더 시스템 요약

| 항목 | 값 | 비고 |
|------|-----|------|
| **발생** | 매일 `baseOrderChance(0.1)` + `orderChanceIncrement(0.05)` 누적, 또는 **10일** 경과 시 100% 1회 발생 | `TryGenerateOrder` |
| **최대 동시 오더** | 5개 | `maxOrderItems` |
| **수락 대기** | 7일 | `orderAcceptanceDelayDays` — 이 안에 수락하지 않으면 소멸 |
| **납기** | 수락 후 오더별 **9~15일** | `OrderData.durationDays` — 미완료 시 소멸 + 해당 거래처 신뢰도 `-rewardTrust/2` |
| **의뢰인 타입** | Individual(항상), Company(Wealth≥500만 해금), Government(Wealth≥1,000만 해금) | 초·중반에는 Individual만 |

**보상 산식** (`OrderDataHandler.CreateOrderInstance`):

- **요청 수량**: 자원별 `count = max(1, round( (Wealth × scaleFactor) / 자원 수 / currentValue ))`  
  → **Wealth**(신용+재고가치+건물가치)가 크면 오더 규모가 커짐.
- **보상 금액**: `rewardCredit = Σ(count × currentValue) × (priceMultiplier + trustBonus)`  
  - `trustBonus = (거래처 신뢰도 - 50) × 0.001` (최대 약 +0.05 수준).
- **시장 대비**: `priceMultiplier`가 전 오더 **1.23~1.45**이므로, 시장 판매(기준가, 수수료 5%)보다 **오더 완료가 항상 유리**.

---

### 7.2 오더 데이터 벨런스 요약

| 구간 | scaleFactor | priceMultiplier | durationDays | rewardTrust |
|------|-------------|-----------------|--------------|-------------|
| **저부담** (소규모) | 0.11~0.13 | 1.23~1.27 | 9~11 | 18~22 |
| **중간** | 0.14~0.18 | 1.28~1.35 | 11~13 | 23~30 |
| **고부담** (대규모) | 0.19~0.25 | 1.35~1.45 | 13~15 | 30~40 |

- **자원 종류**: Raw(iron_ore, oil), Metal(iron_ingot, aluminum_ingot, steel_ingot, pure_gold), Component(electronic_components, machine_parts, premium_wood), Essentials·Luxuries(premium_clothing, premium_furniture), Weapon(small_arms) 등 — **초반 재고(예: wood 5000)로 납품 가능한 오더**와 **생산/조달 필요 오더**가 혼재.

---

### 7.3 전체 벨런스와의 연계

| 항목 | 내용 |
|------|------|
| **초반 수입 보강** | 일일 고정비 110을 건물만으로 채우기 어려우므로, **오더 1~2회 완료**로 수일치 분(예: 1,000~3,000) 수입이 나오면 초반 유지에 크게 도움. |
| **오더 규모** | 요청량이 **Wealth**에 비례. 초기 Wealth ≈ 초기자금+재고가치(예: wood 5000×16)라서, **재고가 많을수록 오더 규모·보상이 커짐**. 반대로 재고를 다 팔면 Wealth·오더 규모가 줄어듦. |
| **가격 프리미엄** | `priceMultiplier` 1.23~1.45로 **시장보다 23~45% 유리** → 오더 우선 수행이 이득. |
| **리스크** | 수락 후 **납기일(9~15일) 내** 납품 실패 시 오더 소멸 + **신뢰도 감소**. 고가 자원(small_arms, premium_furniture 등) 오더는 생산 능력·재고를 보고 수락 여부를 결정하는 것이 안전. |

---

### 7.4 오더 벨런스 판정

| 항목 | 판정 | 비고 |
|------|------|------|
| **보상 수준** | **적정** | 시장 대비 프리미엄으로 오더가 의미 있는 수입원이며, 초반 110 달성 보조에 기여 가능. |
| **발생 빈도** | **적정** | 10일 내 1회 보장으로, 수입이 없는 구간이 너무 길지 않음. |
| **규모(scaleFactor)** | **주의** | Wealth에 비례해 커지므로, **초기 재고가 큰 경우** 첫 오더가 “너무 큰 수량”으로 나올 수 있음. 반대로 재고를 거의 없애면 오더가 매우 작아져 보상이 미미해질 수 있음. |
| **Government/Company 해금** | **후반 전용** | Wealth 500만/1,000만은 후반 구간이므로, 초·중반은 Individual만 고려하면 됨. |

---

### 7.5 권장 사항 (오더 연계)

- **초반**: 오더를 **초반 수입 보강**으로 활용하려면, 수락 전에 **납기일 안에 납품 가능한지**(재고·생산 능력)를 확인. 원목·저가 재료 위주 오더는 초기 재고로 처리 가능한 경우가 많음.
- **데이터 조정**: 오더가 너무 크게 느껴지면 `scaleFactor` 소폭 인하(예: 0.2 → 0.15); 너무 작으면 인상. 초반용 Individual 오더의 `durationDays`를 12~15일로 두면 납기 여유가 생김.

이 절은 `OrderData`, `OrderState`, `OrderDataHandler`, `InitialOrderData` 및 `Assets/Datas/Order/*.asset` 기준으로 작성되었습니다.

---

## 8. 연구(Research) 벨런스 평가 및 보고

**연계**: 연구원은 **일급 22**(재평가 반영)의 유지비가 들며, RP(연구력) 생산만으로는 직접 수입이 없음(자동 특허 모드가 아니면). 연구 해금은 **건물·오더 확대**로 이어지므로, **연구 비용 vs 연구원 유지비·해금 타이밍**이 벨런스에 영향을 줍니다.  
**기준**: `Assets/Datas/Research/*.asset`, `ResearchDataHandler.cs`, `InitialResearchData.asset`, `InitialEmployeeData.researchPointsPerResearcher`.

---

### 8.1 연구 시스템 요약

| 항목 | 값 | 비고 |
|------|-----|------|
| **초기 RP** | 100 | `InitialResearchData.initialResearchPoint` |
| **연구원당 RP/일** | 1 | `InitialEmployeeData.researchPointsPerResearcher` (에셋 기준; 스크립트 기본값 5와 다를 수 있음) |
| **일일 RP 생산** | `연구원 수 × researchPointsPerResearcher × currentEfficiency` | `ResearchDataHandler.CalculateDailyRPProduction` |
| **연구원 일급** | 22 | Researcher `baseSalary` (재평가 반영) |
| **자동 특허 모드** | RP를 신용(돈)으로 전환 가능 | 켜면 `generatedRP`만큼 `ModifyCredit` 호출 → 현재 1 RP = 1 크레딧이면 연구원 1명 = 1/일 수입 vs 22/일 급여로 **적자** |

→ 연구원 1명 기준: **1 RP/일**, **22/일 급여**. 연구는 “비용(RP)”만 소모하고, 수입은 해금된 건물·오더로 간접적으로만 발생합니다.

---

### 8.2 연구별 RP 비용 및 “연구원·유지비” 관점

| 연구 id | researchPointCost | 연구원 1명 시 소요 일수 | 해당 기간 연구원 급여(대략) |
|---------|-------------------|-------------------------|-----------------------------|
| factory_basics | 0 | 0 | 0 |
| mining | 1,000 | 1,000일 | 22,000 |
| smelting | 1,200 | 1,200일 | 26,400 |
| chemical_processing | 2,400 | 2,400일 | 52,800 |
| components | 1,400 | 1,400일 | 30,800 |
| oil_extraction | 2,000 | 2,000일 | 44,000 |
| engine_manufacturing | 3,000 | 3,000일 | 66,000 |
| heavy_industry | 3,600 | 3,600일 | 79,200 |
| military_production | 5,000 | 5,000일 | 110,000 |
| vehicle_assembly | 7,000 | 7,000일 | 154,000 |

- **초기 RP 100**만으로는 mining(1,000)에도 **900 RP = 900일** 추가 필요(연구원 1명 가정).
- **연구원 1명**으로 첫 해금(mining)까지 **약 2.7년** 걸리며, 그동안 연구원 급여만 **약 22,000** 지출. 게임 플레이 관점에서 **진행이 지나치게 느림**.

---

### 8.3 전체 벨런스와의 연계

| 항목 | 내용 |
|------|------|
| **일일 고정비** | 연구원 1명이면 **76(전원 급여) 중 22**가 연구원 몫. RP는 수입이 아니므로, 연구는 “고정비를 감수하고 미래 해금을 위한 투자”로만 작동. |
| **해금 타이밍** | Smelter, Mine, Chemical Plant, Component/Engine/Weapon/Vehicle 등 **수익 건물**이 연구에 묶여 있음. 연구가 너무 느리면 중·후반 콘텐츠 도달이 지연됨. |
| **자동 특허** | 1 RP = 1 크레딧이면 연구원 1명 = 1/일 수입 vs 22/일 급여 → **채용만으로는 적자**. 특허 모드를 수입원으로 쓰려면 `researchPointsPerResearcher`를 크게 올리거나, RP당 크레딧 환산을 별도로 두는 설계가 필요. |

---

### 8.4 연구 벨런스 판정

| 항목 | 판정 | 비고 |
|------|------|------|
| **RP 생산량** | **과소** | 연구원당 1 RP/일은 현재 연구 비용(1,000~7,000)과 맞지 않음. 첫 연구(mining)까지 **900일** 이상 소요. |
| **연구 비용** | **과다** | 1,000~7,000 RP는 “연구원 1명·1 RP/일” 가정 시 **수백~수천 일** 수준. 플레이 페이스와 맞추려면 비용 인하 또는 RP/일 상향 필요. |
| **연구원 유지비** | **고려됨** | 연구원 급여 22/일은 전체 고정비(76)의 일부로 이미 반영. 다만 **RP 대비 가치**(해금 이득 vs 22×일수)가 현재 수치로는 매우 나쁨. |

---

### 8.5 권장 사항 (연구 벨런스)

- **RP 생산 상향 (권장)**  
  - `InitialEmployeeData.researchPointsPerResearcher`를 **5~20** 정도로 올리면, 연구원 1명으로 mining(1,000)을 **50~200일** 안에 도달 가능.  
  - 예: **10**이면 초기 100 + 90일 = 1,000 → **약 90일**에 mining 해금, 그동안 연구원 급여 **약 1,980**.

- **연구 비용 인하 (대안)**  
  - `researchPointCost`를 구간별로 대폭 낮춤. 예: mining 100, smelting 150, components 200, …  
  - 그러면 “연구원당 1 RP/일”이어도 첫 단계 해금이 **수십~백 일** 단위로 맞춰짐.

- **자동 특허 모드**  
  - “연구력 판매”로 수입을 내려면 **1 RP당 크레딧**을 연구원 급여와 맞추거나(예: 1 RP = 25 크레딧), `researchPointsPerResearcher`를 올려서 RP/일 수입이 22를 넘도록 조정하는 것이 자연스러움.

- **초기 RP**  
  - `initialResearchPoint` 100은 “첫 연구까지 버티기”용으로는 적절할 수 있으나, **연구 비용·RP/일**과 함께 보아, 위 조정 중 하나와 맞추는 것이 좋음.

이 절은 `ResearchData`, `ResearchDataHandler`, `InitialResearchData`, `InitialEmployeeData` 및 `Assets/Datas/Research/*.asset` 기준으로 작성되었습니다.
