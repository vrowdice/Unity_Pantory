# 🧭 Pantory AI 에이전트 페어 프로그래밍 가이드 (AGENTS.md)

이 문서는 Pantory 프로젝트의 기획 비전, 자원 체인, 경제 밸런스 설계를 담당하는 **AI 에이전트(개발/기획/검증)를 위한 종합 인덱스 및 레퍼런스 지침서**입니다.

프로젝트 루트의 [agents](file:///c:/Users/Vrowdice/Desktop/Project/Unity/Unity_Pantory/agents) 폴더 내에 수록된 핵심 기획 분석 문서들은 기능에 따라 분류되어 있으며, 모든 AI 에이전트는 코드를 수정하거나 새로운 시스템을 설계하기 전에 반드시 본 인덱스를 참조하여 각 도메인 지식을 동기화해야 합니다.

---

## 📁 기능별 기획 문서 분류 및 요약 (agents/)

각 항목의 파일명 링크를 클릭하면 해당 도메인의 상세 기획/분석 문서로 직접 이동할 수 있습니다.

### 1. 🏛️ 코어 게임 디자인 및 기획 비전 (Core Game Design & Vision)
* **파일명**: [PANTORY_GDD.md](file:///c:/Users/Vrowdice/Desktop/Project/Unity/Unity_Pantory/agents/PANTORY_GDD.md)
* **기능 요약**: 
  - Pantory의 세계관, **50년 이상의 초장기 템포**가 지니는 게임성 비전, 그리고 자원 채집 → 가공 → 완제품 제작 → 시장 거래로 이어지는 핵심 **코어 루프(Core Loop)**를 정의합니다.
  - 플레이어가 단일 품목 올인이 아닌 **횡적 확장 빌드(물량 승부)** vs **종적 연구 러시 빌드(테크 선점)**라는 다채로운 샌드박스 빌드 다양성을 느낄 수 있도록 돕습니다.
  - 3단계로 이어지는 **세대적 3대 위기(초반 노조 → 중반 전쟁 → 후반 자동화)**를 게임 진행 템포에 녹여내어, 타이쿤 게임의 깊이 있는 긴장감과 극복의 성취감을 제공합니다.

### 2. 📊 핵심 요약 및 발표 프레젠테이션 (Executive Summary & Pitching)
* **파일명**: [PANTORY_PPT_SUMMARY.md](file:///c:/Users/Vrowdice/Desktop/Project/Unity/Unity_Pantory/agents/PANTORY_PPT_SUMMARY.md)
* **기능 요약**: 
  - 코어 기획을 10개의 핵심 섹션으로 압축하여 발표 자료(PPT) 및 빠른 개념 숙지에 최적화된 **익스프레스 서머리북**입니다.
  - 한 줄 장르 소개, 플레이 판타지, 자원 가격대별 카테고리 기획 원칙, 시설군(벌목/제재/경공업/물류) 설계 의도, 뉴스 및 오더의 역할 등을 슬라이드 노트 형식으로 쉽고 빠르게 대조할 수 있도록 돕습니다.

### 3. 📦 마스터 자원 및 건물 데이터베이스 (Resource & Building Database)
* **상세 경로**: [agents/balance/RESOURCE_ANALYSIS.md](file:///c:/Users/Vrowdice/Desktop/Project/Unity/Unity_Pantory/agents/balance/RESOURCE_ANALYSIS.md)
* **기능 요약**: 
  - 프로젝트 내에 실제 탑재된 **30종의 자원(원자재, 금속, 필수품, 부품, 사치품 등)의 실제 baseValue 가격**, **완전히 0개로 시작하는 초기 재고량**, 레시피 요구 사항을 총망라한 데이터베이스 마스터입니다.
  - **18종 건물**의 실제 건설비와 일일 유지비, **연구 해금 트랙**의 요구 연구력(RP), **13종 뉴스**의 지속 일수, **23종 오더(의뢰)**의 마스터 ID 및 **38개 시장 참여자**들의 자본 스케일 등 하드웨어 콘텐츠 파라미터를 정확하게 매핑 및 수록하고 있습니다.

### 4. 🧮 3단계 경제성 시뮬레이션 및 AI 자동 검증 지침 (Game Economy & AI Harness standard)
성장 단계 및 대규모 고용에 따른 세대적 위기에 맞춘 3대 경제 밸런스 기획 분석서 및 하네스 파이프라인 세부 항목입니다.

* **[BALANCE_EARLY.md](file:///c:/Users/Vrowdice/Desktop/Project/Unity/Unity_Pantory/agents/balance/BALANCE_EARLY.md) (초반 밸런스 분석 - 1~100명 구간)**
  - **요약**: 100,000 크레딧 시작 자금과 가구 공방의 일일 흑자 기어 구조 및 횡적 확장(생산 라인 증설) vs 종적 테크 러시 비즈니스 분기를 분석합니다.
  - **특징**: 수입 후 단순 조립 시 마진이 0%에 수렴하는 조밀한 소비재 가격(가구 300, 도구 140, 의류 128) 설계를 통해 **자급자족 수직 계열화의 성취감**을 극대화합니다. 모든 자원은 완전히 0개에서 시작합니다.
  - **다양성**: 플레이어의 생산 라인 확장을 감안한 유연한 샌드박스 빌드 다양성을 지원합니다.

* **[BALANCE_MID.md](file:///c:/Users/Vrowdice/Desktop/Project/Unity/Unity_Pantory/agents/balance/BALANCE_MID.md) (중반 밸런스 분석 - 100~1000명 구간)**
  - **요약**: 연구원(80/일)과 매니저(120/일) 채용 시점에 유도되는 인건비 장벽(Pacing Barrier)과 50년 장기 경영에서의 장기 연구 효율 가이드를 담고 있습니다.
  - **초반 위기 연동**: 라인 증가 및 대규모 인력 확보에 따라 **직원 50~100명 돌파 시 노동조합 메인 이벤트(초반 위기)**가 트리거되며, 일일 파업 손실(200/일)이라는 재정적 파산 위기의 생존 투쟁을 연결합니다.
  - **중반 위기 반영**: **직원 300~400명 돌파 시 전쟁 메인 이벤트(중반 위기)**가 발동하여, 공급망 붕괴 및 국가적 군수품 조달 강제 오더 속에서 공장 포트폴리오를 빠르게 무기 체인으로 전환하는 위기 극복의 성취감을 제공합니다.

* **[BALANCE_LATE.md](file:///c:/Users/Vrowdice/Desktop/Project/Unity/Unity_Pantory/agents/balance/BALANCE_LATE.md) (후반 밸런스 및 AI 하네스 가이드 - 1000명+ 구간)**
  - **요약**: 직원 수가 **1000명 돌파 시 자동화 메인 이벤트(후반 위기)**가 강제 트리거되어, 인건비 절감을 위한 기계 자동화 도입과 이에 대치하는 대규모 해고 노동자 폭동/사보타주 리스크 사이의 메타적 외줄타기를 다룹니다.
  - **최종 보상**: 수십 년 고생 끝에 도달한 최종 완제품 자동차(6,600 / 마진 +3,643), 탱크(27,500 / 마진 +2,680), 중화기(16,500 / 마진 +12,210)의 **압도적인 초고마진 폭리**를 통해 화려한 금융 지배 및 최종적 성취감을 보장합니다.
  - **AI 가이드**: 차세대 AI 에이전트가 통과시켜야 하는 **4대 경제성 검증 공식(Syntax Guard, CapEx, Negative Margin, Union Deficit)**을 수록하고 있습니다.

---

## 🤖 AI 에이전트를 위한 활용 표준 규칙 (Harness Protocol)

1. **GDD 동기화**: 코드 작성이나 시스템 기능 수정 전에 반드시 [PANTORY_GDD.md](file:///c:/Users/Vrowdice/Desktop/Project/Unity/Unity_Pantory/agents/PANTORY_GDD.md)를 먼저 숙지하여 시스템의 기획적 맥락을 유지해야 합니다.
2. **밸런스 검증 필수화**: ScriptableObject 에셋 수치나 가격 및 레시피를 수정하는 AI는, 임무 완수 전에 반드시 [BALANCE_LATE.md](file:///c:/Users/Vrowdice/Desktop/Project/Unity/Unity_Pantory/agents/balance/BALANCE_LATE.md) 3절에 제시된 **4대 밸런스 검증 파이프라인 공식**에 수정안을 대입하여 경제적 유효성을 자동 시뮬레이션 및 검사해야 합니다.
3. **참조 불일치 방지**: 에셋의 `id` 필드가 파일명과 상이할 수 있으므로, [RESOURCE_ANALYSIS.md](file:///c:/Users/Vrowdice/Desktop/Project/Unity/Unity_Pantory/agents/balance/RESOURCE_ANALYSIS.md) 마스터 테이블의 실제 `id` 속성명을 이중 체크하여 Unity Addressable 또는 DataManager 참조가 누락되지 않도록 방지하십시오.
