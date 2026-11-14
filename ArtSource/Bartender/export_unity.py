import bpy, os
out='/Users/kiskalloakos/Downloads/GAMES/LASTCALL/Assets/Resources/Art/Bartender'
for key,name in [('body-skin.png','Body'),('head-skin.png','Head'),('hair-skin','Hair')]:
 im=bpy.data.images[key];im.filepath_raw=os.path.join(out,name+'.png');im.file_format='PNG';im.save()
for mat in bpy.data.materials:
 mat.use_nodes=True;mat.node_tree.nodes.clear();n=mat.node_tree.nodes
 output=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfPrincipled');tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images[{'Body':'body-skin.png','Head':'head-skin.png','Hair':'hair-skin'}[mat.name]]
 mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);mat.node_tree.links.new(bs.outputs['BSDF'],output.inputs['Surface'])
m=bpy.data.objects['Bartender'].data
for poly in m.polygons:
 if poly.material_index==0:
  for loop in poly.loop_indices: m.uv_layers[0].data[loop].uv=m.uv_layers[1].data[loop].uv
while len(m.uv_layers)>1:m.uv_layers.remove(m.uv_layers[1])
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT')
for name in ['Bartender','Armatue']:bpy.data.objects[name].select_set(True)
bpy.context.view_layer.objects.active=bpy.data.objects['Armatue']
bpy.ops.export_scene.fbx(filepath=os.path.join(out,'Bartender.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
print('EXPORTED',os.listdir(out))
