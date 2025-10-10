import os
import re

# 프로젝트 루트 디렉토리
PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../"))
DATAS_PATH = os.path.join(PROJECT_ROOT, "Assets/Datas/Resource")
IMAGES_PATH = os.path.join(PROJECT_ROOT, "Assets/Images/Resource")

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
        sprite_id_match = re.search(r'internalID:\s*(-?\d+)', content)
        sprite_id = "21300000"
        
        return guid, sprite_id
        
    except Exception as e:
        print(f"  메타 파일 읽기 오류: {e}")
        return None, None

def update_asset_icon(asset_file_path, image_guid, sprite_id):
    """asset 파일의 icon 필드를 업데이트 (덮어쓰기)"""
    try:
        with open(asset_file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 기존 icon 라인 찾기 (모든 경우 매칭)
        # {fileID: 0} 또는 {fileID: xxx, guid: xxx, type: 3} 모두 매칭
        icon_pattern = r'(  icon: )\{[^}]+\}'
        
        # 새로운 icon 참조 생성
        new_icon = f'\\1{{fileID: {sprite_id}, guid: {image_guid}, type: 3}}'
        
        # 교체
        new_content = re.sub(icon_pattern, new_icon, content)
        
        # 변경사항이 있는 경우에만 저장
        if new_content != content:
            with open(asset_file_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
            return True
        
        return False
        
    except Exception as e:
        print(f"  asset 파일 업데이트 오류: {e}")
        return False

def process_category(category_name):
    """카테고리별로 이미지 적용"""
    data_folder = os.path.join(DATAS_PATH, category_name)
    image_folder = os.path.join(IMAGES_PATH, category_name)
    
    if not os.path.exists(data_folder) or not os.path.exists(image_folder):
        return
    
    asset_files = [f for f in os.listdir(data_folder) if f.endswith('.asset')]
    
    if not asset_files:
        return
    
    print(f"\n[{category_name}] {len(asset_files)}개 파일 처리 중...")
    
    updated_count = 0
    for asset_file in asset_files:
        # asset 파일명에서 id 추출 (파일명.asset)
        item_id = os.path.splitext(asset_file)[0]
        
        # 대응하는 이미지 파일 찾기
        image_file = f"{item_id}.png"
        image_path = os.path.join(image_folder, image_file)
        meta_path = f"{image_path}.meta"
        
        if not os.path.exists(image_path):
            continue
        
        if not os.path.exists(meta_path):
            print(f"  건너뜀: {item_id} (.meta 파일 없음)")
            continue
        
        # 이미지 GUID 추출
        guid, sprite_id = get_image_guid(meta_path)
        if not guid:
            print(f"  건너뜀: {item_id} (GUID 추출 실패)")
            continue
        
        # asset 파일 업데이트 (덮어쓰기)
        asset_file_path = os.path.join(data_folder, asset_file)
        if update_asset_icon(asset_file_path, guid, sprite_id):
            print(f"  ✓ 적용됨: {item_id}")
            updated_count += 1
        else:
            print(f"  - 변경사항 없음: {item_id}")
    
    print(f"  총 {updated_count}개 업데이트됨")

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
    print("\n=== Unity Resource 이미지 자동 적용 ===\n")
    
    # 자동으로 카테고리 감지
    categories = get_all_categories()
    print(f"감지된 카테고리: {categories}\n")
    
    total_processed = 0
    for category in categories:
        process_category(category)
        total_processed += 1
    
    print(f"\n작업 완료! (처리된 카테고리: {total_processed}개)")
    print("\nUnity 에디터에서 프로젝트를 새로고침하세요.")

if __name__ == "__main__":
    main()

