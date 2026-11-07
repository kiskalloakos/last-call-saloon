using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public class SaloonGame : MonoBehaviour
    {
        public Tuning tuning = new Tuning();
        public SaloonConnection Connection { get; private set; }
        public SaloonVoice Voice { get; private set; }
        public Snapshot View { get; private set; }
        public Camera ViewCamera { get; private set; }
        public string PlayerName = "Stranger", Address = "127.0.0.1", Port = "7777", JoinCode = "";
        public bool MenuOpen = true;
        public float Sensitivity = 2;
        public bool Sandbox { get; private set; }
        public bool Hosting => Connection.Hosting;
        public bool InRoom => Connection.Connected;
        public int PlayerCount => players.Count;
        public Phase CurrentPhase => phase;
        public string LocalId => Connection.LocalId.ToString();
        public PlayerView Self => View?.players?.FirstOrDefault(p => p.id == LocalId);
        public bool ShowRole;
        [NonSerialized] public bool AutomatedInput;
        public string InteractionHint = "";
        public string VoiceRoom => View?.voiceRoom ?? "";
        readonly Dictionary<ulong, Participant> players = new Dictionary<ulong, Participant>();
        readonly Dictionary<string, SaloonAvatar> avatars = new Dictionary<string, SaloonAvatar>();
        readonly List<string> events = new List<string>();
        Phase phase;
        float phaseEnds, nextBroadcast, nextInput, yaw, pitch, helloAt;
        int beers;
        string outcome = "", voiceRoom = "";
        bool helloSent,leaving;
        Vector3 cameraVelocity;
        public IReadOnlyList<string> Events => events;

        class Participant
        {
            public ulong id;
            public string name, notice="", busyAction="";
            public Role role;
            public GameObject body;
            public CharacterController controller;
            public CapsuleCollider hitbox;
            public Vector2 move;
            public float yaw,pitch,lastInput,lastAction,nextShot,busyUntil,vertical;
            public int hp,ammo,beer,shots,seat=-1;
            public bool drawn,bot;
            public bool Alive => hp>0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(FindAnyObjectByType<SaloonGame>()==null)new GameObject("Saloon Prototype").AddComponent<SaloonGame>();
        }

        void Awake()
        {
            Application.runInBackground=true;
            Application.targetFrameRate=90;
            // Query-only hitboxes; movement collision remains on CharacterController.
            for(int layer=0;layer<32;layer++)Physics.IgnoreLayerCollision(8,layer,true);
            SaloonWorld.Build();
            PlayerName=PlayerPrefs.GetString("SaloonName","Stranger");
            var existing=Camera.main;
            ViewCamera=existing!=null?existing:new GameObject("First person camera").AddComponent<Camera>();
            ViewCamera.tag="MainCamera";ViewCamera.nearClipPlane=.035f;ViewCamera.farClipPlane=140;
            ViewCamera.fieldOfView=78;ViewCamera.clearFlags=CameraClearFlags.SolidColor;ViewCamera.backgroundColor=new Color(.44f,.32f,.20f);
            ViewCamera.transform.position=new Vector3(0,1.75f,-5.6f);ViewCamera.transform.rotation=Quaternion.Euler(0,0,0);
            if(ViewCamera.GetComponent<AudioListener>()==null)ViewCamera.gameObject.AddComponent<AudioListener>();
            Connection=gameObject.AddComponent<SaloonConnection>();Connection.Initialize();
            Connection.Joined+=OnJoined;Connection.Left+=OnLeft;Connection.CommandReceived+=OnCommand;Connection.SnapshotReceived+=ApplySnapshot;
            Connection.LostConnection+=()=>{if(!leaving)Leave();};
            Voice=gameObject.AddComponent<SaloonVoice>();Voice.Game=this;
            gameObject.AddComponent<SaloonHUD>().Game=this;
            gameObject.AddComponent<SaloonAutomation>().Game=this;
            gameObject.AddComponent<SaloonCloudVerification>().Game=this;
            AddEvent("Prototype ready. Five humans for a real match; solo practice has inert targets.");
        }

        void OnJoined(ulong id)
        {
            if(!Hosting)return;
            if(players.ContainsKey(id))return;
            var p=CreateParticipant(id,"Stranger "+(players.Count+1),false);
            players.Add(id,p);AddEvent(p.name+" entered the saloon.");
        }

        Participant CreateParticipant(ulong id,string name,bool bot)
        {
            int index=players.Count;
            var body=new GameObject("Player collision "+id);body.transform.position=SaloonWorld.Spawns[index%5];
            var identity=body.AddComponent<SaloonBody>();identity.id=id;
            var controller=body.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.30f;controller.center=new Vector3(0,.9f,0);controller.stepOffset=.25f;controller.skinWidth=.025f;
            var target=new GameObject("Player hitbox");target.layer=8;target.transform.SetParent(body.transform,false);
            var hitbox=target.AddComponent<CapsuleCollider>();hitbox.height=1.8f;hitbox.radius=.32f;hitbox.center=Vector3.up*.9f;
            return new Participant{id=id,name=name,body=body,controller=controller,hitbox=hitbox,hp=tuning.health,ammo=tuning.cylinder,bot=bot};
        }

        void OnLeft(ulong id)
        {
            if(!Hosting)return;
            if(players.TryGetValue(id,out var p))
            {
                AddEvent(p.name+" disconnected.");Destroy(p.body);players.Remove(id);
                if(phase==Phase.Live||phase==Phase.Introductions)Finish("MATCH ABORTED — PLAYER DISCONNECTED");
            }
        }

        public void HostLan(bool sandbox)
        {
            if(!ushort.TryParse(Port,out var port)||port==0){Connection.Status="Use a port from 1 to 65535.";return;}
            Sandbox=sandbox;voiceRoom="saloon-"+Guid.NewGuid().ToString("N");
            Connection.StartLan(true,"127.0.0.1",port);
            if(!Connection.Hosting)return;
            MenuOpen=false;LockCursor();
            if(sandbox)
                StartPractice();
        }

        public void StartPractice()
        {
            if(!Hosting||(phase!=Phase.Lobby&&phase!=Phase.Finished))return;
            Sandbox=true;
            ulong next=10000;
            while(players.Count<5)
            {
                while(players.ContainsKey(next))next++;
                players.Add(next,CreateParticipant(next,"Practice target "+(next-9999),true));
                next++;
            }
            StartMatch();
        }

        public void ClearPracticeTargets()
        {
            if(!Hosting||(phase!=Phase.Lobby&&phase!=Phase.Finished))return;
            foreach(var id in players.Values.Where(p=>p.bot).Select(p=>p.id).ToArray())
            {Destroy(players[id].body);players.Remove(id);}
            Sandbox=false;phase=Phase.Lobby;Connection.Accepting=true;outcome="";
            Broadcast();
        }

        public void JoinLan()
        {
            if(!ushort.TryParse(Port,out var port)||port==0){Connection.Status="Use a valid port.";return;}
            Sandbox=false;Connection.StartLan(false,Address.Trim(),port);MenuOpen=false;
        }

        public async void Cloud(bool host)
        {
            Sandbox=false;
            if(host)voiceRoom="saloon-"+Guid.NewGuid().ToString("N");
            await Connection.StartCloud(host,JoinCode);
            if(Connection.Connected){MenuOpen=false;LockCursor();}
        }

        public void StartMatch()
        {
            if(!Hosting||players.Count!=5||(phase!=Phase.Lobby&&phase!=Phase.Finished))return;
            var deck=SaloonRules.Deck.ToArray();
            for(int i=deck.Length-1;i>0;i--){int j=UnityEngine.Random.Range(0,i+1);(deck[i],deck[j])=(deck[j],deck[i]);}
            int index=0;
            foreach(var p in players.Values)
            {
                p.role=deck[index];p.hp=tuning.health;p.ammo=tuning.cylinder;p.beer=0;p.drawn=false;p.seat=-1;p.busyUntil=0;p.busyAction="";p.move=Vector2.zero;p.vertical=0;p.shots=0;p.notice="";
                p.controller.enabled=false;p.body.transform.position=SaloonWorld.Spawns[index++];p.controller.enabled=true;p.hitbox.enabled=true;
            }
            beers=tuning.sharedBeers;outcome="";phase=Phase.Introductions;phaseEnds=Time.time+tuning.openingSeconds;
            Connection.Accepting=false;AddEvent("Roles dealt privately. Introductions: weapons are locked.");
            Broadcast();
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)){MenuOpen=!MenuOpen;if(MenuOpen)UnlockCursor();else LockCursor();}
            if(!InRoom){UnlockCursor();return;}
            if(!helloSent&&Time.unscaledTime>=helloAt){Connection.Send(new Command{kind="hello",name=PlayerName});helloAt=Time.unscaledTime+1;}
            if(Self!=null&&Self.name==CleanName(PlayerName))helloSent=true;
            if(!MenuOpen&&Self!=null)
            {
                if(Cursor.lockState!=CursorLockMode.Locked&&Input.GetMouseButtonDown(0))LockCursor();
                if(Cursor.lockState==CursorLockMode.Locked)
                {
                    yaw+=Input.GetAxis("Mouse X")*Sensitivity;
                    pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*Sensitivity,-75,75);
                    ShowRole=Input.GetKey(KeyCode.Tab);
                    if(Input.GetKeyDown(KeyCode.Q))Act("draw");
                    if(Input.GetKeyDown(KeyCode.R))Act("reload");
                    if(Input.GetKeyDown(KeyCode.E))Act("interact");
                    if(Input.GetKeyDown(KeyCode.B))Act("drink");
                    if(Input.GetKeyDown(KeyCode.G))Act("give");
                    if(Input.GetMouseButtonDown(0))Act("fire");
                }
            }
            else ShowRole=false;
            if(!AutomatedInput&&Time.unscaledTime>=nextInput)
            {
                var move=!MenuOpen&&Cursor.lockState==CursorLockMode.Locked?new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical")):Vector2.zero;
                Connection.Send(new Command{kind="input",x=move.x,z=move.y,yaw=yaw,pitch=pitch});nextInput=Time.unscaledTime+.033f;
            }
            if(Hosting)TickServer();
            UpdateCamera();UpdateInteractionHint();
        }

        public void Act(string kind)=>Connection.Send(new Command{kind=kind});

        void TickServer()
        {
            if(phase==Phase.Introductions&&Time.time>=phaseEnds)
            {phase=Phase.Live;phaseEnds=Time.time+tuning.matchSeconds;AddEvent("Weapons unlocked. No turns, no dice.");}
            if(phase==Phase.Live&&Time.time>=phaseEnds)Finish("DRAW — TIME EXPIRED");
            foreach(var p in players.Values)
            {
                if(!p.Alive)continue;
                if(p.busyUntil>0&&Time.time>=p.busyUntil)
                {
                    if(p.busyAction=="reload")p.ammo=tuning.cylinder;
                    if(p.busyAction=="drink")p.hp=Mathf.Min(tuning.health,p.hp+1);
                    p.busyUntil=0;p.busyAction="";
                }
                if(p.seat<0)
                {
                    if(Time.time-p.lastInput>.3f)p.move=Vector2.zero;
                    var world=Quaternion.Euler(0,p.yaw,0)*new Vector3(p.move.x,0,p.move.y);
                    if(!p.controller.enabled)continue;
                    p.vertical=p.controller.isGrounded?-1:p.vertical-20*Time.deltaTime;
                    float speed=p.busyUntil>Time.time?tuning.walkSpeed*.35f:tuning.walkSpeed;
                    if(phase==Phase.Finished)world=Vector3.zero;
                    p.controller.Move((world*speed+Vector3.up*p.vertical)*Mathf.Min(Time.deltaTime,.05f));
                }
            }
            if(Time.unscaledTime>=nextBroadcast){Broadcast();nextBroadcast=Time.unscaledTime+.05f;}
        }

        void OnCommand(ulong id,Command cmd)
        {
            if(!Hosting||!players.TryGetValue(id,out var p))return;
            if(cmd.kind=="hello") {p.name=CleanName(cmd.name);return;}
            if(cmd.kind=="input")
            {
                if(!Finite(cmd.x)||!Finite(cmd.z)||!Finite(cmd.yaw)||!Finite(cmd.pitch))return;
                p.move=Vector2.ClampMagnitude(new Vector2(cmd.x,cmd.z),1);p.yaw=Mathf.Repeat(cmd.yaw,360);p.pitch=Mathf.Clamp(cmd.pitch,-75,75);p.lastInput=Time.time;return;
            }
            if(!p.Alive||phase==Phase.Finished||Time.time-p.lastAction<.09f)return;
            p.lastAction=Time.time;
            if(cmd.kind=="interact"&&p.seat>=0){Stand(p);return;}
            if(p.busyUntil>Time.time){p.notice="Finish what you are doing first.";return;}
            switch(cmd.kind)
            {
                case "draw":
                    if(phase!=Phase.Live){p.notice="Weapons unlock after introductions.";break;}
                    p.drawn=!p.drawn;p.busyUntil=Time.time+tuning.drawSeconds;p.busyAction="draw";break;
                case "fire":Fire(p);break;
                case "reload":
                    if(!p.drawn||phase!=Phase.Live||p.ammo>=tuning.cylinder)break;
                    p.busyUntil=Time.time+tuning.reloadSeconds;p.busyAction="reload";p.notice="Reloading — you are exposed.";break;
                case "interact":Interact(p);break;
                case "drink":
                    if(p.beer<=0){p.notice="Take a beer from the brass tap at the bar first.";break;}
                    if(p.hp>=tuning.health){p.notice="You are already at full health.";break;}
                    p.beer--;p.drawn=false;p.busyUntil=Time.time+tuning.drinkSeconds;p.busyAction="drink";p.notice="Drinking — weapon holstered.";break;
                case "give":Give(p);break;
            }
        }

        static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
        static string CleanName(string name)
        {
            var cleaned=new string((name??"Stranger").Where(c=>char.IsLetterOrDigit(c)||c==' '||c=='_'||c=='-').Take(20).ToArray()).Trim();
            return string.IsNullOrWhiteSpace(cleaned)?"Stranger":cleaned;
        }

        Vector3 Eye(Participant p)=>p.body.transform.position+Vector3.up*(p.seat>=0?1.35f:1.65f);
        Vector3 Forward(Participant p)=>Quaternion.Euler(p.pitch,p.yaw,0)*Vector3.forward;
        RaycastHit? Target(Participant p,float distance)
        {
            Physics.SyncTransforms();
            var hits=Physics.RaycastAll(Eye(p),Forward(p),distance,~0,QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                var body=hit.collider.GetComponentInParent<SaloonBody>();
                if(body!=null&&body.id==p.id)continue;
                return hit;
            }
            return null;
        }

        void Fire(Participant p)
        {
            if(phase!=Phase.Live||!p.drawn||Time.time<p.nextShot)return;
            if(p.ammo<=0){p.notice="Empty cylinder — R to reload.";return;}
            p.ammo--;p.shots++;p.nextShot=Time.time+tuning.shotInterval;
            var hit=Target(p,65);
            if(hit.HasValue)
            {
                var body=hit.Value.collider.GetComponentInParent<SaloonBody>();
                if(body!=null&&players.TryGetValue(body.id,out var victim)&&victim.Alive)
                {
                    victim.hp--;p.notice="Hit.";victim.notice="You were hit!";
                    // Damage interrupts drinking/reloading; the consumed beer is lost.
                    victim.busyUntil=0;victim.busyAction="";
                    if(!victim.Alive)
                    {
                        victim.drawn=false;victim.move=Vector2.zero;victim.controller.enabled=false;victim.hitbox.enabled=false;
                        AddEvent(victim.name+" was eliminated.");
                        string result=SaloonRules.Winner(players.Values.Where(x=>x.Alive).Select(x=>x.role));
                        if(!string.IsNullOrEmpty(result))Finish(result);
                    }
                }
            }
        }

        void Interact(Participant p)
        {
            var hit=Target(p,tuning.interactionDistance);
            var point=hit?.collider.GetComponentInParent<InteractionPoint>();
            if(point==null){p.notice="Look at a chair to sit, or the brass tap to take a beer.";return;}
            if(point.kind==InteractionKind.Beer)
            {
                if(p.beer>0){p.notice="You can carry one beer. Drink or give it away first.";return;}
                if(beers<=0){p.notice="The shared beer supply is empty.";return;}
                beers--;p.beer++;p.notice="Beer taken. B to drink; G to offer it to someone.";return;
            }
            if(players.Values.Any(x=>x.Alive&&x.seat==point.index)){p.notice="That seat is occupied.";return;}
            p.controller.enabled=false;p.body.transform.position=point.sitPosition;p.controller.enabled=true;
            p.seat=point.index;p.move=Vector2.zero;p.drawn=false;p.notice="Seated. E to stand.";
        }

        void Stand(Participant p)
        {
            var original=p.body.transform.position;
            foreach(var offset in new[]{Vector3.back,Vector3.right,Vector3.left,Vector3.forward})
            {
                var pos=original+Quaternion.Euler(0,p.yaw,0)*offset*.9f;pos.y=.08f;
                var collisions=Physics.OverlapCapsule(pos+Vector3.up*.4f,pos+Vector3.up*1.45f,.32f);
                if(collisions.Any(c=>c.GetComponentInParent<SaloonBody>()?.id!=p.id))continue;
                p.controller.enabled=false;p.body.transform.position=pos;p.controller.enabled=true;p.seat=-1;p.notice="";return;
            }
            p.notice="There is no room to stand — someone is blocking your chair.";
        }

        void Give(Participant p)
        {
            if(p.beer<=0){p.notice="You have no beer to offer.";return;}
            var hit=Target(p,tuning.interactionDistance);var body=hit?.collider.GetComponentInParent<SaloonBody>();
            if(body==null||!players.TryGetValue(body.id,out var target)||!target.Alive||target.beer>0){p.notice="Look at a nearby living player with empty hands.";return;}
            p.beer--;target.beer++;p.notice="You gave "+target.name+" a beer.";target.notice=p.name+" gave you a beer.";
        }

        void Finish(string result)
        {
            phase=Phase.Finished;outcome=result;Connection.Accepting=true;AddEvent(result);Broadcast();
            Debug.Log("SALOON_RESULT "+result);
        }

        public Snapshot PrivateSnapshot(ulong recipient)
        {
            if(!players.TryGetValue(recipient,out var own))return null;
            return new Snapshot
            {
                players=players.Values.Select(p=>new PlayerView{id=p.id.ToString(),name=p.name,x=p.body.transform.position.x,y=p.body.transform.position.y,z=p.body.transform.position.z,yaw=p.yaw,pitch=p.pitch,alive=p.Alive,drawn=p.drawn,seated=p.seat>=0,sheriff=p.role==Role.Sheriff,busy=p.busyUntil>Time.time,shots=p.shots,revealedRole=phase==Phase.Finished?p.role.ToString():""}).ToArray(),
                phase=phase,remaining=Mathf.Max(0,phaseEnds-Time.time),outcome=outcome,ownRole=own.role.ToString(),
                ally=own.role==Role.Outlaw?string.Join(", ",players.Values.Where(p=>p.role==Role.Outlaw&&p.id!=recipient).Select(p=>p.name)):"",
                health=own.hp,ammo=own.ammo,beers=beers,carriedBeer=own.beer,notice=own.notice,
                sandbox=Sandbox,busyRemaining=Mathf.Max(0,own.busyUntil-Time.time),voiceRoom=voiceRoom
            };
        }

        void Broadcast()
        {
            foreach(var p in players.Values)
                if(!p.bot)Connection.SendSnapshot(p.id,PrivateSnapshot(p.id));
        }

        void ApplySnapshot(Snapshot snapshot)
        {
            if(snapshot==null||snapshot.players==null)return;
            View=snapshot;
            foreach(var p in snapshot.players)
            {
                if(!avatars.TryGetValue(p.id,out var avatar))
                {
                    avatar=new GameObject("Avatar "+p.name).AddComponent<SaloonAvatar>();
                    avatar.Initialize(this,p,Array.IndexOf(snapshot.players,p));avatars.Add(p.id,avatar);
                }
                avatar.Apply(p,p.id==LocalId);
            }
            foreach(var id in avatars.Keys.Where(id=>!snapshot.players.Any(p=>p.id==id)).ToArray()){Destroy(avatars[id].gameObject);avatars.Remove(id);}
        }

        void UpdateCamera()
        {
            var self=Self;if(self==null)return;
            var target=new Vector3(self.x,self.y+(self.seated?1.35f:1.65f),self.z);
            if((ViewCamera.transform.position-target).sqrMagnitude>9)ViewCamera.transform.position=target;
            else ViewCamera.transform.position=Vector3.SmoothDamp(ViewCamera.transform.position,target,ref cameraVelocity,.045f);
            ViewCamera.transform.rotation=Quaternion.Euler(pitch,yaw,0);
            ViewCamera.fieldOfView=Mathf.Lerp(ViewCamera.fieldOfView,!MenuOpen&&Input.GetMouseButton(1)?60:78,Time.deltaTime*12);
        }

        void UpdateInteractionHint()
        {
            InteractionHint="";if(Self==null||!Self.alive)return;
            if(Self.seated){InteractionHint="E  /  stand up";return;}
            if(Physics.Raycast(ViewCamera.transform.position,ViewCamera.transform.forward,out var hit,tuning.interactionDistance,~0,QueryTriggerInteraction.Ignore))
            {
                var point=hit.collider.GetComponentInParent<InteractionPoint>();
                if(point!=null)InteractionHint=point.kind==InteractionKind.Seat?"E  /  take a seat":"E  /  take a beer";
                if(hit.collider.GetComponentInParent<SaloonBody>()!=null&&View.carriedBeer>0)InteractionHint="G  /  offer your beer";
            }
        }

        public async void Leave()
        {
            if(leaving)return;leaving=true;
            await Voice.Leave();await Connection.Disconnect();
            foreach(var p in players.Values)Destroy(p.body);players.Clear();
            foreach(var a in avatars.Values)Destroy(a.gameObject);avatars.Clear();
            View=null;phase=Phase.Lobby;Sandbox=false;helloSent=false;outcome="";MenuOpen=true;UnlockCursor();leaving=false;
        }

        public void SaveName(){PlayerPrefs.SetString("SaloonName",PlayerName);PlayerPrefs.Save();}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Integration harness: exercises the real host commands, physics, and action timers.
        internal System.Collections.IEnumerator VerifyGameplay()
        {
            yield return null;
            var shooter=players[Connection.LocalId];var target=players.Values.First(p=>p.bot);
            void Check(bool value,string message){if(!value){Debug.LogError("SALOON_GAMEPLAY FAIL "+message);Application.Quit(6);throw new InvalidOperationException(message);}Debug.Log("SALOON_CHECK "+message);}
            void Place(Participant p,Vector3 position,float direction=0,float look=0)
            {p.controller.enabled=false;p.body.transform.position=position;p.controller.enabled=true;p.yaw=direction;p.pitch=look;p.move=Vector2.zero;p.lastInput=Time.time;p.seat=-1;}
            void Do(Participant p,string action){p.lastAction=-100;OnCommand(p.id,new Command{kind=action});}
            foreach(var other in players.Values.Where(p=>p!=shooter&&p!=target))Place(other,new Vector3(6, .08f,-5+(int)(other.id%3)*1.1f));
            Do(shooter,"draw");Check(!shooter.drawn,"opening blocks weapon drawing");
            int initialAmmo=shooter.ammo;shooter.drawn=true;Fire(shooter);shooter.drawn=false;
            Check(shooter.ammo==initialAmmo,"opening blocks damage and ammunition consumption");
            yield return new WaitForSeconds(1.2f);
            Check(phase==Phase.Live,"opening transitions to continuous real-time play");
            Place(shooter,new Vector3(0,.08f,-3));Place(target,new Vector3(0,.08f,0));
            Do(shooter,"draw");yield return new WaitForSeconds(tuning.drawSeconds+.1f);
            int hp=target.hp;
            Physics.SyncTransforms();
            var aimedAt=Target(shooter,65);
            Debug.Log("SALOON_TRACE shot phase="+phase+" drawn="+shooter.drawn+" origin="+Eye(shooter)+" target="+target.body.transform.position+" firstHit="+(aimedAt.HasValue?aimedAt.Value.collider.name:"none"));
            Fire(shooter);
            Check(target.hp==hp-1&&shooter.ammo==initialAmmo-1,"hitscan applies one damage and consumes one round; actual hp="+target.hp+" ammo="+shooter.ammo);
            Fire(shooter);Check(target.hp==hp-1,"fire-rate limiter rejects an immediate second shot");
            target.drawn=true;target.yaw=180;Fire(target);Check(shooter.hp==tuning.health-1,"other player can damage the host");
            Place(shooter,new Vector3(1.6f,.08f,2.1f),0,2);
            int supply=beers;Do(shooter,"interact");Check(shooter.beer==1&&beers==supply-1,"bar tap removes one shared beer");
            Do(shooter,"drink");Check(!shooter.drawn&&shooter.busyAction=="drink","drinking visibly holsters and occupies hands");
            yield return new WaitForSeconds(.4f);Check(shooter.hp==tuning.health-1,"healing does not apply before drinking completes");
            yield return new WaitForSeconds(tuning.drinkSeconds);
            Check(shooter.hp==tuning.health&&shooter.beer==0,"completed drinking restores one health");
            Do(shooter,"interact");Place(target,new Vector3(1.6f,.08f,.5f));shooter.yaw=180;shooter.pitch=0;
            Do(shooter,"give");Check(shooter.beer==0&&target.beer==1,"beer transfers to a nearby player through line of sight");
            Do(target,"drink");int targetAmmo=target.ammo;Do(target,"fire");
            Check(target.ammo==targetAmmo,"occupied hands prevent firing during drinking");
            shooter.drawn=true;shooter.nextShot=0;Fire(shooter);
            Check(target.busyUntil==0&&target.beer==0&&target.hp==1,"taking damage interrupts drinking and consumes the started beer");
            Place(shooter,new Vector3(-5,.08f,.7f),0,40);Do(shooter,"interact");
            Check(shooter.seat==0,"looking at a stool seats the player");
            Do(shooter,"interact");Check(shooter.seat<0,"seated player can stand into free space");
            shooter.drawn=true;shooter.ammo=2;Do(shooter,"reload");
            Check(shooter.busyAction=="reload"&&shooter.ammo==2,"reload has a real vulnerable duration");
            Do(shooter,"fire");Check(shooter.ammo==2,"occupied hands prevent firing during reload");
            yield return new WaitForSeconds(tuning.reloadSeconds+.1f);Check(shooter.ammo==tuning.cylinder,"reload fills the cylinder after its timer");
            Place(shooter,new Vector3(4,.08f,5),90);Place(target,new Vector3(6,.08f,5));
            hp=target.hp;Fire(shooter);Check(target.hp==hp,"solid partition blocks damage");
            phase=Phase.Live;phaseEnds=Time.time+100;
            Place(shooter,new Vector3(0,.08f,-3));Place(target,new Vector3(0,.08f,0));
            shooter.nextShot=0;target.hp=1;target.role=Role.Outlaw;shooter.role=Role.Sheriff;
            var others=players.Values.Where(p=>p!=shooter&&p!=target).ToArray();others[0].role=Role.Deputy;others[1].role=Role.Outlaw;others[2].role=Role.Renegade;
            Fire(shooter);Check(!target.Alive&&!target.controller.enabled,"lethal damage disables the eliminated player's collision");
            Check(phase==Phase.Live,"eliminating one Outlaw does not end a match early");
            Check(PrivateSnapshot(target.id).health==0,"elimination reaches the victim's private snapshot");
            Debug.Log("SALOON_GAMEPLAY PASS");Application.Quit(0);
        }
#endif
        void AddEvent(string message){events.Add(message);if(events.Count>12)events.RemoveAt(0);Debug.Log("SALOON "+message);}
        void LockCursor(){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        void UnlockCursor(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        void OnApplicationQuit(){if(Connection!=null&&Connection.Manager!=null)Connection.Manager.Shutdown();}
    }
}
