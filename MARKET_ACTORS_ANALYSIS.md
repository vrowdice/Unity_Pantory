# 마켓 액터 상황 분석 및 설명

## 📊 ResourceType 매핑
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

## 🏭 제조업체 (Manufacturers)

### 1. **Artisan Furniture Workshop** (소규모)
- **역할**: 고급 가구 제작소
- **스케일**: Small (1)
- **특징**:
  - Provider: 동적 할당, outputs 없음 (동적 선택)
  - Consumer: 예산 1,200~2,200, persistentOrders 없음
  - 선호 자원: wood(2), furniture(5)
  - 재할당: Provider 10일, Consumer 6일
- **전략**: 프리미엄 가구 생산, 원자재(나무) 구매

### 2. **Automotive Industry Group** (대규모)
- **역할**: 자동차 제조사
- **스케일**: Large (2)
- **특징**:
  - Provider: upkeep 3종 (원자재 필요), 가격 +15%, 배치 판매 불가
  - Consumer: 예산 5,000~8,000 (높음)
  - 선호 자원: metal(1), component(7), electronics(8), vehicle(9)
  - 재할당: Provider 7일, Consumer 5일
- **전략**: 차량 생산, 부품/전자제품 구매

### 3. **Electronics Manufacturing Corp** (대규모)
- **역할**: 전자제품 제조사
- **스케일**: Large (2)
- **특징**:
  - Provider: upkeep 2종 (원자재 필요), 가격 +10%
  - Consumer: 예산 2,800~4,600
  - 선호 자원: metal(1), component(7), electronics(8)
  - 재할당: Provider 6일, Consumer 6일
- **전략**: 전자제품 생산, 금속/부품 구매

### 4. **Elite Armaments Group** (대규모)
- **역할**: 프리미엄 무기 제조사
- **스케일**: Large (2)
- **특징**:
  - Provider: upkeep 5종 (많은 원자재 필요), 가격 +18%, 배치 판매 불가
  - Consumer: 예산 4,200~7,200 (매우 높음)
  - 선호 자원: metal(1), tool(3), weapon(4), component(7), vehicle(9)
  - 재할당: Provider 5일, Consumer 5일 (빠른 전환)
- **전략**: 고급 무기 생산, 다양한 원자재 구매

### 5. **Precision Components Ltd** (소규모)
- **역할**: 정밀 부품 제조사
- **스케일**: Small (1)
- **특징**:
  - Provider: upkeep 2종, 가격 +8%
  - Consumer: 예산 1,800~3,000
  - 선호 자원: metal(1), component(7)
  - 재할당: Provider 6일, Consumer 6일
- **전략**: 부품 생산, 금속 구매

### 6. **Precision Weapons Workshop** (소규모)
- **역할**: 정밀 무기 공방
- **스케일**: Small (1)
- **특징**:
  - Provider: upkeep 2종, 가격 +20% (최고 프리미엄), 배치 판매 불가
  - Consumer: 예산 1,800~3,200, bulkBuying 불가
  - 선호 자원: metal(1), wood(2), weapon(4), component(7)
  - 재할당: Provider 7일, Consumer 7일
- **전략**: 고급 무기 생산, 금속/나무 구매

### 7. **Vanguard Steelworks** (소규모)
- **역할**: 제철소
- **스케일**: Small (1)
- **특징**:
  - Provider: upkeep 2종, 가격 +5%
  - Consumer: 예산 1,500~2,600
  - 선호 자원: metal(1), component(7)
  - 재할당: Provider 7일, Consumer 7일
- **전략**: 금속 생산, 원자재 구매

---

## 🏪 유통업체 (Traders/Brokers)

### 8. **Industrial Brokerage Network** (대규모)
- **역할**: 산업용 자원 중개업체
- **스케일**: Large (2)
- **특징**:
  - Provider: 동적 할당, 가격 기본값
  - Consumer: 예산 3,000~4,800
  - 선호 자원: metal(1), tool(3), weapon(4), component(7)
  - 재할당: Provider 6일, Consumer 4일 (빠른 소비 전환)
- **전략**: 금속/도구/무기/부품 거래

### 9. **Global Resource Syndicate** (대규모)
- **역할**: 글로벌 자원 통합 거래소
- **스케일**: Large (2)
- **특징**:
  - Provider: upkeep 4종 (많은 원자재), 가격 +15%
  - Consumer: 예산 3,200~5,200
  - 선호 자원: raw(0), metal(1), wood(2), tool(3), furniture(5), clothing(6), component(7)
  - 재할당: Provider 5일, Consumer 5일 (빠른 전환)
- **전략**: 다양한 자원 거래, 원자재 통합

### 10. **Imperial Logistics Bureau** (대규모)
- **역할**: 제국 물류국
- **스케일**: Large (2)
- **특징**:
  - Provider: 동적 할당, 가격 기본값
  - Consumer: 예산 3,600~5,200
  - 선호 자원: 모든 타입 (0~9)
  - 재할당: Provider 8일, Consumer 5일
- **전략**: 전략적 물류 관리, 모든 자원 거래

### 11. **National Exchange** (대규모)
- **역할**: 국립 거래소
- **스케일**: Large (2)
- **특징**:
  - Provider: 동적 할당, 가격 +8%
  - Consumer: 예산 2,000~3,200
  - 선호 자원: raw(0), metal(1), wood(2), furniture(5), clothing(6)
  - 재할당: Provider 6일, Consumer 6일
- **전략**: 정부 규제 거래, 기본 자원 거래

### 12. **Premium Industrial Logistics** (대규모)
- **역할**: 프리미엄 산업 물류
- **스케일**: Large (2)
- **특징**:
  - Provider: upkeep 3종, 가격 +25% (최고 프리미엄)
  - Consumer: 예산 3,600~6,200 (매우 높음)
  - 선호 자원: metal(1), tool(3), weapon(4), component(7), electronics(8)
  - 재할당: Provider 5일, Consumer 5일
- **전략**: 고급 산업 자원 거래

### 13. **Verdant Continental Exchange** (대규모)
- **역할**: 대륙 목재 유통업체
- **스케일**: Large (2)
- **특징**:
  - Provider: upkeep 3종 (나무 관련), 가격 +8%
  - Consumer: 예산 1,800~3,600
  - 선호 자원: raw(0), wood(2), weapon(4), furniture(5)
  - 재할당: Provider 6일, Consumer 6일
- **전략**: 목재/가구 거래, 원자재 구매

---

## 🏛️ 군사/정부 기관 (Military/Government)

### 14. **Continental Defense Force** (대규모)
- **역할**: 대륙 방위군
- **스케일**: Large (2)
- **특징**:
  - Provider: 동적 할당, 가격 기본값
  - Consumer: 예산 4,200~6,400 (높음), patienceSeconds 7800 (매우 인내심)
  - 선호 자원: tool(3), weapon(4), component(7), vehicle(9)
  - 재할당: Provider 9일, Consumer 5일
- **전략**: 군수품 구매, 안정적 수요

### 15. **Elite Security Forces** (대규모)
- **역할**: 엘리트 보안군
- **스케일**: Large (2)
- **특징**:
  - Provider: 동적 할당, 가격 기본값
  - Consumer: 예산 3,000~5,000, satisfactionDecay 0.12 (높음)
  - 선호 자원: weapon(4), vehicle(9)
  - 재할당: Provider 8일, Consumer 4일 (빠른 소비 전환)
- **전략**: 고급 무기/차량 구매

### 16. **Military Armory** (소규모)
- **역할**: 수도군 공방
- **스케일**: Small (1)
- **특징**:
  - Provider: upkeep 3종, 가격 +10%, 배치 판매 불가
  - Consumer: 예산 0~0 (소비 안 함), persistentOrders 없음
  - 선호 자원: weapon(4), component(7), vehicle(9)
  - 재할당: Provider 8일, Consumer 8일
- **전략**: 군수품 생산 전용, 소비 없음

---

## 🌲 원자재 생산업체 (Raw Material Producers)

### 17. **Frontier Sawmill** (소규모)
- **역할**: 개척지 제재소
- **스케일**: Small (1)
- **특징**:
  - Provider: upkeep 1종 (원목 필요), 가격 기본값
  - Consumer: 예산 800~1,400 (낮음)
  - 선호 자원: raw(0), wood(2), furniture(5)
  - 재할당: Provider 9일, Consumer 6일
- **전략**: 목재 가공, 원목 구매

### 18. **Iron Mining Consortium** (대규모)
- **역할**: 철광산 카르텔
- **스케일**: Large (2)
- **특징**:
  - Provider: 동적 할당, 가격 -15% (저가 공략)
  - Consumer: 예산 0~0 (소비 안 함), persistentOrders 없음
  - 선호 자원: raw(0), metal(1)
  - 재할당: Provider 4일 (매우 빠른 전환), Consumer 8일
- **전략**: 저가 원자재 생산, 소비 없음

### 19. **Lumber Consortium** (대규모)
- **역할**: 목재 카르텔
- **스케일**: Large (2)
- **특징**:
  - Provider: 동적 할당, 가격 -10% (저가 공략)
  - Consumer: 예산 0~0 (소비 안 함), persistentOrders 없음
  - 선호 자원: raw(0), wood(2)
  - 재할당: Provider 6일, Consumer 8일
- **전략**: 저가 목재 생산, 소비 없음

### 20. **Smelter Collective** (소규모)
- **역할**: 제련소 협동조합
- **스케일**: Small (1)
- **특징**:
  - Provider: upkeep 1종 (광석 필요), 가격 기본값
  - Consumer: 예산 1,000~1,800
  - 선호 자원: raw(0), metal(1)
  - 재할당: Provider 6일, Consumer 6일
- **전략**: 금속 제련, 광석 구매

---

## 🏪 소매업체 (Retailers)

### 21. **Fashion Retail Chain** (대규모)
- **역할**: 패션 소매 체인
- **스케일**: Large (2)
- **특징**:
  - Provider: 동적 할당, 가격 기본값
  - Consumer: 예산 1,200~2,200, satisfactionDecay 0.11 (높음)
  - 선호 자원: clothing(6)만
  - 재할당: Provider 7일, Consumer 5일
- **전략**: 의류 거래 전용

### 22. **Luxury Furniture Showroom** (소규모)
- **역할**: 럭셔리 가구 쇼룸
- **스케일**: Small (1)
- **특징**:
  - Provider: 동적 할당, 가격 기본값
  - Consumer: 예산 1,500~2,800, bulkBuying 불가
  - 선호 자원: wood(2), furniture(5)
  - 재할당: Provider 8일, Consumer 6일
- **전략**: 고급 가구 거래

---

## 🏭 중공업 (Heavy Industry)

### 23. **Northwind Industrial Union** (대규모)
- **역할**: 중공업 연합
- **스케일**: Large (2)
- **특징**:
  - Provider: upkeep 2종 (철, 목재 필요), 가격 +5%
  - Consumer: 예산 2,600~4,400
  - 선호 자원: metal(1), wood(2), tool(3), component(7), electronics(8)
  - 재할당: Provider 6일, Consumer 6일
- **전략**: 도구/합금 생산, 철/목재 구매

---

## 📊 종합 분석

### 액터 분류

#### 1. **제조업체 (Manufacturers)** - 7개
- Artisan Furniture Workshop, Automotive Industry Group, Electronics Manufacturing Corp
- Elite Armaments Group, Precision Components Ltd, Precision Weapons Workshop, Vanguard Steelworks
- **특징**: upkeep 필요, 생산 중심

#### 2. **유통업체 (Traders)** - 6개
- Industrial Brokerage Network, Global Resource Syndicate, Imperial Logistics Bureau
- National Exchange, Premium Industrial Logistics, Verdant Continental Exchange
- **특징**: 동적 할당, 거래 중심

#### 3. **군사/정부 (Military/Government)** - 3개
- Continental Defense Force, Elite Security Forces, Military Armory
- **특징**: 높은 예산, 무기/차량 선호

#### 4. **원자재 생산업체 (Raw Material Producers)** - 4개
- Frontier Sawmill, Iron Mining Consortium, Lumber Consortium, Smelter Collective
- **특징**: 저가 공략 또는 원자재 가공

#### 5. **소매업체 (Retailers)** - 2개
- Fashion Retail Chain, Luxury Furniture Showroom
- **특징**: 특정 자원 타입 전용

#### 6. **중공업 (Heavy Industry)** - 1개
- Northwind Industrial Union
- **특징**: 도구/합금 생산

---

## ⚠️ 특이사항 및 잠재적 문제

### 1. **예산이 0인 액터들**
- **Iron Mining Consortium**: Consumer 예산 0~0 (소비 안 함)
- **Lumber Consortium**: Consumer 예산 0~0 (소비 안 함)
- **Military Armory**: Consumer 예산 0~0 (소비 안 함)
- **영향**: 이들은 소비하지 않으므로 시장 수요에 기여하지 않음

### 2. **높은 예산 액터들** (시장 활성화 기여)
- **Automotive Industry Group**: 5,000~8,000
- **Elite Armaments Group**: 4,200~7,200
- **Continental Defense Force**: 4,200~6,400
- **Premium Industrial Logistics**: 3,600~6,200

### 3. **빠른 재할당 액터들** (시장 변동성 증가)
- **Iron Mining Consortium**: Provider 4일 (매우 빠름)
- **Elite Armaments Group**: Provider/Consumer 5일
- **Global Resource Syndicate**: Provider/Consumer 5일
- **Premium Industrial Logistics**: Provider/Consumer 5일

### 4. **저가 공략 액터들** (가격 하락 압력)
- **Iron Mining Consortium**: 가격 -15%
- **Lumber Consortium**: 가격 -10%
- **영향**: 원자재 가격 하락 압력

### 5. **프리미엄 액터들** (가격 상승 압력)
- **Precision Weapons Workshop**: 가격 +20%
- **Premium Industrial Logistics**: 가격 +25%
- **Elite Armaments Group**: 가격 +18%
- **영향**: 고급 제품 가격 상승 압력

---

## 🎯 시장 균형 분석

### 공급망 구조
1. **원자재 생산**: Iron Mining, Lumber Consortium → 저가 공급
2. **1차 가공**: Smelter Collective, Frontier Sawmill → 원자재 구매, 가공품 생산
3. **2차 제조**: 제조업체들 → 가공품 구매, 완제품 생산
4. **유통**: 유통업체들 → 모든 단계 거래
5. **최종 소비**: 군사/정부, 소매업체 → 완제품 구매

### 예상 시장 동향
- **원자재**: 저가 공략 액터들로 인해 가격 하락 압력
- **가공품**: 원자재 가격 하락 → 가공품 가격도 하락 가능
- **완제품**: 프리미엄 액터들로 인해 고급 제품 가격 상승
- **균형**: 저가 원자재 + 프리미엄 완제품 구조

---

## 💡 개선 제안

### 1. **소비 없는 액터들의 예산 설정**
- Iron Mining Consortium, Lumber Consortium, Military Armory
- 현재 예산 0~0으로 설정되어 있어 소비하지 않음
- 시장 활성화를 위해 최소 예산 설정 고려

### 2. **균형 조정**
- 저가 공략 액터들의 가격 할인율 조정 (-15%, -10% → -5%, -3%)
- 프리미엄 액터들의 가격 프리미엄 조정 (+20%, +25% → +15%, +18%)

### 3. **재할당 주기 조정**
- 너무 빠른 재할당(4일)은 시장 불안정성 증가
- 최소 5일 이상 권장

