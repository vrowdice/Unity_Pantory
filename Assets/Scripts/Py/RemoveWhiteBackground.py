import os
from PIL import Image
import numpy as np

# ============================================
# 설정 상수
# ============================================
# 흰색 배경을 제거할 이미지 파일들이 있는 폴더 경로
IMAGES_FOLDER_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "../../Images/Building")

# 흰색으로 간주할 임계값 (0-255, 값이 클수록 더 넓은 범위의 밝은 색을 흰색으로 간주)
WHITE_THRESHOLD = 240  # 240 이상이면 흰색으로 간주

# ============================================

def remove_white_background(image_path, output_path=None, threshold=WHITE_THRESHOLD):
    """
    이미지의 흰색 배경을 투명하게 제거
    
    Args:
        image_path: 입력 이미지 경로
        output_path: 출력 이미지 경로 (None이면 원본 파일 덮어쓰기)
        threshold: 흰색으로 간주할 RGB 값의 임계값 (0-255)
    
    Returns:
        bool: 성공 여부
    """
    try:
        # 이미지 열기
        img = Image.open(image_path)
        
        # RGBA 모드로 변환 (투명도 지원)
        if img.mode != 'RGBA':
            img = img.convert('RGBA')
        
        # NumPy 배열로 변환
        data = np.array(img)
        
        # RGB 채널 추출
        r, g, b, a = data[:, :, 0], data[:, :, 1], data[:, :, 2], data[:, :, 3]
        
        # 흰색 픽셀 마스크 생성 (R, G, B 모두 threshold 이상인 픽셀)
        white_mask = (r >= threshold) & (g >= threshold) & (b >= threshold)
        
        # 흰색 픽셀의 알파 값을 0으로 설정 (투명하게)
        data[:, :, 3] = np.where(white_mask, 0, a)
        
        # PIL Image로 다시 변환
        img_transparent = Image.fromarray(data, 'RGBA')
        
        # 출력 경로 설정
        if output_path is None:
            output_path = image_path
        
        # PNG로 저장 (투명도 지원)
        img_transparent.save(output_path, 'PNG')
        
        return True
        
    except Exception as e:
        print(f"  오류 발생 ({os.path.basename(image_path)}): {e}")
        return False

def process_all_images(folder_path, threshold=WHITE_THRESHOLD):
    """
    폴더 내 모든 이미지 파일의 흰색 배경 제거
    
    Args:
        folder_path: 이미지 파일들이 있는 폴더 경로
        threshold: 흰색으로 간주할 RGB 값의 임계값
    """
    if not os.path.exists(folder_path):
        print(f"폴더를 찾을 수 없습니다: {folder_path}")
        return
    
    # 지원하는 이미지 확장자
    image_extensions = {'.png', '.jpg', '.jpeg', '.bmp', '.tiff', '.tif'}
    
    # 폴더 내 모든 파일 검색
    image_files = [
        f for f in os.listdir(folder_path)
        if os.path.splitext(f.lower())[1] in image_extensions
    ]
    
    if not image_files:
        print(f"처리할 이미지 파일이 없습니다: {folder_path}")
        return
    
    print(f"\n[흰색 배경 제거] {len(image_files)}개 이미지 처리 중...")
    print(f"폴더: {folder_path}")
    print(f"흰색 임계값: {threshold} 이상\n")
    
    success_count = 0
    fail_count = 0
    
    for image_file in image_files:
        image_path = os.path.join(folder_path, image_file)
        
        print(f"  처리 중: {image_file}", end=" ... ")
        
        if remove_white_background(image_path, threshold=threshold):
            print("완료")
            success_count += 1
        else:
            print("실패")
            fail_count += 1
    
    print(f"\n처리 완료: 성공 {success_count}개, 실패 {fail_count}개")

def main():
    """메인 실행 함수"""
    print("\n" + "="*60)
    print("이미지 흰색 배경 제거 도구")
    print("="*60)
    
    # 절대 경로로 변환
    abs_folder_path = os.path.abspath(IMAGES_FOLDER_PATH)
    
    print(f"\n이미지 폴더: {abs_folder_path}")
    print(f"흰색 임계값: {WHITE_THRESHOLD} 이상")
    
    process_all_images(abs_folder_path, threshold=WHITE_THRESHOLD)
    
    print("\n작업 완료!")

if __name__ == "__main__":
    main()

