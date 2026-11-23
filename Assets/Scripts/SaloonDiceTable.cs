using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine;

namespace Saloon
{
    // Presentation follows the host's roll; animation never chooses or exposes a hidden face.
    public sealed class SaloonDiceTable : MonoBehaviour
    {
        Transform board,sign,coinStencil,tableCover;
        readonly Transform[] cups=new Transform[2],dice=new Transform[6];
        readonly int[] faces=new int[6];
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        static readonly float[] impacts={.12f,.3f,.49f,.68f,.87f,1.3f,1.48f,1.67f,1.85f,2.06f};
        SaloonGame game;
        DuelView view;
        int shownRound=-1;
        string shownNear;
        float snapshotAt,rollElapsed,revealAt=-100,lastRollTime;
        bool own,revealed,paused;
        AudioSource clatter;
        AudioClip knock;
        public bool Visible=>board!=null&&board.gameObject.activeSelf;
        public int VisibleDice=>dice.Count(d=>d.gameObject.activeSelf);
        public bool Throwing=>Visible&&view?.stage=="rolling"&&RollTime<SaloonGame.DiceRollSeconds;
        public float ThrowHeight=>VisibleDice==0?0:dice.Where(d=>d.gameObject.activeSelf).Max(d=>d.localPosition.y);
        float RollTime=>rollElapsed+(paused?0:Time.time-snapshotAt);

        public void Initialize()
        {
            game=GetComponent<SaloonGame>();
            board=new GameObject("Bluff Dice cups and dice").transform;board.SetParent(transform,false);board.position=SaloonCoinTable.Center;
            Material ivory=Mat("Dice aged ivory",new Color(.94f,.86f,.68f)),ink=Mat("Dice recessed pips",new Color(.065f,.036f,.022f));
            Material leather=Mat("Dice oxblood leather",new Color(.19f,.046f,.026f)),brass=Mat("Dice antique brass",new Color(.63f,.40f,.15f));
            brass.SetFloat("_Metallic",.65f);brass.SetFloat("_Glossiness",.38f);
            tableCover=Primitive(PrimitiveType.Cube,"Oxblood leather playing surface",board,Vector3.zero,new Vector3(1.32f,.004f,1.63f),Mat("Dice table oxblood",new Color(.085f,.035f,.025f)));
            Primitive(PrimitiveType.Cylinder,"Brass rolling tray edge",board,new Vector3(0,.005f,0),new Vector3(1.18f,.005f,1.36f),brass);
            Primitive(PrimitiveType.Cylinder,"Dark green rolling baize",board,new Vector3(0,.011f,0),new Vector3(1.15f,.004f,1.33f),Mat("Dice deep green felt",new Color(.025f,.105f,.074f)));
            Mesh rounded=RoundedDie();meshes.Add(rounded);
            for(int side=0;side<2;side++)
            {
                cups[side]=new GameObject("Leather dice shaker").transform;cups[side].SetParent(board,false);
                Mesh cup=CupMesh();meshes.Add(cup);MeshPart("Tapered leather cup",cups[side],cup,leather);
                foreach(float y in new[]{.012f,.04f,.265f})
                {
                    Mesh band=Band(y);meshes.Add(band);MeshPart("Brass cup binding",cups[side],band,brass);
                }
                for(int i=0;i<3;i++)
                {
                    int index=side*3+i;
                    dice[index]=new GameObject("Thrown ivory die "+(index+1)).transform;dice[index].SetParent(board,false);dice[index].localScale=Vector3.one*.135f;
                    MeshPart("Rounded ivory",dice[index],rounded,ivory);
                    // All six sides are modeled so tumbling reads as a real die. Opposites total seven.
                    for(int face=1;face<=6;face++)
                    {
                        var root=new GameObject("Face "+face).transform;root.SetParent(dice[index],false);root.localRotation=FaceRotation(face);
                        var positions=new[]{new Vector2(-.25f,-.25f),new Vector2(.25f,.25f),new Vector2(-.25f,.25f),new Vector2(.25f,-.25f),Vector2.zero,new Vector2(-.25f,0),new Vector2(.25f,0)};
                        for(int pip=0;pip<7;pip++)
                        {
                            if(!(pip<2&&face>=2||pip>=2&&pip<4&&face>=4||pip==4&&face%2==1||pip>=5&&face==6))continue;
                            Primitive(PrimitiveType.Sphere,"Inlaid pip",root,new Vector3(positions[pip].x,.498f,positions[pip].y),new Vector3(.145f,.025f,.145f),ink);
                        }
                    }
                }
            }
            clatter=board.gameObject.AddComponent<AudioSource>();clatter.playOnAwake=false;clatter.spatialBlend=1;clatter.minDistance=.6f;clatter.maxDistance=5;clatter.rolloffMode=AudioRolloffMode.Linear;
            knock=Knock();
            SaloonSmokingArea.SetLayer(board,SaloonSmokingArea.IndoorLayer);board.gameObject.SetActive(false);
            var stencilRoot=GameObject.Find(SaloonRoomSigns.RootName);
            if(stencilRoot!=null)coinStencil=stencilRoot.transform.Cast<Transform>().FirstOrDefault(t=>t.name.StartsWith("Coin Flip"));
            sign=GameObject.CreatePrimitive(PrimitiveType.Quad).transform;sign.name="Bluff Dice wall stencil";sign.SetParent(transform,false);
            sign.position=new Vector3(-5,2.37f,SaloonRoomWindows.FrontZ+.096f);sign.rotation=Quaternion.Euler(0,180,0);sign.localScale=new Vector3(3.1f,2.81f,1);
            var collider=sign.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            var paint=Mat("Bluff Dice wall ink",new Color(.43f,.25f,.085f));paint.SetFloat("_Glossiness",0);
            paint.mainTexture=Resources.Load<Texture2D>("Art/RoomStencils/BluffDice");paint.SetFloat("_Mode",1);paint.SetFloat("_Cutoff",.35f);
            paint.EnableKeyword("_ALPHATEST_ON");paint.SetOverrideTag("RenderType","TransparentCutout");paint.renderQueue=(int)RenderQueue.AlphaTest;
            var renderer=sign.GetComponent<MeshRenderer>();renderer.sharedMaterial=paint;renderer.shadowCastingMode=ShadowCastingMode.Off;
            SaloonSmokingArea.SetLayer(sign,SaloonSmokingArea.IndoorLayer);sign.gameObject.SetActive(false);
        }
        Material Mat(string name,Color color)
        {var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",.18f);materials.Add(m);return m;}
        static Transform Primitive(PrimitiveType shape,string name,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        static void MeshPart(string name,Transform parent,Mesh mesh,Material material)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        static Quaternion FaceRotation(int face)
        {return Quaternion.Euler(face==6?180:face==2?90:face==5?-90:0,0,face==3?90:face==4?-90:0);}
        static Mesh RoundedDie()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();float[] grid={-.5f,-.4f,.4f,.5f};
            for(int face=1;face<=6;face++)
            {
                int offset=vertices.Count;var rotation=FaceRotation(face);
                for(int z=0;z<4;z++)for(int x=0;x<4;x++)
                {
                    var p=new Vector3(grid[x],.5f,grid[z]);var core=new Vector3(Mathf.Clamp(p.x,-.4f,.4f),.4f,Mathf.Clamp(p.z,-.4f,.4f));
                    vertices.Add(rotation*(core+(p-core).normalized*.1f));
                }
                for(int z=0;z<3;z++)for(int x=0;x<3;x++)
                {int a=offset+z*4+x;triangles.AddRange(new[]{a,a+4,a+1,a+1,a+4,a+5});}
            }
            var mesh=new Mesh{name="Soft beveled dice"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }
        static Mesh CupMesh()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            // Outer wall, closed top and inner cavity. The open mouth faces the baize.
            float[] heights={0,.29f,.29f,.265f,.018f,0};float[] radii={.18f,.137f,0,.119f,.16f,.18f};
            for(int ring=0;ring<heights.Length;ring++)for(int i=0;i<=32;i++)
            {float angle=i*Mathf.PI/16;vertices.Add(new Vector3(Mathf.Cos(angle)*radii[ring],heights[ring],Mathf.Sin(angle)*radii[ring]));}
            for(int ring=0;ring<heights.Length-1;ring++)for(int i=0;i<32;i++)
            {int a=ring*33+i;triangles.AddRange(new[]{a,a+33,a+1,a+1,a+33,a+34});}
            var mesh=new Mesh{name="Hollow leather shaker"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }
        static Mesh Band(float y)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<=32;i++)for(int j=0;j<2;j++)
            {float h=y+j*.009f,r=Mathf.Lerp(.18f,.137f,h/.29f)+.001f,a=i*Mathf.PI/16;vertices.Add(new Vector3(Mathf.Cos(a)*r,h,Mathf.Sin(a)*r));}
            for(int i=0;i<32;i++){int a=i*2;triangles.AddRange(new[]{a,a+1,a+2,a+2,a+1,a+3});}
            var mesh=new Mesh{name="Cup brass binding"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }
        static AudioClip Knock()
        {
            const int rate=22050;var samples=new float[2205];var random=new System.Random(31);float previous=0;
            for(int i=0;i<samples.Length;i++)
            {float t=i/(float)rate,noise=(float)random.NextDouble()*2-1;previous=Mathf.Lerp(previous,noise,.6f);samples[i]=(previous*.65f+Mathf.Sin(t*1900)*.35f)*Mathf.Exp(-t*65)*.65f;}
            var clip=AudioClip.Create("Dice on baize",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        public void Apply(Snapshot snapshot,PlayerView self)
        {
            bool diceRound=snapshot?.coinRoomGame=="bluff";
            sign.gameObject.SetActive(diceRound);if(coinStencil!=null)coinStencil.gameObject.SetActive(!diceRound);
            if(snapshot?.phase==Phase.Aftermath&&self!=null&&Visible){rollElapsed=SaloonGame.DiceRollSeconds;paused=true;Animate();return;}
            var next=self?.room==1&&snapshot?.duel?.game=="bluff"?snapshot.duel:snapshot?.diceTable;
            if(snapshot?.phase!=Phase.Turns||self==null||next?.game!="bluff"){Clear();return;}
            own=snapshot.duel?.game=="bluff"&&self.room==1;
            string nearId=own?self.id:next.coinAId,farId=nearId==next.coinAId?next.coinBId:next.coinAId;
            bool fresh=shownRound!=snapshot.round||shownNear!=nearId;
            if(fresh)
            {
                shownRound=snapshot.round;shownNear=nearId;revealAt=-100;lastRollTime=0;
                var a=snapshot.players.FirstOrDefault(p=>p.id==nearId);var b=snapshot.players.FirstOrDefault(p=>p.id==farId);
                if(a!=null&&b!=null){var direction=new Vector3(b.x-a.x,0,b.z-a.z);if(direction.sqrMagnitude>.01f)board.rotation=Quaternion.LookRotation(direction);}
                tableCover.rotation=Quaternion.identity;
            }
            bool opening=next.stage=="result"||next.stage=="playing";
            if(opening&&!revealed&&!fresh)revealAt=Time.time;
            revealed=opening;view=next;snapshotAt=Time.time;paused=game.DicePracticeHelp||next.stage=="waiting";
            rollElapsed=next.stage=="waiting"?0:next.stage=="rolling"?Mathf.Clamp(SaloonGame.DiceRollSeconds-next.remaining,0,SaloonGame.DiceRollSeconds):SaloonGame.DiceRollSeconds;
            var near=own?next.yourDice:next.revealedDiceA;var far=own?next.opponentDice:next.revealedDiceB;
            for(int side=0;side<2;side++)
            {
                bool speaker=next.bluffSpeakerId==(side==0?nearId:farId);
                cups[side].gameObject.SetActive(speaker);
                var values=side==0?near:far;
                for(int i=0;i<3;i++){int index=side*3+i;faces[index]=speaker&&values?.Length==3?values[i]:0;dice[index].gameObject.SetActive(faces[index]>=1&&faces[index]<=6);}
            }
            board.gameObject.SetActive(true);Animate();
        }
        void Update(){if(Visible)Animate();}
        void Animate()
        {
            float time=RollTime;
            if(!paused&&view.stage=="rolling")
            {
                foreach(float impact in impacts)if(lastRollTime<impact&&time>=impact&&time-impact<.2f)
                {clatter.pitch=.86f+impact*.13f;clatter.PlayOneShot(knock,impact<1?.32f:.65f);}
            }
            lastRollTime=time;
            for(int side=0;side<2;side++)
            {
                float z=side==0?-.24f:.24f;
                bool open=revealed||side==0&&own&&view.bluffIsSpeaker;
                float lift=revealed?Mathf.SmoothStep(0,1,(Time.time-revealAt)/.65f):Mathf.SmoothStep(0,1,(time-1)/.45f);
                cups[side].localPosition=new Vector3(open?-.43f*lift:0,.024f+(open?Mathf.Sin(lift*Mathf.PI)*.32f:0),z);
                cups[side].localRotation=Quaternion.Euler(0,0,open?-18*lift:0);
                if(time<1.05f&&!paused)
                {
                    cups[side].localPosition+=new Vector3(Mathf.Sin(time*36)*.045f,.10f+Mathf.Abs(Mathf.Sin(time*18))*.06f,Mathf.Cos(time*32)*.025f);
                    cups[side].localRotation=Quaternion.Euler(Mathf.Sin(time*30)*14,Mathf.Sin(time*24)*12,Mathf.Cos(time*30)*18);
                }
                for(int i=0;i<3;i++)
                {
                    int index=side*3+i;if(faces[index]<1)continue;
                    float t=Mathf.Clamp01((time-1.05f-i*.055f)/1.05f),ease=1-Mathf.Pow(1-t,3);
                    var end=new Vector3((i-1)*.19f+.08f,.088f,z+(i==1?.045f:-.035f));
                    float bounce=Mathf.Abs(Mathf.Sin(t*Mathf.PI*3))*(1-t)*.28f;
                    dice[index].localPosition=Vector3.Lerp(new Vector3(-.06f,.12f,z+.04f),end,ease)+Vector3.up*bounce;
                    var finish=Quaternion.Euler(0,i*23-18,0)*Quaternion.Inverse(FaceRotation(faces[index]));
                    dice[index].localRotation=finish*Quaternion.Euler((1-t)*720,(1-t)*(i+1)*180,(1-t)*540);
                    // The authoritative top face becomes legible only after the throw settles.
                    dice[index].gameObject.SetActive(time>=1.05f);
                }
            }
        }
        public void Clear()
        {if(board!=null)board.gameObject.SetActive(false);shownRound=-1;shownNear=null;view=null;revealed=false;if(clatter!=null)clatter.Stop();}
        void OnDestroy()
        {
            if(coinStencil!=null)coinStencil.gameObject.SetActive(true);
            foreach(var m in materials)if(m!=null)Destroy(m);foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
            if(knock!=null)Destroy(knock);if(board!=null)Destroy(board.gameObject);if(sign!=null)Destroy(sign.gameObject);
        }
    }
}
