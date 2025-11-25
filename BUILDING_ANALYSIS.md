# 건물(Building) 상황 분석 및 설명

## 📊 BuildingType 매핑
- **0**: Distribution (유통/인프라)
- **1**: Production (생산)

## 📊 ResourceType 매핑 (건물 생산 가능 자원)
- **0**: raw (원자재)
- **1**: metal (금속)
- **2**: wood (나무)
- **3**: tool (도구)
- **4**: weapon (무기)
- **5**: furniture (가구)
- **6**: clothing (옷)
- **7**: component (부품)
- **8**: electronics (전자제품)
- **9**: vehicle (차량)

---

## 🏭 생산 건물 (Production Buildings) - 12개

### 📍 1차 산업: 채굴 (Extraction) - 3개

#### 1. **Mine** (광산)
- **ID**: `mine`
- **타입**: Production (1)
- **역할**: 광물 채굴 - 철광석, 석탄, 구리, 금, 알루미늄 광석 생산
- **특징**:
  - **생산 자원**: raw(0) - iron_ore, coal, copper_ore, gold_ore, aluminum_ore
  - **입력 필요 없음**: 지하에서 직접 채굴
  - **크기**: 2×2
  - **건설 비용**: 500 (높음)
  - **유지비**: 25 (높음)
  - **생산 속도**: 0.8 (느림)
  - **처리 시간**: 1.5 (느림)
  - **입력 위치**: {0, 0} (없음)
  - **출력 위치**: {1, 0}
- **전략**: 자급자족의 핵심, 시장 의존도 감소, 높은 투자 필요

#### 2. **Oil Pump** (시추기)
- **ID**: `oil_pump`
- **타입**: Production (1)
- **역할**: 석유 시추 - 원유 생산
- **특징**:
  - **생산 자원**: raw(0) - oil
  - **입력 필요 없음**: 지하에서 직접 시추
  - **크기**: 2×2
  - **건설 비용**: 600 (매우 높음)
  - **유지비**: 35 (매우 높음)
  - **생산 속도**: 0.7 (느림)
  - **처리 시간**: 1.8 (느림)
  - **입력 위치**: {0, 0} (없음)
  - **출력 위치**: {1, 0}
- **전략**: 전략 자원 생산, 매우 높은 유지비, 화학 공장의 원료

#### 3. **Sawmill** (제재소)
- **ID**: `sawmill`
- **타입**: Production (1)
- **역할**: 벌목 - 원목 채집
- **특징**:
  - **생산 자원**: raw(0) - wood_log
  - **입력 필요 없음**: 주변 숲에서 직접 채집
  - **크기**: 2×2
  - **건설 비용**: 140 (저렴)
  - **유지비**: 7 (저렴)
  - **생산 속도**: 1.0
  - **처리 시간**: 1.0
  - **입력 위치**: {0, 0} (없음)
  - **출력 위치**: {1, 0}
- **전략**: 초반 원자재 공급, 저렴한 비용, 목재 가공의 기초

---

### 🔧 2차 산업: 가공 (Processing) - 3개

#### 4. **Smelter** (제련소)
- **ID**: `smelter`
- **타입**: Production (1)
- **역할**: 금속 제련 - 광석을 괴로 변환
- **특징**:
  - **생산 자원**: metal(1) - iron_ingot, copper_ingot, aluminum_ingot, pure_gold 등
  - **입력**: 광석(raw)
  - **크기**: 2×2
  - **건설 비용**: 200
  - **유지비**: 12
  - **생산 속도**: 1.0
  - **처리 시간**: 1.2
  - **입력 위치**: {-1, 1}
  - **출력 위치**: {2, 1}
- **전략**: 금속 산업의 핵심, 광산과 연계하여 운영

#### 5. **Lumber Mill** (목재소)
- **ID**: `lumber_mill`
- **타입**: Production (1)
- **역할**: 목재 가공 - 원목을 목재/직물로 변환
- **특징**:
  - **생산 자원**: wood(2) - fine_wood, fabric
  - **입력**: wood_log
  - **크기**: 2×2
  - **건설 비용**: 180
  - **유지비**: 10
  - **생산 속도**: 1.0
  - **처리 시간**: 1.0
  - **입력 위치**: {-1, 1}
  - **출력 위치**: {2, 1}
- **전략**: 제재소 옆에 배치 시 효율적, 목재 및 직물 생산

#### 6. **Chemical Plant** (화학 공장)
- **ID**: `chemical_plant`
- **타입**: Production (1)
- **역할**: 화학 가공 - 원유를 직물로 변환
- **특징**:
  - **생산 자원**: Essentials(5) - fabric
  - **입력**: oil
  - **크기**: 2×2
  - **건설 비용**: 250
  - **유지비**: 15
  - **생산 속도**: 1.0
  - **처리 시간**: 1.1
  - **입력 위치**: {-1, 1}
  - **출력 위치**: {2, 1}
- **전략**: 석유 의존 직물 생산, 나무 대안 제공

---

### 🏭 3차 산업: 제조 (Manufacturing) - 6개

#### 7. **Component Factory** (부품 공장)
- **ID**: `component_plant`
- **타입**: Production (1)
- **역할**: 부품 제조 - 기계 부품, 전자 부품, 정밀 도구 생산
- **특징**:
  - **생산 자원**: component(7) - machine_parts, electronic_components, precision_tools
  - **입력**: metal ingots
  - **크기**: 2×2
  - **건설 비용**: 280
  - **유지비**: 14
  - **생산 속도**: 1.0
  - **처리 시간**: 1.0
  - **입력 위치**: {-1, 1}
  - **출력 위치**: {2, 1}
- **전략**: 중간 제작 자원, 차량/무기 제작 필수

#### 8. **Light Factory** (경공업 공장)
- **ID**: `light_factory`
- **타입**: Production (1)
- **역할**: 경공업 - 가구, 의류, 간단한 도구 생산
- **특징**:
  - **생산 자원**: tool(3), furniture(5), clothing(6)
  - **입력**: wood, fabric
  - **크기**: 2×2
  - **건설 비용**: 240
  - **유지비**: 12
  - **생산 속도**: 1.0
  - **처리 시간**: 0.9 (빠름)
  - **입력 위치**: {-1, 1}
  - **출력 위치**: {2, 1}
- **전략**: 다목적 생산, 시장 수요에 따라 제품 전환 가능, 초반 수익원

#### 9. **Engine Factory** (엔진 공장)
- **ID**: `engine_plant`
- **타입**: Production (1)
- **역할**: 엔진 제조 - 차량용 엔진 생산
- **특징**:
  - **생산 자원**: component(7) - engine
  - **입력**: machine_parts, electronic_components, steel_ingot
  - **크기**: 2×2
  - **건설 비용**: 450 (높음)
  - **유지비**: 22 (높음)
  - **생산 속도**: 0.8 (느림)
  - **처리 시간**: 2.0 (느림)
  - **입력 위치**: {-1, 1}
  - **출력 위치**: {2, 1}
- **전략**: 차량 생산의 선행 건물, 고가 자원, 복잡한 제작 체인

#### 10. **Heavy Factory** (중공업 공장)
- **ID**: `heavy_factory`
- **타입**: Production (1)
- **역할**: 중공업 - 정밀 도구 및 고급 제품 생산
- **특징**:
  - **생산 자원**: tool(3) - precision_tools, luxury_goods
  - **입력**: steel_ingot, advanced components
  - **크기**: 2×2
  - **건설 비용**: 500 (높음)
  - **유지비**: 25 (높음)
  - **생산 속도**: 0.9
  - **처리 시간**: 1.5
  - **입력 위치**: {-1, 1}
  - **출력 위치**: {2, 1}
- **전략**: 고급 제품 생산, 높은 투자 필요, 프리미엄 시장 타겟

#### 11. **Military Factory** (군수 공장)
- **ID**: `military_plant`
- **타입**: Production (1)
- **역할**: 군수품 생산 - 소형 무기, 중화기, 탄약 생산
- **특징**:
  - **생산 자원**: weapon(4) - small_arms, heavy_arms, munitions
  - **입력**: steel_ingot, machine_parts
  - **크기**: 2×2
  - **건설 비용**: 450 (높음)
  - **유지비**: 22 (높음)
  - **생산 속도**: 1.0
  - **처리 시간**: 1.4
  - **입력 위치**: {-1, 1}
  - **출력 위치**: {2, 1}
- **전략**: 전쟁 시 수요 급증, 고가 자원, 전략적 중요성

#### 12. **Vehicle Plant** (차량 조립 공장)
- **ID**: `vehicle_plant`
- **타입**: Production (1)
- **역할**: 차량 조립 - 자동차, 전차, 전투기 생산
- **특징**:
  - **생산 자원**: vehicle(9) - car, tank, airplane
  - **입력**: engine, steel_ingot, components, electronics
  - **크기**: 3×3 (대형)
  - **건설 비용**: 800 (매우 높음)
  - **유지비**: 40 (매우 높음)
  - **생산 속도**: 0.5 (느림)
  - **처리 시간**: 3.0 (매우 느림)
  - **입력 위치**: {-1, 1}
  - **출력 위치**: {3, 1}
- **전략**: 최고가 자원 생산, 복잡한 제작 체인, 대규모 투자 필요

---

## 🏗️ 인프라 건물 (Infrastructure Buildings) - 3개

### 13. **Road** (도로)
- **ID**: `road`
- **타입**: Distribution (0)
- **역할**: 생산 라인 연결 인프라
- **특징**:
  - **크기**: 1×1
  - **건설 비용**: 10 (매우 저렴)
  - **유지비**: 1 (매우 저렴)
  - **입출력 위치**: 없음
- **전략**: 모든 생산 라인의 기초, 필수 인프라

### 14. **Load** (로딩 스테이션)
- **ID**: `load`
- **타입**: Distribution (0)
- **역할**: 창고에서 생산 라인으로 자원 로딩
- **특징**:
  - **크기**: 1×1
  - **건설 비용**: 100
  - **유지비**: 10
  - **입력 위치**: {-1, 0}
  - **출력 위치**: 없음 (생산 라인으로 직접 전달)
- **전략**: 생산 라인의 시작점, 창고와 생산 라인 연결

### 15. **Unload** (언로딩 스테이션)
- **ID**: `unload`
- **타입**: Distribution (0)
- **역할**: 생산 라인에서 창고로 완제품 언로딩
- **특징**:
  - **크기**: 1×1
  - **건설 비용**: 100
  - **유지비**: 10
  - **입력 위치**: 없음 (생산 라인에서 직접 수신)
  - **출력 위치**: {1, 0}
- **전략**: 생산 라인의 종료점, 완제품을 창고로 이동

---

## 📊 종합 분석

### 건물 분류

#### 1. **1차 산업 (Extraction)** - 3개
- **Mine**: 광물 채굴
- **Oil Pump**: 석유 시추
- **Sawmill**: 벌목

#### 2. **2차 산업 (Processing)** - 3개
- **Smelter**: 금속 제련
- **Lumber Mill**: 목재 가공
- **Chemical Plant**: 화학 가공

#### 3. **3차 산업 (Manufacturing)** - 6개
- **Component Factory**: 부품 제조
- **Light Factory**: 경공업
- **Engine Factory**: 엔진 제조
- **Heavy Factory**: 중공업
- **Military Factory**: 군수품
- **Vehicle Plant**: 차량 조립

#### 4. **인프라 (Infrastructure)** - 3개
- **Road**: 도로
- **Load**: 로딩 스테이션
- **Unload**: 언로딩 스테이션

---

## 💰 비용 분석

### 건설 비용 (Base Cost)

#### **저가 건물** (10~200)
- **Road**: 10 (최저가)
- **Sawmill**: 140
- **Lumber Mill**: 180
- **Smelter**: 200

#### **중가 건물** (240~450)
- **Light Factory**: 240
- **Chemical Plant**: 250
- **Component Factory**: 280
- **Engine Factory**: 450
- **Military Factory**: 450

#### **고가 건물** (500~800)
- **Mine**: 500
- **Heavy Factory**: 500
- **Oil Pump**: 600
- **Vehicle Plant**: 800 (최고가)

### 유지비 (Maintenance Cost)

#### **저유지비** (1~15)
- **Road**: 1 (최저)
- **Sawmill**: 7
- **Lumber Mill**: 10
- **Load/Unload**: 10
- **Smelter**: 12
- **Light Factory**: 12
- **Component Factory**: 14
- **Chemical Plant**: 15

#### **중유지비** (22~25)
- **Engine Factory**: 22
- **Military Factory**: 22
- **Mine**: 25
- **Heavy Factory**: 25

#### **고유지비** (35~40)
- **Oil Pump**: 35 (매우 높음)
- **Vehicle Plant**: 40 (최고)

---

## ⚙️ 생산 효율 분석

### 생산 속도 (Base Production Rate)

#### **표준 속도** (1.0)
- 대부분의 생산 건물: Sawmill, Lumber Mill, Smelter, Component Factory, Light Factory, Military Factory

#### **저속도** (0.5~0.9)
- **Vehicle Plant**: 0.5 (가장 느림)
- **Oil Pump**: 0.7
- **Mine**: 0.8
- **Engine Factory**: 0.8
- **Heavy Factory**: 0.9

### 처리 시간 (Processing Time)

#### **빠른 처리** (0.9~1.2)
- **Light Factory**: 0.9 (가장 빠름)
- **Sawmill, Lumber Mill, Component Factory**: 1.0
- **Chemical Plant**: 1.1
- **Smelter**: 1.2

#### **느린 처리** (1.4~3.0)
- **Military Factory**: 1.4
- **Mine**: 1.5
- **Heavy Factory**: 1.5
- **Oil Pump**: 1.8
- **Engine Factory**: 2.0
- **Vehicle Plant**: 3.0 (가장 느림)

### 효율성 지표 (생산 속도 / 처리 시간)

#### **고효율** (>1.0)
- **Light Factory**: 1.0 / 0.9 = 1.11 (최고 효율)
- **Sawmill, Lumber Mill, Component Factory**: 1.0 / 1.0 = 1.0

#### **표준 효율** (0.8~1.0)
- **Chemical Plant**: 1.0 / 1.1 = 0.91
- **Smelter**: 1.0 / 1.2 = 0.83
- **Military Factory**: 1.0 / 1.4 = 0.71
- **Heavy Factory**: 0.9 / 1.5 = 0.6
- **Mine**: 0.8 / 1.5 = 0.53

#### **저효율** (<0.5)
- **Oil Pump**: 0.7 / 1.8 = 0.39
- **Engine Factory**: 0.8 / 2.0 = 0.4
- **Vehicle Plant**: 0.5 / 3.0 = 0.17 (최저 효율)

---

## 📐 크기 분석

### 소형 건물 (1×1)
- **Road**: 1×1
- **Load**: 1×1
- **Unload**: 1×1

### 중형 건물 (2×2)
- **모든 생산 건물** (Mine, Oil Pump, Sawmill, Smelter, Lumber Mill, Chemical Plant, Component Factory, Light Factory, Engine Factory, Heavy Factory, Military Factory)

### 대형 건물 (3×3)
- **Vehicle Plant**: 3×3 (유일한 대형 건물)

---

## 🔗 생산 체인 분석

### 원자재 생산 체인
```
Mine (iron_ore, coal, copper_ore, gold_ore, aluminum_ore)
Oil Pump (oil)
Sawmill (wood_log)
```

### 1차 가공 체인
```
Mine → Smelter (ore → ingot)
Sawmill → Lumber Mill (log → fine_wood, fabric)
Oil Pump → Chemical Plant (oil → fabric)
```

### 2차 제작 체인
```
Smelter → Component Factory (ingot → machine_parts, electronic_components)
Lumber Mill → Light Factory (wood/fabric → furniture/clothing/tools)
Component Factory → Engine Factory (parts → engine)
```

### 3차 완제품 체인
```
Component Factory + Smelter → Heavy Factory (steel/parts → precision_tools)
Component Factory + Smelter → Military Factory (steel/parts → weapons)
Engine Factory + Components → Vehicle Plant (engine/steel → vehicles)
```

---

## 🎯 전략적 건물 우선순위

### 초반 필수 건물 (게임 시작)
1. **Sawmill**: 원자재 공급의 기초
2. **Lumber Mill**: 목재 가공
3. **Smelter**: 금속 제련 (광산 건설 전까지는 시장 구매)
4. **Road**: 생산 라인 연결
5. **Load/Unload**: 물류 시스템

### 초반 수익 건물 (안정적 수요)
1. **Light Factory**: 빠른 생산 속도, 다목적 생산, 저렴한 비용
2. **Component Factory**: 중간 제작 자원

### 중반 확장 건물 (자급자족)
1. **Mine**: 시장 의존도 감소, 자급자족 시작
2. **Oil Pump**: 석유 자급자족
3. **Chemical Plant**: 직물 생산 경로 확장

### 중반 고가 자원 건물
1. **Engine Factory**: 차량 생산 준비
2. **Military Factory**: 전쟁 대비
3. **Heavy Factory**: 고급 제품 생산

### 후반 투자 건물 (대규모 투자)
1. **Vehicle Plant**: 최고가 자원 생산

---

## ⚠️ 특이사항 및 개선점

### 1. **자급자족 가능**
- ✅ **1차 산업 건물 추가**: Mine, Oil Pump로 원자재 자급자족 가능
- ✅ **시장 의존도 감소**: 플레이어가 자원 생산 선택권 확보
- **영향**: 전략적 선택의 여지 증가, 시장 가격 변동에 덜 취약

### 2. **2차 산업 분리**
- ✅ **Smelter**: 금속 제련 전문화
- ✅ **Lumber Mill**: 목재 가공 전문화
- ✅ **Chemical Plant**: 화학 가공 전문화
- **영향**: 각 건물의 역할이 명확해짐, 전략적 배치 중요성 증가

### 3. **3차 산업 통합**
- ✅ **Light Factory**: Furniture + Textile + Tool 통합
- ✅ **Component Factory**: Machine Parts + Electronic Components + Precision Tools 통합
- ✅ **Military Factory**: Small Arms + Heavy Arms + Munitions 통합
- **영향**: 건물 수 감소, 관리 편의성 증가, 유연한 생산 전환 가능

### 4. **생산 효율 격차**
- **최고 효율**: Light Factory (1.11)
- **최저 효율**: Vehicle Plant (0.17)
- **격차**: 약 6.5배
- **영향**: 차량 생산은 투자 대비 효율이 낮지만 최고가 자원

### 5. **비용 대비 효율**
- **저비용 고효율**: Light Factory (240/12, 효율 1.11)
- **고비용 저효율**: Vehicle Plant (800/40, 효율 0.17)
- **영향**: 초반에는 저비용 건물이 유리, 후반에는 고가 자원 생산 필요

---

## 📈 건물 투자 회수 분석

### 건설 비용 대비 유지비 비율
- **낮은 비율** (<10%): Road (10%), Sawmill (5%), Lumber Mill (5.6%)
- **표준 비율** (10~15%): 대부분의 건물
- **높은 비율** (>15%): Oil Pump (5.8%), Vehicle Plant (5%)

### 예상 회수 기간 (가정: 일일 수익 = 유지비의 2배)
- **빠른 회수** (<50일): Light Factory, Lumber Mill
- **표준 회수** (50~100일): 대부분의 건물
- **느린 회수** (>100일): Vehicle Plant, Oil Pump

---

## 🔄 생산 라인 설계 가이드

### 기본 생산 라인 구조
```
[Storage] → Load → Road → [Production Building] → Road → Unload → [Storage]
```

### 최적화 팁
1. **입력 위치**: 대부분 {-1, 1} (왼쪽 위)
2. **출력 위치**: 대부분 {2, 1} (오른쪽 위)
3. **1차 산업 예외**: 입력 {0, 0}, 출력 {1, 0}
4. **Vehicle Plant**: 출력 {3, 1} (더 오른쪽)

### 효율적인 라인 배치
- **단일 자원 라인**: 하나의 건물만 사용
- **다단계 라인**: 여러 건물을 연결하여 복잡한 제작 체인 구성
- **병렬 라인**: 같은 건물을 여러 개 배치하여 생산량 증가

### 전략적 배치 예시
- **Sawmill + Lumber Mill**: 제재소 옆에 목재소 배치
- **Mine + Smelter**: 광산 옆에 제련소 배치
- **Oil Pump + Chemical Plant**: 시추기 옆에 화학 공장 배치

---

## 📋 건물 총계

- **총 건물 수**: 15개
- **생산 건물 (Production)**: 12개
  - **1차 산업**: 3개
  - **2차 산업**: 3개
  - **3차 산업**: 6개
- **인프라 건물 (Infrastructure)**: 3개

---

## 🎮 게임플레이 전략

### 초반 전략 (자금 부족)
1. **Sawmill + Lumber Mill**: 기본 생산 체인 구축
2. **Light Factory**: 빠른 수익 창출, 다목적 생산
3. **Road + Load/Unload**: 물류 시스템 구축
4. **시장에서 광석 구매**: Mine 건설 전까지

### 중반 전략 (자금 확보)
1. **Mine**: 자급자족 시작, 시장 의존도 감소
2. **Smelter**: 금속 제련 시작
3. **Component Factory**: 중간 제작 자원 생산
4. **Oil Pump + Chemical Plant**: 석유 자급자족

### 후반 전략 (대규모 투자)
1. **Engine Factory**: 차량 생산 준비
2. **Vehicle Plant**: 최고가 자원 생산
3. **Military Factory**: 전쟁 대비
4. **Heavy Factory**: 고급 제품 생산
5. **병렬 생산 라인**: 생산량 증대

---

## ✅ 리팩토링 완료 내역

### 추가된 건물
- ✅ **Mine**: 광물 채굴 (1차 산업)
- ✅ **Oil Pump**: 석유 시추 (1차 산업)
- ✅ **Smelter**: 금속 제련 (2차 산업, 기존 Refinery Plant 변경)
- ✅ **Lumber Mill**: 목재 가공 (2차 산업)
- ✅ **Chemical Plant**: 화학 가공 (2차 산업)
- ✅ **Light Factory**: 경공업 통합 (3차 산업)
- ✅ **Engine Factory**: 엔진 제조 (3차 산업)
- ✅ **Heavy Factory**: 중공업 (3차 산업)
- ✅ **Military Factory**: 군수품 (3차 산업, 기존 Weapon Factory 변경)

### 통합된 건물
- ✅ **Light Factory**: Furniture Workshop + Textile Factory + Tool Workshop 통합
- ✅ **Component Factory**: Machine Parts + Electronic Components + Precision Tools 통합
- ✅ **Military Factory**: Small Arms + Heavy Arms + Munitions 통합

### 삭제된 건물
- ❌ **Assembly Plant**: Light Factory로 통합
- ❌ **Clothing Plant**: Light Factory로 통합
- ❌ **Furniture Plant**: Light Factory로 통합
- ❌ **Tool Plant**: Light Factory로 통합
- ❌ **Electronics Plant**: Component Factory로 통합
- ❌ **Refinery Plant**: Smelter로 변경

### 개선 효과
- ✅ **자급자족 가능**: 1차 산업 건물 추가로 시장 의존도 감소
- ✅ **역할 명확화**: 2차 산업 건물 분리로 각 건물의 역할 명확
- ✅ **관리 편의성**: 3차 산업 건물 통합으로 건물 수 감소
- ✅ **전략적 선택**: 플레이어가 생산 경로를 선택할 수 있는 유연성 증가

---

이 분석 문서는 플레이어가 효율적인 생산 라인을 설계하고 전략적으로 건물을 선택하는 데 도움이 됩니다.
