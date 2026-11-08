using System;
using System.IO;
using System.Linq;
using Saloon;
using UnityEditor;
using UnityEngine;

public static class SaloonWesternCharacter
{
    const string Source="Assets/tt-3d/Low-PolyWesternStarterPack/Character/Prefabs/gunman.prefab";
    [MenuItem("Saloon/Prepare Western Gunman character")]
    public static void Prepare()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        if(source==null)throw new InvalidOperationException("Import Low Poly Western Starter Pack first.");
        Directory.CreateDirectory("Assets/Resources/Art/Western");AssetDatabase.Refresh();
        var holder=new GameObject("Western Gunman");
        try
        {
            var character=(GameObject)PrefabUtility.InstantiatePrefab(source,holder.transform);
            var animator=character.GetComponent<Animator>();
            if(animator==null||!animator.avatar.isHuman||!animator.avatar.isValid)throw new InvalidOperationException("Invalid Gunman avatar.");
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var skins=character.GetComponentsInChildren<SkinnedMeshRenderer>();
            bool first=true;var bounds=new Bounds();
            foreach(var skin in skins)
            {
                var baked=new Mesh();skin.BakeMesh(baked);
                foreach(var vertex in baked.vertices)
                {
                    var point=skin.transform.TransformPoint(vertex);
                    if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                }
                UnityEngine.Object.DestroyImmediate(baked);skin.updateWhenOffscreen=true;
                // Seated poses and ragdolls extend beyond the pack's original standing box.
                skin.localBounds=new Bounds(new Vector3(0,.8f,0),Vector3.one*4);
            }
            float scale=1.78f/bounds.size.y;character.transform.localScale*=scale;
            character.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
            holder.AddComponent<SaloonCharacterMotion>();
            PrepareSpeech(holder,animator,skins.First(s=>s.name=="gunman"));
            foreach(var skin in skins.Where(s=>s.name!="gunman"))
            {
                var gripMesh=UnityEngine.Object.Instantiate(skin.sharedMesh);
                if(!PrepareGrip(holder,animator,skin,gripMesh)){UnityEngine.Object.DestroyImmediate(gripMesh);continue;}
                string gripPath="Assets/Resources/Art/Western/Grip_"+skin.name+".asset";
                var existingGrip=AssetDatabase.LoadAssetAtPath<Mesh>(gripPath);
                if(existingGrip!=null){EditorUtility.CopySerialized(gripMesh,existingGrip);UnityEngine.Object.DestroyImmediate(gripMesh);gripMesh=existingGrip;}
                else AssetDatabase.CreateAsset(gripMesh,gripPath);
                skin.sharedMesh=gripMesh;
            }
            PrefabUtility.SaveAsPrefabAsset(holder,"Assets/Resources/Art/Western/Gunman.prefab");
            AssetDatabase.SaveAssets();
            Debug.Log("SALOON_WESTERN_PREPARED height=1.78 source="+Source+" materials="+string.Join(",",skins.SelectMany(r=>r.sharedMaterials).Select(m=>m.name).Distinct()));
        }
        finally{UnityEngine.Object.DestroyImmediate(holder);}
    }

    static void PrepareSpeech(GameObject holder,Animator animator,SkinnedMeshRenderer face)
    {
        var head=animator.GetBoneTransform(HumanBodyBones.Head);
        var center=holder.transform.InverseTransformPoint(head.position);
        var baked=new Mesh();face.BakeMesh(baked);
        var vertices=baked.vertices;var delta=new Vector3[vertices.Length];int changed=0;
        for(int i=0;i<vertices.Length;i++)
        {
            var p=holder.transform.InverseTransformPoint(face.transform.TransformPoint(vertices[i]))-center;
            float weight=Mathf.SmoothStep(0,1,(-.014f-p.y)/.025f)*Mathf.SmoothStep(0,1,(p.y+.115f)/.025f)*
                (1-Mathf.SmoothStep(0,1,(Mathf.Abs(p.x)-.035f)/.055f))*Mathf.SmoothStep(0,1,(p.z-.025f)/.04f);
            if(weight<=0)continue;changed++;
            delta[i]=face.transform.InverseTransformVector(holder.transform.TransformVector(new Vector3(p.x*.04f,-.028f,.003f)*weight));
        }
        UnityEngine.Object.DestroyImmediate(baked);
        if(changed==0)throw new InvalidOperationException("No lower face vertices found for speech.");
        var mesh=UnityEngine.Object.Instantiate(face.sharedMesh);mesh.name="Gunman with talking jaw";
        mesh.AddBlendShapeFrame(SaloonSpeechMouth.Shape,100,delta,new Vector3[delta.Length],new Vector3[delta.Length]);
        PrepareGrip(holder,animator,face,mesh);
        const string path="Assets/Resources/Art/Western/GunmanTalkingFace.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing!=null){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
        else AssetDatabase.CreateAsset(mesh,path);
        face.sharedMesh=mesh;
        var mouth=holder.AddComponent<SaloonSpeechMouth>();mouth.FaceRenderer=face;mouth.HeadBone=head;
        mouth.MouthPosition=head.InverseTransformPoint(holder.transform.TransformPoint(center+new Vector3(0,-.014f,.1055f)));
        mouth.MouthRotation=Quaternion.Inverse(head.rotation)*holder.transform.rotation;
        Debug.Log("SALOON_SPEECH_PREPARED lowerFaceVertices="+changed);
    }

    static bool PrepareGrip(GameObject holder,Animator animator,SkinnedMeshRenderer face,Mesh mesh)
    {
        var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        var baked=new Mesh();face.BakeMesh(baked);var bakedVertices=baked.vertices;
        var weights=mesh.boneWeights;var deltas=new Vector3[mesh.vertexCount];int changed=0;
        var bind=mesh.bindposes;var skinning=face.bones.Select((bone,i)=>bone.localToWorldMatrix*bind[i]).ToArray();
        bool InHand(int index)=>face.bones[index]==hand||face.bones[index].IsChildOf(hand);
        for(int i=0;i<deltas.Length;i++)
        {
            var w=weights[i];
            float handWeight=(InHand(w.boneIndex0)?w.weight0:0)+(InHand(w.boneIndex1)?w.weight1:0)+(InHand(w.boneIndex2)?w.weight2:0)+(InHand(w.boneIndex3)?w.weight3:0);
            if(handWeight<.1f)continue;
            var world=face.transform.TransformPoint(bakedVertices[i]);var p=hand.InverseTransformPoint(world);var closed=p;
            if(p.y>.028f)
            {
                // The source has very broad mitten polygons. Fit their outer surface to
                // a compact fist instead of folding those polygons through finger joints.
                var radial=new Vector3((p.x+.004f)/.065f,(p.y-.085f)/.10f,(p.z+.035f)/.065f).normalized;
                closed=new Vector3(.008f,.060f,-.021f)+Vector3.Scale(radial,new Vector3(.035f,.043f,.038f));
            }
            float seam=Mathf.SmoothStep(0,1,(p.y-.012f)/.03f)*handWeight;
            var mixed=new Matrix4x4();
            for(int row=0;row<4;row++)for(int col=0;col<4;col++)
                mixed[row,col]=skinning[w.boneIndex0][row,col]*w.weight0+skinning[w.boneIndex1][row,col]*w.weight1+skinning[w.boneIndex2][row,col]*w.weight2+skinning[w.boneIndex3][row,col]*w.weight3;
            // Blend shapes are applied before skinning; undo the bind/pose transform.
            // Applying a posed-world delta directly would pull fingers into long spikes.
            deltas[i]=mixed.inverse.MultiplyVector(hand.TransformVector((closed-p)*seam));
            if(deltas[i].sqrMagnitude>1e-10f)changed++;
        }
        UnityEngine.Object.DestroyImmediate(baked);
        if(changed==0)return false;
        mesh.AddBlendShapeFrame(SaloonGunGrip.Shape,100,deltas,new Vector3[deltas.Length],new Vector3[deltas.Length]);
        Debug.Log("SALOON_GRIP_PREPARED mesh="+face.name+" handVertices="+changed);return true;
    }
}
