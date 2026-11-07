"""Rebuild the Crimson Crown Palace atlas and its Blender and Unity models."""
import argparse
import math
import struct
import sys
import zlib
from collections import defaultdict
from pathlib import Path

import bpy
import numpy as np

NAME = "CrimsonCrown_Palace_Atlas"
WIDTH, HEIGHT = 512, 1024
PADDING = 2
EPSILON = 1e-6
EMISSION_STRENGTH = 12
SURFACE_OFFSET = 8


def srgb(value):
    return value * 12.92 if value <= .0031308 else 1.055 * value ** (1 / 2.4) - .055


def linear(value):
    return value / 12.92 if value <= .04045 else ((value + .055) / 1.055) ** 2.4


def rgb_bytes(values):
    return tuple(round(255 * srgb(value)) for value in values)


def source_pixels(image):
    width, height = image.size
    pixels = np.empty(width * height * 4, dtype=np.float32)
    image.pixels.foreach_get(pixels)
    return np.rint(pixels.reshape(height, width, 4)[..., :3] * 255).astype(np.uint8)


def write_png(path, pixels):
    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

    rows = b"".join(b"\0" + row.tobytes() for row in pixels[::-1])
    header = struct.pack(">IIBBBBB", WIDTH, HEIGHT, 8, 6, 0, 0, 0)
    path.write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header)
                     + chunk(b"IDAT", zlib.compress(rows, 9)) + chunk(b"IEND", b""))


def place_tile(atlas, tile, x, y, alpha):
    h, w = tile.shape[:2]
    region = atlas[y:y+h+2*PADDING, x:x+w+2*PADDING]
    if region.shape[:2] != (h + 2 * PADDING, w + 2 * PADDING) or region[..., 3].any():
        raise ValueError(f"Atlas tile {w}x{h} at ({x}, {y}) exceeds its reserved space")
    region[..., :3] = np.pad(tile, ((PADDING, PADDING), (PADDING, PADDING), (0, 0)), mode="wrap")
    region[..., 3] = alpha
    return x + PADDING, y + PADDING, w, h


def atlas_uv(tile, u=.5, v=.5):
    x, y, w, h = tile
    return (x + w * u) / WIDTH, (y + h * v) / HEIGHT


def build_atlas(materials, atlas_path):
    atlas = np.zeros((HEIGHT, WIDTH, 4), dtype=np.uint8)
    entries = {}
    images = {}
    swatch_y = 950
    black_emission = atlas_uv(place_tile(atlas, np.zeros((1, 1, 3), dtype=np.uint8), 0, 915, 255))
    emission_types = {"Window_StainedGlass": 1, "Flame_Amber": 2, "Flame_Core": 3}

    for index, material in enumerate(sorted(materials, key=lambda item: item.name)):
        name = material.name
        nodes = material.node_tree.nodes
        surface = nodes.get("Principled BSDF")
        image = next((node.image for node in nodes if node.type == "TEX_IMAGE"), None)
        metal = surface.inputs["Metallic"].default_value
        rough = surface.inputs["Roughness"].default_value
        emission = surface.inputs["Emission Strength"].default_value
        color = surface.inputs["Base Color"].default_value[:3]
        x = index * 8
        # glTF samples these channels as linear data. Blender samples the upper row as sRGB.
        linear_pbr = np.array([[(0, round(rough * 255), round(metal * 255))]], dtype=np.uint8)
        srgb_pbr = np.array([[rgb_bytes((0, rough, metal))]], dtype=np.uint8)
        pbr_uv = atlas_uv(place_tile(atlas, linear_pbr, x, swatch_y, 255))
        place_tile(atlas, srgb_pbr, x, swatch_y + SURFACE_OFFSET, 255)

        unity_metal = name.startswith("Metal") or name in {"Gold_Highlight", "Bronze_Shadow"}
        smoothness_class = 2 if image is None else int(name == "Floor_Amethyst")
        flags = emission_types.get(name, 0) | (int(unity_metal) << 2) | (smoothness_class << 3)
        entry = {"surface": pbr_uv, "emission": black_emission, "tiled": image is not None}
        entries[name] = entry
        if image:
            images[name] = (source_pixels(image), flags, emission)
        else:
            swatch = np.array([[rgb_bytes(color)]], dtype=np.uint8)
            entry["base"] = place_tile(atlas, swatch, x, 970, 255 - flags)
            if emission:
                glow = np.array([[rgb_bytes([c * emission / EMISSION_STRENGTH for c in color])]], dtype=np.uint8)
                entry["emission"] = atlas_uv(place_tile(atlas, glow, x, 980, 255))

    # Preserve every input texel. The tall carpet has a dedicated column.
    carpet, flags, _ = images.pop("Carpet_Royal")
    entries["Carpet_Royal"]["base"] = place_tile(atlas, carpet, 0, 0, 255 - flags)
    x0 = carpet.shape[1] + 2 * PADDING
    x, y, row_height = x0, 0, 0
    for name, (pixels, flags, emission) in sorted(images.items(), key=lambda item: -item[1][0].shape[0]):
        h, w = pixels.shape[:2]
        if w + 2 * PADDING > WIDTH - x0:
            tile_x, tile_y = 0, 775
        else:
            if x + w + 2 * PADDING > WIDTH:
                x, y, row_height = x0, y + row_height, 0
            tile_x, tile_y = x, y
            x += w + 2 * PADDING
            row_height = max(row_height, h + 2 * PADDING)
        entries[name]["base"] = place_tile(atlas, pixels, tile_x, tile_y, 255 - flags)
        if emission:
            glow = np.array([round(255 * srgb(linear(i / 255) * emission / EMISSION_STRENGTH))
                             for i in range(256)], dtype=np.uint8)
            entries[name]["emission"] = place_tile(atlas, glow[pixels], x0, 760, 255)

    atlas[..., 3][atlas[..., 3] == 0] = 255
    write_png(atlas_path, atlas)
    return entries


def shared_material(atlas_path):
    material = bpy.data.materials.new(NAME)
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    surface = nodes.get("Principled BSDF")
    image = bpy.data.images.load(str(atlas_path))
    image.alpha_mode = "CHANNEL_PACKED"
    image.pack()

    def sample(uv_name, y_offset=0):
        uv = nodes.new("ShaderNodeUVMap")
        uv.uv_map = uv_name
        coords = uv.outputs["UV"]
        if y_offset:
            offset = nodes.new("ShaderNodeVectorMath")
            offset.operation = "ADD"
            offset.inputs[1].default_value = (0, y_offset / HEIGHT, 0)
            links.new(coords, offset.inputs[0])
            coords = offset.outputs["Vector"]
        texture = nodes.new("ShaderNodeTexImage")
        texture.image = image
        texture.interpolation = "Closest"
        texture.extension = "EXTEND"
        links.new(coords, texture.inputs["Vector"])
        return texture.outputs["Color"]

    links.new(sample("UVMap"), surface.inputs["Base Color"])
    pbr = nodes.new("ShaderNodeSeparateColor")
    links.new(sample("Surface", SURFACE_OFFSET), pbr.inputs["Color"])
    links.new(pbr.outputs["Green"], surface.inputs["Roughness"])
    links.new(pbr.outputs["Blue"], surface.inputs["Metallic"])
    links.new(sample("Emission"), surface.inputs["Emission Color"])
    surface.inputs["Emission Strength"].default_value = EMISSION_STRENGTH
    return material, image


def clip_edge(polygon, axis, boundary, direction):
    result = []
    previous_uv, previous_position = polygon[-1]
    previous_distance = direction * (previous_uv[axis] - boundary)
    for uv, position in polygon:
        distance = direction * (uv[axis] - boundary)
        if (previous_distance >= 0) != (distance >= 0):
            t = previous_distance / (previous_distance - distance)
            result.append((previous_uv.lerp(uv, t), previous_position.lerp(position, t)))
        if distance >= 0:
            result.append((uv, position))
        previous_uv, previous_position, previous_distance = uv, position, distance
    return result


def repeat_tiles(triangle):
    u, v = zip(*(uv[:] for uv, _ in triangle))
    left = math.floor(min(u) + EPSILON)
    right = max(left, math.ceil(max(u) - EPSILON) - 1)
    bottom = math.floor(min(v) + EPSILON)
    top = max(bottom, math.ceil(max(v) - EPSILON) - 1)
    if left == right and bottom == top:
        yield left, bottom, triangle
        return
    for x in range(left, right + 1):
        for y in range(bottom, top + 1):
            polygon = triangle
            for axis, boundary, direction in ((0, x, 1), (0, x + 1, -1), (1, y, 1), (1, y + 1, -1)):
                if len(polygon) < 3:
                    break
                polygon = clip_edge(polygon, axis, boundary, direction)
            if len(polygon) >= 3:
                yield x, y, polygon


class MeshGroup:
    def __init__(self, for_unity=False):
        self.vertices = []
        self.indices = {}
        self.faces = []
        self.for_unity = for_unity
        self.layers = ("UVMap", "Lightmap", "AtlasOrigin", "AtlasScale") if for_unity else ("UVMap", "Surface", "Emission")
        self.uvs = [[] for _ in self.layers]

    def add(self, polygon, entry, tile_x=0, tile_y=0):
        for i in range(1, len(polygon) - 1):
            triangle = (polygon[0], polygon[i], polygon[i + 1])
            a, b, c = (point for _, point in triangle)
            if (b - a).cross(c - a).length_squared < 1e-18:
                continue
            indices = []
            for uv, point in triangle:
                key = tuple(round(value, 7) for value in point)
                if key not in self.indices:
                    self.indices[key] = len(self.vertices)
                    self.vertices.append(tuple(point))
                indices.append(self.indices[key])
            if len(set(indices)) != 3:
                continue
            self.faces.append(indices)
            for uv, _ in triangle:
                if self.for_unity:
                    x, y, width, height = entry["base"]
                    local_uv = tuple(uv) if entry["tiled"] else (.5, .5)
                    for layer, value in zip(self.uvs, (local_uv, local_uv, (x / WIDTH, y / HEIGHT),
                                                        (width / WIDTH, height / HEIGHT))):
                        layer.append(value)
                    continue
                u = min(1, max(0, uv.x - tile_x)) if entry["tiled"] else .5
                v = min(1, max(0, uv.y - tile_y)) if entry["tiled"] else .5
                self.uvs[0].append(atlas_uv(entry["base"], u, v))
                self.uvs[1].append(entry["surface"])
                glow = entry["emission"]
                self.uvs[2].append(atlas_uv(glow, u, v) if len(glow) == 4 else glow)

    def make_object(self, name, collection, material):
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata(self.vertices, [], self.faces)
        mesh.materials.append(material)
        for layer, values in zip(self.layers, self.uvs):
            uv_layer = mesh.uv_layers.new(name=layer, do_init=False)
            uv_layer.uv.foreach_set("vector", np.asarray(values, dtype=np.float32).ravel())
        obj = bpy.data.objects.new(name, mesh)
        collection.objects.link(obj)
        return obj


def group_meshes(objects, entries, material):
    groups = defaultdict(MeshGroup)
    unity_groups = defaultdict(lambda: MeshGroup(for_unity=True))
    for obj in objects:
        collection = obj.users_collection[0]
        mesh = obj.data
        transform = obj.matrix_world
        mesh.calc_loop_triangles()
        uv = mesh.uv_layers.active.uv
        positions = [transform @ vertex.co for vertex in mesh.vertices]
        group = groups[collection]
        unity_group = unity_groups[collection]
        for triangle in mesh.loop_triangles:
            entry = entries[mesh.materials[triangle.material_index].name]
            polygon = [(uv[loop].vector.copy(), positions[vertex])
                       for loop, vertex in zip(triangle.loops, triangle.vertices)]
            unity_group.add(polygon, entry)
            if entry["tiled"]:
                for x, y, fragment in repeat_tiles(polygon):
                    group.add(fragment, entry, x, y)
            else:
                group.add(polygon, entry)
    for obj in objects:
        bpy.data.objects.remove(obj, do_unlink=True)
    return (
        [group.make_object(f"{collection.name}__Atlas", collection, material)
         for collection, group in unity_groups.items()],
        groups,
    )


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True, type=Path, help="Archived editable palace .blend.")
    parser.add_argument("--output", required=True, type=Path, help="Directory for the three rebuilt assets.")
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    args = parser.parse_args(argv)
    output = args.output.resolve()
    atlas_path = output / "palace_crimson_crown_atlas_1.png"
    fbx_path = output / "Building_Palace_CrimsonCrown_1.fbx"
    blend_path = output / "Building_Palace_CrimsonCrown_Atlas_1.blend"
    if any(path.exists() for path in (atlas_path, fbx_path, blend_path)):
        parser.error("An output already exists. Choose a different output directory.")
    bpy.ops.wm.open_mainfile(filepath=str(args.source.resolve(strict=True)))
    output.mkdir(parents=True, exist_ok=True)
    objects = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    materials = {slot.material for obj in objects for slot in obj.material_slots}
    entries = build_atlas(materials, atlas_path)
    material, image = shared_material(atlas_path)
    visibility = {collection: (collection.hide_viewport, collection.hide_render)
                  for obj in objects for collection in obj.users_collection}
    for collection in visibility:
        collection.hide_viewport = collection.hide_render = False

    unity_meshes, portable_groups = group_meshes(objects, entries, material)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for previous in materials:
        bpy.data.materials.remove(previous)
    for previous in list(bpy.data.images):
        if previous != image and previous.users == 0:
            bpy.data.images.remove(previous)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in unity_meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = unity_meshes[0]
    bpy.ops.export_scene.fbx(filepath=str(fbx_path), use_selection=True,
                             object_types={"MESH"}, axis_forward="-Z", axis_up="Y", apply_unit_scale=True,
                             add_leaf_bones=False, path_mode="RELATIVE", mesh_smooth_type="FACE")
    unity_triangles = sum(len(obj.data.polygons) for obj in unity_meshes)
    for obj in unity_meshes:
        mesh = obj.data
        bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.meshes.remove(mesh)
    merged = [group.make_object(f"{collection.name}__Atlas", collection, material)
              for collection, group in portable_groups.items()]
    bpy.context.view_layer.update()
    for obj in merged:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = merged[0]
    for collection, (viewport, render) in visibility.items():
        collection.hide_viewport, collection.hide_render = viewport, render
    image.filepath = bpy.path.relpath(str(atlas_path), start=str(output))
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    print(f"Atlas {WIDTH}x{HEIGHT}, {len(materials)} source surfaces, "
          f"{len(merged)} mesh groups, {unity_triangles} FBX triangles, "
          f"{sum(len(obj.data.polygons) for obj in merged)} portable triangles")


if __name__ == "__main__":
    main()
