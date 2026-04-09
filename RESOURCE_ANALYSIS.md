# 자원·밸런스 데이터 스냅샷

`Assets/Datas` 기준 (코드: `ResourceType`, 자산 YAML). 옛 문서의 ResourceType 숫자·초기 재고·가격은 폐기되었습니다.

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
| transactionFee | 0.08 |

개별 자원의 `marketSensitivity`, `meanReversionStrength` 등은 **각 `ResourceData` 자산**에만 있는 경우가 많습니다.

---

## 자원 마스터 (30종)

`baseValue` / `initialAmount` = 자산 값. `recipe` = `requirements` 요약 (원자재는 `-`).

| id | type | baseValue | initial | recipe |
|----|------|-----------|---------|--------|
| aluminum_ore | raw | 60 | 40 | - |
| coal | raw | 20 | 80 | - |
| copper_ore | raw | 40 | 60 | - |
| gold_ore | raw | 160 | 15 | - |
| iron_ore | raw | 28 | 80 | - |
| oil | raw | 32 | 50 | - |
| wood_log | raw | 22 | 200 | - *(primaryOutputPerBatch: 2)* |
| aluminum_ingot | metal | 190 | 0 | aluminum_ore ×3 |
| copper_ingot | metal | 130 | 0 | copper_ore ×3 |
| iron_ingot | metal | 95 | 60 | iron_ore ×3 |
| pure_gold | metal | 530 | 0 | gold_ore ×3 |
| steel_ingot | metal | 220 | 0 | iron_ingot ×2 |
| heavy_arms | weapon | 4500 | 0 | steel_ingot ×10, machine_parts ×5 |
| munitions | weapon | 320 | 0 | steel_ingot ×1, coal ×1 |
| small_arms | weapon | 950 | 0 | steel_ingot ×2, fine_wood ×1, machine_parts ×1 |
| basic_clothing | essentials | 170 | 0 | fabric ×3 |
| basic_furniture | essentials | 750 | 0 | fine_wood ×3, machine_parts ×1 |
| simple_tools | essentials | 190 | 15 | iron_ingot ×1, fine_wood ×1 |
| premium_clothing | luxuries | 950 | 0 | fabric ×5, pure_gold ×1 |
| premium_furniture | luxuries | 1800 | 0 | premium_wood ×3, fabric ×2, machine_parts ×4 |
| electronic_components | component | 340 | 0 | copper_ingot ×1, aluminum_ingot ×1 |
| engine | component | 900 | 0 | machine_parts ×1, electronic_components ×1, steel_ingot ×1 |
| fabric | component | 45 | 0 | oil ×1 |
| fine_wood | component | 50 | 30 | wood_log ×2 |
| machine_parts | component | 220 | 20 | iron_ingot ×2 |
| precision_tools | component | 520 | 10 | steel_ingot ×1, machine_parts ×1 |
| premium_wood | component | 140 | 0 | fine_wood ×2 |
| airplane | vehicle | 8000 | 0 | engine ×2, aluminum_ingot ×10, electronic_components ×5, small_arms ×1 |
| car | vehicle | 3000 | 0 | engine ×1, steel_ingot ×2, electronic_components ×2, oil ×2, aluminum_ingot ×1 |
| tank | vehicle | 13500 | 0 | engine ×1, heavy_arms ×1, steel_ingot ×20, machine_parts ×5 |

**변경 메모:** `fine_wood.asset`의 입력 레시피를 `wood_log ×2`로 정상화했습니다. (이전에는 `resource: {fileID: 0}`로 입력이 무시되어 공짜 생산 가능성이 있었습니다.)

---

## 건물 (`Assets/Datas/Building`, 15종)

파일 `weapon_plant.asset`의 **`id`는 `military_plant`** (파일명과 불일치).

| id | buildCost | maintenanceCost /일 |
|----|-----------|---------------------|
| logging_camp | 140 | 15 |
| road | 10 | 0 |
| mine | 500 | 35 |
| light_factory | 240 | 22 |
| vehicle_plant | 800 | 70 |
| unload | 100 | 5 |
| oil_pump | 600 | 25 |
| engine_plant | 450 | 30 |
| component_plant | 280 | 25 |
| military_plant | 450 | 35 |
| heavy_factory | 500 | 40 |
| sawmill | 180 | 15 |
| chemical_plant | 250 | 18 |
| smelter | 200 | 18 |
| load | 100 | 5 |

배치 상한·수급 보너스 등은 건물 데이터가 아닌 **`Effect`**(예: RawResourceSearch, `Building_MaxPlacedCount`)와 연구 완료 상태에 묶인 경우가 많습니다.

---

## 연구 (`Assets/Datas/Research`)

### 건물 언락 (`UnlockBuilding/`)

| id | researchPointCost |
|----|-------------------|
| factory_basics | 0 |
| mining | 1000 |
| smelting | 1200 |
| components | 1400 |
| oil_extraction | 2000 |
| chemical_processing | 2400 |
| engine_manufacturing | 3000 |
| heavy_industry | 3600 |
| military_production | 5000 |
| vehicle_assembly | 7000 |

### 원자재 탐색 (`RawResourceSearch/`)

`search_*` 계열은 자산상 대부분 **`researchPointCost: 0`** (티어/슬롯 해금용으로 쓰이는 구조로 보임).

---

## 직원 (`Assets/Datas/Employee`)

| id | baseSalary | hiringCost | baseEfficiency |
|----|------------|------------|----------------|
| worker | 15 | 100 | 0.5 |
| technician | 22 | 100 | 0.5 |
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

---

## 이 문서를 갱신할 때

1. `Assets/Datas/Resource/**/*.asset`에서 `id`, `type`, `baseValue`, `initialAmount`, `requirements`를 샘플링해 표와 불일치가 없는지 확인합니다.  
2. `Building`은 **`id` 필드**를 기준으로 스크립트가 동작하므로 파일명과 다르면 표에 주석을 남깁니다.  
3. 연구·이펙트는 개수와 카테고리만 유지하고, 세부 값은 자산을 단일 출처(Single source of truth)로 둡니다.
