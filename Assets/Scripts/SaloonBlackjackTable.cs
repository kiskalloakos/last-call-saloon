using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public sealed class SaloonBlackjackTarget : MonoBehaviour
    {
        public string action;
    }

    // Client presentation only. Card IDs and legal actions come from the private host snapshot.
    public sealed class SaloonBlackjackTable : MonoBehaviour
    {
        public const int InteractionLayer=11;
        public static readonly Vector3 Center=new Vector3(5,.78f,-3.8f);
        SaloonGame game;
        SaloonCardArt art;
        Transform furniture,board;
        TextMesh playerLabel,dealerLabel,status;
        GameObject hit,stand,deal,deck;
        Material oak,rail,felt,brass,enabledButton,disabledButton,hoverButton;
        readonly List<Material> materials=new List<Material>();
        readonly List<Card> cards=new List<Card>();
        AudioSource audioSource;AudioClip slide;
        DuelView view;int shownHand=-1;string shownPlayer;
        GameObject hovered;
        public string PointerHint { get; private set; }="";
        public int CardCount=>cards.Count;
        public int HiddenCount=>cards.Count(c=>c.id<0);
        public bool Visible=>board!=null&&board.gameObject.activeSelf;
        class Card
        {
            public Transform transform;
            public Renderer renderer;
            public int id,index;
            public bool dealer,sounded;
            public Vector3 destination;
            public float born,flipAt=-100;
        }
        public void Initialize(SaloonGame owner)
        {
            game=owner;art=new SaloonCardArt();
            oak=Mat("Blackjack walnut",new Color(.22f,.105f,.047f),.25f);
            rail=Mat("Blackjack leather rail",new Color(.13f,.065f,.042f),.18f);
            felt=Mat("Blackjack green baize",new Color(.045f,.20f,.145f),0);
            brass=Mat("Blackjack brass inlay",new Color(.69f,.50f,.24f),.5f);
            enabledButton=Mat("Blackjack active control",new Color(.23f,.34f,.23f),.1f);
            disabledButton=Mat("Blackjack inactive control",new Color(.09f,.15f,.12f),.1f);
            hoverButton=Mat("Blackjack selected control",new Color(.54f,.40f,.17f),.1f);
            BuildFurniture();
            board=new GameObject("Blackjack cards and table controls").transform;board.SetParent(transform,false);board.position=Center;
            playerLabel=Label("Player hand",new Vector3(0,.008f,-.63f),.008f,0);
            dealerLabel=Label("Dealer hand",new Vector3(0,.008f,.63f),.008f,180);
            status=Label("Hand status",new Vector3(-.10f,.009f,0),.008f,0);
            hit=Button("HIT",new Vector3(-.23f,.012f,-.49f),"hit",0);
            stand=Button("STAND",new Vector3(.23f,.012f,-.49f),"stand",0);
            deal=Button("DEAL",new Vector3(0,.012f,.49f),"deal",180);
            deck=Box("Draw pile",board,new Vector3(.62f,.025f,0),new Vector3(.20f,.05f,.29f),art.Back);
            var target=deck.AddComponent<SaloonBlackjackTarget>();target.action="hit";
            var collider=deck.AddComponent<BoxCollider>();collider.isTrigger=true;deck.layer=InteractionLayer;
            for(int i=0;i<4;i++)Box("Deck paper edge",board,new Vector3(.62f,.008f+i*.009f,0),new Vector3(.202f,.001f,.292f),art.Edge);
            var sound=new GameObject("Card sounds");sound.transform.SetParent(board,false);audioSource=sound.AddComponent<AudioSource>();audioSource.spatialBlend=.85f;audioSource.minDistance=1;audioSource.maxDistance=5;
            slide=CardSound();board.gameObject.SetActive(false);
        }
        Material Mat(string name,Color color,float gloss)
        {var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",gloss);materials.Add(m);return m;}
        static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material mat)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
            // Use the built-in cube mesh without retaining a physics collider on decoration.
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);primitive.SetActive(false);
            go.AddComponent<MeshFilter>().sharedMesh=primitive.GetComponent<MeshFilter>().sharedMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=mat;Destroy(primitive);return go;
        }
        void BuildFurniture()
        {
            furniture=SaloonTableFurniture.Build(2,"Blackjack room table",oak,rail,felt,brass);
            CenterBoardHeight=SaloonTableFurniture.Surface;
        }
        float CenterBoardHeight;
        TextMesh Label(string text,Vector3 position,float size,float yaw)
        {
            var go=new GameObject(text);go.transform.SetParent(board,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(90,yaw,0);
            var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=64;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.88f,.77f,.51f);label.richText=false;SaloonWorldText.Attach(label);return label;
        }
        GameObject Button(string name,Vector3 position,string action,float yaw)
        {
            var go=Box(name+" felt control",board,position,new Vector3(.36f,.012f,.095f),disabledButton);
            var collider=go.AddComponent<BoxCollider>();collider.isTrigger=true;go.layer=InteractionLayer;
            go.AddComponent<SaloonBlackjackTarget>().action=action;
            Label(name,position+Vector3.up*.008f,.009f,yaw);return go;
        }
        public void Apply(Snapshot snapshot,PlayerView self)
        {
            var next=snapshot?.duel?.game=="blackjack"?snapshot.duel:snapshot?.blackjackTable;
            if(snapshot?.phase!=Phase.Turns||self==null||next?.game!="blackjack")
            {ClearHand();return;}
            bool newHand=next.handNumber!=shownHand||shownPlayer!=next.playerId;
            if(newHand){ClearCards();shownHand=next.handNumber;shownPlayer=next.playerId;}
            view=next;board.gameObject.SetActive(true);
            var player=snapshot.players.FirstOrDefault(p=>p.id==next.playerId);var dealer=snapshot.players.FirstOrDefault(p=>p.id==next.dealerId);
            if(newHand&&player!=null&&dealer!=null)
            {
                var direction=new Vector3(dealer.x-player.x,0,dealer.z-player.z);
                if(direction.sqrMagnitude>.01f)board.rotation=Quaternion.LookRotation(direction);
            }
            board.position=new Vector3(Center.x,CenterBoardHeight,Center.z);
            SyncCards(next.playerHand,false,newHand);SyncCards(next.dealerHand,true,newHand);
            playerLabel.text=Short(next.playerName)+"  /  PLAYER  /  "+next.playerTotal;
            dealerLabel.text=Short(next.dealerName)+"  /  DEALER  /  "+(next.dealerTotal<0?"?":next.dealerTotal.ToString());
            status.transform.localRotation=Quaternion.Euler(90,next.isDealer?180:0,0);
            status.text=next.stage=="redealing"?"PUSH\nDEAL AGAIN":next.stage=="playing"?Short(next.firstName)+" OPENS":
                next.canHit?"YOUR HAND\n"+Mathf.CeilToInt(next.remaining)+"s":next.canDeal?"YOUR DEAL\nDRAW TO 17":next.isDealer&&next.dealerTotal<0?"PLAYER'S HAND":next.dealerTotal<0?"DEALING":"DEALER'S HAND";
            SetButton(hit,next.canHit);SetButton(stand,next.canHit);SetButton(deal,next.canDeal);
            deck.GetComponent<SaloonBlackjackTarget>().action=next.canDeal?"deal":"hit";
            deck.GetComponent<Collider>().enabled=next.canHit||next.canDeal;
        }
        static string Short(string name)=>string.IsNullOrEmpty(name)?"":name.Length>16?name.Substring(0,16):name;
        void SetButton(GameObject button,bool active)
        {button.GetComponent<Collider>().enabled=active;button.GetComponent<Renderer>().sharedMaterial=active?enabledButton:disabledButton;}
        void SyncCards(int[] hand,bool dealer,bool initial)
        {
            if(hand==null)return;
            for(int i=0;i<hand.Length;i++)
            {
                var card=cards.FirstOrDefault(c=>c.dealer==dealer&&c.index==i);
                if(card==null)
                {
                    var go=new GameObject(dealer?"Dealer card "+(i+1):"Player card "+(i+1));go.transform.SetParent(board,false);
                    go.AddComponent<MeshFilter>().sharedMesh=art.Mesh;
                    card=new Card{transform=go.transform,renderer=go.AddComponent<MeshRenderer>(),id=hand[i],index=i,dealer=dealer,born=Time.time+(initial?(i*2+(dealer?1:0))*.22f:0)};
                    cards.Add(card);card.renderer.sharedMaterials=new[]{art.Face(card.id),art.Back,art.Edge};
                }
                else if(card.id!=hand[i])
                {bool flip=card.id<0;card.id=hand[i];card.renderer.sharedMaterials=new[]{art.Face(card.id),art.Back,art.Edge};if(flip)card.flipAt=Time.time;}
                float spacing=Mathf.Min(.23f,1.03f/Mathf.Max(1,hand.Length-1));
                card.destination=new Vector3((i-(hand.Length-1)*.5f)*spacing,.008f+i*.0014f,dealer?.235f:-.235f);
            }
        }
        void Update()
        {
            if(!Visible)return;
            foreach(var card in cards)
            {
                float t=Mathf.Clamp01((Time.time-card.born)/.42f),ease=1-Mathf.Pow(1-t,3);
                card.transform.localPosition=Vector3.Lerp(new Vector3(.62f,.065f,0),card.destination,ease)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.075f);
                float roll=card.id<0?180:card.flipAt>Time.time-.45f?Mathf.Lerp(180,0,Mathf.SmoothStep(0,1,(Time.time-card.flipAt)/.45f)):0;
                card.transform.localRotation=Quaternion.Euler(0,card.dealer?180:0,roll);
                card.renderer.enabled=Time.time>=card.born;
                if(!card.sounded&&t>.02f){card.sounded=true;audioSource.PlayOneShot(slide,.22f);}
            }
            if(game.MenuOpen||game.ShowRole||!game.PlayingBlackjack)ClearHover();
        }
        public string ActionForRay(Ray ray)
        {
            if(!Visible||view==null)return null;
            if(!Physics.Raycast(ray,out var contact,3,1<<InteractionLayer,QueryTriggerInteraction.Collide))return null;
            var target=contact.collider.GetComponent<SaloonBlackjackTarget>();
            if(target==null)return null;
            return target.action=="deal"?(view.canDeal?"deal":null):(view.canHit?target.action:null);
        }
        public void HandlePointer()
        {
            ClearHover();
            if(Cursor.lockState==CursorLockMode.Locked){PointerHint="Release right mouse to play the cards";return;}
            Ray ray=game.ViewCamera.ScreenPointToRay(Input.mousePosition);string action=ActionForRay(ray);
            PointerHint=action=="hit"?"Click · Hit — deal another card":action=="stand"?"Click · Stand — keep this hand":action=="deal"?"Click · Deal your next card":view?.canHit==true?"Click HIT or the deck / STAND · Hold right mouse to look":view?.canDeal==true?"Click the deck to deal · Draw to 17":"Watch the cards · Hold right mouse to look";
            if(action==null)return;
            if(Physics.Raycast(ray,out var contact,3,1<<InteractionLayer,QueryTriggerInteraction.Collide))
            {
                hovered=contact.collider.gameObject;
                if(hovered!=deck)hovered.GetComponent<Renderer>().sharedMaterial=hoverButton;
            }
            if(Input.GetMouseButtonDown(0))game.ChooseBlackjack(action);
        }
        void ClearHover()
        {
            if(hovered!=null&&hovered!=deck)hovered.GetComponent<Renderer>().sharedMaterial=hovered.GetComponent<Collider>().enabled?enabledButton:disabledButton;
            hovered=null;PointerHint="";
        }
        public void ClearHand(){if(board==null)return;ClearHover();ClearCards();board.gameObject.SetActive(false);view=null;shownHand=-1;shownPlayer=null;}
        void ClearCards(){foreach(var card in cards)if(card.transform!=null){card.transform.gameObject.SetActive(false);Destroy(card.transform.gameObject);}cards.Clear();}
        static AudioClip CardSound()
        {
            const int rate=22050;var random=new System.Random(72);var samples=new float[2205];float previous=0;
            for(int i=0;i<samples.Length;i++){float noise=(float)random.NextDouble()*2-1;samples[i]=(noise-previous)*Mathf.Sin(i/(float)samples.Length*Mathf.PI)*.16f;previous=noise;}
            var clip=AudioClip.Create("Card slides on felt",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void OnDestroy(){art?.Dispose();foreach(var mat in materials)if(mat!=null)Destroy(mat);if(slide!=null)Destroy(slide);if(board!=null)Destroy(board.gameObject);if(furniture!=null)Destroy(furniture.gameObject);}
    }
}
