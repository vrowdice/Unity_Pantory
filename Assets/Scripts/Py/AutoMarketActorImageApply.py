import os
import re
from dotenv import load_dotenv

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../"))
PY_DIR = os.path.dirname(os.path.abspath(__file__))
load_dotenv(os.path.join(PROJECT_ROOT, ".env"))
load_dotenv(os.path.join(PY_DIR, ".env"))
DATAS_PATH = os.path.join(PROJECT_ROOT, "Assets/Datas/MarketActor/Individual")
IMAGES_PATH = os.path.join(PROJECT_ROOT, "Assets/Images/MarketActor/Individual")

def get_image_guid(meta_file_path):
    """이미지의 .meta 파일에서 GUID와 sprite ID 추출"""
    try:
        with open(meta_file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # GUID 추출
        guid_match = re.search(r'^guid:\s*([a-f0-9]+)', content, re.MULTILINE)
        if not guid_match:
            return None, None
        
        guid = guid_match.group(1)
        
        # sprite ID 추출 (첫 번째 sprite의 internalID 사용)
        sprite_id = "21300000"
        
        return guid, sprite_id
        
    except Exception as e:
        print(f"  메타 파일 읽기 오류: {e}")
        return None, None

def update_asset_icon(asset_file_path, image_guid, sprite_id):
    """asset 파일의 icon 또는 portrait 필드를 업데이트 (덮어쓰기)"""
    try:
        with open(asset_file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        new_content = content
        updated = False
        
        # icon 필드 업데이트 (있는 경우)
        icon_pattern = r'(  icon: )\{[^}]+\}'
        new_icon = f'\\1{{fileID: {sprite_id}, guid: {image_guid}, type: 3}}'
        if re.search(icon_pattern, new_content):
            new_content = re.sub(icon_pattern, new_icon, new_content)
            updated = True
        
        # portrait 필드 업데이트 (있는 경우)
        portrait_pattern = r'(  portrait: )\{[^}]+\}'
        new_portrait = f'\\1{{fileID: {sprite_id}, guid: {image_guid}, type: 3}}'
        if re.search(portrait_pattern, new_content):
            new_content = re.sub(portrait_pattern, new_portrait, new_content)
            updated = True
        
        # 변경사항이 있는 경우에만 저장
        if updated and new_content != content:
            with open(asset_file_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
            return True
        
        return False
        
    except Exception as e:
        print(f"  asset 파일 업데이트 오류: {e}")
        return False

def get_asset_id(asset_file_path):
    """asset 파일에서 실제 id 필드 추출"""
    try:
        with open(asset_file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # id 필드 추출
        id_match = re.search(r'^  id:\s*([a-zA-Z_][a-zA-Z0-9_]*)', content, re.MULTILINE)
        if id_match:
            return id_match.group(1).strip()
        
        return None
    except Exception as e:
        print(f"  asset 파일 읽기 오류: {e}")
        return None

def process_market_actors():
    """모든 MarketActor에 이미지 적용"""
    if not os.path.exists(DATAS_PATH) or not os.path.exists(IMAGES_PATH):
        print("데이터 폴더 또는 이미지 폴더가 없습니다.")
        return
    
    asset_files = [f for f in os.listdir(DATAS_PATH) if f.endswith('.asset')]
    
    if not asset_files:
        print("처리할 .asset 파일이 없습니다.")
        return
    
    print(f"\n[MarketActor] {len(asset_files)}개 파일 처리 중...")
    
    updated_count = 0
    skipped_count = 0
    for asset_file in asset_files:
        asset_file_path = os.path.join(DATAS_PATH, asset_file)
        
        # asset 파일에서 실제 id 추출
        item_id = get_asset_id(asset_file_path)
        if not item_id:
            print(f"  건너뜀: {asset_file} (id 필드 없음)")
            skipped_count += 1
            continue
        
        # 대응하는 이미지 파일 찾기
        image_file = f"{item_id}.png"
        image_path = os.path.join(IMAGES_PATH, image_file)
        meta_path = f"{image_path}.meta"
        
        if not os.path.exists(image_path):
            print(f"  건너뜀: {item_id} (이미지 파일 없음: {image_file})")
            skipped_count += 1
            continue
        
        if not os.path.exists(meta_path):
            print(f"  건너뜀: {item_id} (.meta 파일 없음)")
            skipped_count += 1
            continue
        
        # 이미지 GUID 추출
        guid, sprite_id = get_image_guid(meta_path)
        if not guid:
            print(f"  건너뜀: {item_id} (GUID 추출 실패)")
            skipped_count += 1
            continue
        
        # asset 파일 업데이트 (덮어쓰기)
        if update_asset_icon(asset_file_path, guid, sprite_id):
            print(f"  적용됨: {item_id} ({asset_file})")
            updated_count += 1
        else:
            print(f"  - 변경사항 없음: {item_id} ({asset_file})")
    
    print(f"  총 {updated_count}개 업데이트됨, {skipped_count}개 건너뜀")

def main():
    """메인 실행 함수"""
    print("\n=== Unity MarketActor 이미지 자동 적용 ===\n")
    
    process_market_actors()
    
    print(f"\n작업 완료!")
    print("\nUnity 에디터에서 프로젝트를 새로고침하세요.")

if __name__ == "__main__":
    main()

