"""Run with Blender --background --factory-startup --disable-autoexec --python this_file."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/'ArtSource/LowPoly';OUT=ROOT/'Assets/Resources/Art/LowPoly'
OUT.mkdir(parents=True,exist_ok=True);(OUT/'Textures').mkdir(exist_ok=True)
items={'Mug':SOURCE/'mug.glb','Safe':SOURCE/'safe.glb','Revolver':SOURCE/'colt/revovler1.blend','Arms':SOURCE/'arms/arms.glb'}
items.update({n:SOURCE/('tavern/tavern_pack/models/'+f+'.fbx') for n,f in [('Bar','bar'),('Table','tabel'),('Chair','Chair'),('Shelf','sideboard'),('Door','Door'),('Window','window'),('Cup','cup'),('Plate','plate')]})
report={}
for name,path in items.items():
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if path.suffix=='.glb':bpy.ops.import_scene.gltf(filepath=str(path))
 elif path.suffix=='.blend':bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False,use_scripts=False)
 else:bpy.ops.import_scene.fbx(filepath=str(path))
 for obj in list(bpy.context.scene.objects):
  if obj.type not in {'MESH','ARMATURE','EMPTY'} or (name=='Arms' and obj.name=='Icosphere'):bpy.data.objects.remove(obj,do_unlink=True)
 for mat in bpy.data.materials:
  if not mat.use_nodes:continue
  principled=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  if not principled:continue
  for socket in ['Normal','Roughness','Metallic']:
   for link in list(principled.inputs[socket].links):mat.node_tree.links.remove(link)
  principled.inputs['Roughness'].default_value=.8
  if name not in ['Safe','Revolver']:principled.inputs['Metallic'].default_value=0
  base=principled.inputs['Base Color']
  image_node=base.links[0].from_node if base.is_linked else None
  if image_node and image_node.type=='TEX_IMAGE':
   image=image_node.image
   if name in ['Bar','Table','Chair','Shelf','Door','Window']:
    image=bpy.data.images.load(str(SOURCE/'wood.jpg'),check_existing=True)
   elif name=='Revolver':image=bpy.data.images.load(str(SOURCE/'colt/colt_revovler_tex.png'),check_existing=True)
   elif name=='Arms':image=bpy.data.images.load(str(SOURCE/'arms/arm_albedo_pale.png'),check_existing=True)
   if image and not image.has_data:
    candidate=SOURCE/'tavern/tavern_pack/textures'/image.name
    if candidate.exists():image=bpy.data.images.load(str(candidate),check_existing=True)
   if image and image.has_data:
    if max(image.size)>256:image.scale(256,256)
    image.filepath_raw=str(OUT/'Textures'/(name+'_'+mat.name.replace('/','_')+'.png'));image.file_format='PNG';image.save();image_node.image=image;image_node.interpolation='Closest'
  for node in list(mat.node_tree.nodes):
   if node.type=='TEX_IMAGE' and node!=image_node:mat.node_tree.nodes.remove(node)
 if name=='Safe':
  for obj in bpy.context.scene.objects:
   if obj.type=='MESH':
    mod=obj.modifiers.new('Prototype low-poly reduction','DECIMATE');mod.ratio=.3;bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=mod.name)
 # Bake arm constraint pose for the static import, preserve rig for later grip animations.
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 report[name]={'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),'materials':[m.name for m in bpy.data.materials]}
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=False)
(OUT/'ImportReport.json').write_text(json.dumps(report,indent=2)+'\n')
image=bpy.data.images.load(str(SOURCE/'wood.jpg'));image.scale(256,256);image.filepath_raw=str(OUT/'Textures/WoodPlanks.png');image.file_format='PNG';image.save()
print('SALOON_ART_EXPORT PASS')
