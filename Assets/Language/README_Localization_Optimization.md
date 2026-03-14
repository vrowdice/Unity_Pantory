# 로컬라이제이션 테이블 최적화 제안

## 현재 상태
- **테이블 수**: 22개 이상 (Common, Building, BuildingDescription, BuildingType, EmployeeType, EmployeeDescription, MainPanelType, MarketPanelType, ResourceDisplayName, ResourceType, Research, ResearchDescription, MarketActor, MarketActorDescription, MarketActorType, WarningMessage, ConfirmMessage, Effect, News, NewsDescription, Order, Tutorial, Expenses 등)
- **패턴**: 같은 엔티티가 **이름 / 설명 / 타입**으로 2~3개 테이블로 쪼개져 있음

---

## 권장안: 도메인별 통합 (테이블 수 약 절반으로 축소)

**원칙**: 한 도메인(건물, 직원, 시장 등)의 **이름 + 설명 + 타입**을 **하나의 테이블**로 묶고, 키만 구분한다.

| 통합 후 테이블 | 통합할 기존 테이블 | 키 규칙 예시 |
|----------------|--------------------|--------------|
| **Common** | Common + ConfirmMessage + WarningMessage + MainPanelType + MarketPanelType | 기존 Common 유지, 확인/경고/패널은 `Confirm_LoadConfirm`, `Warn_NotEnoughResources`, `Panel_Storage` 등 접두사 |
| **Building** | Building + BuildingDescription + BuildingType | 건물 ID 그대로, 설명은 `{id}_Desc`, 타입은 `Type_Distribution` 등 |
| **Employee** | EmployeeType + EmployeeDescription | 타입명 그대로, 설명은 `{Type}_Desc` |
| **MarketActor** | MarketActor + MarketActorDescription + MarketActorType | ID 그대로, 설명 `{id}_Desc`, 타입 접두사 |
| **Research** | Research + ResearchDescription | ID 그대로, 설명 `{id}_Desc` |
| **News** | News + NewsDescription | ID 그대로, 설명 `{id}_Desc` |
| **Resource** | ResourceDisplayName + ResourceType | 기존 키 유지 (이미 구분됨) |
| **Order** | Order | 변경 없음 |
| **Effect** | Effect | 변경 없음 |
| **Tutorial** | Tutorial | 변경 없음 |
| **Expenses** | Expenses | 변경 없음 |

결과: **22개 → 약 11개 테이블**, `LocalizationUtils` 상수와 코드 호출부만 정리하면 됨.

---

## 구현 시 유의사항

1. **키 충돌 방지**  
   Common에 넣는 메시지/패널은 반드시 접두사(`Confirm_`, `Warn_`, `Panel_`)를 붙여서 기존 Common 키와 겹치지 않게 한다.

2. **코드 변경**  
   - `LocalizationUtils.cs`: 통합된 테이블명만 상수로 두고, 나머지 상수 제거.
   - 호출부: 테이블 이름을 바꾸고, **설명/타입**은 키만 새 규칙에 맞게 변경.  
     예: `TABLE_BUILDING_DESCRIPTION` + `id` → `TABLE_BUILDING` + `id + "_Desc"`

3. **Unity 로컬라이제이션 에셋**  
   CSV 통합 후 Unity에서 해당 테이블을 다시 가리키거나, 기존 로케일 에셋을 새 테이블 구조에 맞게 재생성해야 할 수 있음.

---

## 대안

- **최소 테이블(3~4개)**: Common / Entities(이름 전부) / Descriptions(설명 전부) / Misc. 테이블 수는 최소지만 CSV와 키 규칙이 커지고, 팀원이 키를 찾기 어려워질 수 있음.
- **테이블 수 유지, 폴더만 정리**: Table 아래에 `UI/`, `Entities/`, `Messages/` 등 폴더만 만들어 정리. 코드 변경 없이 구조만 정리할 때 적합.

원하시면 **Common 확장**(ConfirmMessage, WarningMessage, MainPanelType, MarketPanelType만 먼저 Common으로 통합)부터 적용하는 단계별 패치 순서도 정리해 드릴 수 있습니다.
