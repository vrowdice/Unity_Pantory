import os
import re
import time
from dotenv import load_dotenv
from google import genai
from google.genai import types
from PIL import Image
from io import BytesIO

# .env 파일에서 환경 변수 로드
PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../"))
PY_SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

# 프로젝트 루트의 .env 파일 먼저 시도
env_path = os.path.join(PROJECT_ROOT, ".env")
load_dotenv(env_path)

# Py 폴더의 Api.env 파일도 시도 (fallback)
api_env_path = os.path.join(PY_SCRIPT_DIR, "Api.env")
if not os.getenv("GOOGLE_AI_API_KEY") and not os.getenv("GOOGLE_API_KEY"):
    load_dotenv(api_env_path)

# Google AI API 키 설정 (.env 파일 또는 환경 변수에서 가져오기)
GOOGLE_AI_API_KEY = os.getenv("GOOGLE_AI_API_KEY") or os.getenv("GOOGLE_API_KEY")
IMAGEN_MODEL = os.getenv("IMAGEN_MODEL", "imagen-4.0-fast-generate-001")

# Imagen 클라이언트 초기화
imagen_client = None
if not GOOGLE_AI_API_KEY:
    print("⚠️  경고: GOOGLE_AI_API_KEY가 설정되지 않았습니다.")
    print(f"   프로젝트 루트에 .env 파일을 생성하고 GOOGLE_AI_API_KEY=your_key를 추가하세요.")
    print(f"   또는 환경 변수로 설정하세요.\n")
elif GOOGLE_AI_API_KEY:
    try:
        os.environ["GOOGLE_API_KEY"] = GOOGLE_AI_API_KEY
        imagen_client = genai.Client(api_key=GOOGLE_AI_API_KEY)
    except Exception as e:
        print(f"Imagen 클라이언트 초기화 실패: {e}")
        imagen_client = None

# 프로젝트 루트 디렉토리 (이미 위에서 정의됨)
DATAS_PATH = os.path.join(PROJECT_ROOT, "Assets/Datas/Building")
IMAGES_PATH = os.path.join(PROJECT_ROOT, "Assets/Images/Building")

def parse_asset_file(file_path):
    """Unity .asset 파일에서 정보 추출"""
    data = {}
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
            
        # id, displayName, description, buildingType 추출
        id_match = re.search(r'^  id:\s*([a-zA-Z_][a-zA-Z0-9_]*)', content, re.MULTILINE)
        name_match = re.search(r'^  displayName:\s*(.+)', content, re.MULTILINE)
        # description은 여러 줄일 수 있으므로 특별 처리
        description_match = re.search(r'^  description:\s*(.+?)(?=\n  [a-zA-Z]|\n---|\Z)', content, re.MULTILINE | re.DOTALL)
        building_type_match = re.search(r'^  buildingType:\s*(\d+)', content, re.MULTILINE)
        
        if id_match:
            data['id'] = id_match.group(1).strip()
        if name_match:
            data['displayName'] = name_match.group(1).strip()
        if description_match:
            # 여러 줄 description을 한 줄로 정리
            desc_text = description_match.group(1).strip()
            desc_text = re.sub(r'\s+', ' ', desc_text)  # 여러 공백을 하나로
            data['description'] = desc_text
        if building_type_match:
            data['buildingType'] = int(building_type_match.group(1))
            
    except Exception as e:
        print(f"파일 읽기 오류 ({file_path}): {e}")
    
    return data

def get_building_type_name(building_type_num):
    """BuildingType 번호를 이름으로 변환"""
    types = {
        0: "Storage",
        1: "Production",
        2: "Processing",
        3: "Infrastructure"
    }
    return types.get(building_type_num, "unknown")

def generate_image_prompt(item_data):
    """Building 데이터를 기반으로 이미지 생성 프롬프트 만들기"""
    name = item_data.get('displayName', 'Unknown Building')
    description = item_data.get('description', '')
    building_type = item_data.get('buildingType', 1)
    building_type_name = get_building_type_name(building_type)
    
    # BuildingType별 특징 설정
    if building_type == 0:  # Storage
        type_features = "warehouse features: large loading doors, few windows, simple rectangular structure, storage facility appearance"
        type_style = "storage facility, warehouse"
    elif building_type == 1:  # Production
        type_features = "factory features: multiple windows, chimneys or vents, manufacturing equipment visible, production facility appearance"
        type_style = "manufacturing plant, factory, production facility"
    elif building_type == 2:  # Processing
        type_features = "processing facility features: pipes, tanks, complex industrial equipment, refinery or processing plant appearance"
        type_style = "processing facility, refinery, industrial processing plant"
    elif building_type == 3:  # Infrastructure
        type_features = "infrastructure features: utility building appearance, transport or utility facility, simple functional structure"
        type_style = "infrastructure building, utility building"
    else:
        type_features = "industrial building features"
        type_style = "industrial building"
    
    # description과 name을 적극 활용
    building_context = f"{name}"
    if description:
        building_context += f" - {description}"
    
    prompt = f"""Create a casual pixel art building front facade for: {building_context}

BUILDING TYPE: {type_style}
SPECIFIC FEATURES: {type_features}

REQUIREMENTS:
- MANDATORY: This must be a completely flat 2D image, no 3D perspective, no depth, no shadows, no shading suggesting 3D
- 512x512 pixels (size specification only, do not render as text)
- Pure white background #FFFFFF - this is just background, do not draw ground
- Square building front view, straight on
- Building shape: Perfect square when viewed from front
- Building width and height should be approximately equal (square proportions)
- Casual pixel art style, sharp pixels, no anti-aliasing
- Early 20th century industrial architecture
- Include distinctive features that match the building type and description
- Draw ONLY the building, NO ground, NO base, NO foundation, NO floor
- Building should float on white background with no ground line or base
- NO text, NO letters, NO numbers
- NO side view, NO angled view, NO isometric view
- NO 3D perspective, NO depth, NO shadows, NO shading suggesting 3D
- Completely flat 2D sprite style, like a game icon
"""
    
    return prompt

def process_generated_image(img, item_name):
    """생성된 이미지 후처리 (흰색 배경 유지, 품질 개선)"""
    try:
        # RGB 모드로 변환 (흰색 배경 유지)
        if img.mode != 'RGB':
            img = img.convert('RGB')
        
        # 512x512로 리사이즈
        img = img.resize((512, 512), Image.Resampling.NEAREST)
        
        print(f"  흰색 배경 이미지로 저장: {item_name}")
        
        return img
        
    except Exception as e:
        print(f"  이미지 후처리 오류: {e}")
        return img

def generate_image_with_imagen(prompt, output_path, item_name, description="", building_type=1):
    """Google Imagen 4.0 API를 사용해서 이미지 생성"""
    try:
        # description이 있으면 간단한 프롬프트에도 포함
        desc_part = f", {description}" if description else ""
        
        # BuildingType별 특징 키워드 추가
        building_type_features = {
            0: "warehouse with large loading doors few windows simple structure storage facility",
            1: "factory with multiple windows chimneys vents manufacturing equipment production facility",
            2: "processing facility with pipes tanks complex industrial equipment refinery",
            3: "infrastructure utility building transport facility simple functional structure"
        }
        type_features = building_type_features.get(building_type, "industrial building")
        
        # name과 description을 더 적극적으로 활용
        building_name = item_name
        if description:
            building_context = f"{building_name} - {description}"
        else:
            building_context = building_name
        
        simple_prompt = f"completely flat 2D image mandatory, casual pixel art square building front facade for {building_context}, {type_features}, include distinctive features matching building type, square building viewed from front straight on, building width and height equal square proportions, draw only building no ground no base no foundation no floor building floats on white background, flat 2D sprite style like game icon, 512x512 pixels size not text, pure white background, building fills 70-80 percent centered, sharp pixels no anti-aliasing, early 20th century industrial architecture, no text no letters no numbers, no side view no angled view no isometric view, no 3D perspective no depth no shadows no shading"
        
        # Imagen으로 실제 이미지 생성
        if imagen_client:
            try:
                response = imagen_client.models.generate_images(
                    model=IMAGEN_MODEL,
                    prompt=simple_prompt,
                    config=types.GenerateImagesConfig(
                        number_of_images=1,
                    )
                )
                
                # 생성된 이미지 저장
                if response.generated_images:
                    generated_image = response.generated_images[0]
                    
                    # PIL Image로 변환 (여러 방법 시도)
                    img = None
                    try:
                        # 방법 1: 직접 PIL Image인 경우
                        if hasattr(generated_image.image, 'resize'):
                            img = generated_image.image
                        # 방법 2: _pil_image 속성이 있는 경우
                        elif hasattr(generated_image.image, '_pil_image'):
                            img = generated_image.image._pil_image
                        # 방법 3: bytes 데이터인 경우
                        elif isinstance(generated_image.image, bytes):
                            img = Image.open(BytesIO(generated_image.image))
                        # 방법 4: 다른 속성들 확인
                        else:
                            # 일반적인 속성명 시도
                            for attr_name in ['pil_image', 'data', 'bytes', 'content']:
                                if hasattr(generated_image.image, attr_name):
                                    img_data = getattr(generated_image.image, attr_name)
                                    if isinstance(img_data, bytes):
                                        img = Image.open(BytesIO(img_data))
                                        break
                    except Exception as conv_error:
                        print(f"  이미지 변환 오류: {conv_error}")
                        return False
                    
                    if img and hasattr(img, 'resize'):
                        # 이미지 후처리
                        img_final = process_generated_image(img, item_name)
                        img_final.save(output_path, 'PNG')
                        print(f"  생성 완료: {item_name}")
                        return True
                    else:
                        print(f"  이미지 변환 실패: PIL Image로 변환할 수 없음")
                        return False
                else:
                    return False
                    
            except Exception as e:
                error_str = str(e)
                if "429" in error_str or "RESOURCE_EXHAUSTED" in error_str or "quota" in error_str.lower():
                    print(f"  ⚠️  API 할당량 초과: 일일 70회 제한에 도달했습니다.")
                    print(f"     내일 다시 시도하거나 Google AI 플랜을 업그레이드하세요.")
                    print(f"     https://ai.dev/usage?tab=rate-limit 에서 사용량 확인 가능")
                else:
                    print(f"  API 오류: {e}")
                return False
        else:
            return False
        
    except Exception as e:
        print(f"  오류: {item_name} - {e}")
        return False

def process_buildings():
    """모든 Building 처리"""
    os.makedirs(IMAGES_PATH, exist_ok=True)
    
    if not os.path.exists(DATAS_PATH):
        print(f"데이터 폴더가 없습니다: {DATAS_PATH}")
        return
    
    asset_files = [f for f in os.listdir(DATAS_PATH) if f.endswith('.asset')]
    
    if not asset_files:
        print("처리할 .asset 파일이 없습니다.")
        return
    
    print(f"\n[Building] {len(asset_files)}개 파일 처리 중...")
    
    for asset_file in asset_files:
        file_path = os.path.join(DATAS_PATH, asset_file)
        item_data = parse_asset_file(file_path)
        
        if not item_data or 'id' not in item_data:
            continue
        
        item_id = item_data['id']
        item_name = item_data.get('displayName', item_id)
        output_image_path = os.path.join(IMAGES_PATH, f"{item_id}.png")
        
        if os.path.exists(output_image_path):
            print(f"  건너뜀: {item_name} (이미지 이미 존재)")
            continue
        
        prompt = generate_image_prompt(item_data)
        description = item_data.get('description', '')
        building_type = item_data.get('buildingType', 1)
        success = generate_image_with_imagen(prompt, output_image_path, item_name, description, building_type)
        
        # API가 작동하지 않으면 아무것도 생성하지 않음
        if not success:
            print(f"  생성 실패: {item_name} (API 미사용)")
            # 할당량 초과 시 더 이상 시도하지 않음
            if "429" in str(success) or "quota" in str(success).lower():
                print(f"\n⚠️  API 할당량 초과로 인해 처리를 중단합니다.")
                break
        
        time.sleep(2.0)  # 할당량 절약을 위해 대기 시간 증가

def main():
    """메인 실행 함수"""
    print("\nUnity Building 이미지 자동 생성기 (Imagen 4.0)")
    
    if imagen_client:
        print("Imagen 4.0 API 활성화\n")
    else:
        print("API 키 없음 - 플레이스홀더만 생성\n")
    
    process_buildings()
    
    print("\n작업 완료")

if __name__ == "__main__":
    main()

