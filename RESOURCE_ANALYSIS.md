# 자원·밸런스 데이터 스냅샷

`Assets/Datas` 기준 (코드: `ResourceType`, 자산 YAML). 옛 문서의 ResourceType 숫자·초기 재고·가격은 폐기되었습니다.

**밸런스 전제:** 이 프로젝트는 단기 세션형이 아니라 **기록 50년 이상을 목표로 하는 초장기 템포 게임**입니다.  
연구·생산·시장 밸런스 해석 시 "초반 체감 속도"와 함께 "수십 년 누적 운영 안정성"을 동시에 기준으로 삼습니다.

---

## ResourceType (`Assets/Scripts/Type/ResourceType.cs`)

| 값 | 열거 이름   | 비고        |
|----|-------------|-------------|
| 0  | raw         | 원자재      |
| 1  | metal       | 괴/정련품   |
| 2  | weapon      | 무기·탄약류 |
| 3  | essentials  | 생필품      |
| 4  | luxuries    | 명품        |
| 5  | component   | 부품·가공목재 등 |
| 6  | vehicle     | 차량        |

---

## 전역 시장 튜닝 (`Assets/Datas/InitialResourceData.asset`)

| 필드 | 값 |
|------|-----|
| volatilityMultiplier | 0.01 |
| maxChangePriceMultiplier | 1.2 |
| priceHistoryCapacity | 60 |
| transactionFee | 0.03 |

개별 자원의 `marketSensitivity`, `meanReversionStrength` 등은 **각 `ResourceData` 자산**에만 있는 경우가 많습니다.

---

## 자원 마스터 (30종)

`baseValue` / `initialAmount` = 자산 값. `recipe` = `requirements` 요약 (원자재는 `-`).

| id | type | baseValue | initial | recipe |
|----|------|-----------|---------|--------|
| aluminum_ore | raw | 78 | 40 | - |
| coal | raw | 26 | 80 | - |
| copper_ore | raw | 52 | 60 | - |
| gold_ore | raw | 208 | 15 | - |
| iron_ore | raw | 36 | 80 | - |
| oil | raw | 42 | 50 | - |
| wood_log | raw | 29 | 200 | - *(primaryOutputPerBatch: 2)* |
| aluminum_ingot | metal | 247 | 0 | aluminum_ore ×3 |
| copper_ingot | metal | 169 | 0 | copper_ore ×3 |
| iron_ingot | metal | 124 | 60 | iron_ore ×3 |
| pure_gold | metal | 689 | 0 | gold_ore ×3 |
| steel_ingot | metal | 286 | 0 | iron_ingot ×2 |
| heavy_arms | weapon | 5850 | 0 | steel_ingot ×10, machine_parts ×5 |
| munitions | weapon | 416 | 0 | steel_ingot ×1, coal ×1 |
| small_arms | weapon | 1235 | 0 | steel_ingot ×2, fine_wood ×1, machine_parts ×1 |
| basic_clothing | essentials | 221 | 0 | fabric ×3 |
| basic_furniture | essentials | 975 | 0 | fine_wood ×3, machine_parts ×1 |
| simple_tools | essentials | 299 | 15 | iron_ingot ×1, fine_wood ×1 |
| premium_clothing | luxuries | 1235 | 0 | fabric ×5, pure_gold ×1 |
| premium_furniture | luxuries | 2340 | 0 | premium_wood ×3, fabric ×2, machine_parts ×4 |
| electronic_components | component | 442 | 0 | copper_ingot ×1, aluminum_ingot ×1 |
| engine | component | 1170 | 0 | machine_parts ×1, electronic_components ×1, steel_ingot ×1 |
| fabric | component | 59 | 0 | oil ×1 |
| fine_wood | component | 65 | 30 | wood_log ×2 |
| machine_parts | component | 286 | 20 | iron_ingot ×2 |
| precision_tools | component | 676 | 10 | steel_ingot ×1, machine_parts ×1 |
| premium_wood | component | 182 | 0 | fine_wood ×2 |
| airplane | vehicle | 10400 | 0 | engine ×2, aluminum_ingot ×10, electronic_components ×5, small_arms ×1 |
| car | vehicle | 3900 | 0 | engine ×1, steel_ingot ×2, electronic_components ×2, oil ×2, aluminum_ingot ×1 |
| tank | vehicle | 17550 | 0 | engine ×1, heavy_arms ×1, steel_ingot ×20, machine_parts ×5 |

**변경 메모:** `fine_wood.asset`의 입력 레시피를 `wood_log ×2`로 정상화했습니다. (이전에는 `resource: {fileID: 0}`로 입력이 무시되어 공짜 생산 가능성이 있었습니다.)

---

## 건물 (`Assets/Datas/Building`, 18종)

파일 `weapon_plant.asset`의 **`id`는 `military_plant`** (파일명과 불일치).

| id | buildCost | maintenanceCost /일 |
|----|-----------|---------------------|
| chemical_plant | 9600 | 18 |
| component_plant | 10800 | 25 |
| engine_plant | 16800 | 30 |
| gold_mine | 23400 | 48 |
| heavy_factory | 18600 | 40 |
| light_factory | 9000 | 18 |
| load | 3900 | 5 |
| logging_camp | 5400 | 15 |
| mine | 18600 | 35 |
| military_plant | 16800 | 35 |
| oil_pump | 22200 | 25 |
| road | 300 | 0 |
| sawmill | 6900 | 15 |
| smelter | 7800 | 18 |
| splitter | 750 | 1 |
| tunnel | 900 | 1 |
| unload | 3900 | 5 |
| vehicle_plant | 29400 | 70 |

배치 상한·수급 보너스 등은 건물 데이터가 아닌 **`Effect`**(예: RawResourceSearch, `Building_MaxPlacedCount`)와 연구 완료 상태에 묶인 경우가 많습니다.

---

## 연구 (`Assets/Datas/Research`)

**연구 인력 스케일 전제:** 연구원은 시스템상 **재화가 있으면 고용을 반복해 늘릴 수 있음** (상한은 경제·급여·만족도 등으로 사실상 제한).  
밸런스·일수 추정은 “고정 N명”이 아니라 **그 시점 현금흐름이 감당하는 연구원 수 → 일일 RP**가 선형으로 커진다는 전제로 잡는다. 연구비는 대략 `(목표 해금 일수) × (그때 기대 일일 RP)`로 역산해 맞추는 편이 안전하다.

**초반 유치 가정(플레이 검증):** 초반에도 연구원 **약 30~50명** 정도는 경제적으로 유지 가능한 구간이 있다.  
`researchPointsPerResearcher = 1`, 효율을 대략 `0.5~1.0`으로 두면 일일 RP는 대략 **15~50** 수준 → 예: `mining`(1400 RP)은 대략 **28~93일** 규모로 읽을 수 있다 (`ResearchDataHandler.CalculateDailyRPProduction` 기준).

### 건물 언락 (`UnlockBuilding/`)

| id | researchPointCost |
|----|-------------------|
| factory_basics | 0 |
| mining | 1400 |
| smelting | 1700 |
| components | 2000 |
| oil_extraction | 3200 |
| chemical_processing | 4000 |
| engine_manufacturing | 5500 |
| heavy_industry | 7000 |
| military_production | 9500 |
| vehicle_assembly | 15000 |
| unlock_gold_mine | 1500 |

### 원자재 탐색 (`RawResourceSearch/`)

`search_*` 계열: 직전 커브 대비 **tier2~3은 약 3배**, **tier4~5는 3배보다 더 올려** 후반 편차를 크게 준다 (forest/mine/oil 동일 티어 비용).

| 계열 | tier1 | tier2 | tier3 | tier4 | tier5 |
|------|-------|-------|-------|-------|-------|
| forest | 0 | 720 | 1620 | 4500 | 12000 |
| mine | 0 | 720 | 1620 | 4500 | 12000 |
| oil | 0 | 720 | 1620 | 4500 | 12000 |
| gold_mine | 0 | 1000 | 2200 | 8000 | — |

---

## 장기 템포 진행 목표 (50년+ 기준)

아래 표는 수치 고정 규칙이 아니라, 밸런스 검증 시 사용하는 **연 단위 목표 가이드**입니다.

| 구간 | 권장 도달 시점 | 플레이 상태 목표 | 체크 포인트 |
|------|----------------|------------------|-------------|
| 초반 정착 | 0~5년 | 벌목/제재/경공업 운영 흑자 전환, `mining`~`components` 진입 | 적자 장기 고착이 아닌지, 연구 선택지가 1개로 고정되지 않는지 |
| 중반 확장 | 5~15년 | 금속-부품-엔진 라인 구축, `oil_extraction`~`engine_manufacturing` 완료 | 연구-건설-인력 확장이 같은 속도로 따라가는지 |
| 중후반 전환 | 15~30년 | `heavy_industry`/`military_production` 선택적 진입, 고부가 상품 비중 증가 | 단일 품목 올인보다 포트폴리오 운영이 유리한지 |
| 장기 체제 | 30~50년+ | `vehicle_assembly` 포함한 고티어 체인 안정화, 이벤트 변동 대응 | 시장 변동(뉴스/오더)에 대응 가능한 현금흐름 유지 여부 |

### 연구 비용 커브 검증 체크리스트

- `UnlockBuilding` 커브는 초반(1400~2000), 중반(3200~7000), 후반(9500~15000)으로 단계가 분리되어야 함
- `RawResourceSearch`는 각 라인 tier1 무료 + tier2~5 점진 상승 구조가 유지되어야 함
- 특정 티어에서 체감이 급락하면 **연구비 인하보다 RP 생산식(연구원 효율/일일 RP 계산·정수 버림)과 고용·급여 곡선**을 우선 점검 (연구원은 재화로 스케일 가능)
- 50년+ 템포 기준에서 "느림"은 허용되지만, **선택 불능(사실상 해금 불가)** 상태는 비정상으로 간주

---

## 직원 (`Assets/Datas/Employee`)

| id | baseSalary | hiringCost | baseEfficiency |
|----|------------|------------|----------------|
| worker | 13 | 100 | 0.5 |
| technician | 19 | 100 | 0.5 |
| researcher | 35 | 100 | 0.5 |
| manager | 50 | 100 | 0.5 |

만족도·급여 관련 보정은 `Assets/Datas/Effect`의 `salary_satisfaction_effect`, `satisfaction_efficiency_effect` 등과 연동됩니다.

---

## 이펙트 (`Assets/Datas/Effect`, 57개)

- **RawResourceSearch/** — 채굴·산림·유전 탐색 티어, 건물 배치 한도 등 (`Building_MaxPlacedCount` 패턴).
- **PriceEventEffect/** — 뉴스·이벤트용 자원별 가격 배율 조정.
- **기타** — `lack_of_managers`, 직원/만족도 보정 이펙트 등.

상세 수치는 자산별로 편차가 크므로, 밸런스 수정 시 해당 **`EffectData` / `InitialEffectData` 참조**를 에디터에서 직접 확인하는 것이 안전합니다.

---

## 뉴스 (`Assets/Datas/News`, 13개)

| id | durationDays |
|----|--------------|
| News_Arms_Race | 60 |
| News_Chip_Shortage | 60 |
| News_Aviation_Breakthrough | 60 |
| News_Auto_Surge | 45 |
| News_Cyber_Monday | 3 |
| News_Gold_Surge | 14 |
| News_Housing_Boom | 90 |
| News_Luxury_Ban | 40 |
| News_Mining_Collapse | 7 |
| News_Oil_Shock | 14 |
| News_Peace_Treaty | 365 |
| News_Steel_Tariffs | 30 |
| News_Textile_Strike | 10 |

---

## 주문 (`Assets/Datas/Order`, 23개)

발주 주체·보상은 각 `OrderData` + `MarketActor` 참조로 정의. **`id`는 파일명과 다를 수 있음** (예: `order_wealthy_merchant_premium_clothing.asset` → id `order_wealthy_merchant_iron_ingot`).

등록된 id 일람:  
`order_art_collector_premium_furniture`, `order_diplomatic_corps_premium_furniture`, `order_global_resource_syndicate_multi`, `order_imperial_academy_multi`, `order_imperial_guard_small_arms`, `order_imperial_logistics_bureau_multi`, `order_imperial_treasury_multi`, `order_industrial_brokerage_network_multi`, `order_industrial_investor_aluminum_ingot`, `order_master_craftsman_premium_wood`, `order_merchant_guild_aluminum_ingot`, `order_military_procurement_office_multi`, `order_national_exchange_multi`, `order_noble_estate_premium_furniture`, `order_premium_industrial_logistics_multi`, `order_private_collector_pure_gold`, `order_public_works_department_multi`, `order_royal_court_premium_furniture`, `order_scholar_electronic_components`, `order_treasury_reserve_pure_gold`, `order_verdant_continental_exchange_multi`, `order_weapon_enthusiast_small_arms`, `order_wealthy_merchant_iron_ingot`.

---

## 시장 행위자 (`Assets/Datas/MarketActor`, 38개)

폴더별 역할 구분:

- **Company/** — 산업체·유통·광업 컨소시엄 등 (주문·수요 패턴의 상대).
- **Government/** — 조달청, 왕실, 학원, 공공 등.
- **Individual/** — 상인, 수집가, 학자 등.

구체 스펙은 각 `MarketActor` 자산이 참조하는 주문·선호 자원을 따릅니다.

### Company 초기 자본금 재조정 메모 (플레이어 경쟁성)

- 플레이어 초기 자금(`InitialFinancesData.initialCredit`)은 `50000`.
- 일부 Company의 `baseWealth`가 플레이어와 같거나 낮아 초반 경쟁 구도가 약해지는 문제가 있어 하위 구간을 상향 조정.
- 조정값:
  - `IronMiningConsortium`: `40000 -> 75000`
  - `LumberConsortium`: `45000 -> 80000`
  - `FrontierSawmill`: `50000 -> 85000`
  - `ArtisanFurnitureWorkshop`: `60000 -> 90000`
  - `VerdantContinentalExchange`: `65000 -> 95000`
  - `NorthwindIndustrialUnion`: `70000 -> 100000`
- 결과적으로 Company 최저 자본금이 플레이어 초기 자금보다 높아져 초반 시장 경쟁 강도를 안정적으로 확보.

### Company 2차 상향 메모 (50년 템포 대응)

- 장기 플레이(50년+) 기준에서 자본 스케일이 작아 성장 체감이 빨리 평탄해지는 문제를 줄이기 위해,
  Company 전원의 `baseWealth`를 추가로 **1.7배 일괄 상향**.
- 현재 Company `baseWealth` 범위:
  - 최소 `136000`
  - 최대 `340000`
- 플레이어 초기 자금(`50000`) 대비 Company 하한이 충분히 높아 초반 경쟁 구도가 명확하며,
  장기 누적 구간에서도 기업 간 규모 차이를 유지하도록 조정.

---

## 이 문서를 갱신할 때

1. `Assets/Datas/Resource/**/*.asset`에서 `id`, `type`, `baseValue`, `initialAmount`, `requirements`를 샘플링해 표와 불일치가 없는지 확인합니다.  
2. `Building`은 **`id` 필드**를 기준으로 스크립트가 동작하므로 파일명과 다르면 표에 주석을 남깁니다.  
3. 연구·이펙트는 개수와 카테고리만 유지하고, 세부 값은 자산을 단일 출처(Single source of truth)로 둡니다.
