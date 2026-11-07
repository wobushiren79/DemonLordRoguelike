"""Recover individually placed objects from the original palace's grouped meshes."""

from collections import defaultdict

import bpy
import numpy as np
from mathutils import Vector

PART_TAG = "palace_placeable"
LEAVES = {"Leaf_Jade", "Leaf_Light", "Leaf_Olive"}
PHASE_TILED = {"Stone_Ivory", "Metal_Gold", "Metal_Dark", "Wood_Walnut", "Velvet_Red"}


def bilateral(kind, positions):
    return [(f"{kind}_{side}_{i:02}", (sign * x, y))
            for side, sign in (("West", -1), ("East", 1))
            for i, (x, y) in enumerate(positions, 1)]


PILLARS = [(8.35, y) for y in (-11, -4, 3, 11)]
LAYOUT = {
    "04_Colonnade": ("Pillar", bilateral("Pillar", PILLARS)),
    "07_Banners": ("Banner", bilateral("Banner", [(x, y - .75) for x, y in PILLARS] + [(5, 15.42)])),
    "08_Candelabra": ("Candelabrum", bilateral("Candelabrum", [(4.55, y) for y in (-12, -6, -.3, 7.7, 13.4)] + [(7, 14.6)])),
    "09_Urns_And_Plants": ("Planter", bilateral("Planter", [(7.05, y) for y in (-14.3, 4.2, 13.9)])),
    "10_Side_Furniture": ("Bench", bilateral("Bench", [(10.05, y) for y in (-7.5, -.5, 7.5)])),
    "14_Chandeliers_Removable": ("Chandelier", [("Chandelier_01", (0, -7)), ("Chandelier_02", (0, 6.6))]),
}


def same_surface(left, right, material):
    if material in PHASE_TILED:
        # The legacy surface grain was projected in world space. Preserve its
        # density and orientation, while giving copies a common local phase.
        delta = left - right
        return np.allclose(delta, delta[:, :1], rtol=0, atol=3e-5)
    if any(node.type == "TEX_IMAGE" for node in bpy.data.materials[material].node_tree.nodes):
        return np.allclose(left, right, rtol=0, atol=3e-5)
    return True


def same_part(left, right):
    if left.keys() != right.keys():
        return False
    for material, (positions, uv) in left.items():
        other_positions, other_uv = right[material]
        if (positions.shape != other_positions.shape or
                not np.allclose(positions, other_positions, rtol=0, atol=3e-5) or
                not same_surface(uv, other_uv, material)):
            return False
    return True


def part_mesh(name, surfaces):
    vertices, faces, uv, material_indices = [], [], [], []
    materials = list(surfaces)
    for slot, material in enumerate(materials):
        positions, coords = surfaces[material]
        # Keep flat faces and original per-corner UVs. Welding identical
        # positions keeps the source convenient to edit.
        indexed = {}
        for triangle, face_uv in zip(positions, coords):
            face = []
            for point in triangle:
                key = tuple(round(float(value), 7) for value in point)
                if key not in indexed:
                    indexed[key] = len(vertices)
                    vertices.append(point)
                face.append(indexed[key])
            if len(set(face)) == 3:
                faces.append(face)
                material_indices.append(slot)
                uv.extend(face_uv)
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    for material in materials:
        mesh.materials.append(bpy.data.materials[material])
    mesh.polygons.foreach_set("material_index", material_indices)
    layer = mesh.uv_layers.new(name="UVMap", do_init=False)
    layer.uv.foreach_set("vector", np.asarray(uv, dtype=np.float32).ravel())
    return mesh


def parent_in_place(child, parent):
    world = child.matrix_world.copy()
    child.parent = parent
    child.matrix_world = world


def split_legacy_props():
    """Split legacy collections once. Revised sources already use linked meshes."""
    if any(obj.get(PART_TAG) for obj in bpy.data.objects):
        return
    placed = {}
    for group_name, (kind, locations) in LAYOUT.items():
        collection = bpy.data.collections[group_name]
        legacy = sorted((o for o in collection.objects if o.type == "MESH"), key=lambda o: o.name)
        bins = {name: defaultdict(list) for name, _ in locations}
        centers = np.asarray([xy for _, xy in locations])
        for obj in legacy:
            mesh = obj.data
            mesh.calc_loop_triangles()
            uv_layer = mesh.uv_layers.active.uv
            for tri in mesh.loop_triangles:
                positions = np.asarray([(obj.matrix_world @ mesh.vertices[i].co)[:] for i in tri.vertices])
                center = positions[:, :2].mean(axis=0)
                distances = np.sum((centers - center) ** 2, axis=1)
                index = int(np.argmin(distances))
                if distances[index] > (3.0 if kind == "Chandelier" else 2.0) ** 2:
                    raise ValueError(f"Unassigned {kind} triangle near {center}")
                material = mesh.materials[tri.material_index].name
                uv = [uv_layer[i].vector[:] for i in tri.loops]
                bins[locations[index][0]][material].append((positions, uv))

        prototypes = defaultdict(list)
        for name, xy in locations:
            bucket = bins[name]
            z = min(p[:, 2].min() for triangles in bucket.values() for p, _ in triangles)
            pivot = Vector((*xy, float(z)))
            splits = [(name, kind, {m: tris for m, tris in bucket.items() if m not in LEAVES})]
            if kind == "Planter":
                splits.append((f"{name}_Foliage", "Foliage", {m: tris for m, tris in bucket.items() if m in LEAVES}))
            for object_name, family, surfaces in splits:
                local = {m: (np.asarray([p for p, _ in tris]) - np.asarray(pivot), np.asarray([uv for _, uv in tris]))
                         for m, tris in surfaces.items()}
                variants = prototypes[family]
                mesh = next((mesh for shape, mesh in variants if same_part(local, shape)), None)
                if mesh is None:
                    mesh = part_mesh(f"Prop_{family}_{len(variants) + 1:02}", local)
                    variants.append((local, mesh))
                obj = bpy.data.objects.new(object_name, mesh)
                collection.objects.link(obj)
                obj.location = pivot
                obj[PART_TAG] = True
                placed[object_name] = obj
        for obj in legacy:
            bpy.data.objects.remove(obj, do_unlink=True)

    bpy.context.view_layer.update()
    # Some collections are hidden in the reference view and therefore haven't
    # had their transforms evaluated. Update those objects when unhiding them.
    for name, obj in placed.items():
        if name.endswith("_Foliage"):
            parent_in_place(obj, placed[name.removesuffix("_Foliage")])
        elif name.startswith("Banner_") and not name.endswith("_05"):
            parent_in_place(obj, placed[name.replace("Banner_", "Pillar_", 1)])
    for light in bpy.data.objects:
        if light.type != "LIGHT" or light.data.type != "POINT":
            continue
        kind = "Candelabrum_" if light.name.startswith("Warm candle pool") else "Chandelier_"
        if not light.name.startswith(("Warm candle pool", "Chandelier warm fill")):
            continue
        candidates = [obj for name, obj in placed.items() if name.startswith(kind)]
        parent = min(candidates, key=lambda obj: (obj.location.xy - light.location.xy).length_squared)
        if (parent.location.xy - light.location.xy).length < .05:
            parent_in_place(light, parent)
