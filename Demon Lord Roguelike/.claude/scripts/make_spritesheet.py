from PIL import Image
import os

frames_dir = r"e:\Unity\Project\DLR\DemonLordRoguelike\Demon Lord Roguelike\Assets\Out\meteorite_frames"
output_path = r"e:\Unity\Project\DLR\DemonLordRoguelike\Demon Lord Roguelike\Assets\Out\meteorite_fall_4x4.png"

cols, rows = 4, 4
frame_count = cols * rows  # 16

frames = []
for i in range(frame_count):
    path = os.path.join(frames_dir, f"{i}.png")
    img = Image.open(path).convert("RGBA")
    frames.append(img)

fw, fh = frames[0].size
sheet = Image.new("RGBA", (fw * cols, fh * rows), (0, 0, 0, 0))

for idx, img in enumerate(frames):
    x = (idx % cols) * fw
    y = (idx // cols) * fh
    sheet.paste(img, (x, y))

sheet.save(output_path)
print(f"Saved spritesheet: {output_path} ({fw*cols}x{fh*rows}px, {frame_count} frames)")
