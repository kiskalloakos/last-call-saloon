using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    // The host owns the call and result; every indoor client animates the same tabletop toss.
    public sealed class SaloonCoinTable : MonoBehaviour
    {
        public static readonly Vector3 Center=new Vector3(-5,SaloonTableFurniture.Surface,-3.8f);
        Transform furniture,board,coin;
        Material oak,rail,felt,brass;
        readonly List<Material> materials=new List<Material>();
        DuelView view;
        string shownA;
        int shownRound=-1;
        float snapshotAt,landAt=-100;
        Quaternion landFrom;
        public bool Visible=>board!=null&&board.gameObject.activeSelf;
        public float TossHeight=>coin==null?0:coin.localPosition.y;
        public string FaceUp=>coin==null?"":Vector3.Dot(coin.up,Vector3.up)>=0?"HEADS":"TAILS";

        public void Initialize(SaloonGame owner)
        {
            oak=Mat("Coin table walnut",new Color(.22f,.105f,.047f),.25f);
            rail=Mat("Coin table leather",new Color(.13f,.065f,.042f),.18f);
            felt=Mat("Coin table baize",new Color(.045f,.20f,.145f),0);
            brass=Mat("Coin table brass",new Color(.69f,.50f,.24f),.5f);
            furniture=SaloonTableFurniture.Build(1,"Coin Flip room table",oak,rail,felt,brass);
            board=new GameObject("Coin toss").transform;board.SetParent(transform,false);board.position=Center;
            BuildCoin();SaloonSmokingArea.SetLayer(coin,SaloonSmokingArea.IndoorLayer);
            board.gameObject.SetActive(false);
        }
        Material Mat(string name,Color color,float gloss)
        {var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",gloss);materials.Add(m);return m;}
        void BuildCoin()
        {
            coin=new GameObject("Gold coin — Heads and Tails").transform;coin.SetParent(board,false);coin.localPosition=Vector3.up*.012f;
            var gold=Mat("Coin gold rim",new Color(.76f,.49f,.12f),.7f);gold.SetFloat("_Metallic",.55f);
            var face=Mat("Coin engraved face",new Color(.9f,.67f,.24f),.45f);face.SetFloat("_Metallic",.35f);
            void Disk(string name,float y,float radius,float thickness,Material mat)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(coin,false);go.transform.localPosition=Vector3.up*y;go.transform.localScale=new Vector3(radius,thickness/2,radius);
                var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);go.GetComponent<Renderer>().sharedMaterial=mat;
            }
            Disk("Milled gold edge",0,.22f,.012f,gold);Disk("Heads inset",.0065f,.192f,.002f,face);Disk("Tails inset",-.0065f,.192f,.002f,face);
            foreach(bool heads in new[]{true,false})
            {
                var faceRoot=new GameObject(heads?"Heads face":"Tails face").transform;faceRoot.SetParent(coin,false);
                faceRoot.localRotation=Quaternion.Euler(heads?90:-90,0,0);faceRoot.localPosition=Vector3.up*(heads?.008f:-.008f);
                foreach(bool initial in new[]{true,false})
                {
                    var go=new GameObject(initial?"Engraved initial":"Engraved side");go.transform.SetParent(faceRoot,false);go.transform.localPosition=new Vector3(0,initial?.014f:-.038f,0);
                    var text=go.AddComponent<TextMesh>();text.text=initial?(heads?"H":"T"):(heads?"HEADS":"TAILS");text.fontSize=64;text.characterSize=initial?.016f:.0046f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.27f,.15f,.035f);SaloonWorldText.Attach(text,true);
                }
            }
        }
        public void Apply(Snapshot snapshot,PlayerView self)
        {
            var next=self?.room==1&&snapshot?.duel?.game=="coin"?snapshot.duel:snapshot?.coinTable;
            if(snapshot?.phase!=Phase.Turns||self==null||next?.game!="coin"){Clear();return;}
            bool fresh=shownRound!=next.coinRound||shownA!=next.coinAId;
            if(fresh){landAt=-100;coin.localRotation=Quaternion.identity;shownRound=next.coinRound;shownA=next.coinAId;}
            if(!string.IsNullOrEmpty(next.result)&&string.IsNullOrEmpty(view?.result)){landAt=Time.time;landFrom=coin.localRotation;}
            view=next;snapshotAt=Time.time;board.gameObject.SetActive(true);
            var a=snapshot.players.FirstOrDefault(p=>p.id==next.coinAId);var b=snapshot.players.FirstOrDefault(p=>p.id==next.coinBId);
            if(fresh&&a!=null&&b!=null)
            {
                var direction=new Vector3(b.x-a.x,0,b.z-a.z);if(direction.sqrMagnitude>.01f)board.rotation=Quaternion.LookRotation(direction);
            }
            UpdateToss();
        }
        void Update()
        {
            if(!Visible)return;UpdateToss();
        }
        void UpdateToss()
        {
            float elapsed=Time.time-snapshotAt;
            if(view.stage=="flipping")
            {
                float progress=Mathf.Clamp((view.coinElapsed+elapsed)/Mathf.Max(.1f,view.coinDuration),0,.97f);
                coin.localPosition=Vector3.up*(.012f+Mathf.Sin(progress*Mathf.PI)*.38f);
                coin.localRotation=Quaternion.Euler(0,progress*360,progress*1440);
            }
            else
            {
                coin.localPosition=Vector3.up*.012f;
                var finish=Quaternion.Euler(0,0,view.result=="TAILS"?180:0);
                coin.localRotation=!string.IsNullOrEmpty(view.result)?Quaternion.Slerp(landFrom,finish,Mathf.Clamp01((Time.time-landAt)/.12f)):Quaternion.identity;
            }
        }
        public void Clear()
        {if(board==null)return;board.gameObject.SetActive(false);view=null;shownRound=-1;shownA=null;}
        void OnDestroy()
        {foreach(var material in materials)if(material!=null)Destroy(material);if(board!=null)Destroy(board.gameObject);if(furniture!=null)Destroy(furniture.gameObject);}
    }
}
