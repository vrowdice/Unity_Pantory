import os
import re
import time
from dotenv import load_dotenv
from google import genai
from google.genai import types
from PIL import Image
from io import BytesIO

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../"))
PY_DIR = os.path.dirname(os.path.abspath(__file__))
load_dotenv(os.path.join(PROJECT_ROOT, ".env"))
load_dotenv(os.path.join(PY_DIR, ".env"))

GOOGLE_AI_API_KEY = os.getenv("GOOGLE_AI_API_KEY") or os.getenv("GOOGLE_API_KEY")
IMAGEN_MODEL = os.getenv("IMAGEN_MODEL", "imagen-4.0-fast-generate-001")

imagen_client = None
if not GOOGLE_AI_API_KEY:
    print("경고: GOOGLE_AI_API_KEY가 없습니다. 프로젝트 루트 또는 Py 폴더에 .env 파일에 추가하세요.")
elif GOOGLE_AI_API_KEY:
    try:
        os.environ["GOOGLE_API_KEY"] = GOOGLE_AI_API_KEY
        imagen_client = genai.Client(api_key=GOOGLE_AI_API_KEY)
    except Exception as e:
        print(f"Imagen 클라이언트 초기화 실패: {e}")
        imagen_client = None

DATAS_PATH = os.path.join(PROJECT_ROOT, "Assets/Datas/Resource")
IMAGES_PATH = os.path.join(PROJECT_ROOT, "Assets/Images/Resource")

def parse_asset_file(file_path):
    """Unity .asset 파일에서 정보 추출"""
    data = {}
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
            
        # id, displayName, description 추출 (더 정확한 정규식)
        id_match = re.search(r'^  id:\s*([a-zA-Z_][a-zA-Z0-9_]*)', content, re.MULTILINE)
        name_match = re.search(r'^  displayName:\s*(.+)', content, re.MULTILINE)
        type_match = re.search(r'^  type:\s*(\d+)', content, re.MULTILINE)
        description_match = re.search(r'^  description:\s*(.+)', content, re.MULTILINE)
        
        if id_match:
            data['id'] = id_match.group(1).strip()
        if name_match:
            data['displayName'] = name_match.group(1).strip()
        if type_match:
            data['type'] = int(type_match.group(1))
        if description_match:
            data['description'] = description_match.group(1).strip()
            
    except Exception as e:
        print(f"파일 읽기 오류 ({file_path}): {e}")
    
    return data

def get_resource_type_name(type_num):
    """타입 번호를 이름으로 변환 (ResourceType.cs와 일치)"""
    types = {
        0: "raw",           # raw = 0
        1: "metal",         # metal = 1
        2: "weapon",        # weapon = 2
        3: "Essentials",    # Essentials = 3
        4: "Luxuries",      # Luxuries = 4
        5: "component",     # component = 5
        6: "vehicle"        # vehicle = 6
    }
    return types.get(type_num, "unknown")

def generate_image_prompt(item_data, category):
    """아이템 데이터를 기반으로 이미지 생성 프롬프트 만들기"""
    name = item_data.get('displayName', 'Unknown Item')
    description = item_data.get('description', '')
    
    # 카테고리별 스타일 설정 (ResourceType.cs와 일치)
    if category == "Raw":
        style = "pixel art game item icon, raw material or ore, natural resource"
    elif category == "Metal":
        style = "pixel art game item icon, metal ingot or refined metal, shiny metallic material"
    elif category == "Weapon":
        style = "pixel art game item icon, early 20th century weapon, World War era firearm, vintage military weapon"
    elif category == "Essentials":
        style = "pixel art game item icon, essential items, basic necessities, simple tools, basic furniture, basic clothing"
    elif category == "Luxuries":
        style = "pixel art game item icon, luxury items, premium goods, high-quality furniture, premium clothing, luxury goods"
    elif category == "Component":
        style = "pixel art game item icon, mechanical component or industrial part"
    elif category == "Vehicle":
        style = "pixel art game item icon, early 20th century vehicle, vintage automobile, 1920s-1940s style transportation, World War era vehicle"
    else:
        style = "pixel art game item icon"
    
    # description이 있으면 프롬프트에 포함
    desc_text = f"\nDescription: {description}" if description else ""
    
    prompt = f"""Create a {style} for: {name}.{desc_text}
    
CRITICAL REQUIREMENTS:
- Perfect pixel art style, exactly 64x64 pixels
- Pure white background #FFFFFF - completely solid white, no other background colors
- NO text, NO letters, NO words, NO labels anywhere
- NO anti-aliasing, sharp crisp pixels only
- Simple clean design, centered composition
- Limited color palette (max 8 colors)
- Fantasy RPG game icon style
- High contrast, clear visibility
- No broken or corrupted pixels
- Professional game asset quality
"""
    
    return prompt

def pixelate_image(img, pixel_size=32):
    """이미지를 픽셀아트 스타일로 변환"""
    # 원본 크기 저장
    original_size = img.size
    
    # 작은 크기로 다운스케일 (nearest neighbor)
    small_img = img.resize((pixel_size, pixel_size), Image.Resampling.NEAREST)
    
    # 다시 원본 크기로 업스케일 (nearest neighbor로 픽셀 효과)
    pixelated_img = small_img.resize(original_size, Image.Resampling.NEAREST)
    
    return pixelated_img

def process_generated_image(img, item_name):
    """생성된 이미지 후처리 (흰색 배경 유지, 품질 개선)"""
    try:
        # RGB 모드로 변환 (흰색 배경 유지)
        if img.mode != 'RGB':
            img = img.convert('RGB')
        
        # 64x64로 리사이즈
        img = img.resize((64, 64), Image.Resampling.NEAREST)
        
        print(f"  흰색 배경 이미지로 저장: {item_name}")
        
        return img
        
    except Exception as e:
        print(f"  이미지 후처리 오류: {e}")
        return img

def generate_image_with_imagen(prompt, output_path, item_name, description="", category=""):
    """Google Imagen 4.0 API를 사용해서 이미지 생성"""
    try:
        # description이 있으면 간단한 프롬프트에도 포함
        desc_part = f", {description}" if description else ""
        
        # 카테고리별 스타일 키워드 추가 (ResourceType.cs와 일치)
        category_keywords = {
            "Raw": "raw material, ore, natural resource",
            "Metal": "metal ingot, shiny metallic",
            "Weapon": "early 20th century weapon, World War era firearm, vintage military weapon",
            "Essentials": "essential items, basic necessities, simple tools, basic furniture, basic clothing",
            "Luxuries": "luxury items, premium goods, high-quality furniture, premium clothing, luxury goods",
            "Component": "mechanical component, industrial part",
            "Vehicle": "early 20th century vehicle, vintage automobile, 1920s-1940s style, World War era vehicle"
        }
        category_style = category_keywords.get(category, "game item")
        
        # 픽셀아트 프롬프트 (품질 강화) - 흰색 배경 사용
        # Vehicle과 Weapon은 근대 스타일 강조
        era_style = ""
        if category == "Vehicle":
            era_style = ", 1920s-1940s era, vintage style, early industrial age"
        elif category == "Weapon":
            era_style = ", World War era, early 20th century military style"
        
        # Motor는 엔진/원동기 스타일로 특별 처리
        motor_style = ""
        if item_name.lower() == "motor" or "prime mover" in description.lower() or "engine" in description.lower() or "combustion" in description.lower():
            motor_style = ", internal combustion engine, mechanical prime mover, early 20th century industrial engine, not electric motor"
            category_style = "mechanical engine, prime mover, industrial power source"
        
        simple_prompt = f"64x64 pixel art game icon of {item_name}{desc_part}, {category_style}{era_style}{motor_style}, pure white background #FFFFFF, no text no letters, sharp pixels, retro RPG style, high quality, clean design, no anti-aliasing, professional game asset"
        
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
                    
                    img = None
                    try:
                        if hasattr(generated_image.image, 'resize'):
                            img = generated_image.image
                        elif hasattr(generated_image.image, '_pil_image'):
                            img = generated_image.image._pil_image
                        elif isinstance(generated_image.image, bytes):
                            img = Image.open(BytesIO(generated_image.image))
                        else:
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
                print(f"  API 오류: {e}")
                return False
        else:
            return False
        
    except Exception as e:
        print(f"  오류: {item_name} - {e}")
        
        
        return False

def create_placeholder_image(output_path, item_name):
    """플레이스홀더 이미지 생성 (64x64 픽셀아트)"""
    try:
        from PIL import Image, ImageDraw
        
        # 64x64 픽셀아트
        img = Image.new('RGB', (64, 64), color=(64, 64, 64))
        draw = ImageDraw.Draw(img)
        
        # 간단한 사각형 그리기 (아이콘 표시) - 64x64에 맞게 크기 조정
        draw.rectangle([16, 16, 48, 48], fill=(128, 128, 128), outline=(200, 200, 200))
        
        img.save(output_path)
        return True
        
    except Exception as e:
        print(f"  플레이스홀더 오류: {e}")
        return False

def process_category(category_name):
    """카테고리별로 모든 아이템 처리"""
    data_folder = os.path.join(DATAS_PATH, category_name)
    image_folder = os.path.join(IMAGES_PATH, category_name)
    
    os.makedirs(image_folder, exist_ok=True)
    
    if not os.path.exists(data_folder):
        return
    
    asset_files = [f for f in os.listdir(data_folder) if f.endswith('.asset')]
    
    if not asset_files:
        return
    
    print(f"\n[{category_name}] {len(asset_files)}개 파일 처리 중...")
    
    for asset_file in asset_files:
        file_path = os.path.join(data_folder, asset_file)
        item_data = parse_asset_file(file_path)
        
        if not item_data or 'id' not in item_data:
            continue
        
        item_id = item_data['id']
        item_name = item_data.get('displayName', item_id)
        output_image_path = os.path.join(image_folder, f"{item_id}.png")
        
        if os.path.exists(output_image_path):
            continue
        
        prompt = generate_image_prompt(item_data, category_name)
        description = item_data.get('description', '')
        success = generate_image_with_imagen(prompt, output_image_path, item_name, description, category_name)
        
        # API가 작동하지 않으면 아무것도 생성하지 않음
        if not success:
            print(f"  생성 실패: {item_name} (API 미사용)")
        
        time.sleep(1.0)

def get_all_categories():
    """데이터 폴더에서 자동으로 모든 카테고리 감지"""
    if not os.path.exists(DATAS_PATH):
        return []
    
    categories = []
    for item in os.listdir(DATAS_PATH):
        item_path = os.path.join(DATAS_PATH, item)
        if os.path.isdir(item_path):
            # .asset 파일이 있는 폴더만 카테고리로 인식
            asset_files = [f for f in os.listdir(item_path) if f.endswith('.asset')]
            if asset_files:
                categories.append(item)
    
    return sorted(categories)

def main():
    """메인 실행 함수"""
    print("\nUnity Resource 이미지 자동 생성기 (Imagen 4.0)")
    
    if imagen_client:
        print("Imagen 4.0 API 활성화\n")
    else:
        print("API 키 없음 - 플레이스홀더만 생성\n")
    
    # 자동으로 카테고리 감지
    categories = get_all_categories()
    print(f"감지된 카테고리: {categories}")
    
    for category in categories:
        process_category(category)
    
    print("\n작업 완료")

if __name__ == "__main__":
    main()

