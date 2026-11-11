using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public sealed class SaloonBeerTarget : MonoBehaviour
    {
        public int station;
        public string action;
    }

    // The mug, fill line, tap and controls all live on the counter. The mouse stays in look mode.
    public sealed class SaloonBeerTaps : MonoBehaviour
    {
        sealed class Tap
        {
            public Transform root,liquid,stream,foam,handle,spill;
            public TextMesh status,pourLabel;
            public Renderer pourButton,drinkButton,leaveButton;
            public float elapsed,receivedAt;
            public bool pouring;
            public PlayerView player;
        }
        public static Vector3 StationPosition(int slot)=>new Vector3(-5+slot*1.5f,1.75f,3.38f);
        readonly List<Tap> taps=new List<Tap>();
        readonly List<Material> materials=new List<Material>();
        SaloonGame game;
        bool holdingTap;float heldFlow=1;
        Snapshot view;
        PlayerView self;
        Material enabledButton,disabledButton;
        Material Mat(string name,Color color)
        {var m=new Material(Shader.Find("Standard")){name=name,color=color};materials.Add(m);return m;}
        static Transform Shape(Transform root,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=pos;go.transform.localScale=scale;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        static void Target(Transform shape,int station,string action,Vector3? size=null)
        {
            shape.gameObject.layer=SaloonBlackjackTable.InteractionLayer;
            var collider=shape.gameObject.AddComponent<BoxCollider>();collider.isTrigger=true;if(size.HasValue)collider.size=size.Value;
            var target=shape.gameObject.AddComponent<SaloonBeerTarget>();target.station=station;target.action=action;
        }
        static TextMesh Label(Transform root,string name,Vector3 position,float size,bool flat)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;
            go.transform.localRotation=flat?Quaternion.Euler(90,0,0):Quaternion.identity;
            var label=go.AddComponent<TextMesh>();label.fontSize=64;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;
            label.alignment=TextAlignment.Center;label.color=new Color(1,.89f,.62f);SaloonWorldText.Attach(label);return label;
        }
        public void Initialize(SaloonGame owner)
        {
            game=owner;
            var brass=Mat("Beer tap brass",new Color(.65f,.44f,.15f));
            var dark=Mat("Beer tap handle",new Color(.14f,.07f,.025f));
            var amber=Mat("Poured amber beer",new Color(.94f,.49f,.04f));
            var cream=Mat("Beer head",new Color(1,.91f,.68f));
            var mark=Mat("Beer full line",new Color(.43f,.72f,.43f));
            enabledButton=Mat("Beer counter control available",new Color(.20f,.30f,.18f));
            disabledButton=Mat("Beer counter control closed",new Color(.12f,.105f,.07f));
            for(int i=0;i<5;i++)
            {
                var root=new GameObject("Pouring station "+(i+1)).transform;root.SetParent(transform,false);root.position=StationPosition(i)+Vector3.down*.31f;
                Shape(root,"Drip tray",PrimitiveType.Cube,new Vector3(0,.008f,0),new Vector3(.46f,.016f,.32f),dark);
                Shape(root,"Brass tower",PrimitiveType.Cylinder,new Vector3(0,.27f,.16f),new Vector3(.07f,.27f,.07f),brass);
                var spout=Shape(root,"Tap spout",PrimitiveType.Cylinder,new Vector3(0,.46f,.04f),new Vector3(.04f,.12f,.04f),brass);spout.localRotation=Quaternion.Euler(90,0,0);
                var handle=Shape(root,"Tap handle",PrimitiveType.Cube,new Vector3(0,.62f,.16f),new Vector3(.055f,.18f,.05f),dark);
                Shape(root,"Mug base",PrimitiveType.Cylinder,new Vector3(0,.026f,-.06f),new Vector3(.20f,.012f,.20f),brass);
                Shape(root,"Mug rim",PrimitiveType.Cylinder,new Vector3(0,.31f,-.06f),new Vector3(.21f,.007f,.21f),brass);
                for(int side=-1;side<=1;side+=2)Shape(root,"Mug side",PrimitiveType.Cube,new Vector3(side*.097f,.17f,-.06f),new Vector3(.012f,.28f,.15f),brass);
                Shape(root,"Mug handle",PrimitiveType.Cube,new Vector3(.15f,.18f,-.06f),new Vector3(.08f,.14f,.028f),brass);
                Shape(root,"Full line",PrimitiveType.Cube,new Vector3(0,.29f,-.158f),new Vector3(.21f,.008f,.009f),mark);
                var liquid=Shape(root,"Beer level",PrimitiveType.Cylinder,new Vector3(0,.045f,-.06f),new Vector3(.18f,.001f,.18f),amber);
                var foam=Shape(root,"Foam",PrimitiveType.Cylinder,new Vector3(0,.05f,-.06f),new Vector3(.18f,.008f,.18f),cream);
                var stream=Shape(root,"Pouring stream",PrimitiveType.Cylinder,new Vector3(0,.33f,-.06f),new Vector3(.023f,.13f,.023f),amber);
                var spill=Shape(root,"Overflow",PrimitiveType.Cylinder,new Vector3(0,.18f,-.17f),new Vector3(.02f,.12f,.02f),amber);
                Shape(root,"Pouring instructions",PrimitiveType.Cube,new Vector3(-.36f,.22f,.04f),new Vector3(.44f,.26f,.028f),dark);
                Shape(root,"Instructions stand",PrimitiveType.Cube,new Vector3(-.36f,.065f,.04f),new Vector3(.055f,.13f,.055f),brass);
                var status=Label(root,"Pour result on counter",new Vector3(-.36f,.22f,.023f),.006f,false);
                Renderer Control(string name,float x,out TextMesh label)
                {
                    var control=Shape(root,name,PrimitiveType.Cube,new Vector3(x,.045f,-.205f),new Vector3(.37f,.025f,.13f),disabledButton);
                    control.localRotation=Quaternion.Euler(-35,0,0);
                    label=Label(root,name+" lettering",control.localPosition+control.localRotation*Vector3.up*.020f,.0058f,true);
                    label.transform.localRotation=Quaternion.Euler(55,0,0);label.text=name;
                    return control.GetComponent<Renderer>();
                }
                var pourButton=Control("POUR / E",-.43f,out var pourLabel);
                pourButton.gameObject.SetActive(false);
                pourLabel.gameObject.SetActive(false);
                var drinkButton=Control("DRINK / B",0,out _);
                var leaveButton=Control("LEAVE / SPACE",.43f,out _);
                // Assign the render layer first; only the actual controls use the query-only interaction layer.
                SaloonSmokingArea.SetLayer(root,SaloonSmokingArea.IndoorLayer);
                Target(handle,i,"pour",new Vector3(3,1.7f,3));
                var mugTarget=new GameObject("Mug interaction").transform;mugTarget.SetParent(root,false);mugTarget.localPosition=new Vector3(0,.17f,-.06f);
                Target(mugTarget,i,"mug",new Vector3(.23f,.30f,.23f));
                Target(pourButton.transform,i,"pour");Target(drinkButton.transform,i,"drink");Target(leaveButton.transform,i,"leaveBeer");
                taps.Add(new Tap{root=root,liquid=liquid,foam=foam,stream=stream,handle=handle,spill=spill,status=status,pourLabel=pourLabel,
                    pourButton=pourButton,drinkButton=drinkButton,leaveButton=leaveButton});
                root.gameObject.SetActive(false);
            }
        }
        public void Clear(){foreach(var tap in taps)tap.root.gameObject.SetActive(false);view=null;self=null;}
        public void Apply(Snapshot snapshot)
        {
            view=snapshot;self=snapshot.players.FirstOrDefault(p=>p.id==snapshot.ownId);
            var bar=snapshot.phase==Phase.Turns?snapshot.players.Where(p=>p.alive&&p.room==0).ToArray():new PlayerView[0];
            for(int i=0;i<taps.Count;i++)
            {
                var tap=taps[i];var p=bar.FirstOrDefault(player=>player.beerStation==i);
                tap.root.gameObject.SetActive(p!=null);if(p==null)continue;
                tap.player=p;tap.pouring=p.pouring;tap.elapsed=p.pourElapsed;tap.receivedAt=Time.time;
                if(!p.pouring)tap.elapsed=p.carriedBeer>0?SaloonBeerPour.FullSeconds*p.beerHealing/20f:0;
                tap.pourLabel.text=p.pouring?"STOP / E":"POUR / E";
                bool own=p.id==snapshot.ownId;
                SetControl(tap.pourButton,own&&(p.pouring||CanPour));
                SetControl(tap.drinkButton,own&&Ready&&p.health<100);
                SetControl(tap.leaveButton,own&&Ready);
            }
            Update();
        }
        bool Ready=>view?.phase==Phase.Turns&&self?.alive==true&&self.room==0&&self.carriedBeer>0&&view.beerChoiceRemaining>0;
        bool CanPour=>view?.phase==Phase.Turns&&self?.alive==true&&self.room==0&&self.health<100&&view.actionAvailable&&view.beers>0;
        void SetControl(Renderer control,bool available){control.sharedMaterial=available?enabledButton:disabledButton;}
        public string ActionForRay(Ray ray)
        {
            if(view?.phase!=Phase.Turns||self?.alive!=true||self.room!=0||self.drawn)return null;
            int mask=~((1<<8)|(1<<SaloonMovement.SolidLayer)|(1<<SaloonRagdoll.Layer));
            if(!Physics.Raycast(ray,out var hit,game.tuning.interactionDistance,mask,QueryTriggerInteraction.Collide))return null;
            var target=hit.collider.GetComponent<SaloonBeerTarget>();
            if(target==null||target.station!=self.beerStation)return null;
            if(target.action=="pour"||target.action=="mug")
            {
                if(self.pouring)return "stopPour";
                if(target.action=="mug"&&Ready)return self.health<100?"drink":null;
                return CanPour?"interact":null;
            }
            if(!Ready)return null;
            return target.action=="drink"?(self.health<100?"drink":null):target.action;
        }
        public bool HandleClick()
        {
            string action=ActionForRay(new Ray(game.ViewCamera.transform.position,game.ViewCamera.transform.forward));
            if(action==null)return false;
            if(action=="interact"){holdingTap=true;heldFlow=1;}
            game.Act(action);return true;
        }
        public void HandlePourInput()
        {
            if(!holdingTap)return;
            if(!Input.GetMouseButton(0)||game.MenuOpen||game.ShowRole||!game.AtBeerBar)
            {holdingTap=false;game.Act("stopPour");return;}
            float wheel=Input.GetAxis("Mouse ScrollWheel");
            if(Mathf.Abs(wheel)>.001f)
            {
                heldFlow=Mathf.Clamp(heldFlow+wheel*1.5f,.35f,1.5f);
                game.Connection.Send(new Command{kind="pourFlow",x=heldFlow});
            }
        }
        public string HintForRay(Ray ray)
        {
            string action=ActionForRay(ray);
            if(action=="interact")return "Hold tap / E · Pour beer";
            if(action=="stopPour")return "Release to stop · Scroll to adjust flow";
            if(action=="drink")return "Click mug / B · Drink +"+self.beerHealing+"%";
            if(action=="leaveBeer")return "Click / Space · Leave your beer";
            return self?.pouring==true?"Release to stop · Scroll to adjust flow":Ready?"B · Drink    Aim + G · Offer    Space · Leave beer":"";
        }
        void Update()
        {
            foreach(var tap in taps)
            {
                if(!tap.root.gameObject.activeSelf)continue;
                float elapsed=tap.elapsed+(tap.pouring?(Time.time-tap.receivedAt)*tap.player.pourFlow:0);
                float raw=SaloonBeerPour.Fill(elapsed),fill=Mathf.Clamp01(raw),height=fill*.25f;
                tap.liquid.gameObject.SetActive(fill>0);tap.foam.gameObject.SetActive(fill>0);tap.stream.gameObject.SetActive(tap.pouring);tap.spill.gameObject.SetActive(tap.pouring&&raw>1);
                tap.liquid.localScale=new Vector3(.18f,Mathf.Max(.001f,height/2),.18f);tap.liquid.localPosition=new Vector3(0,.04f+height/2,-.06f);
                tap.foam.localPosition=new Vector3(0,.04f+height,-.06f);
                float bottom=.055f+height;tap.stream.localPosition=new Vector3(0,(.45f+bottom)/2,-.06f);tap.stream.localScale=new Vector3(.023f*Mathf.Sqrt(tap.player.pourFlow),(.45f-bottom)/2,.023f*Mathf.Sqrt(tap.player.pourFlow));
                tap.handle.localRotation=Quaternion.Euler(tap.pouring?-35*tap.player.pourFlow:0,0,0);
                var p=tap.player;bool own=p.id==view.ownId;
                tap.status.text=tap.pouring?(raw>1?"OVERFLOW":"FILL TO THE LINE")+"\n+"+SaloonBeerPour.Healing(elapsed)+"%\nFLOW x"+p.pourFlow.ToString("0.0")+" / SCROLL":
                    p.carriedBeer>0?"+"+p.beerHealing+"% HEALING\nB DRINK / G OFFER\nSPACE LEAVE":p.drinking?"DRINKING":p.health>=100?"FULL HEALTH\nWATCH THE BAR":(own&&view.beers<=0)?"TAPS EMPTY":(own&&!view.actionAvailable)?"WAIT FOR YOUR TURN":"POUR ONE BEER\nUP TO +20%\nCLICK TAP / E";
            }
        }
        void OnDestroy()
        {foreach(var tap in taps)if(tap.root!=null)Destroy(tap.root.gameObject);foreach(var material in materials)Destroy(material);}
    }
}
