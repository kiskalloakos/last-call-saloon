"""Extract eugeneloza's CC0 tankard. Run with Blender --disable-autoexec."""
import bpy
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/Beer/Original'
OUT = ROOT / 'Assets/Resources/Art/LowPoly'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE / 'DwarfBeerBarrel.blend'), load_ui=False, use_scripts=False)
bpy.ops.object.select_all(action='DESELECT')
combined = bpy.data.objects['Cube.001']
combined.select_set(True)
bpy.context.view_layer.objects.active = combined
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.separate(type='LOOSE')
bpy.ops.object.mode_set(mode='OBJECT')
# The source combines barrel, stool, tap and mug into one mesh. The mug is the
# only loose component with 84 vertices (verified against the retained archive).
mug = next(o for o in bpy.data.objects if o.type == 'MESH' and len(o.data.vertices) == 84)
for obj in list(bpy.data.objects):
    if obj != mug:
        bpy.data.objects.remove(obj, do_unlink=True)
mug.name = 'CC0 Tankard'
bpy.context.view_layer.objects.active = mug
mug.select_set(True)
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
center = sum((mug.matrix_world @ Vector(c) for c in mug.bound_box), Vector()) / 8
bottom = min((mug.matrix_world @ Vector(c)).z for c in mug.bound_box)
for vertex in mug.data.vertices:
    vertex.co = mug.matrix_world @ vertex.co - Vector((center.x, center.y, bottom))
mug.location = (0, 0, 0)

image = bpy.data.images.load(str(SOURCE / 'DwarfBeerBarrel_texture4.png'))
image.scale(512, 512)
image.filepath_raw = str(OUT / 'Textures/BeerMug_Tankard.png')
image.file_format = 'PNG'
image.save()
material = bpy.data.materials.new('Tankard')
material.use_nodes = True
shader = material.node_tree.nodes.get('Principled BSDF')
shader.inputs['Roughness'].default_value = .8
texture = material.node_tree.nodes.new('ShaderNodeTexImage')
texture.image = image
material.node_tree.links.new(texture.outputs['Color'], shader.inputs['Base Color'])
mug.data.materials.clear()
mug.data.materials.append(material)
for polygon in mug.data.polygons:
    polygon.material_index = 0

# Fit a beer/foam surface inside the source rim, shared by every in-game view.
# These small additions are original geometry; the tankard and UVs stay intact.
def fill_disc(name, radius, height, depth, color):
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=radius, depth=depth,
                                       location=(-.0528, .0612, height))
    disc = bpy.context.object
    disc.name = name
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = color
    mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .75
    disc.data.materials.append(mat)

fill_disc('Amber ale', .163, .401, .018, (.68, .30, .035, 1))
fill_disc('Ale foam', .163, .414, .012, (.94, .85, .63, 1))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(OUT / 'BeerMug.fbx'), use_selection=True,
                         object_types={'MESH'}, bake_anim=False, axis_forward='-Z',
                         axis_up='Y', path_mode='STRIP')
print('SALOON_BEER_IMPORT PASS', len(mug.data.vertices), 'vertices')
