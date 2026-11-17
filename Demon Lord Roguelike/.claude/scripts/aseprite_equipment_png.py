"""Prepare equipment cels, previews, and PNG references."""
import argparse
import json
import struct
from collections import Counter
from io import BytesIO
from math import sqrt
from pathlib import Path

from PIL import Image, ImageColor


PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"
PNG_STATIC_CHUNKS = set(
    b"IHDR PLTE IDAT IEND cHRM gAMA iCCP sRGB cICP mDCV cLLI sBIT bKGD hIST "
    b"tRNS pHYs sPLT eXIf tIME tEXt zTXt iTXt".split()
)
PNG_TEXT_CHUNKS = {b"tEXt", b"zTXt", b"iTXt"}
PNG_DISCARDABLE_TEXT = {b"Comment", b"Software", b"Creation Time", b"date:create", b"date:modify"}
PNG_REFERENCE_METADATA = {
    b"cHRM", b"gAMA", b"iCCP", b"sRGB", b"cICP", b"mDCV", b"cLLI", b"pHYs", b"eXIf",
}
REFERENCE_PALETTES = (128, 144, 160, 192, 224, 256)


def png_chunks(data):
    if not data.startswith(PNG_SIGNATURE):
        raise ValueError("Input must be a PNG")
    offset = len(PNG_SIGNATURE)
    while offset + 12 <= len(data):
        size, kind = struct.unpack_from(">I4s", data, offset)
        end = offset + size + 12
        if end > len(data):
            break
        yield kind, data[offset:end]
        if kind == b"IEND":
            if end < len(data):
                yield None, data[end:]
            return
        offset = end
    raise ValueError("Truncated PNG")


def discardable_metadata(kind, raw):
    return kind == b"tIME" or (
        kind in PNG_TEXT_CHUNKS and raw[8:-4].split(b"\0", 1)[0] in PNG_DISCARDABLE_TEXT
    )


def optimized_png(original):
    chunks = list(png_chunks(original))
    with Image.open(BytesIO(original)) as reference:
        reference.verify()
    if any(kind not in PNG_STATIC_CHUNKS for kind, _ in chunks):
        return original

    kept = [(kind, raw) for kind, raw in chunks if not discardable_metadata(kind, raw)]
    stripped = PNG_SIGNATURE + b"".join(raw for _, raw in kept)
    best = min(original, stripped, key=len)

    with Image.open(BytesIO(original)) as reference:
        if reference.mode in ("L", "LA", "P", "RGB", "RGBA"):
            buffer = BytesIO()
            reference.save(buffer, format="PNG", optimize=True)
            encoded = list(png_chunks(buffer.getvalue()))
            if encoded[0] == kept[0]:
                # Reuse only the image stream so the original display metadata stays byte-for-byte intact.
                replacement = [raw for kind, raw in encoded if kind == b"IDAT"]
                start = next(i for i, (kind, _) in enumerate(kept) if kind == b"IDAT")
                stop = next(i for i in range(start, len(kept)) if kept[i][0] != b"IDAT")
                prefix = [raw for _, raw in kept[:start]]
                suffix = [raw for _, raw in kept[stop:]]
                repacked = PNG_SIGNATURE + b"".join(prefix + replacement + suffix)
                best = min(best, repacked, key=len)
        if best is not original:
            with Image.open(BytesIO(best)) as candidate:
                if (
                    candidate.size != reference.size or candidate.mode != reference.mode
                    or candidate.tobytes() != reference.tobytes()
                ):
                    return original
    return best


def reference_paths(input_path, output_path):
    input_path, output_path = Path(input_path), Path(output_path)
    if output_path.resolve() == input_path.resolve():
        raise ValueError("Input and output must differ")
    if output_path.exists():
        raise FileExistsError(output_path)
    return input_path, output_path


def optimize_reference(input_path, output_path):
    input_path, output_path = reference_paths(input_path, output_path)
    original = input_path.read_bytes()
    best = optimized_png(original)
    with output_path.open("xb") as output:
        output.write(best)
    return {
        "reference": str(output_path), "input_bytes": len(original), "output_bytes": len(best),
        "saved_bytes": len(original) - len(best),
    }


def encode_reference(image, colors, metadata):
    transparent = image.mode == "RGBA"
    method = Image.Quantize.FASTOCTREE if transparent else Image.Quantize.MEDIANCUT
    encoded_image = image.quantize(colors=colors, method=method, dither=Image.Dither.NONE)
    if transparent:
        expected_alpha = image.getchannel("A").tobytes()
        if encoded_image.convert("RGBA").getchannel("A").tobytes() != expected_alpha:
            # Indexed PNG has only one alpha value per palette entry.
            encoded_image = image

    buffer = BytesIO()
    encoded_image.save(buffer, format="PNG", optimize=True)
    data = buffer.getvalue()
    if metadata:
        # Display metadata remains valid after IHDR and before the new palette/image data.
        _, header = next(png_chunks(data))
        ihdr_end = len(PNG_SIGNATURE) + len(header)
        data = data[:ihdr_end] + metadata + data[ihdr_end:]
    palette = len(encoded_image.getcolors()) if encoded_image.mode == "P" else None
    return data, palette


def compress_reference(input_path, output_path):
    input_path, output_path = reference_paths(input_path, output_path)
    original = input_path.read_bytes()
    budget = len(original) // 10
    chunks = list(png_chunks(original))
    with Image.open(BytesIO(original)) as reference:
        reference.verify()
    for kind, _ in chunks:
        # A new palette can invalidate the source background index or sample precision.
        if kind not in PNG_STATIC_CHUNKS or kind in {b"sBIT", b"bKGD"}:
            raise ValueError(f"Reference PNG has unsupported data for rescaling: {kind!r}")
    bit_depth, color_type = chunks[0][1][16:18]
    if bit_depth > 8 or color_type in (0, 4) and any(kind == b"iCCP" for kind, _ in chunks):
        raise ValueError("Rescaling requires PNG samples of at most 8 bits and a compatible color profile")
    metadata = b"".join(
        raw for kind, raw in chunks
        if kind in PNG_REFERENCE_METADATA or kind in PNG_TEXT_CHUNKS and not discardable_metadata(kind, raw)
    )

    with Image.open(BytesIO(original)) as reference:
        source = reference.convert("RGBA")
    if source.getchannel("A").getextrema() == (255, 255):
        source = source.convert("RGB")
    source.info.clear()
    longest = max(source.size)
    # Start at the scale of the approved 627 px to 241 px reference.
    edge = max(1, round(longest * 241 / 627))

    def sample(long_edge):
        size = tuple(max(1, round(length * long_edge / longest)) for length in source.size)
        resized = source.resize(size, Image.Resampling.LANCZOS)
        if source.mode == "RGBA" and not resized.getchannel("A").getbbox():
            raise ValueError(f"Reference PNG has no visible content at {long_edge} px")
        return resized

    scaled = sample(edge)
    data, palette = encode_reference(scaled, REFERENCE_PALETTES[0], metadata)
    while len(data) > budget:
        if edge == 1:
            raise ValueError(
                f"Cannot fit a usable PNG within {budget} bytes: {edge} px needs {len(data)} bytes"
            )
        edge = max(1, min(edge - 1, int(edge * sqrt(budget / len(data)) * 0.99)))
        scaled = sample(edge)
        data, palette = encode_reference(scaled, REFERENCE_PALETTES[0], metadata)
    if palette is not None and len(data) * 10 < budget * 9:
        for colors in REFERENCE_PALETTES[1:]:
            candidate, candidate_palette = encode_reference(scaled, colors, metadata)
            if len(candidate) > budget:
                break
            if candidate_palette is not None and candidate_palette > palette:
                data, palette = candidate, candidate_palette

    with Image.open(BytesIO(data)) as candidate:
        candidate.load()
        if candidate.size != scaled.size or (
            source.mode == "RGBA" and candidate.convert("RGBA").getchannel("A").tobytes() != scaled.getchannel("A").tobytes()
        ):
            raise ValueError("Compressed PNG failed the dimension or alpha check")
    with output_path.open("xb") as output:
        output.write(data)
    return {
        "reference": str(output_path), "input_bytes": len(original), "output_bytes": len(data),
        "saved_bytes": len(original) - len(data), "budget_bytes": budget,
        "width": scaled.width, "height": scaled.height, "palette_colors": palette,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="op", required=True)
    contour = sub.add_parser("contour")
    contour.add_argument("--manifest", type=Path, required=True)
    contour.add_argument("--output-dir", type=Path, required=True)
    selection = contour.add_mutually_exclusive_group(required=True)
    selection.add_argument("--layer", action="append", help="Exact layer path; repeat for each layer")
    selection.add_argument("--prefix", help="Layer name prefix for legacy equipment")
    contour.add_argument("--exclude", action="append", default=[])
    contour.add_argument("--color", default="#111219")
    contour.add_argument("--min-luma", type=float, default=22)
    contour.add_argument("--keep-highlight", type=float, default=170)
    preview = sub.add_parser("preview")
    preview.add_argument("--input", type=Path, required=True)
    preview.add_argument("--output", type=Path, required=True)
    preview.add_argument("--scale", type=int, default=6)
    preview.add_argument("--background", help="Optional solid preview background, e.g. #4f5056")
    reference = sub.add_parser("optimize-reference", help="Losslessly optimize a PNG reference")
    reference.add_argument("--input", type=Path, required=True)
    reference.add_argument("--output", type=Path, required=True)
    compress = sub.add_parser("compress-reference", help="Compress a PNG reference to one tenth of its input size")
    compress.add_argument("--input", type=Path, required=True)
    compress.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.op == "optimize-reference":
        print(json.dumps(optimize_reference(args.input, args.output), ensure_ascii=False))
        return
    if args.op == "compress-reference":
        try:
            report = compress_reference(args.input, args.output)
        except (ValueError, OSError) as error:
            parser.exit(1, f"{error}\n")
        print(json.dumps(report, ensure_ascii=False))
        return
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
    selected_layers = set(args.layer or [])
    layer_counts = Counter(entry["layer"] for entry in data["cels"])
    for layer in sorted(selected_layers):
        if layer_counts[layer] == 0:
            raise ValueError(f"Layer has no cel in the exported frame: {layer}")
        if layer_counts[layer] > 1:
            raise ValueError(f"Ambiguous layer path: {layer}; select the intended cel in the source manifest")
    if args.output_dir.exists():
        raise FileExistsError(args.output_dir)
    args.output_dir.mkdir(parents=True)
    outline = ImageColor.getcolor(args.color, "RGBA")
    replacements, changed = [], 0
    for entry in data["cels"]:
        layer = entry["layer"]
        selected = layer in selected_layers if selected_layers else layer.rsplit("/", 1)[-1].startswith(args.prefix)
        if not selected or layer in args.exclude:
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
            replacement = {"layer": layer, "png": entry["png"], "x": entry["x"], "y": entry["y"]}
            if "layer_indices" in entry:
                replacement["layer_indices"] = entry["layer_indices"]
            replacements.append(replacement)
            changed += count
    if not replacements:
        raise ValueError("No contours changed; inspect the selected layers and thresholds")
    manifest = {"width": data["width"], "height": data["height"], "mode": "replace", "frame": data["frame"], "cels": replacements}
    (args.output_dir / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"changed_pixels": changed, "changed_cels": len(replacements), "manifest": str(args.output_dir / "manifest.json")}, ensure_ascii=False))


if __name__ == "__main__":
    main()
