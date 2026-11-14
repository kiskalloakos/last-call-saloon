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
        public SaloonGraphics Graphics { get; private set; }
        public bool GraphicsMenuOpen;
        public SaloonBlackjackTable BlackjackTable { get; private set; }
        public SaloonCoinTable CoinTable { get; private set; }
        public SaloonBeerTaps BeerTaps { get; private set; }
        public SaloonSafeHighlight SafeHighlight { get; private set; }
        public bool AtBeerBar=>View?.phase==Phase.Turns&&Self?.room==0&&Self.alive;
        public bool PlayingCoin=>View?.phase==Phase.Turns&&Self?.room==1&&View.duel?.game=="coin"&&(View.duel.stage=="choosing"||View.duel.stage=="flipping");
        public bool PlayingTableGame=>PlayingBlackjack||PlayingCoin;
        public bool OutsideForSmoke=>Self!=null&&Self.room==3&&SaloonPrivacy.Separated(View.phase);
        public bool PlayingBlackjack=>View?.phase==Phase.Turns&&View.duel?.game=="blackjack"&&(View.duel.stage=="dealing"||View.duel.stage=="blackjack"||View.duel.stage=="redealing");
        public Snapshot View { get; private set; }
        public Camera ViewCamera { get; private set; }
        public string PlayerName = "Stranger", Address = "127.0.0.1", Port = "7777", JoinCode = "";
        public bool MenuOpen = true;
        public float Sensitivity = 2;
        public bool Sandbox { get; private set; }
        public bool BotsEnabled;
        public int BotMatchSize=5;
        public bool Hosting => Connection.Hosting;
        public bool InRoom => Connection.Connected;
        public int PlayerCount => players.Count;
        public int HumanCount=>players.Values.Count(p=>!p.bot);
        public int PlannedMatchSize=>BotsEnabled?Mathf.Max(HumanCount,Mathf.Clamp(BotMatchSize,4,5)):HumanCount;
        public bool CanStartMatch=>Hosting&&(phase==Phase.Lobby||phase==Phase.Finished)&&players.Values.All(p=>p.disconnectedUntil==0)&&SaloonRules.SupportedMatchSize(PlannedMatchSize);
        public string LobbyStartStatus=>PlannedMatchSize>5?"Rules for 6–8 players will be added later. Start with 4 or 5.":!BotsEnabled&&HumanCount<4?"Need 4 or 5 human players, or turn bots on.":BotsEnabled?"Missing seats will be filled with bots when the host starts.":"No bots. Start with 4 or 5 human players.";
        public Phase CurrentPhase => phase;
        public string LocalId => Connection.LocalId.ToString();
        public PlayerView Self => View?.players?.FirstOrDefault(p => p.id == LocalId);
        public bool ShowRole;
        [NonSerialized] public bool AutomatedInput;
        public string InteractionHint = "";
        [NonSerialized] public int RoomChooserRenderFrames;
        public string VoiceRoom => View?.voiceRoom ?? "";
        readonly Dictionary<ulong, Participant> players = new Dictionary<ulong, Participant>();
        readonly Dictionary<string, SaloonAvatar> avatars = new Dictionary<string, SaloonAvatar>();
        readonly List<string> events = new List<string>();
        Phase phase;
        readonly SaloonRounds rounds = new SaloonRounds();
        float nextBroadcast, nextInput, yaw, pitch, helloAt;
        int beers;
        string outcome = "", voiceRoom = "", pendingOutcome="";
        bool helloSent,leaving;
        Phase shownPhase;int shownPose=-1;
        bool MatchActive=>phase!=Phase.Lobby&&phase!=Phase.Finished;
        Vector3 cameraVelocity;
        public IReadOnlyList<string> Events => events;

        class Participant
        {
            public float disconnectedUntil;
            public bool rematchVote;
            public ulong id;
            public string name, notice="", busyAction="",voiceId="";
            public string safeReport="";
            public bool bartenderUsed;
            public string bartenderQuestion="",bartenderAnswer="";
            public int inspectedProgress=-1;
            public StoryProgress storyProgress;
            public readonly HashSet<ulong> storySeenDead=new HashSet<ulong>();
            public readonly List<string> evidence=new List<string>();
            public readonly Dictionary<ulong,float> safeWatch=new Dictionary<ulong,float>();
            public Role role;
            public GameObject body;
            public CharacterController controller;
            public CapsuleCollider hitbox,solid;
            public SphereCollider head;
            public Vector2 move;
            public float yaw,pitch,bodyYaw,lastInput,lastAction,nextShot,busyUntil,vertical;
            public float lastStance=-100;
            public int reloads,headshots,confirmedHealth,confirmedMaxHealth;
            public bool confirmedKill,confirmedFriendly;public float confirmedAt=-100;
            public int hp,ammo,beer,shots,hits,seat=-1,room,chosenRoom,poseVersion,beerPickupRound=-1;
            public float hitAt=-10;public Vector3 hitDirection;
            public bool gatherStation;
            public ulong? companion;
            public int planVersion;
            public bool planSelected;
            public bool fireAfterDraw;
            public bool cigaretteBuff;
            public bool drawn,bot,bag,crouched,crouchHeld;
            public int escapeRound;
            public float botThink,drinkReady,giftUntil;
            public float botAimAt=-1;public Vector3 botAim;
            public float botRoamAt;public Vector3 botMoveWorld;
            public float deathAt=-1;public Vector3 deathDirection;
            public int giftSequence;public ulong giftTarget;
            public int beerHealing,drinkHealing,beerStation=-1;
            public float pourStarted,beerChoiceUntil,botPourSeconds,pourFlow=1,pourTick;
            public int puffs;public float smokeAt=-100;
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
            Graphics=gameObject.AddComponent<SaloonGraphics>();
            // Query-only hitboxes; movement collision remains on CharacterController.
            for(int layer=0;layer<32;layer++)Physics.IgnoreLayerCollision(8,layer,true);
            SaloonWorld.Build();SaloonArt.Apply();SaloonRoomWindows.Apply();SaloonRoomSigns.Apply();SaloonSmokingArea.Apply();
            BlackjackTable=gameObject.AddComponent<SaloonBlackjackTable>();BlackjackTable.Initialize(this);
            CoinTable=gameObject.AddComponent<SaloonCoinTable>();CoinTable.Initialize(this);
            BeerTaps=gameObject.AddComponent<SaloonBeerTaps>();BeerTaps.Initialize(this);
            SafeHighlight=gameObject.AddComponent<SaloonSafeHighlight>();SafeHighlight.Initialize(this);
            gameObject.AddComponent<SaloonBartenderNPC>();
            PlayerName=PlayerPrefs.GetString("SaloonName","Stranger");
            var existing=Camera.main;
            ViewCamera=existing!=null?existing:new GameObject("First person camera").AddComponent<Camera>();
            ViewCamera.tag="MainCamera";ViewCamera.nearClipPlane=.035f;ViewCamera.farClipPlane=140;
            ViewCamera.fieldOfView=78;ViewCamera.clearFlags=CameraClearFlags.SolidColor;ViewCamera.backgroundColor=new Color(.44f,.32f,.20f);
            ViewCamera.transform.position=new Vector3(0,1.75f,-5.6f);ViewCamera.transform.rotation=Quaternion.Euler(0,0,0);
            if(ViewCamera.GetComponent<AudioListener>()==null)ViewCamera.gameObject.AddComponent<AudioListener>();
            Connection=gameObject.AddComponent<SaloonConnection>();Connection.Initialize();
            Connection.AutoReconnect=true;Connection.CanResume=id=>players.TryGetValue(id,out var p)&&p.disconnectedUntil>Time.realtimeSinceStartup;
            Connection.Joined+=OnJoined;Connection.Left+=OnLeft;Connection.CommandReceived+=OnCommand;Connection.SnapshotReceived+=ApplySnapshot;
            Connection.LostConnection+=()=>{if(!leaving&&this!=null&&Application.isPlaying)LeaveWithReason(Connection.Status);};
            Voice=gameObject.AddComponent<SaloonVoice>();Voice.Game=this;
            gameObject.AddComponent<SaloonHUD>().Game=this;
            gameObject.AddComponent<SaloonPosseAudio>().Game=this;
            gameObject.AddComponent<SaloonAutomation>().Game=this;
            gameObject.AddComponent<SaloonCloudVerification>().Game=this;
            AddEvent("Prototype ready. Four or five players per match, optional bots; lobby capacity eight.");
        }

        void OnJoined(ulong id)
        {
            if(!Hosting)return;
            if(players.TryGetValue(id,out var returning))
            {
                if(returning.disconnectedUntil>0&&returning.disconnectedUntil<=Time.realtimeSinceStartup)
                {Connection.ClosePlayer(id);return;}
                returning.disconnectedUntil=0;returning.move=Vector2.zero;returning.poseVersion++;
                AddEvent(returning.name+" reconnected.");Broadcast();return;
            }
            if(MatchActive){Connection.ClosePlayer(id);Connection.ForgetPlayer(id);return;}
            var replacement=players.Values.Where(p=>p.bot).OrderByDescending(p=>p.id).FirstOrDefault();
            if(replacement!=null)RemoveBot(replacement.id);
            if(players.Count>=SaloonRules.LobbyCapacity)
            {
                Connection.ClosePlayer(id);Connection.ForgetPlayer(id);
                return;
            }
            var p=CreateParticipant(id,"Stranger "+(players.Count+1),false);
            players.Add(id,p);AddEvent(p.name+" entered the saloon.");
        }

        Participant CreateParticipant(ulong id,string name,bool bot)
        {
            int index=players.Count;
            var body=new GameObject("Player collision "+id);body.transform.position=SaloonWorld.Spawns[index%SaloonWorld.Spawns.Length];
            var identity=body.AddComponent<SaloonBody>();identity.id=id;
            var controller=body.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.30f;controller.center=new Vector3(0,.9f,0);controller.stepOffset=.25f;controller.skinWidth=.025f;
            var target=new GameObject("Player hitbox");target.layer=8;target.transform.SetParent(body.transform,false);
            var hitbox=target.AddComponent<CapsuleCollider>();hitbox.height=1.8f;hitbox.radius=.32f;hitbox.center=Vector3.up*.9f;
            var headObject=new GameObject("Player head hitbox");headObject.layer=8;headObject.transform.SetParent(body.transform,false);
            var head=headObject.AddComponent<SphereCollider>();
            var solidObject=new GameObject("Seated player collision");solidObject.layer=SaloonMovement.SolidLayer;solidObject.transform.SetParent(body.transform,false);
            var solid=solidObject.AddComponent<CapsuleCollider>();solid.radius=.30f;
            var player=new Participant{id=id,name=name,body=body,controller=controller,hitbox=hitbox,head=head,solid=solid,hp=tuning.health,ammo=tuning.cylinder,bot=bot};
            UpdatePlayerColliders(player);return player;
        }

        void OnLeft(ulong id)
        {
            if(!Hosting)return;
            if(players.TryGetValue(id,out var p))
            {
                if(p.disconnectedUntil>0)return;
                p.disconnectedUntil=Time.realtimeSinceStartup+SaloonConnection.ReconnectGraceSeconds;
                p.move=Vector2.zero;p.rematchVote=false;
                AddEvent(p.name+" disconnected — place reserved for 20 seconds.");Broadcast();
            }
        }

        public void HostLan(bool sandbox)
        {
            if(sandbox){StartSolo();return;}
            if(Connection.Busy||InRoom)return;
            if(!ushort.TryParse(Port,out var port)||port==0){Connection.Status="Use a port from 1 to 65535.";return;}
            Sandbox=false;voiceRoom="saloon-"+Guid.NewGuid().ToString("N");
            Connection.StartLan(true,"127.0.0.1",port);
            if(!Connection.Hosting)return;
            MenuOpen=true;SyncPointer();
        }

        public void StartSolo()
        {
            if(Connection.Busy||InRoom)return;
            Sandbox=true;voiceRoom="saloon-"+Guid.NewGuid().ToString("N");
            Connection.StartSolo();if(!Connection.Solo)return;
            MenuOpen=false;SyncPointer();StartPractice();
        }

        public void StartPractice()
        {
            if(!Hosting||(phase!=Phase.Lobby&&phase!=Phase.Finished))return;
            BotsEnabled=true;StartMatch();
        }

        void RemoveBot(ulong id){Destroy(players[id].body);players.Remove(id);}
        void TrimBots(int target)
        {
            foreach(var id in players.Values.Where(p=>p.bot).OrderByDescending(p=>p.id).Select(p=>p.id).ToArray())
            {if(players.Count<=target)break;RemoveBot(id);}
        }
        void FillBots(int target)
        {
            TrimBots(target);
            ulong next=10000;
            while(players.Count<target)
            {
                while(players.ContainsKey(next))next++;
                players.Add(next,CreateParticipant(next,"Practice player "+(next-9999),true));
                next++;
            }
        }

        void ReopenLobby()
        {
            phase=Phase.Lobby;Connection.Accepting=true;outcome="";pendingOutcome="";ResetHeist();int index=0;
            foreach(var p in players.Values)
            {
                p.rematchVote=false;
                p.cigaretteBuff=false;
                p.beerChoiceUntil=0;p.pourStarted=0;p.beerHealing=0;p.beer=0;
                p.reloads=p.headshots=0;p.confirmedAt=-100;p.hits=0;p.hitAt=-10;p.hitDirection=Vector3.zero;p.role=Role.Unknown;p.hp=tuning.health;p.ammo=tuning.cylinder;p.seat=-1;p.drawn=false;p.busyUntil=0;p.busyAction="";p.move=Vector2.zero;p.deathAt=-1;p.notice="";p.poseVersion++;
                p.crouched=p.crouchHeld=false;UpdateCrouch(p);
                p.controller.enabled=false;p.body.transform.position=SaloonWorld.Spawns[index++%SaloonWorld.Spawns.Length];p.controller.enabled=true;p.hitbox.enabled=true;
            }
        }
        public void SetBotsEnabled(bool enabled)
        {
            if((InRoom&&!Hosting)||MatchActive)return;
            BotsEnabled=enabled;if(!Hosting)return;
            if(!enabled)TrimBots(HumanCount);
            if(phase==Phase.Finished)ReopenLobby();
            Sandbox=players.Values.Any(p=>p.bot);Broadcast();
        }
        public void SetBotMatchSize(int count)
        {
            if(!SaloonRules.SupportedMatchSize(count)||(InRoom&&!Hosting)||MatchActive)return;
            BotMatchSize=count;if(!Hosting)return;
            if(phase==Phase.Finished)ReopenLobby();
            TrimBots(Mathf.Max(count,HumanCount));Sandbox=players.Values.Any(p=>p.bot);Broadcast();
        }
        public void ClearPracticeTargets(){SetBotsEnabled(false);}

        public void JoinLan()
        {
            if(!ushort.TryParse(Port,out var port)||port==0){Connection.Status="Use a valid port.";return;}
            Sandbox=false;Connection.StartLan(false,Address.Trim(),port);MenuOpen=true;
        }

        public async void Cloud(bool host)
        {
            Sandbox=false;
            if(host)voiceRoom="saloon-"+Guid.NewGuid().ToString("N");
            await Connection.StartCloud(host,JoinCode);
            if(Connection.Connected){MenuOpen=true;SyncPointer();}
        }

        public void StartMatch()
        {
            if(!CanStartMatch)return;
            if(!BotsEnabled)TrimBots(HumanCount);
            int count=PlannedMatchSize;
            if(!SaloonRules.SupportedMatchSize(count)){Connection.Status=LobbyStartStatus;Broadcast();return;}
            if(BotsEnabled)FillBots(count);
            Sandbox=players.Values.Any(p=>p.bot);
            var deck=SaloonRules.DeckFor(players.Count);
            for(int i=deck.Length-1;i>0;i--){int j=UnityEngine.Random.Range(0,i+1);(deck[i],deck[j])=(deck[j],deck[i]);}
            int index=0;
            foreach(var p in players.Values)
            {
                p.rematchVote=false;
                p.cigaretteBuff=false;
                p.beerChoiceUntil=0;p.pourStarted=0;p.beerHealing=0;p.beer=0;
                p.reloads=p.headshots=0;p.confirmedAt=-100;p.hits=0;p.hitAt=-10;p.hitDirection=Vector3.zero;p.role=deck[index];p.hp=tuning.health;p.ammo=tuning.cylinder;p.beer=0;p.beerPickupRound=-1;p.drawn=false;p.seat=-1;p.busyUntil=0;p.busyAction="";p.move=Vector2.zero;p.vertical=0;p.shots=0;p.notice="";p.nextShot=0;p.lastAction=-100;
                p.crouched=p.crouchHeld=false;UpdateCrouch(p);
                p.deathAt=-1;p.deathDirection=Vector3.zero;p.puffs=0;p.smokeAt=-100;
                p.controller.enabled=false;p.body.transform.position=SaloonWorld.Spawns[index++%SaloonWorld.Spawns.Length];p.controller.enabled=true;p.hitbox.enabled=true;
            }
            var order=players.Keys.OrderBy(_=>UnityEngine.Random.value).ToArray();
            // Give the opening role reveal its own time before the first discussion deadline.
            rounds.Start(order,Time.time+RoleIntroDuration,tuning.discussionSeconds,tuning.turnSeconds,tuning.roundLimit,tuning.choosingSeconds,tuning.regroupSeconds);
            ResetHeist();beers=tuning.sharedBeers;outcome="";phase=rounds.Phase;PositionEveryone(true);
            Connection.Accepting=false;AddEvent("Roles dealt privately. Round 1: discussion. One action each after discussion.");
            Broadcast();
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(BartenderMenuOpen)CloseBartender();
                else if(GraphicsMenuOpen){Graphics.Revert();GraphicsMenuOpen=false;}
                else MenuOpen=!MenuOpen;
                SyncPointer();
            }
            if(GraphicsMenuOpen)MenuOpen=true;
            ShowRole=!RoleIntroActive&&!MenuOpen&&Self?.alive==true&&Input.GetKey(KeyCode.Tab);
            if(!InRoom){UnlockCursor();return;}
            if(BartenderMenuOpen&&(!AtBeerBar||!CanQuestionBartender||!NearBartender))CloseBartender();
            SyncPointer();UpdateSpectator();
            if(!helloSent&&Time.unscaledTime>=helloAt){Connection.Send(new Command{kind="hello",name=PlayerName});helloAt=Time.unscaledTime+1;}
            if(Self!=null&&Self.name==CleanName(PlayerName))helloSent=true;
            if(!RoleIntroActive&&!MenuOpen&&!ShowRole&&Self?.alive==true&&Input.GetKeyDown(KeyCode.I))ToggleBartender();
            if(!RoleIntroActive&&!MenuOpen&&!ShowRole&&!BartenderMenuVisible&&Self?.alive==true)
            {
                if(View?.phase==Phase.Discussion&&Input.GetMouseButtonDown(1))ToggleRoomPlanner();
                if(View?.phase==Phase.Discussion&&Input.GetKeyDown(KeyCode.Space)){ToggleReady();SyncPointer();}
                if(!PlayingTableGame&&Input.GetKeyDown(KeyCode.F))Act("stance");
                if(AtBeerBar)
                {
                    if(Input.GetKeyDown(KeyCode.E))Act("interact");
                    if(Input.GetKeyUp(KeyCode.E))Act("stopPour");
                    if(Input.GetKeyDown(KeyCode.B))Act("drink");
                    if(Input.GetKeyDown(KeyCode.G))Act("give");
                    if(Input.GetKeyDown(KeyCode.Space))Act(Self.carriedBeer>0?"leaveBeer":"pass");
                }
                if(!NeedsPointer&&Cursor.lockState!=CursorLockMode.Locked&&Input.GetMouseButtonDown(0))LockCursor();
                if(Cursor.lockState==CursorLockMode.Locked)
                {
                    yaw+=Input.GetAxis("Mouse X")*Sensitivity;
                    pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*Sensitivity,-75,75);
                    if(OutsideForSmoke)
                    {if(Input.GetMouseButtonDown(0))Act("smoke");}
                    else if(!PlayingTableGame)
                    {
                        if(Input.GetKeyDown(KeyCode.Q))Act("draw");
                        if(Input.GetKeyDown(KeyCode.R))Act("reload");
                        if(!AtBeerBar&&Input.GetKeyDown(KeyCode.E))Act("interact");
                        if(!AtBeerBar&&Input.GetKeyDown(KeyCode.B))Act("drink");
                        if(!AtBeerBar&&Input.GetKeyDown(KeyCode.G))Act("give");
                        if(!AtBeerBar&&View.phase!=Phase.Discussion&&Input.GetKeyDown(KeyCode.Space))Act("pass");
                        if(Input.GetKeyDown(KeyCode.H))Act("rob");
                        if(Input.GetKeyDown(KeyCode.X))Act("escape");
                        if(Input.GetKeyDown(KeyCode.C))Act("inspect");
                        if(Input.GetMouseButtonDown(0)&&!BeerTaps.HandleClick())Act("fire");
                    }
                }
            }
            if(!AutomatedInput&&Time.unscaledTime>=nextInput)
            {
                bool movementInput=!RoleIntroActive&&!MenuOpen&&!ShowRole&&Cursor.lockState==CursorLockMode.Locked;
                var move=movementInput?new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical")):Vector2.zero;
                Connection.Send(new Command{kind="input",x=move.x,z=move.y,crouch=movementInput&&(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl)),yaw=yaw,pitch=pitch,poseVersion=Self?.poseVersion??-1});nextInput=Time.unscaledTime+.033f;
            }
            if(!RoleIntroActive)BeerTaps.HandlePourInput();
            if(Hosting)TickServer();
            UpdateCamera();SaloonSmokingArea.ApplyCamera(ViewCamera,externalCamera?SaloonSmokingArea.Contains(externalPosition):CameraPlayer?.room==3);
            UpdateInteractionHint();
        }

        public void Act(string kind)=>Connection.Send(new Command{kind=kind});

        void TickServer()
        {
            foreach(var expired in players.Values.Where(p=>p.disconnectedUntil>0&&Time.realtimeSinceStartup>=p.disconnectedUntil).ToArray())
            {
                Destroy(expired.body);players.Remove(expired.id);Connection.ForgetPlayer(expired.id);
                AddEvent(expired.name+" did not reconnect in time.");
                if(MatchActive)Finish("MATCH ABORTED — RECONNECT WINDOW EXPIRED");
            }
            foreach(var person in players.Values)TickSafeWork(person);
            TickEvidence();
            foreach(var p in players.Values)
            {
                UpdateCrouch(p);UpdatePlayerColliders(p);
                if(!p.Alive)continue;
                TickBeer(p);
                if(p.bot&&!verifying)TickBotMovement(p);
                if(p.busyUntil>0&&Time.time>=p.busyUntil)
                {
                    if(p.busyAction=="reload"){p.ammo=tuning.cylinder;p.notice="Reload complete · "+p.ammo+" rounds ready.";}
                    if(p.busyAction=="drink"||p.busyAction=="giftDrink")
                    {int before=p.hp;p.hp=SaloonHealth.AfterBeer(p.hp,p.drinkHealing);p.notice="Beer finished · +"+(p.hp-before)+"% health.";}
                    if(p.busyAction=="rob")CompleteRobbery(p);
                    bool fire=p.busyAction=="draw"&&p.fireAfterDraw;p.fireAfterDraw=false;
                    p.busyUntil=0;p.busyAction="";
                    if(fire&&CanFinishDrawShot(p))Fire(p);
                }
                p.bodyYaw=SaloonCharacterMotion.FollowBody(p.bodyYaw,p.yaw,CanWalk(p)&&p.move.sqrMagnitude>.01f,Mathf.Min(Time.deltaTime,.05f));
                if(p.seat<0)
                {
                    if(Time.time-p.lastInput>.3f)p.move=Vector2.zero;
                    var world=Quaternion.Euler(0,p.yaw,0)*new Vector3(p.move.x,0,p.move.y);
                    if(!p.controller.enabled)continue;
                    p.vertical=p.controller.isGrounded?-1:p.vertical-20*Time.deltaTime;
                    float speed=p.busyUntil>Time.time?tuning.walkSpeed*.35f:tuning.walkSpeed;
                    if(p.crouched)speed*=SaloonMovement.CrouchSpeed;
                    if(!CanWalk(p))world=Vector3.zero;
                    p.controller.Move((world*speed+Vector3.up*p.vertical)*Mathf.Min(Time.deltaTime,.05f));
                    if(phase==Phase.Turns)
                    {
                        var bounded=SaloonMovement.ClampToRoom(p.body.transform.position,p.room);
                        if((bounded-p.body.transform.position).sqrMagnitude>.0001f)
                        {p.controller.enabled=false;p.body.transform.position=bounded;p.controller.enabled=true;p.notice="Stay in your assigned room until the round ends.";}
                    }
                }
            }
            if(MatchActive)
            {
                TickDuels();
                if(!verifying)TickPracticeBots();
                if(phase==Phase.Finished){Broadcast();return;}
                var before=phase;rounds.Tick(Time.time,id=>players.TryGetValue(id,out var p)&&p.Alive,id=>players[id].room!=3);phase=rounds.Phase;
                if(before!=phase)
                {
                    if((phase==Phase.Regroup||phase==Phase.Finished)&&!string.IsNullOrEmpty(pendingOutcome))Finish(pendingOutcome);
                    else if(phase==Phase.Turns){PositionEveryone(false);AddEvent("Room assignments locked. One action each.");}
                    else if(phase==Phase.Aftermath)ResolveRoundEnding();
                    else if(phase==Phase.Regroup)
                    {
                        PositionEveryone(true);AddEvent("Regrouping at the table.");
                        int stage=SaloonPosse.Stage(rounds.Round,phase);
                        if(stage>0)AddEvent(SaloonPosse.Dialogue(stage));
                    }
                    else if(phase==Phase.Choosing)AddEvent("Choose a room and invite a companion. First to ready reserves the seat.");
                    else AddEvent("Round "+rounds.Round+": seated discussion.");
                }

            }
            TickBartenderArrival();
            if(Time.unscaledTime>=nextBroadcast){Broadcast();nextBroadcast=Time.unscaledTime+.1f;}
        }

        void PositionEveryone(bool gather)
        {
            var assignments=rounds.AssignRooms(players.Values.Where(p=>p.Alive).Select(p=>p.id),id=>rounds.IsReady(id)?players[id].chosenRoom:0,RoomCapacities(),preventSolo:true);
            var slots=new int[4];int index=0;
            foreach(var p in players.Values)
            {
                int homeSlot=index++;
                // Unused drinks stay behind when this round regroups. Never carry a mug into the next room.
                if(gather){p.beer=0;p.beerHealing=0;p.beerChoiceUntil=0;p.pourStarted=0;}
                if(!p.Alive)continue;
                int previousRoom=p.room;
                p.room=gather?0:assignments[p.id];if(!gather&&p.room!=0)p.escapeRound=0;
                if(!gather&&p.room==3&&previousRoom!=3)p.cigaretteBuff=true;
                while(!gather&&players.Values.Any(dead=>!dead.Alive&&!ObserverReady(dead)&&!dead.gatherStation&&dead.room==p.room&&dead.seat==100+slots[p.room]))slots[p.room]++;
                int slot=gather?homeSlot:slots[p.room]++;p.gatherStation=gather;p.beerStation=!gather&&p.room==0?slot:-1;
                Vector3 position=SaloonStations.Position(gather,p.room,slot);float facing=SaloonStations.Facing(gather,p.room,slot);
                p.crouched=p.crouchHeld=false;UpdateCrouch(p);
                p.controller.enabled=false;p.body.transform.position=position;p.seat=!gather&&p.room==3?-1:100+slot;p.move=Vector2.zero;
                p.notice=gather?"":(p.room==0&&p.chosenRoom!=0?"Returned to Bar: your side room was unmatched or unavailable.":"Assigned to "+SaloonStations.Name(p.room)+".");
                p.botMoveWorld=Vector3.zero;p.botRoamAt=gather?0:Time.time+.2f+(p.id%5)*.12f;
                p.botAimAt=-1;p.botThink=gather?0:Time.time+1.2f+(p.id%5)*.4f;
                p.lastStance=-100;p.yaw=p.bodyYaw=facing;p.pitch=0;p.poseVersion++;UpdatePlayerColliders(p);p.drawn=false;p.fireAfterDraw=false;p.busyAction="";p.busyUntil=0;
                if(p.seat<0)p.controller.enabled=true;
                if(gather){p.chosenRoom=0;p.companion=null;p.planSelected=false;}
            }
            SetupDuels(gather);
        }

        public void ToggleReady()=>EditReady();
        public void ChooseRoom(int room,string companion)=>EditRoom(room,companion);

        bool CanFinishDrawShot(Participant p)=>p.Alive&&p.drawn&&phase==Phase.Turns&&rounds.CanAct(p.id,Time.time)&&DuelAllows(p);
        string ActionBlockReason(Participant p)
        {
            if(!p.Alive)return "You are eliminated. Spectate until the next match.";
            if(phase==Phase.Discussion)return "Discussion: lock your plan before the timer ends. Everyone locking in starts early.";
            if(phase!=Phase.Turns)return phase==Phase.Aftermath?"Round complete. Waiting for the final effects.":phase==Phase.Regroup?"Regrouping. Your next action is next round.":"Actions open during room actions.";
            if(p.room==3)return "Outside is for talking and smoking. No saloon actions.";
            if(p.busyUntil>Time.time)return p.busyAction=="pour"?"Pouring · E to stop near the fill line.":p.busyAction=="reload"?"Reloading — wait for the cylinder to finish.":p.busyAction=="drink"||p.busyAction=="giftDrink"?"Drinking — wait for the sip to finish.":"Finish your current action first.";
            if(rounds.HasSpent(p.id))return "Action spent. One action per round; you can still move and dodge.";
            if(Time.time>=rounds.Deadline)return "The action deadline has passed.";
            if(!DuelAllows(p))
            {
                if(roomTurns.ContainsKey(p.room))return "Wait for your turn at this table.";
                if(duels.TryGetValue(p.room,out var duel))
                    return !duel.revealed?"Finish the room minigame before acting.":Time.time<duel.combatAt?"Get ready. You can stand and dodge before weapons unlock.":"Your opponent has the opening action. Wait for their shot or pass.";
            }
            return "";
        }
        bool SpendAction(Participant p,float duration=0)
        {
            string reason=ActionBlockReason(p);
            if(reason!=""){p.notice=reason;return false;}
            if(rounds.TrySpend(p.id,Time.time,duration)){AdvanceRoomTurn(p.room);return true;}
            p.notice="Action unavailable. Wait for the next round.";return false;
        }

        void OnCommand(ulong id,Command cmd)
        {
            if(!Hosting||!players.TryGetValue(id,out var p))return;
            if(p.disconnectedUntil>0)return;
            if(cmd.kind=="lobby"&&phase==Phase.Finished){ReopenLobby();Broadcast();return;}
            if(cmd.kind=="rematch"&&phase==Phase.Finished)
            {
                p.rematchVote=!p.rematchVote;
                if(players.Values.Where(x=>!x.bot).All(x=>x.rematchVote&&x.disconnectedUntil==0)&&CanStartMatch)StartMatch();
                Broadcast();return;
            }
            if(cmd.kind=="hello") {p.name=CleanName(cmd.name);return;}
            if(cmd.kind=="voice")
            {
                string voiceIdentity=cmd.name??"";
                if(voiceIdentity.Length<=128&&voiceIdentity.All(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_'))p.voiceId=voiceIdentity;
                return;
            }
            if(cmd.kind=="input")
            {
                if(!Finite(cmd.x)||!Finite(cmd.z)||!Finite(cmd.yaw)||!Finite(cmd.pitch)||cmd.poseVersion!=p.poseVersion)return;
                p.crouchHeld=cmd.crouch;
                p.move=CanWalk(p)?Vector2.ClampMagnitude(new Vector2(cmd.x,cmd.z),1):Vector2.zero;p.yaw=Mathf.Repeat(cmd.yaw,360);p.pitch=Mathf.Clamp(cmd.pitch,-75,75);p.lastInput=Time.time;return;
            }
            if(cmd.kind=="ready")
            {
                if(phase==Phase.Discussion&&p.Alive&&AcceptPlanCommand(p,cmd))TryReady(p,cmd.x>0);
                return;
            }
            if(cmd.kind=="room"||cmd.kind=="planReady")
            {
                if((phase!=Phase.Choosing&&phase!=Phase.Discussion)||!p.Alive||(phase==Phase.Choosing&&Time.time>=rounds.Deadline))return;
                int room=(int)cmd.x;if(room<0||room>3||!AcceptPlanCommand(p,cmd))return;
                ulong? companion=null;
                if(!string.IsNullOrEmpty(cmd.name)&&ulong.TryParse(cmd.name,out var companionId)&&companionId!=p.id&&players.TryGetValue(companionId,out var other)&&other.Alive)companion=companionId;
                if(!CanSelectPlan(p,room,companion))return;
                SetRoomPlan(p,room,companion);
                if(Sandbox&&(!verifying||cmd.kind=="planReady"))PlanPracticeBots();
                if(cmd.kind=="planReady")TryReady(p,true);
                else p.notice=companion.HasValue?"Invitation sent. Waiting for acceptance.":"Room selected. Lock your choice when ready.";
                return;
            }
            if(cmd.kind=="acceptInvite"||cmd.kind=="joinInvite"){AcceptRoomInvitation(p,cmd,cmd.kind=="joinInvite");return;}
            if(p.room==3&&SaloonPrivacy.Separated(phase))
            {if(cmd.kind=="smoke")TrySmoke(p);else p.notice="Left-click to smoke. No weapons or saloon actions outside.";return;}
            if(cmd.kind=="coin"){ClaimCoin(p,(int)cmd.x);return;}
            if(cmd.kind=="blackjack"){PlayBlackjack(p,cmd.name);return;}
            if(cmd.kind=="pourFlow"){if(p.Alive&&p.busyAction=="pour"&&Finite(cmd.x))p.pourFlow=Mathf.Clamp(cmd.x,.35f,1.5f);return;}
            if(TryBeerCommand(p,cmd.kind))return;
            // Standing is free movement input, independent of the shot/action debounce.
            if(cmd.kind=="stance")
            {
                if(p.Alive&&phase!=Phase.Finished&&Time.time-p.lastStance>=.15f)
                {p.lastStance=Time.time;ToggleStance(p);}
                return;
            }
            // Buffer one trigger press during any legal draw; revalidate when the draw finishes.
            if(cmd.kind=="fire"&&p.Alive&&p.drawn&&p.busyAction=="draw"&&p.busyUntil>Time.time&&CanFinishDrawShot(p))
            {p.fireAfterDraw=true;return;}
            if(!p.Alive||phase==Phase.Finished||Time.time-p.lastAction<.09f)return;
            p.lastAction=Time.time;
            if(cmd.kind=="interact"&&p.seat>=0&&!MatchActive){Stand(p);return;}
            if(p.busyUntil>Time.time){p.notice=ActionBlockReason(p);return;}
            switch(cmd.kind)
            {
                case "draw":
                    if(phase!=Phase.Discussion&&phase!=Phase.Turns)break;
                    if(phase==Phase.Turns&&!DuelAllows(p)){p.notice="Wait for your opening.";break;}
                    p.drawn=!p.drawn;p.busyUntil=Time.time+tuning.drawSeconds;p.busyAction="draw";break;
                case "fire":Fire(p);break;
                case "rob":BeginRobbery(p);break;
                case "escape":DeclareEscape(p);break;
                case "inspect":InspectSafe(p);break;
                case "bartender":QuestionBartender(p,cmd);break;
                case "reload":
                    if(!p.drawn){p.notice="Draw your revolver with Q before reloading.";break;}
                    if(p.ammo>=tuning.cylinder){p.notice="Cylinder already full. Reloading is not needed.";break;}
                    if(!SpendAction(p,tuning.reloadSeconds))break;
                    p.reloads++;p.busyUntil=Time.time+tuning.reloadSeconds;p.busyAction="reload";p.notice="Reloading — you are exposed.";break;
                case "interact":Interact(p);break;
                case "drink":
                    DrinkBeer(p);break;
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

        Vector3 Eye(Participant p)=>p.body.transform.position+Vector3.up*SaloonMovement.EyeHeight(p.seat>=0,p.crouched);
        Vector3 Forward(Participant p)=>Quaternion.Euler(p.pitch,p.yaw,0)*Vector3.forward;
        RaycastHit? Target(Participant p,float distance)
        {
            Physics.SyncTransforms();
            var hits=Physics.RaycastAll(Eye(p),Forward(p),distance,~((1<<SaloonRagdoll.Layer)|(1<<SaloonMovement.SolidLayer)),QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                var body=hit.collider.GetComponentInParent<SaloonBody>();
                if(body!=null&&(body.id==p.id||(players.TryGetValue(body.id,out var player)&&(!player.Alive||hit.collider==player.controller))))continue;
                return hit;
            }
            return null;
        }

        void Fire(Participant p)
        {
            if(p.room==3&&SaloonPrivacy.Separated(phase))return;
            if(!p.drawn){p.notice="Revolver holstered. Press Q to draw before shooting.";return;}
            if(Time.time<p.nextShot){p.notice="Wait for the revolver to recover before firing.";return;}
            if(p.ammo<=0){p.notice="Empty cylinder — R to reload.";return;}
            if(!SpendAction(p))return;
            p.ammo--;p.shots++;NoteBartenderShot(p);p.nextShot=Time.time+tuning.shotInterval;p.notice="";
            int damage=p.cigaretteBuff?SaloonHealth.CigaretteDamage:SaloonHealth.RevolverDamage;
            p.cigaretteBuff=false;
            var hit=Target(p,65);
            if(hit.HasValue)
            {
                var body=hit.Value.collider.GetComponentInParent<SaloonBody>();
                if(body!=null&&players.TryGetValue(body.id,out var victim)&&victim.Alive&&(!MatchActive||victim.room==p.room))
                {
                    if(hit.Value.collider!=victim.head)return;
                    bool friendly=SaloonRules.Allies(p.role,victim.role);
                    TakeDamage(victim,Forward(p),damage);
                    if(!friendly)p.storyProgress|=StoryProgress.EnemyHit;
                    p.headshots++;p.confirmedAt=Time.time;p.confirmedHealth=victim.hp;p.confirmedMaxHealth=tuning.health;p.confirmedKill=!victim.Alive;p.confirmedFriendly=friendly;
                    victim.notice="";
                    if(friendly)TakeDamage(p,-Forward(p));
                    // Resolve the outcome after both losses, including a double elimination.
                    if(!victim.Alive||!p.Alive)
                    {
                        string result=SaloonRules.Winner(players.Values.Where(x=>x.Alive).Select(x=>x.role));
                        if(!string.IsNullOrEmpty(result))QueueFinish(result);
                    }
                }
            }
        }

        bool ObserverReady(Participant p)=>!p.Alive&&p.deathAt>=0&&Time.time-p.deathAt>=SaloonRagdoll.ObserverDelay;
        void TakeDamage(Participant p,Vector3 direction=default,int damage=SaloonHealth.RevolverDamage)
        {
            if(!p.Alive)return;
            p.hp=SaloonHealth.AfterDamage(p.hp,damage);p.hits++;p.hitAt=Time.time;p.hitDirection=direction.sqrMagnitude>.01f?direction.normalized:Vector3.back;
            bool keepDrinking=p.Alive&&(p.busyAction=="drink"||p.busyAction=="giftDrink"||p.busyAction=="pour");
            if(!keepDrinking){p.busyUntil=0;p.busyAction="";}
            if(p.Alive)return;
            p.cigaretteBuff=false;p.beer=0;p.beerHealing=0;p.beerChoiceUntil=0;
            p.deathAt=Time.time;p.deathDirection=direction;
            p.bag=false;p.escapeRound=0;p.drawn=false;p.move=Vector2.zero;p.controller.enabled=false;p.hitbox.enabled=false;p.head.enabled=false;p.solid.enabled=false;
            AddEvent(p.name+" was eliminated.");
        }

        void Interact(Participant p)
        {
            if(MatchActive)
            {
                BeginPour(p);return;
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
            if(p.busyAction=="pour"){p.notice="Stop pouring before leaving the tap.";return;}
            if(TableLocksSeat(p)){p.notice="Finish the table minigame before leaving your chair.";return;}
            var original=p.body.transform.position;
            Physics.SyncTransforms();
            var exits=new List<Vector3>();
            if(p.room==1||p.room==2)
            {
                // The side gaps fit a capsule but leave no room to dodge. Use the clear table ends.
                float center=p.room==1?-5:5,side=original.x<center?-.55f:.55f;
                exits.Add(new Vector3(center+side,.08f,-2.4f));
                exits.Add(new Vector3(center+side,.08f,-5.1f));
            }
            foreach(float distance in new[]{.9f,1.15f,1.4f})
            foreach(var offset in new[]{Vector3.back,Vector3.right,Vector3.left,Vector3.forward,new Vector3(1,0,-1).normalized,new Vector3(-1,0,-1).normalized,new Vector3(1,0,1).normalized,new Vector3(-1,0,1).normalized})
                exits.Add(original+Quaternion.Euler(0,p.yaw,0)*offset*distance);
            foreach(var candidate in exits)
            {
                var pos=candidate;pos.y=.08f;
                if(phase==Phase.Turns&&!SaloonMovement.InRoom(pos,p.room))continue;
                var collisions=Physics.OverlapCapsule(pos+Vector3.up*.4f,pos+Vector3.up*1.45f,.32f,~((1<<8)|(1<<SaloonRagdoll.Layer)),QueryTriggerInteraction.Ignore);
                if(collisions.Any(c=>c.GetComponentInParent<SaloonBody>()?.id!=p.id))continue;
                p.controller.enabled=false;p.body.transform.position=pos;p.controller.enabled=true;p.seat=-1;p.poseVersion++;p.move=Vector2.zero;p.vertical=0;p.notice="";UpdatePlayerColliders(p);return;
            }
            p.notice="There is no room to stand — someone is blocking your chair.";
        }

        void Give(Participant p)
        {
            if(p.beer<=0){p.notice="You have no beer to offer.";return;}
            var hit=Target(p,tuning.interactionDistance);var body=hit?.collider.GetComponentInParent<SaloonBody>();
            if(body==null||!players.TryGetValue(body.id,out var target)||!target.Alive||target.beer>0||target.busyUntil>Time.time||(MatchActive&&target.room!=p.room)){p.notice="Look at a nearby player whose hands are free.";return;}
            if(!CommitBeerUse(p,tuning.handoffSeconds+tuning.drinkSeconds))return;
            target.drinkHealing=PrepareBeerHealing(target,p.beerHealing);p.beerHealing=0;
            p.beer--;p.drawn=false;p.giftTarget=target.id;p.giftSequence++;p.giftUntil=Time.time+tuning.handoffSeconds;
            p.busyAction="give";p.busyUntil=p.giftUntil;
            target.drawn=false;target.busyAction="giftDrink";target.drinkReady=p.giftUntil;target.busyUntil=p.giftUntil+tuning.drinkSeconds;
            p.notice="Beer offered to "+target.name+".";target.notice=p.name+" offered you a beer — drinking.";

        }

        void Finish(string result)
        {
            pendingOutcome="";
            foreach(var p in players.Values)p.rematchVote=false;
            phase=Phase.Finished;outcome=result;Connection.Accepting=true;AddEvent(result);Broadcast();
            Debug.Log("SALOON_RESULT "+result);
        }

        void QueueFinish(string result)
        {
            if(!string.IsNullOrEmpty(pendingOutcome))return;
            pendingOutcome=result;rounds.BeginAftermath(Time.time);phase=Phase.Aftermath;Broadcast();
        }

        public Snapshot PrivateSnapshot(ulong recipient)
        {
            if(!players.TryGetValue(recipient,out var own))return null;
            ObserveStoryProgress(own);
            return new Snapshot
            {
                players=players.Values.Where(p=>SaloonPrivacy.ShareActivity(phase,own.room,p.room)).Select(p=>new PlayerView{id=p.id.ToString(),name=p.name,voiceId=p.voiceId,x=p.body.transform.position.x,y=p.body.transform.position.y,z=p.body.transform.position.z,yaw=p.yaw,pitch=p.pitch,bodyYaw=p.bodyYaw,alive=p.Alive,planSelected=p.planSelected,ready=rounds.IsReady(p.id),drawn=p.drawn,seated=p.seat>=0,crouched=p.crouched,sheriff=p.role==Role.Sheriff,busy=p.busyUntil>Time.time&&(p.busyAction!="rob"||own.id==p.id),shots=p.shots,reloads=p.reloads,reloading=p.busyAction=="reload"&&p.busyUntil>Time.time,reloadRemaining=Mathf.Max(0,p.busyUntil-Time.time),reloadDuration=tuning.reloadSeconds,health=p.hp,maxHealth=tuning.health,carriedBeer=p.beer,beerHealing=p.beerHealing,beerStation=p.beerStation,pouring=p.busyAction=="pour",pourFlow=p.pourFlow,pourElapsed=p.busyAction=="pour"?Mathf.Clamp(Time.time-p.pourStarted,0,SaloonBeerPour.MaxSeconds):0,drinking=(p.busyAction=="drink"||(p.busyAction=="giftDrink"&&Time.time>=p.drinkReady))&&p.busyUntil>Time.time,
                    hits=p.hits,hitAge=p.hits>0?Mathf.Max(0,Time.time-p.hitAt):99,hitX=p.hitDirection.x,hitY=p.hitDirection.y,hitZ=p.hitDirection.z,
                    observer=ObserverReady(p),bot=p.bot,disconnected=p.disconnectedUntil>0,rematchVote=p.rematchVote,deathAge=!p.Alive&&p.deathAt>=0?Time.time-p.deathAt:0,deathX=p.deathDirection.x,deathY=p.deathDirection.y,deathZ=p.deathDirection.z,
                    drinkRemaining=(p.busyAction=="drink"||p.busyAction=="giftDrink")?Mathf.Max(0,p.busyUntil-Time.time):0,
                    drinkDuration=tuning.drinkSeconds,giftRemaining=Mathf.Max(0,p.giftUntil-Time.time),
                    giftSequence=phase==Phase.Turns&&own.room==p.room&&p.giftUntil>Time.time?p.giftSequence:0,
                    giftTarget=phase==Phase.Turns&&own.room==p.room&&p.giftUntil>Time.time?p.giftTarget.ToString():"",
                    robbing=own.id==p.id&&p.busyAction=="rob"&&p.busyUntil>Time.time,
                    moneyBag=own.id==p.id&&SaloonRules.CanRob(p.role)&&p.bag,escaping=own.id==p.id&&SaloonRules.CanRob(p.role)&&p.escapeRound>0,room=p.room,chosenRoom=own.id==p.id||phase==Phase.Discussion||phase==Phase.Choosing?p.chosenRoom:-1,companion=own.id==p.id||phase==Phase.Discussion||phase==Phase.Choosing?p.companion?.ToString()??"":"",poseVersion=p.poseVersion,planVersion=own.id==p.id?p.planVersion:0,
                    puffs=p.room==3&&SaloonPrivacy.Separated(phase)?p.puffs:0,smokeAge=p.room==3&&SaloonPrivacy.Separated(phase)&&p.puffs>0?Time.time-p.smokeAt:100,revealedRole=phase==Phase.Finished||SaloonRules.KnowsRole(own.role,p.role)?p.role.ToString():""}).ToArray(),
                knownRoles=players.Values.Where(p=>SaloonRules.KnowsRole(own.role,p.role)).Select(p=>new KnownRole{id=p.id.ToString(),name=p.name,role=p.role.ToString()}).ToArray(),
                invitations=InvitationsFor(own),
                ownId=recipient.ToString(),rematchVotes=players.Values.Count(p=>!p.bot&&p.rematchVote),rematchRequired=HumanCount,ownRematchVote=own.rematchVote,
                reconnectNotice=string.Join(" · ",players.Values.Where(p=>p.disconnectedUntil>0).Select(p=>p.name+" reconnecting ("+Mathf.CeilToInt(p.disconnectedUntil-Time.realtimeSinceStartup)+"s)")),
                phase=phase,remaining=MatchActive?Mathf.Max(0,rounds.Deadline-Time.time):0,
                round=rounds.Round,roundLimit=rounds.Limit,
                actionsRemaining=phase==Phase.Turns?players.Values.Count(p=>p.Alive&&p.room!=3&&!rounds.HasSpent(p.id)):0,
                actionsInProgress=phase==Phase.Turns?players.Values.Count(p=>p.Alive&&p.room!=3&&rounds.HasSpent(p.id)&&(p.busyUntil>Time.time||p.beerChoiceUntil>Time.time)):0,
                actionBlockReason=ActionBlockReason(own),
                hitSequence=own.headshots,confirmedHitAge=Time.time-own.confirmedAt,confirmedHealth=own.confirmedHealth,confirmedMaxHealth=own.confirmedMaxHealth,confirmedKill=own.confirmedKill,confirmedFriendly=own.confirmedFriendly,
                posseStage=SaloonPosse.Stage(rounds.Round,phase),posseRoundsRemaining=SaloonPosse.RoundsRemaining(rounds.Round,phase),
                activePlayerId=phase==Phase.Turns?rounds.Active?.ToString():"",
                activePlayerName=phase==Phase.Turns&&rounds.Active.HasValue?players[rounds.Active.Value].name:"",
                turnOrder=rounds.Order.Select(id=>id.ToString()).ToArray(),
                actionAvailable=own.room!=3&&own.Alive&&phase==Phase.Turns&&rounds.CanAct(recipient,Time.time)&&DuelAllows(own),outcome=outcome,ownRole=own.role.ToString(),duel=PrivateDuel(own),coinTable=SaloonPrivacy.Separated(phase)&&own.room==3?null:PublicCoinTable(),blackjackTable=SaloonPrivacy.Separated(phase)&&own.room==3?null:PublicBlackjackTable(),roomTurn=PrivateRoomTurn(own),
                ally="",
                health=own.hp,ammo=own.ammo,beers=beers,carriedBeer=own.beer,beerHealing=own.beerHealing,pouring=own.busyAction=="pour",pourElapsed=own.busyAction=="pour"?Mathf.Clamp(Time.time-own.pourStarted,0,SaloonBeerPour.MaxSeconds):0,beerChoiceRemaining=Mathf.Max(0,own.beerChoiceUntil-Time.time),notice=own.notice,cigaretteBuff=own.cigaretteBuff,
                evidence=own.evidence.ToArray(),storyProgress=own.storyProgress,
                sandbox=Sandbox,moneyBag=SaloonRules.CanRob(own.role)&&own.bag,escapePending=SaloonRules.CanRob(own.role)&&own.escapeRound>0,
                outlawTeamMoneyBag=SaloonRules.CanRob(own.role)&&players.Values.Any(p=>SaloonRules.CanRob(p.role)&&p.Alive&&p.bag),
                outlawTeamEscapePending=SaloonRules.CanRob(own.role)&&players.Values.Any(p=>SaloonRules.CanRob(p.role)&&p.Alive&&p.bag&&p.escapeRound>0),
                botsEnabled=BotsEnabled,botMatchSize=BotMatchSize,lobbyCapacity=SaloonRules.LobbyCapacity,roomCapacities=own.room==3&&SaloonPrivacy.Separated(phase)?new[]{5,2,2,2}:RoomCapacities(),plannedRoomFull=PlanFull(own),
                bartenderArrived=bartenderArrived,bartenderArrivalRemaining=bartenderArrived?Mathf.Max(0,bartenderArrivedAt+6-Time.time):0,
                bartenderMood=!bartenderArrived||own.room==3&&SaloonPrivacy.Separated(phase)?-1:BartenderMood,
                bartenderBarShots=!bartenderArrived||own.room==3&&SaloonPrivacy.Separated(phase)?-1:bartenderBarShots,
                bartenderUsed=SaloonRules.CanAskBartender(own.role)&&own.bartenderUsed,
                bartenderQuestion=SaloonRules.CanAskBartender(own.role)?own.bartenderQuestion:"",bartenderAnswer=SaloonRules.CanAskBartender(own.role)?own.bartenderAnswer:"",
                safeProgress=SaloonRules.CanRob(own.role)?safeProgress:own.role==Role.Sheriff?own.inspectedProgress:-1,safeReport=own.role==Role.Sheriff?own.safeReport:"",
                objectiveNotice=own.room==3&&SaloonPrivacy.Separated(phase)?"":SaloonRules.CanRob(own.role)&&own.bag?(own.escapeRound>0?"Escape pending — hold the money at the bar.":rounds.HasSpent(own.id)?"Money secured — X next round to declare escape.":"Money secured — X during room actions to declare escape."):"",
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
            UpdateRoleIntro(snapshot);
            View=snapshot;Voice?.ValidateArea();
            var self=snapshot.players.FirstOrDefault(p=>p.id==LocalId);
            SyncPlanDraft(snapshot,self);
            ApplyImpactFeedback(self);
            if(self!=null&&self.poseVersion!=shownPose){shownPose=self.poseVersion;yaw=self.yaw;pitch=self.pitch;}
            if(snapshot.phase!=shownPhase)
            {
                shownPhase=snapshot.phase;
                if(shownPhase==Phase.Lobby)MenuOpen=true;
                if(shownPhase==Phase.Finished)MenuOpen=false;
                if(shownPhase==Phase.Choosing||shownPhase==Phase.Discussion)MenuOpen=false;
                RoomPlannerOpen=shownPhase==Phase.Discussion;
                Debug.Log("SALOON_PHASE "+shownPhase+" host="+Hosting+" round="+snapshot.round);
            }
            BlackjackTable.Apply(snapshot,self);CoinTable.Apply(snapshot,self);BeerTaps.Apply(snapshot);
            SyncPointer();
            foreach(var p in snapshot.players)
            {
                if(!avatars.TryGetValue(p.id,out var avatar))
                {
                    avatar=new GameObject("Avatar "+p.name).AddComponent<SaloonAvatar>();
                    avatar.Initialize(this,p,Array.IndexOf(snapshot.players,p));avatars.Add(p.id,avatar);
                }
                avatar.Apply(p,p.id==ViewpointId);
            }
            foreach(var id in avatars.Keys.Where(id=>!snapshot.players.Any(p=>p.id==id)).ToArray()){avatars[id].Silence();avatars[id].gameObject.SetActive(false);Destroy(avatars[id].gameObject);avatars.Remove(id);}
        }

        void UpdateCamera()
        {
            if(externalCamera){ViewCamera.transform.SetPositionAndRotation(externalPosition,externalRotation);return;}
            if(UpdateDeathCamera())return;
            var self=CameraPlayer;if(self==null)return;
            var target=new Vector3(self.x,self.y+SaloonMovement.EyeHeight(self.seated,self.crouched),self.z);
            if(cameraCut||(ViewCamera.transform.position-target).sqrMagnitude>9)ViewCamera.transform.position=target;
            else ViewCamera.transform.position=Vector3.SmoothDamp(ViewCamera.transform.position,target,ref cameraVelocity,.045f);
            var rotation=Quaternion.Euler(IsSpectating?self.pitch:pitch,IsSpectating?self.yaw:yaw,0)*(IsSpectating?Quaternion.identity:CameraImpact());
            ViewCamera.transform.rotation=IsSpectating&&!cameraCut?Quaternion.Slerp(ViewCamera.transform.rotation,rotation,1-Mathf.Exp(-Time.deltaTime*20)):rotation;
            cameraCut=false;
            ViewCamera.fieldOfView=78;
        }

        void UpdateInteractionHint()
        {
            InteractionHint="";if(Self==null||!Self.alive)return;
            if(OutsideForSmoke){InteractionHint="Left-click · Take a drag     V · Talk / T · Open mic     WASD · Stroll";return;}
            if(PlayingTableGame)return;
            if(View.phase==Phase.Turns&&NearSafe)
            {
                InteractionHint=View.ownRole=="Sheriff"?"C  /  inspect this safe":View.ownRole=="Outlaw"&&!View.moneyBag?"H  /  work this safe or collect the money":"";
                if(!string.IsNullOrEmpty(InteractionHint))return;
            }
            if(AtBeerBar&&CanQuestionBartender&&NearBartender){InteractionHint="I  /  bartender · "+(View.bartenderUsed?"read your private answer":"one private question per match · mood affects his answer");return;}
            if(AtBeerBar){InteractionHint=BeerTaps.HintForRay(new Ray(ViewCamera.transform.position,ViewCamera.transform.forward));return;}
            if(View.phase==Phase.Turns)
            {InteractionHint="";return;}
            if(View.phase!=Phase.Lobby&&View.phase!=Phase.Finished)return;
            if(Self.seated){InteractionHint="E  /  stand up";return;}
            if(Physics.Raycast(ViewCamera.transform.position,ViewCamera.transform.forward,out var hit,tuning.interactionDistance,~0,QueryTriggerInteraction.Ignore))
            {
                var point=hit.collider.GetComponentInParent<InteractionPoint>();
                if(point!=null)InteractionHint=point.kind==InteractionKind.Seat?"E  /  take a seat":"E  /  take a beer";
                if(hit.collider.GetComponentInParent<SaloonBody>()!=null&&View.carriedBeer>0)InteractionHint="G  /  offer your beer";
            }
        }

        public async void Leave()=>await LeaveAsync();
        async System.Threading.Tasks.Task LeaveAsync()
        {
            if(leaving)return;leaving=true;
            if(Voice!=null)await Voice.Leave();
            if(this==null)return;
            await Connection.Disconnect();
            if(this==null)return;
            foreach(var p in players.Values)Destroy(p.body);players.Clear();
            foreach(var a in avatars.Values)Destroy(a.gameObject);avatars.Clear();
            HealthDelta=0;HealthDeltaUntil=0;previousHp=previousShots=-1;shotKick=hitKick=HitFlash=0;ResetPlanDraft();RoomPlannerOpen=false;duels.Clear();roomTurns.Clear();pendingOutcome="";
            BlackjackTable.ClearHand();CoinTable.Clear();BeerTaps.Clear();
            spectateId=null;shownViewpoint=null;View=null;phase=Phase.Lobby;Sandbox=false;helloSent=false;outcome="";MenuOpen=true;UnlockCursor();leaving=false;
        }

        async void LeaveWithReason(string reason)
        {
            Debug.LogWarning("SALOON_DISCONNECTED "+reason);
            await LeaveAsync();
            if(this!=null)Connection.Status=reason;
        }

        public void SaveName(){PlayerPrefs.SetString("SaloonName",PlayerName);PlayerPrefs.Save();}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool VerificationFinished { get; private set; }
        public bool VerificationPassed { get; private set; }

        // Exercises actual host commands, snapshots, raycasts and timed actions in Play mode.
        public System.Collections.IEnumerator VerifyGameplay()
        {
            VerificationFinished=false;VerificationPassed=false;verifying=true;SaloonSnapshotVerification.Run();yield return null;
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_GAMEPLAY FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_CHECK "+text);}
            var host=players[Connection.LocalId];var other=players.Values.First(p=>p.bot);
            host.role=Role.Sheriff;other.role=Role.Outlaw; // Resource checks below exercise an enemy hit.
            var packed=SaloonSnapshotWire.Encode(PrivateSnapshot(host.id),out int legacyBytes);
            Check(packed.Length<legacyBytes/2,"real five-player snapshot uses less than half the old wire bytes");
            Debug.Log("SALOON_WIRE_SIZE legacy="+legacyBytes+" packed="+packed.Length);
            void Do(Participant p,string kind){p.lastAction=-100;OnCommand(p.id,new Command{kind=kind});}
            Check(players.Values.All(p=>p.seat>=100&&!p.controller.enabled),"all players seated with movement disabled");
            var start=host.body.transform.position;OnCommand(host.id,new Command{kind="input",x=1,z=1});Do(host,"interact");TickServer();
            Check(host.body.transform.position==start&&host.move==Vector2.zero&&host.seat>=100,"input and stand commands cannot move seated players");
            host.drawn=true;int ammo=host.ammo;Do(host,"fire");Check(host.ammo==ammo,"discussion blocks shots");
            rounds.Start(players.Keys,Time.time,30,30,2,1,1);phase=rounds.Phase;TickServer();Check(phase==Phase.Discussion,"discussion allows planning before its deadline");
            OnCommand(host.id,new Command{kind="room",x=1,name=other.id.ToString()});OnCommand(other.id,new Command{kind="room",x=1,name=host.id.ToString()});
            Check(host.chosenRoom==1&&host.companion==other.id,"room nomination accepted by host");
            foreach(var p in players.Values)OnCommand(p.id,new Command{kind="ready",x=1});TickServer();
            yield return new WaitForSeconds(.15f);Check(phase==Phase.Turns&&host.room==1&&other.room==1,"mutual companions occupy the same side room");
            duels.Clear(); // Legacy shared-action fixture; coin authorization is exercised separately below.
            var view=PrivateSnapshot(host.id);Check(view.players.First(p=>p.id==other.id.ToString()).room==1,"room roster replicated");
            Check(rounds.CanAct(host.id,Time.time)&&rounds.CanAct(other.id,Time.time),"all living players have a simultaneous action");
            OnCommand(host.id,new Command{kind="room",x=2});Check(host.chosenRoom==1,"room changes locked during actions");
            host.drawn=true;host.yaw=90;Do(host,"fire");Check(host.ammo==ammo-1,"one shot spends ammunition and action");host.nextShot=0;Do(host,"fire");Check(host.ammo==ammo-1,"second action denied");
            foreach(var p in players.Values)rounds.TrySpend(p.id,Time.time);
            TickServer();Check(phase==Phase.Aftermath&&host.room==1,"completed actions keep everyone in their room for aftermath");
            yield return new WaitForSeconds(3.15f);Check(phase==Phase.Regroup&&players.Values.All(p=>p.room==0&&p.seat>=100),"aftermath then regroups everyone at the common table");
            yield return new WaitForSeconds(1.15f);Check(phase==Phase.Discussion&&rounds.Round==2,"regroup begins the next discussion");
            Check(host.ammo==ammo-1&&host.hp==tuning.health,"round boundaries preserve resources");
            var world=GameObject.Find("Saloon World");var roomSigns=world.transform.Find("Minigame room signs");
            var worldLabels=world.GetComponentsInChildren<TextMesh>();
            Check(roomSigns!=null&&worldLabels.Length==2&&worldLabels.All(t=>t.transform.parent==roomSigns&&t.GetComponent<SaloonWorldText>()!=null),"only depth-tested minigame room signs appear in the environment");
            yield return VerifyHeist();
            yield return VerifyBeerGift();
            yield return VerifyReleaseChanges();
            yield return VerifySocialChanges();
            yield return VerifyPolishChanges();
            yield return VerifyPracticeAndFairTurns();
            yield return VerifyRoundCompletion();
            VerificationPassed=true;VerificationFinished=true;Debug.Log("SALOON_GAMEPLAY PASS");if(!Application.isEditor)Application.Quit(0);
        }

#endif
        void AddEvent(string message){events.Add(message);if(events.Count>12)events.RemoveAt(0);Debug.Log("SALOON "+message);}
        void LockCursor(){if(AutomatedInput)return;if(Cursor.lockState!=CursorLockMode.Locked)Cursor.lockState=CursorLockMode.Locked;if(Cursor.visible)Cursor.visible=false;}
        void UnlockCursor(){if(Cursor.lockState!=CursorLockMode.None)Cursor.lockState=CursorLockMode.None;if(!Cursor.visible)Cursor.visible=true;}
        void OnApplicationQuit(){leaving=true;if(Connection!=null&&Connection.Manager!=null)Connection.Manager.Shutdown();}
    }
}
