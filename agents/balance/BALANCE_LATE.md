# 🧮 후반 경제 밸런스 및 AI 하네스 가이드 (BALANCE_LATE.md)

후반(1000명 이상 극대화 구간)은 **자동화(Automation)와 대규모 해고 폭동의 대치 위기** 속에서, **초고가 최종 완제품의 폭발적인 마진**을 통해 50년 경영 대장정의 마침표를 찍는 시기입니다.

---

## 1. 후반 위기: 자동화 (Workforce 1000명 돌파 시 강제 발동)
플레이어가 대형 완성차 조립 공장과 중공업 단지를 거느리며 직원 1000명을 초과하는 초대형 기업이 되는 시점, 게임의 클라이맥스인 **자동화 메인 이벤트(후반 위기)**가 발동합니다.

* **위기 체감**:
  - **기계식 자동화 설비 도입**: 1000명이 넘는 노동 군중의 엄청난 일일 인건비를 상쇄하기 위해, 플레이어는 인력을 대체할 '자동화 생산 기계'를 공장에 본격 도입하기 시작합니다.
  - **노동 군중의 분노와 해고 공포**: 일자리를 잃을 위기에 처한 노동 군중이 분노하여 대규모 폭동, 공장 설비 사보타주(파괴), 사기 폭락 사태를 일으킵니다.
* **경영적 성취감**:
  - 플레이어는 기술 발전이 주는 극강의 가동 효율성과, 군중 직원들의 복지 및 직업적 안정성 사이에서 정교한 외줄타기를 수행해야 합니다. 
  - 50년 장기 집권의 최종 마침표로서 기업적 책임과 기술 혁신의 융합이라는 깊이 있는 경영의 무게감을 줍니다.

---

## 2. 후반 초고가 완제품의 압도적인 하이리턴 (성취감의 종착지)
수십 년의 장기 템포 끝에 마침내 완성한 초고부가 조립 상품들은 플레이어의 그동안의 노고를 보상하기 위해 **엄청난 폭리(초고마진)**를 보장합니다.

* **중무장 완제품 체인 (Vehicle Assembly Plant / Military Factory)**:
  - **`car` (자동차)**: 엔진 1(1170) + 강철 2(572) + 전자부품 2(884) + 오일 2(84) + 알루미늄주괴 1(247) = 2,957 -> 판매가 **6,600 크레딧** (**+3,643 마진**)
  - **`heavy_arms` (중화기)**: 강철주괴 10(2860) + 기계부품 5(1430) = 4,290 -> 판매가 **16,500 크레딧** (**+12,210 마진!**)
  - **`tank` (전투 탱크)**: 엔진 1(1170) + 중화기 1(16500) + 강철 20(5720) + 기계부품 5(1430) = 24,820 -> 판매가 **27,500 크레딧** (**+2,680 마진**)
* **기획 의도**:
  - 온갖 중간재와 원자재의 복잡한 공급망 네트워크를 완성해 낸 플레이어에게 주어지는 **가장 화려한 금융적 카타르시스**입니다. 
  - 이 초고마진은 자동화 폭동으로 인한 가동 마비나 공장 재배치 시 발생하는 대규모 손실을 넉넉히 흡수해 주는 튼튼한 방어벽이 됩니다.

---

## 3. 🤖 AI 에이전트 하네스 엔지니어링 가이드 (Harness Guide)

차세대 AI 에이전트나 자동화 밸런스 테스트 에이전트(Harness)가 Pantory의 밸런스 에셋을 기계적으로 분석·갱신·테스트할 수 있도록 하는 **엔지니어링 표준 파이프라인**입니다.

### 3.1 밸런스 에셋 매핑 트리 (ScriptableObject Schema)
AI는 밸런싱 수정 시 반드시 아래 디렉토리 트리 스키마에 따라 타겟 파일을 매핑해야 합니다.

```text
Assets/ScriptableObjects/
├── Init/
│   ├── InitialFinancesData.asset    # initialCredit (초기 자금 100,000 조율)
│   ├── InitialResearchData.asset    # initialResearchPoint (시작 연구점수 100 조율)
│   └── MainEvent/
│       └── InitialUnionMainEventData.asset 
│                                    # unionEmployeeCountToStart (노조 트리거 50명)
│                                    # unionDaysToComplete (노조 해결 기한 720일)
│                                    # unionDailyCreditCost (데일리 파업 손실 200)
├── Employee/
│   ├── Worker.asset                 # baseSalary: 10, hiringCost: 100
│   ├── Technician.asset             # baseSalary: 20, hiringCost: 150
│   ├── Researcher.asset             # baseSalary: 80, hiringCost: 300
│   └── Manager.asset                # baseSalary: 120, hiringCost: 500
├── Resource/
│   ├── Raw/                         # wood_log (29) 등 baseValue
│   ├── Metal/                       # iron_ingot (124) 등 baseValue
│   └── Essentials/                  # basic_furniture (300) 등 baseValue
└── Building/
    ├── road.asset                   # buildCost: 300, maintenanceCost: 0
    ├── sawmill.asset                # buildCost: 6,900, maintenanceCost: 15 (CapEx)
    └── light_factory.asset          # buildCost: 9,000, maintenanceCost: 18 (CapEx)
```

### 3.2 밸런스 검증 파이프라인 표준 (Harness Validation Pipeline)

AI 에이전트가 밸런스 에셋을 변경하거나 테스트를 가동할 때 반드시 준수해야 하는 **4대 유효성 검증 공식(Standard Constraints)**입니다.

1. **에셋 구문 및 형식 보존 검증 (Syntax Guard)**:
   - Unity `.asset` 파일은 YAML 형식을 사용합니다. AI는 값을 수정할 때 파일의 `fileID`, `guid`, `m_Script` 참조 구조를 절대 훼손하지 않아야 하며, 오직 원시 텍스트의 타겟 필드만 정확하게 교체해야 합니다.
2. **초반 캐시 플로우 최소 생존 한계선 검증 (CapEx Constraint)**:
   - 시작 자금(`initialCredit`)은 극초반 최소 5대 필수 가공 건물 건설비의 합보다 반드시 **2배 이상** 커야 합니다.
   - 공식: `initialCredit >= (load.buildCost + unload.buildCost + logging_camp.buildCost + sawmill.buildCost + light_factory.buildCost) * 2`
3. **가공 마진 역전 리스크 자동 검증 (Negative Margin Guard)**:
   - 완제품 가치가 투입된 재료의 가격 합보다 작아져 생산할수록 손해를 보는 적자 마진을 원천 금지합니다.
   - 공식: `Σ (Requirements[i].count * baseValue[i]) * 1.15 <= FinishedResource.baseValue`
4. **노조 적자 시뮬레이션 공식 검증 (Union Deficit Resilience Check)**:
   - 노조 파업 이벤트가 발동했을 때, 매일 차감되는 파업 손실(`200/일`)과 당시 인원(50~100명)의 인건비 부담이 플레이어의 초반 가구/도구 공장 일일 순수익의 80%를 넘지 않아야 합니다.
   - 공식: `unionDailyCreditCost + (50 * averageWages) <= DailyNetIncomeOfEarlyFactory * 0.8`
