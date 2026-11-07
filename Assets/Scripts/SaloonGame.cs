using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame : MonoBehaviour
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
        readonly SaloonRounds rounds = new SaloonRounds();
        float nextBroadcast, nextInput, yaw, pitch, helloAt;
        int beers;
        string outcome = "", voiceRoom = "";
        bool helloSent,leaving;
        Phase shownPhase;int shownPose=-1;
        bool MatchActive=>phase!=Phase.Lobby&&phase!=Phase.Finished;
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
            public int hp,ammo,beer,shots,seat=-1,room,chosenRoom,poseVersion,beerPickupRound=-1;
            public bool gatherStation;
            public ulong? companion;
            public bool drawn,bot,bag;
            public int escapeRound;
            public float botThink,drinkReady,giftUntil;
            public int giftSequence;public ulong giftTarget;
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
            SaloonWorld.Build();SaloonArt.Apply();
            PlayerName=PlayerPrefs.GetString("SaloonName","Stranger");
            var existing=Camera.main;
            ViewCamera=existing!=null?existing:new GameObject("First person camera").AddComponent<Camera>();
            ViewCamera.tag="MainCamera";ViewCamera.nearClipPlane=.035f;ViewCamera.farClipPlane=140;
            ViewCamera.fieldOfView=78;ViewCamera.clearFlags=CameraClearFlags.SolidColor;ViewCamera.backgroundColor=new Color(.44f,.32f,.20f);
            ViewCamera.transform.position=new Vector3(0,1.75f,-5.6f);ViewCamera.transform.rotation=Quaternion.Euler(0,0,0);
            if(ViewCamera.GetComponent<AudioListener>()==null)ViewCamera.gameObject.AddComponent<AudioListener>();
            Connection=gameObject.AddComponent<SaloonConnection>();Connection.Initialize();
            Connection.Joined+=OnJoined;Connection.Left+=OnLeft;Connection.CommandReceived+=OnCommand;Connection.SnapshotReceived+=ApplySnapshot;
            Connection.LostConnection+=()=>{if(!leaving&&this!=null&&Application.isPlaying)Leave();};
            Voice=gameObject.AddComponent<SaloonVoice>();Voice.Game=this;
            gameObject.AddComponent<SaloonHUD>().Game=this;
            gameObject.AddComponent<SaloonAutomation>().Game=this;
            gameObject.AddComponent<SaloonCloudVerification>().Game=this;
            AddEvent("Prototype ready. Five humans for a real match; practice players pursue their role objectives.");
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
                if(MatchActive)Finish("MATCH ABORTED — PLAYER DISCONNECTED");
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
                players.Add(next,CreateParticipant(next,"Practice player "+(next-9999),true));
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
                p.role=deck[index];p.hp=tuning.health;p.ammo=tuning.cylinder;p.beer=0;p.beerPickupRound=-1;p.drawn=false;p.seat=-1;p.busyUntil=0;p.busyAction="";p.move=Vector2.zero;p.vertical=0;p.shots=0;p.notice="";p.nextShot=0;p.lastAction=-100;
                p.controller.enabled=false;p.body.transform.position=SaloonWorld.Spawns[index++];p.controller.enabled=true;p.hitbox.enabled=true;
            }
            var order=players.Keys.OrderBy(_=>UnityEngine.Random.value).ToArray();
            rounds.Start(order,Time.time,tuning.discussionSeconds,tuning.turnSeconds,tuning.roundLimit,tuning.choosingSeconds,tuning.regroupSeconds);
            ResetHeist();beers=tuning.sharedBeers;outcome="";phase=rounds.Phase;PositionEveryone(true);
            Connection.Accepting=false;AddEvent("Roles dealt privately. Round 1: discussion. One action each after discussion.");
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
                if(View.phase!=Phase.Choosing&&Cursor.lockState!=CursorLockMode.Locked&&Input.GetMouseButtonDown(0))LockCursor();
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
                    if(Input.GetKeyDown(KeyCode.Space))Act("pass");
                    if(Input.GetKeyDown(KeyCode.H))Act("rob");
                    if(Input.GetKeyDown(KeyCode.X))Act("escape");
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
            foreach(var p in players.Values)
            {
                if(!p.Alive)continue;
                if(p.busyUntil>0&&Time.time>=p.busyUntil)
                {
                    if(p.busyAction=="reload")p.ammo=tuning.cylinder;
                    if(p.busyAction=="drink"||p.busyAction=="giftDrink")p.hp=Mathf.Min(tuning.health,p.hp+1);
                    if(p.busyAction=="rob")CompleteRobbery(p);
                    p.busyUntil=0;p.busyAction="";
                }
                if(p.seat<0)
                {
                    if(Time.time-p.lastInput>.3f)p.move=Vector2.zero;
                    var world=Quaternion.Euler(0,p.yaw,0)*new Vector3(p.move.x,0,p.move.y);
                    if(!p.controller.enabled)continue;
                    p.vertical=p.controller.isGrounded?-1:p.vertical-20*Time.deltaTime;
                    float speed=p.busyUntil>Time.time?tuning.walkSpeed*.35f:tuning.walkSpeed;
                    if(MatchActive||phase==Phase.Finished)world=Vector3.zero;
                    p.controller.Move((world*speed+Vector3.up*p.vertical)*Mathf.Min(Time.deltaTime,.05f));
                }
            }
            if(MatchActive)
            {
                if(!verifying)TickPracticeBots();
                if(phase==Phase.Finished){Broadcast();return;}
                var before=phase;rounds.Tick(Time.time,id=>players.TryGetValue(id,out var p)&&p.Alive);phase=rounds.Phase;
                if(before!=phase)
                {
                    if(phase==Phase.Finished)Finish(HeistTimeout());
                    else if(phase==Phase.Turns){PositionEveryone(false);AddEvent("Room assignments locked. One action each.");}
                    else if(phase==Phase.Regroup){ResolveEscapes();if(phase!=Phase.Finished){PositionEveryone(true);AddEvent("Regrouping at the table.");}}
                    else if(phase==Phase.Choosing)AddEvent("Choose a room and invite a companion. Invitations must be mutual.");
                    else AddEvent("Round "+rounds.Round+": seated discussion.");
                }

            }
            if(Time.unscaledTime>=nextBroadcast){Broadcast();nextBroadcast=Time.unscaledTime+.05f;}
        }

        void PositionEveryone(bool gather)
        {
            var capacities=new[]{5,2,2};
            foreach(var dead in players.Values.Where(p=>!p.Alive&&!p.gatherStation))capacities[dead.room]--;
            var assignments=SaloonRounds.AssignRooms(players.Values.Where(p=>p.Alive).Select(p=>p.id),id=>players[id].chosenRoom,id=>players[id].companion,capacities);
            var slots=new int[3];int index=0;
            foreach(var p in players.Values)
            {
                int homeSlot=index++;
                if(!p.Alive)continue;
                p.room=gather?0:assignments[p.id];if(!gather&&p.room!=0)p.escapeRound=0;
                while(!gather&&players.Values.Any(dead=>!dead.Alive&&!dead.gatherStation&&dead.room==p.room&&dead.seat==100+slots[p.room]))slots[p.room]++;
                int slot=gather?homeSlot:slots[p.room]++;p.gatherStation=gather;
                Vector3 position=SaloonStations.Position(gather,p.room,slot);float facing=SaloonStations.Facing(gather,p.room,slot);
                p.controller.enabled=false;p.body.transform.position=position;p.seat=100+slot;p.move=Vector2.zero;
                p.notice=gather?"":("Assigned to "+SaloonStations.Name(p.room)+(p.room==0&&p.chosenRoom!=0?" — invitation unmatched or room full.":"."));
                p.botThink=gather?0:Time.time+1.2f+(p.id%5)*.4f;
                p.yaw=facing;p.pitch=0;p.poseVersion++;p.drawn=false;p.busyAction="";p.busyUntil=0;
                if(gather){p.chosenRoom=0;p.companion=null;}
            }
        }

        public void ChooseRoom(int room,string companion)=>Connection.Send(new Command{kind="room",x=room,name=companion});

        bool SpendAction(Participant p,float duration=0)
        {
            if((phase==Phase.Discussion||phase==Phase.Turns)&&rounds.TrySpend(p.id,Time.time,duration))return true;
            p.notice=phase==Phase.Discussion?"Discussion: talk and position; actions start after the timer.":"Actions are locked or your action is spent. One action per round.";
            return false;
        }

        void OnCommand(ulong id,Command cmd)
        {
            if(!Hosting||!players.TryGetValue(id,out var p))return;
            if(cmd.kind=="hello") {p.name=CleanName(cmd.name);return;}
            if(cmd.kind=="input")
            {
                if(!Finite(cmd.x)||!Finite(cmd.z)||!Finite(cmd.yaw)||!Finite(cmd.pitch))return;
                p.move=MatchActive?Vector2.zero:Vector2.ClampMagnitude(new Vector2(cmd.x,cmd.z),1);p.yaw=Mathf.Repeat(cmd.yaw,360);p.pitch=Mathf.Clamp(cmd.pitch,-75,75);p.lastInput=Time.time;return;
            }
            if(cmd.kind=="room")
            {
                if(phase!=Phase.Choosing||!p.Alive||Time.time>=rounds.Deadline)return;
                int room=(int)cmd.x;if(room<0||room>2)return;
                ulong? companion=null;
                if(!string.IsNullOrEmpty(cmd.name)&&ulong.TryParse(cmd.name,out var companionId)&&companionId!=p.id&&players.TryGetValue(companionId,out var other)&&other.Alive)companion=companionId;
                p.chosenRoom=room;p.companion=companion;p.notice="Room preference saved.";return;
            }
            if(!p.Alive||phase==Phase.Finished||Time.time-p.lastAction<.09f)return;
            p.lastAction=Time.time;
            if(cmd.kind=="interact"&&p.seat>=0&&!MatchActive){Stand(p);return;}
            if(p.busyUntil>Time.time){p.notice="Finish what you are doing first.";return;}
            switch(cmd.kind)
            {
                case "draw":
                    if(phase!=Phase.Discussion&&phase!=Phase.Turns)break;
                    p.drawn=!p.drawn;p.busyUntil=Time.time+tuning.drawSeconds;p.busyAction="draw";break;
                case "fire":Fire(p);break;
                case "rob":BeginRobbery(p);break;
                case "escape":DeclareEscape(p);break;
                case "reload":
                    if(!p.drawn||p.ammo>=tuning.cylinder)break;
                    if(!SpendAction(p,tuning.reloadSeconds))break;
                    p.busyUntil=Time.time+tuning.reloadSeconds;p.busyAction="reload";p.notice="Reloading — you are exposed.";break;
                case "interact":Interact(p);break;
                case "drink":
                    if(p.beer<=0){p.notice="Take a beer from the brass tap at the bar first.";break;}
                    if(p.hp>=tuning.health){p.notice="You are already at full health.";break;}
                    if(!SpendAction(p,tuning.drinkSeconds))break;
                    p.beer--;p.drawn=false;p.busyUntil=Time.time+tuning.drinkSeconds;p.busyAction="drink";p.notice="Drinking — weapon holstered.";break;
                case "give":Give(p);break;
                case "pass":if(SpendAction(p))p.notice="Passed. Your next action is next round.";break;
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
            if(!p.drawn||Time.time<p.nextShot)return;
            if(p.ammo<=0){p.notice="Empty cylinder — R to reload.";return;}
            if(!SpendAction(p))return;
            p.ammo--;p.shots++;p.nextShot=Time.time+tuning.shotInterval;
            var hit=Target(p,65);
            if(hit.HasValue)
            {
                var body=hit.Value.collider.GetComponentInParent<SaloonBody>();
                if(body!=null&&players.TryGetValue(body.id,out var victim)&&victim.Alive&&(!MatchActive||victim.room==p.room))
                {
                    victim.hp--;p.notice="Hit.";victim.notice="You were hit!";
                    // Damage interrupts drinking/reloading; the consumed beer is lost.
                    victim.busyUntil=0;victim.busyAction="";
                    if(!victim.Alive)
                    {
                        victim.bag=false;victim.escapeRound=0;victim.drawn=false;victim.move=Vector2.zero;victim.controller.enabled=false;victim.hitbox.enabled=false;
                        AddEvent(victim.name+" was eliminated.");
                        string result=SaloonRules.Winner(players.Values.Where(x=>x.Alive).Select(x=>x.role));
                        if(!string.IsNullOrEmpty(result))Finish(result);
                    }
                }
            }
        }

        void Interact(Participant p)
        {
            if(MatchActive)
            {
                if(phase!=Phase.Turns||p.room!=0){p.notice="Beer is available at the bar during actions.";return;}
                if(p.beer>0||beers<=0||p.beerPickupRound==rounds.Round){p.notice="Carry one beer; one pickup per round.";return;}
                if(!rounds.CanAct(p.id,Time.time)){p.notice="Your action is already spent.";return;}
                p.beer++;beers--;p.beerPickupRound=rounds.Round;p.notice="Beer ready. B to drink · G to offer.";return;
            }
            var hit=Target(p,tuning.interactionDistance);
            var point=hit?.collider.GetComponentInParent<InteractionPoint>();
            if(point==null){p.notice="Look at a chair to sit, or the brass tap to take a beer.";return;}
            if(point.kind==InteractionKind.Beer)
            {
                if(p.beer>0){p.notice="You can carry one beer. Drink or give it away first.";return;}
                if(beers<=0){p.notice="The shared beer supply is empty.";return;}
                if(!SpendAction(p))return;
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
            if(body==null||!players.TryGetValue(body.id,out var target)||!target.Alive||target.beer>0||target.busyUntil>Time.time||(MatchActive&&target.room!=p.room)){p.notice="Look at a nearby player whose hands are free.";return;}
            if(!SpendAction(p,tuning.handoffSeconds+tuning.drinkSeconds))return;
            p.beer--;p.drawn=false;p.giftTarget=target.id;p.giftSequence++;p.giftUntil=Time.time+tuning.handoffSeconds;
            p.busyAction="give";p.busyUntil=p.giftUntil;
            target.drawn=false;target.busyAction="giftDrink";target.drinkReady=p.giftUntil;target.busyUntil=p.giftUntil+tuning.drinkSeconds;
            p.notice="Beer offered to "+target.name+".";target.notice=p.name+" offered you a beer — drinking.";

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
                players=players.Values.Select(p=>new PlayerView{id=p.id.ToString(),name=p.name,x=p.body.transform.position.x,y=p.body.transform.position.y,z=p.body.transform.position.z,yaw=p.yaw,pitch=p.pitch,alive=p.Alive,drawn=p.drawn,seated=p.seat>=0,sheriff=p.role==Role.Sheriff,busy=p.busyUntil>Time.time,shots=p.shots,health=p.hp,maxHealth=tuning.health,carriedBeer=p.beer,drinking=(p.busyAction=="drink"||(p.busyAction=="giftDrink"&&Time.time>=p.drinkReady))&&p.busyUntil>Time.time,
                    drinkRemaining=(p.busyAction=="drink"||p.busyAction=="giftDrink")?Mathf.Max(0,p.busyUntil-Time.time):0,
                    drinkDuration=tuning.drinkSeconds,giftRemaining=Mathf.Max(0,p.giftUntil-Time.time),
                    giftSequence=phase==Phase.Turns&&own.room==p.room&&p.giftUntil>Time.time?p.giftSequence:0,
                    giftTarget=phase==Phase.Turns&&own.room==p.room&&p.giftUntil>Time.time?p.giftTarget.ToString():"",
                    robbing=CanObserveHeist(own,p)&&p.busyAction=="rob"&&p.busyUntil>Time.time,
                    moneyBag=CanObserveHeist(own,p)&&p.bag,escaping=CanObserveHeist(own,p)&&p.escapeRound>0,room=p.room,chosenRoom=p.chosenRoom,companion=p.companion?.ToString()??"",poseVersion=p.poseVersion,revealedRole=phase==Phase.Finished?p.role.ToString():""}).ToArray(),
                phase=phase,remaining=MatchActive?Mathf.Max(0,rounds.Deadline-Time.time):0,
                round=rounds.Round,roundLimit=rounds.Limit,
                activePlayerId=phase==Phase.Turns?rounds.Active?.ToString():"",
                activePlayerName=phase==Phase.Turns&&rounds.Active.HasValue?players[rounds.Active.Value].name:"",
                turnOrder=rounds.Order.Select(id=>id.ToString()).ToArray(),
                actionAvailable=phase==Phase.Turns&&rounds.CanAct(recipient,Time.time),outcome=outcome,ownRole=own.role.ToString(),
                ally="",
                health=own.hp,ammo=own.ammo,beers=beers,carriedBeer=own.beer,notice=own.notice,
                sandbox=Sandbox,moneyBag=own.bag,escapePending=own.escapeRound>0,
                safeProgress=phase==Phase.Turns&&own.room==0?safeProgress:-1,
                objectiveNotice=own.bag?(own.escapeRound>0?"Escape pending — hold the money at the bar.":"Money secured — escape available."):"",
                busyRemaining=Mathf.Max(0,own.busyUntil-Time.time),voiceRoom=voiceRoom
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
            var self=snapshot.players.FirstOrDefault(p=>p.id==LocalId);
            if(self!=null&&self.poseVersion!=shownPose){shownPose=self.poseVersion;yaw=self.yaw;pitch=self.pitch;}
            if(snapshot.phase!=shownPhase)
            {
                shownPhase=snapshot.phase;
                if(shownPhase==Phase.Choosing)UnlockCursor();else if(!MenuOpen)LockCursor();
            }
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
            if(View.phase!=Phase.Lobby&&View.phase!=Phase.Finished)
                return;
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
            if(Voice!=null)await Voice.Leave();
            if(this==null)return;
            await Connection.Disconnect();
            if(this==null)return;
            foreach(var p in players.Values)Destroy(p.body);players.Clear();
            foreach(var a in avatars.Values)Destroy(a.gameObject);avatars.Clear();
            View=null;phase=Phase.Lobby;Sandbox=false;helloSent=false;outcome="";MenuOpen=true;UnlockCursor();leaving=false;
        }

        public void SaveName(){PlayerPrefs.SetString("SaloonName",PlayerName);PlayerPrefs.Save();}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool VerificationFinished { get; private set; }
        public bool VerificationPassed { get; private set; }

        // Exercises actual host commands, snapshots, raycasts and timed actions in Play mode.
        public System.Collections.IEnumerator VerifyGameplay()
        {
            VerificationFinished=false;VerificationPassed=false;verifying=true;yield return null;
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_GAMEPLAY FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_CHECK "+text);}
            var host=players[Connection.LocalId];var other=players.Values.First(p=>p.bot);
            void Do(Participant p,string kind){p.lastAction=-100;OnCommand(p.id,new Command{kind=kind});}
            Check(players.Values.All(p=>p.seat>=100&&!p.controller.enabled),"all players seated with movement disabled");
            var start=host.body.transform.position;OnCommand(host.id,new Command{kind="input",x=1,z=1});Do(host,"interact");TickServer();
            Check(host.body.transform.position==start&&host.move==Vector2.zero&&host.seat>=100,"input and stand commands cannot move seated players");
            host.drawn=true;int ammo=host.ammo;Do(host,"fire");Check(host.ammo==ammo,"discussion blocks shots");
            rounds.Start(players.Keys,Time.time-1,1,30,2,1,1);phase=rounds.Phase;TickServer();Check(phase==Phase.Choosing,"discussion opens room choices");
            OnCommand(host.id,new Command{kind="room",x=1,name=other.id.ToString()});OnCommand(other.id,new Command{kind="room",x=1,name=host.id.ToString()});
            Check(host.chosenRoom==1&&host.companion==other.id,"room nomination accepted by host");
            yield return new WaitForSeconds(1.15f);Check(phase==Phase.Turns&&host.room==1&&other.room==1,"mutual companions occupy the same side room");
            var view=PrivateSnapshot(host.id);Check(view.players.First(p=>p.id==other.id.ToString()).room==1,"room roster replicated");
            Check(rounds.CanAct(host.id,Time.time)&&rounds.CanAct(other.id,Time.time),"all living players have a simultaneous action");
            OnCommand(host.id,new Command{kind="room",x=2});Check(host.chosenRoom==1,"room changes locked during actions");
            host.drawn=true;host.yaw=90;Do(host,"fire");Check(host.ammo==ammo-1,"one shot spends ammunition and action");host.nextShot=0;Do(host,"fire");Check(host.ammo==ammo-1,"second action denied");
            foreach(var p in players.Values)rounds.TrySpend(p.id,Time.time);
            TickServer();Check(phase==Phase.Regroup&&players.Values.All(p=>p.room==0&&p.seat>=100),"completed actions regroup everyone at the common table");
            yield return new WaitForSeconds(1.15f);Check(phase==Phase.Discussion&&rounds.Round==2,"regroup begins the next discussion");
            Check(host.ammo==ammo-1&&host.hp==tuning.health,"round boundaries preserve resources");
            Check(GameObject.Find("Saloon World").GetComponentsInChildren<TextMesh>().Length==0,"no floating environment labels");
            yield return VerifyHeist();
            yield return VerifyBeerGift();
            yield return VerifyReleaseChanges();
            VerificationPassed=true;VerificationFinished=true;Debug.Log("SALOON_GAMEPLAY PASS");if(!Application.isEditor)Application.Quit(0);
        }

#endif
        void AddEvent(string message){events.Add(message);if(events.Count>12)events.RemoveAt(0);Debug.Log("SALOON "+message);}
        void LockCursor(){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        void UnlockCursor(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        void OnApplicationQuit(){leaving=true;if(Connection!=null&&Connection.Manager!=null)Connection.Manager.Shutdown();}
    }
}
