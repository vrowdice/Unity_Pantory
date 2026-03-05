# Unity 이미지 자동 생성/적용 스크립트 (Imagen 4.0)

## 사용 방법

### 1. 패키지 설치

```bash
cd Assets/Scripts/Py
pip install -r requirements.txt
```

필요 패키지: google-genai, Pillow, python-dotenv

### 2. API 키 (.env)

Imagen을 쓰는 스크립트는 API 키가 필요합니다. 프로젝트 루트 또는 `Assets/Scripts/Py` 폴더에 `.env` 파일을 만들고 다음을 넣으세요.

```
GOOGLE_AI_API_KEY=your_api_key_here
```

선택: `IMAGEN_MODEL=imagen-4.0-fast-generate-001` (기본값과 같으면 생략 가능)

API 키 발급: https://aistudio.google.com/app/apikey

### 3. 스크립트 실행

```bash
cd Assets/Scripts/Py
python AutoBuildingImage.py    # Building 이미지 생성
python AutoBuildingImageApply.py
python AutoMarketActorImage.py
python AutoMarketActorImageApply.py
python AutoResourceImage.py
python AutoResourceImageApply.py
python RemoveWhiteBackground.py  # 배경 처리 (API 불필요)
```

API 키가 없으면 이미지 생성 스크립트는 경고만 내고 생성하지 않습니다.

## 출력

- Building: `Assets/Images/Building/{id}.png`
- MarketActor: `Assets/Images/MarketActor/{id}.png`
- Resource: `Assets/Images/Resource/{Category}/{id}.png`

Apply 스크립트는 해당 이미지를 .asset의 icon/buildingSprite 등에 연결합니다.

## 주의

- API 일일 할당량 제한 있음 (예: 70회). 초과 시 다음 날까지 대기.
- .env는 프로젝트 루트 또는 Py 폴더 중 한 곳에 두면 됨. git에 올리지 말 것.
