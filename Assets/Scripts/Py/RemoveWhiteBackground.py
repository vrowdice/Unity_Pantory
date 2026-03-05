import os
import numpy as np
from PIL import Image
from dotenv import load_dotenv

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.abspath(os.path.join(BASE_DIR, "../../../"))
load_dotenv(os.path.join(PROJECT_ROOT, ".env"))
load_dotenv(os.path.join(BASE_DIR, ".env"))

try:
    from scipy.ndimage import label
    SCIPY_AVAILABLE = True
except ImportError:
    SCIPY_AVAILABLE = False
    print("Warning: scipy module not found.")

TARGET_DIR = os.path.join(BASE_DIR, "../../Images/Resource/Raw")
THRESH_LOWER = 200
THRESH_UPPER = 250

def get_connected_background_mask(binary_mask):
    if not SCIPY_AVAILABLE:
        return binary_mask

    labeled_array, _ = label(binary_mask)
    
    h, w = binary_mask.shape
    corners = [(0, 0), (0, w-1), (h-1, 0), (h-1, w-1)]
    
    background_labels = set()
    for r, c in corners:
        lbl = labeled_array[r, c]
        if lbl > 0:
            background_labels.add(lbl)
            
    return np.isin(labeled_array, list(background_labels))

def process_image(file_path):
    try:
        img = Image.open(file_path).convert('RGBA')
        data = np.array(img, dtype=np.float32)
        
        r, g, b, a = data[:,:,0], data[:,:,1], data[:,:,2], data[:,:,3]
        
        brightness = (r + g + b) / 3.0
        candidate_mask = brightness >= THRESH_LOWER
        target_mask = get_connected_background_mask(candidate_mask)
        alpha_factor = (THRESH_UPPER - brightness) / (THRESH_UPPER - THRESH_LOWER)
        alpha_factor = np.clip(alpha_factor, 0.0, 1.0)
        new_alpha = np.where(target_mask, a * alpha_factor, a)
        data[:,:,3] = new_alpha
        save_path = os.path.splitext(file_path)[0] + ".png"
        
        result = Image.fromarray(data.astype(np.uint8), 'RGBA')
        result.save(save_path, 'PNG')
        
        return True
        
    except Exception as e:
        print(f"Error processing {os.path.basename(file_path)}: {e}")
        return False

def main():
    if not os.path.exists(TARGET_DIR):
        print(f"Directory not found: {TARGET_DIR}")
        return

    image_exts = ('.png', '.jpg', '.jpeg', '.bmp')
    files = [f for f in os.listdir(TARGET_DIR) if f.lower().endswith(image_exts)]
    
    print(f"Processing {len(files)} files in: {TARGET_DIR}")
    print("-" * 40)

    count = 0
    for f in files:
        file_path = os.path.join(TARGET_DIR, f)
        
        if process_image(file_path):
            print(f"[Done] {f}")
            count += 1
        else:
            print(f"[Fail] {f}")

    print("-" * 40)
    print(f"Completed: {count}/{len(files)}")

if __name__ == "__main__":
    main()