using UnityEngine;

namespace Saloon
{
    public sealed class SaloonSpeechMouth : MonoBehaviour
    {
        public const string Shape="SaloonTalk";
        public SkinnedMeshRenderer FaceRenderer;
        public Transform HeadBone;
        public Vector3 MouthPosition;
        public Quaternion MouthRotation=Quaternion.identity;
        [System.NonSerialized] public float PhaseOffset;
        GameObject opening;
        Mesh mouthMesh;
        Material material;
        int shape=-1;
        public float OpenAmount { get; private set; }
        public bool MouthVisible=>opening!=null&&opening.activeSelf;

        public void Initialize()
        {
            if(opening!=null)return;
            shape=FaceRenderer.sharedMesh.GetBlendShapeIndex(Shape);
            opening=new GameObject("Talking mouth");opening.transform.SetParent(HeadBone,false);
            opening.transform.localPosition=MouthPosition;opening.transform.localRotation=MouthRotation;
            // A tiny two-sided dark oval gives the low-poly painted face a visible opening.
            mouthMesh=new Mesh{name="Wonky mouth opening"};var vertices=new Vector3[13];var triangles=new int[72];
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI*2/12;vertices[i+1]=new Vector3(Mathf.Cos(a)*.5f,Mathf.Sin(a)*.5f,0);
                int next=(i+1)%12+1,at=i*6;
                triangles[at]=0;triangles[at+1]=i+1;triangles[at+2]=next;
                triangles[at+3]=0;triangles[at+4]=next;triangles[at+5]=i+1;
            }
            mouthMesh.vertices=vertices;mouthMesh.triangles=triangles;mouthMesh.RecalculateBounds();
            opening.AddComponent<MeshFilter>().sharedMesh=mouthMesh;
            material=new Material(Shader.Find("Unlit/Color")){name="Mouth interior",color=new Color(.045f,.012f,.009f)};
            var renderer=opening.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            Silence();
        }

        public void Tick(float speech,bool allowed,float delta)
        {
            if(opening==null)Initialize();
            if(!allowed||!(speech>0)||float.IsInfinity(speech)){Silence();return;}
            float time=Time.unscaledTime;
            float flap=.5f+.34f*Mathf.Sin(time*22+PhaseOffset)+.16f*Mathf.Sin(time*37+PhaseOffset*1.7f);
            float target=(.2f+.8f*Mathf.Clamp01(flap))*Mathf.Sqrt(Mathf.Clamp01(speech));
            OpenAmount=Mathf.MoveTowards(OpenAmount,target,Mathf.Min(delta,.1f)*(target>OpenAmount?18:12));
            if(shape>=0)FaceRenderer.SetBlendShapeWeight(shape,OpenAmount*100);
            opening.SetActive(OpenAmount>.015f);
            opening.transform.localScale=new Vector3(.055f*(1+.12f*Mathf.Sin(time*13+PhaseOffset)*OpenAmount),.042f*OpenAmount,.01f);
            // The lower edge drops more than the upper edge, like an uneven little jaw.
            opening.transform.localPosition=MouthPosition+MouthRotation*(Vector3.down*.008f*OpenAmount);
        }

        public void Silence()
        {
            OpenAmount=0;if(shape>=0)FaceRenderer.SetBlendShapeWeight(shape,0);
            if(opening!=null)opening.SetActive(false);
        }
        void OnDestroy(){if(opening!=null)Destroy(opening);if(mouthMesh!=null)Destroy(mouthMesh);if(material!=null)Destroy(material);}
    }
}
