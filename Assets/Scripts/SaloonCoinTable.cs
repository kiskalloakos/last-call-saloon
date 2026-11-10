using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public sealed class SaloonCoinTarget : MonoBehaviour
    {
        public int side;
        public string participantId;
    }

    // The host owns the call and result; every indoor client animates the same tabletop toss.
    public sealed class SaloonCoinTable : MonoBehaviour
    {
        public static readonly Vector3 Center=new Vector3(-5,SaloonTableFurniture.Surface,-3.8f);
        const int InteractionLayer=SaloonBlackjackTable.InteractionLayer;
        SaloonGame game;
        Transform furniture,board,coin;
        TextMesh aLabel,bLabel,status;
        Material oak,rail,felt,brass,active,inactive,hover;
        readonly List<Material> materials=new List<Material>();
        readonly List<GameObject> buttons=new List<GameObject>();
        DuelView view;
        string localId,shownA;
        int shownRound=-1;
        float snapshotAt,landAt=-100;
        Quaternion landFrom;
        GameObject hovered;
        public string PointerHint { get; private set; }="";
        public bool Visible=>board!=null&&board.gameObject.activeSelf;
        public float TossHeight=>coin==null?0:coin.localPosition.y;
        public string FaceUp=>coin==null?"":Vector3.Dot(coin.up,Vector3.up)>=0?"HEADS":"TAILS";

        public void Initialize(SaloonGame owner)
        {
            game=owner;
            oak=Mat("Coin table walnut",new Color(.22f,.105f,.047f),.25f);
            rail=Mat("Coin table leather",new Color(.13f,.065f,.042f),.18f);
            felt=Mat("Coin table baize",new Color(.045f,.20f,.145f),0);
            brass=Mat("Coin table brass",new Color(.69f,.50f,.24f),.5f);
            active=Mat("Coin call available",new Color(.23f,.34f,.23f),.1f);
            inactive=Mat("Coin call closed",new Color(.09f,.15f,.12f),.1f);
            hover=Mat("Coin call hover",new Color(.54f,.40f,.17f),.1f);
            furniture=SaloonTableFurniture.Build(1,"Coin Flip room table",oak,rail,felt,brass);
            board=new GameObject("Coin toss and table controls").transform;board.SetParent(transform,false);board.position=Center;
            aLabel=Label(board,"First seat",new Vector3(0,.008f,-.67f),.007f,0);
            bLabel=Label(board,"Second seat",new Vector3(0,.008f,.67f),.007f,180);
            status=Label(board,"Call the coin",new Vector3(0,.009f,.24f),.008f,0);
            foreach(int seat in new[]{0,1})foreach(int side in new[]{0,1})
            {
                float sign=seat==0?1:-1;
                var position=new Vector3((side==0?-.23f:.23f)*sign,.012f,-.49f*sign);
                var button=SaloonTableFurniture.Box((side==0?"HEADS":"TAILS")+" felt control",board,position,new Vector3(.36f,.012f,.11f),inactive);
                button.layer=InteractionLayer;button.AddComponent<BoxCollider>().isTrigger=true;
                button.AddComponent<SaloonCoinTarget>().side=side;buttons.Add(button);
                Label(board,side==0?"HEADS":"TAILS",position+Vector3.up*.008f,.009f,seat==0?0:180);
            }
            BuildCoin();SaloonSmokingArea.SetLayer(coin,SaloonSmokingArea.IndoorLayer);
            board.gameObject.SetActive(false);
        }
        Material Mat(string name,Color color,float gloss)
        {var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",gloss);materials.Add(m);return m;}
        static TextMesh Label(Transform parent,string text,Vector3 position,float size,float yaw)
        {
            var go=new GameObject(text);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(90,yaw,0);
            var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=64;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.88f,.77f,.51f);label.richText=false;SaloonWorldText.Attach(label);return label;
        }
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
            if(next.stage=="playing"&&view?.stage!="playing"){landAt=Time.time;landFrom=coin.localRotation;}
            view=next;localId=self.id;snapshotAt=Time.time;board.gameObject.SetActive(true);
            var a=snapshot.players.FirstOrDefault(p=>p.id==next.coinAId);var b=snapshot.players.FirstOrDefault(p=>p.id==next.coinBId);
            if(fresh&&a!=null&&b!=null)
            {
                var direction=new Vector3(b.x-a.x,0,b.z-a.z);if(direction.sqrMagnitude>.01f)board.rotation=Quaternion.LookRotation(direction);
            }
            string Seat(string id,string name)=>Short(name)+"  /  "+(next.stage=="choosing"?"CALL YOUR SIDE":id==next.headsId?"HEADS":"TAILS");
            aLabel.text=Seat(next.coinAId,a?.name);bLabel.text=Seat(next.coinBId,b?.name);
            bool fromB=self.id==next.coinBId;
            status.transform.localRotation=Quaternion.Euler(90,fromB?180:0,0);status.transform.localPosition=new Vector3(0,.009f,fromB?-.24f:.24f);
            for(int i=0;i<buttons.Count;i++)
            {
                var target=buttons[i].GetComponent<SaloonCoinTarget>();target.participantId=i<2?next.coinAId:next.coinBId;
                bool enabled=next.canChooseCoin&&target.participantId==localId;
                buttons[i].GetComponent<Collider>().enabled=enabled;buttons[i].GetComponent<Renderer>().sharedMaterial=enabled?active:inactive;
            }
            UpdateToss();
        }
        static string Short(string name)=>string.IsNullOrEmpty(name)?"":name.Length>16?name.Substring(0,16):name;
        void Update()
        {
            if(!Visible)return;UpdateToss();
            if(game.MenuOpen||game.ShowRole||!game.PlayingCoin)ClearHover();
        }
        void UpdateToss()
        {
            float elapsed=Time.time-snapshotAt;
            if(view.stage=="flipping")
            {
                float progress=Mathf.Clamp((view.coinElapsed+elapsed)/Mathf.Max(.1f,view.coinDuration),0,.97f);
                coin.localPosition=Vector3.up*(.012f+Mathf.Sin(progress*Mathf.PI)*.38f);
                coin.localRotation=Quaternion.Euler(0,progress*360,progress*1440);
                status.text="COIN IN THE AIR";
            }
            else
            {
                coin.localPosition=Vector3.up*.012f;
                var finish=Quaternion.Euler(0,0,view.result=="TAILS"?180:0);
                coin.localRotation=view.stage=="playing"?Quaternion.Slerp(landFrom,finish,Mathf.Clamp01((Time.time-landAt)/.12f)):Quaternion.identity;
                status.text=view.stage=="playing"?view.result+"\n"+Short(view.firstName)+" OPENS":"CALL IT\n"+Mathf.CeilToInt(Mathf.Max(0,view.remaining-elapsed))+"s";
            }
        }
        public int SideForRay(Ray ray)
        {
            if(!Visible||view?.canChooseCoin!=true||!Physics.Raycast(ray,out var hit,3,1<<InteractionLayer,QueryTriggerInteraction.Collide))return -1;
            var target=hit.collider.GetComponent<SaloonCoinTarget>();return target!=null&&target.participantId==localId?target.side:-1;
        }
        public void HandlePointer()
        {
            ClearHover();
            if(Cursor.lockState==CursorLockMode.Locked){PointerHint="Release right mouse to call the coin";return;}
            var ray=game.ViewCamera.ScreenPointToRay(Input.mousePosition);int side=SideForRay(ray);
            PointerHint=side>=0?"Click · Call "+(side==0?"Heads":"Tails"):view?.canChooseCoin==true?"Click HEADS or TAILS on the table · Hold right mouse to look":"Watch the coin · Hold right mouse to look";
            if(side<0)return;
            if(Physics.Raycast(ray,out var hit,3,1<<InteractionLayer,QueryTriggerInteraction.Collide)){hovered=hit.collider.gameObject;hovered.GetComponent<Renderer>().sharedMaterial=hover;}
            if(Input.GetMouseButtonDown(0))game.ChooseCoin(side);
        }
        void ClearHover()
        {
            if(hovered!=null)hovered.GetComponent<Renderer>().sharedMaterial=hovered.GetComponent<Collider>().enabled?active:inactive;
            hovered=null;PointerHint="";
        }
        public void Clear()
        {if(board==null)return;ClearHover();board.gameObject.SetActive(false);view=null;shownRound=-1;shownA=null;}
        void OnDestroy()
        {foreach(var material in materials)if(material!=null)Destroy(material);if(board!=null)Destroy(board.gameObject);if(furniture!=null)Destroy(furniture.gameObject);}
    }
}
