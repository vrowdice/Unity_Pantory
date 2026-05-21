import os
import numpy as np
from PIL import Image

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
TARGET_DIR = os.path.join(BASE_DIR, "../../Images/Building")

# alpha >= THRESH → 불투명(255), alpha < THRESH → 투명(0)
# "100 아니면 0" = 100% 또는 0% (0~255 스케일에서 255 / 0)
ALPHA_THRESHOLD = 200
OPAQUE_ALPHA = 255
TRANSPARENT_ALPHA = 0

IMAGE_EXTS = (".png", ".jpg", ".jpeg", ".bmp", ".webp")


def binarize_alpha(file_path):
    try:
        img = Image.open(file_path).convert("RGBA")
        data = np.array(img, dtype=np.uint8)

        alpha = data[:, :, 3]
        data[:, :, 3] = np.where(alpha >= ALPHA_THRESHOLD, OPAQUE_ALPHA, TRANSPARENT_ALPHA)

        result = Image.fromarray(data, "RGBA")
        save_path = os.path.splitext(file_path)[0] + ".png"
        result.save(save_path, "PNG")
        return True

    except Exception as e:
        print(f"Error processing {os.path.basename(file_path)}: {e}")
        return False


def main():
    if not os.path.exists(TARGET_DIR):
        print(f"Directory not found: {TARGET_DIR}")
        return

    files = sorted(
        f for f in os.listdir(TARGET_DIR)
        if f.lower().endswith(IMAGE_EXTS)
    )

    print(f"Processing {len(files)} files in: {TARGET_DIR}")
    print(f"Alpha rule: >= {ALPHA_THRESHOLD} → {OPAQUE_ALPHA}, else → {TRANSPARENT_ALPHA}")
    print("-" * 40)

    count = 0
    for name in files:
        file_path = os.path.join(TARGET_DIR, name)
        if binarize_alpha(file_path):
            print(f"[Done] {name}")
            count += 1
        else:
            print(f"[Fail] {name}")

    print("-" * 40)
    print(f"Completed: {count}/{len(files)}")


if __name__ == "__main__":
    main()
