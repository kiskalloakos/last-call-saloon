using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    // Client presentation only. Card IDs and legal actions come from the private host snapshot.
    public sealed class SaloonBlackjackTable : MonoBehaviour
    {
        public const int InteractionLayer=11;
        public static readonly Vector3 Center=new Vector3(5,.78f,-3.8f);
        SaloonCardArt art;
        Transform furniture,board;
        GameObject deck;
        Material oak,rail,felt,brass;
        readonly List<Material> materials=new List<Material>();
        readonly List<Card> cards=new List<Card>();
        AudioSource audioSource;AudioClip slide;
        int shownHand=-1;string shownPlayer;
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
            art=new SaloonCardArt();
            oak=Mat("Blackjack walnut",new Color(.22f,.105f,.047f),.25f);
            rail=Mat("Blackjack leather rail",new Color(.13f,.065f,.042f),.18f);
            felt=Mat("Blackjack green baize",new Color(.045f,.20f,.145f),0);
            brass=Mat("Blackjack brass inlay",new Color(.69f,.50f,.24f),.5f);
            BuildFurniture();
            board=new GameObject("Blackjack cards").transform;board.SetParent(transform,false);board.position=Center;
            deck=Box("Draw pile",board,new Vector3(.62f,.025f,0),new Vector3(.20f,.05f,.29f),art.Back);
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
        public void Apply(Snapshot snapshot,PlayerView self)
        {
            var next=snapshot?.duel?.game=="blackjack"?snapshot.duel:snapshot?.blackjackTable;
            if(snapshot?.phase!=Phase.Turns||self==null||next?.game!="blackjack")
            {ClearHand();return;}
            bool newHand=next.handNumber!=shownHand||shownPlayer!=next.playerId;
            if(newHand){ClearCards();shownHand=next.handNumber;shownPlayer=next.playerId;}
            board.gameObject.SetActive(true);
            var player=snapshot.players.FirstOrDefault(p=>p.id==next.playerId);var dealer=snapshot.players.FirstOrDefault(p=>p.id==next.dealerId);
            if(newHand&&player!=null&&dealer!=null)
            {
                var direction=new Vector3(dealer.x-player.x,0,dealer.z-player.z);
                if(direction.sqrMagnitude>.01f)board.rotation=Quaternion.LookRotation(direction);
            }
            board.position=new Vector3(Center.x,CenterBoardHeight,Center.z);
            SyncCards(next.playerHand,false,newHand);SyncCards(next.dealerHand,true,newHand);
        }
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
                    card=new Card{transform=go.transform,renderer=go.AddComponent<MeshRenderer>(),id=hand[i],index=i,dealer=dealer,born=Time.time+(initial?(i*2+(dealer?1:0))*.65f:0)};
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
                float t=Mathf.Clamp01((Time.time-card.born)/.7f),ease=1-Mathf.Pow(1-t,3);
                card.transform.localPosition=Vector3.Lerp(new Vector3(.62f,.065f,0),card.destination,ease)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.075f);
                float roll=card.id<0?180:card.flipAt>Time.time-.65f?Mathf.Lerp(180,0,Mathf.SmoothStep(0,1,(Time.time-card.flipAt)/.65f)):0;
                card.transform.localRotation=Quaternion.Euler(0,card.dealer?180:0,roll);
                card.renderer.enabled=Time.time>=card.born;
                if(!card.sounded&&t>.02f){card.sounded=true;audioSource.PlayOneShot(slide,.22f);}
            }
        }
        public void ClearHand(){if(board==null)return;ClearCards();board.gameObject.SetActive(false);shownHand=-1;shownPlayer=null;}
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
