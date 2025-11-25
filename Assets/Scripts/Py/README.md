# Unity Resource 이미지 자동 생성기 (Imagen 4.0)

## 🚀 사용 방법

### 1. Python 환경 설정

```bash
# 필요한 패키지 설치
cd Assets/Scripts/Py
pip install -r requirements.txt
```

필요한 패키지:
- `google-genai` - Google Imagen 4.0 API
- `Pillow` - 이미지 처리
- `python-dotenv` - .env 파일 지원

### 2. API 키 설정

**✅ 중요: Imagen 4.0을 사용하려면 API 키가 필요합니다!**

프로젝트 루트 디렉토리에 `.env` 파일을 생성하고 API 키를 추가하세요:

```bash
# 프로젝트 루트에 .env 파일 생성
# (Assets/Scripts/Py/.env.example을 참고하세요)

# .env 파일 내용:
GOOGLE_AI_API_KEY=your_api_key_here
```

**또는 환경 변수로 설정:**
```bash
# Windows (PowerShell)
$env:GOOGLE_AI_API_KEY="your_api_key_here"

# Windows (CMD)
set GOOGLE_AI_API_KEY=your_api_key_here

# Linux/Mac
export GOOGLE_AI_API_KEY=your_api_key_here
```

**API 키 받기:**
1. https://aistudio.google.com/app/apikey 방문
2. Google 계정으로 로그인
3. "Create API Key" 클릭
4. API 키 복사
5. `.env` 파일에 추가 (프로젝트 루트 디렉토리)

### 3. 스크립트 실행

```bash
cd Assets/Scripts/Py
python AutoResourceImage.py
```

**API 키가 있으면:**
- ✅ Imagen 4.0으로 실제 고품질 이미지 생성
- ✅ 자동으로 512x512 PNG로 저장
- ✅ Unity에서 자동 임포트

**API 키가 없으면:**
- ⚠️ 프롬프트 파일만 생성
- ⚠️ 플레이스홀더 이미지 생성

## 📁 출력 파일

스크립트는 다음 파일들을 생성합니다:

1. **프롬프트 파일** (`Assets/Images/Resource/{Category}/{item_id}_prompt.txt`)
   - 원본 프롬프트
   - Gemini가 생성한 향상된 설명
   - 간단한 프롬프트

2. **플레이스홀더 이미지** (`Assets/Images/Resource/{Category}/{item_id}.png`)
   - 임시 이미지 (512x512)
   - 아이템 이름이 표시됨

## 🎨 실제 이미지 생성

프롬프트 파일을 사용해서 다음 AI 서비스로 이미지를 생성하세요:

### 추천 서비스

#### 1️⃣ Leonardo.ai (추천!)
- 무료 크레딧 제공
- 고품질 게임 에셋 생성에 최적화
- 링크: https://leonardo.ai

#### 2️⃣ DALL-E 3
- ChatGPT Plus 구독자는 무료
- 높은 품질
- 링크: https://chat.openai.com

#### 3️⃣ Stable Diffusion
- 완전 무료 (로컬 실행)
- 설치 필요
- 링크: https://github.com/AUTOMATIC1111/stable-diffusion-webui

#### 4️⃣ Midjourney
- Discord를 통해 사용
- 높은 품질
- 링크: https://www.midjourney.com

## 📋 프로세스

1. **스크립트 실행** → 프롬프트 파일 생성
2. **프롬프트 복사** → `{item_id}_prompt.txt`에서 "Enhanced Description" 복사
3. **AI에 입력** → Leonardo.ai 등에 프롬프트 입력
4. **이미지 저장** → 생성된 이미지를 `{item_id}.png`로 저장
5. **Unity에서 확인** → 자동으로 임포트됨

## 🔧 커스터마이징

프롬프트를 수정하려면 `AutoResourceImage.py`의 `generate_image_prompt()` 함수를 편집하세요.

## ⚠️ 주의사항

- API 키 없이도 프롬프트 생성은 가능합니다
- Gemini는 이미지를 직접 생성하지 못하지만, 최적화된 프롬프트를 만들어줍니다
- 생성된 이미지는 Unity에서 자동으로 감지됩니다

## 📊 지원되는 카테고리

- Metal (금속 재료)
- Wood (나무 재료)
- Tool (도구)
- Weapon (무기)

## 💡 팁

- **일괄 생성**: Leonardo.ai의 batch generation 기능 사용
- **일관성**: 같은 모델과 설정을 유지하면 일관된 스타일 유지
- **크기**: 512x512 이상 권장 (Unity에서 자동 리사이징)
- **배경**: 투명 배경 또는 단색 배경 권장

