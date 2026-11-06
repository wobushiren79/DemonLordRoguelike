"""Prepare exported RGBA cels or a nearest-neighbor final preview; never write .aseprite."""
import argparse
import json
from pathlib import Path

from PIL import Image, ImageColor


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="op", required=True)
    contour = sub.add_parser("contour")
    contour.add_argument("--manifest", type=Path, required=True)
    contour.add_argument("--output-dir", type=Path, required=True)
    contour.add_argument("--prefix", required=True)
    contour.add_argument("--exclude", action="append", default=[])
    contour.add_argument("--color", default="#111219")
    contour.add_argument("--min-luma", type=float, default=22)
    contour.add_argument("--keep-highlight", type=float, default=170)
    preview = sub.add_parser("preview")
    preview.add_argument("--input", type=Path, required=True)
    preview.add_argument("--output", type=Path, required=True)
    preview.add_argument("--scale", type=int, default=6)
    preview.add_argument("--background", help="Optional solid preview background, e.g. #4f5056")
    args = parser.parse_args()
    if args.op == "preview":
        if args.output.exists():
            raise FileExistsError(args.output)
        if args.scale < 1:
            raise ValueError("scale must be a positive integer")
        image = Image.open(args.input).convert("RGBA")
        if args.background:
            background = Image.new("RGBA", image.size, ImageColor.getcolor(args.background, "RGBA"))
            background.alpha_composite(image)
            image = background
        image.resize((image.width * args.scale, image.height * args.scale), Image.Resampling.NEAREST).save(args.output)
        print(json.dumps({"preview": str(args.output), "scale": args.scale}))
        return

    data = json.loads(args.manifest.read_text(encoding="utf-8-sig"))
    if args.output_dir.exists():
        raise FileExistsError(args.output_dir)
    args.output_dir.mkdir(parents=True)
    outline = ImageColor.getcolor(args.color, "RGBA")
    replacements, changed = [], 0
    for entry in data["cels"]:
        leaf_name = entry["layer"].rsplit("/", 1)[-1]
        if not leaf_name.startswith(args.prefix) or entry["layer"] in args.exclude:
            continue
        source = (args.manifest.parent / entry["png"]).resolve()
        if not source.is_relative_to(args.manifest.parent.resolve()):
            raise ValueError("PNG path leaves manifest directory")
        image = Image.open(source).convert("RGBA")
        before = image.copy()
        pixels = before.load()
        target = image.load()
        count = 0
        for y in range(image.height):
            for x in range(image.width):
                color = pixels[x, y]
                if not color[3]:
                    continue
                exposed = any(
                    not (0 <= nx < image.width and 0 <= ny < image.height) or pixels[nx, ny][3] == 0
                    for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))
                )
                luminance = 0.2126 * color[0] + 0.7152 * color[1] + 0.0722 * color[2]
                if exposed and args.min_luma <= luminance < args.keep_highlight:
                    target[x, y] = outline[:3] + (color[3],)
                    count += 1
        if count:
            image.save(args.output_dir / entry["png"])
            replacements.append({"layer": entry["layer"], "png": entry["png"], "x": entry["x"], "y": entry["y"]})
            changed += count
    if not replacements:
        raise ValueError("No contours changed; inspect the prefix and thresholds")
    manifest = {"width": data["width"], "height": data["height"], "mode": "replace", "frame": data["frame"], "cels": replacements}
    (args.output_dir / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"changed_pixels": changed, "changed_cels": len(replacements), "manifest": str(args.output_dir / "manifest.json")}, ensure_ascii=False))


if __name__ == "__main__":
    main()
