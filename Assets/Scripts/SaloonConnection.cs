using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Vivox;
using UnityEngine;

namespace Saloon
{
    public class SaloonConnection : MonoBehaviour
    {
        public NetworkManager Manager { get; private set; }
        public bool Solo { get; private set; }
        public bool Hosting => Solo || (Manager != null && Manager.IsServer);
        public bool Connected => Solo || (Manager != null && Manager.IsConnectedClient);
        public ulong LocalId => Solo ? NetworkManager.ServerClientId : Manager != null ? (stableLocalId ?? Manager.LocalClientId) : 0;
        public string Status = "Choose solo practice, LAN, or an online room.";
        public string RoomCode = "";
        public string LastDisconnectDetails { get; private set; }="";
        public const float ReconnectGraceSeconds=20;
        public bool AutoReconnect;
        public bool Reconnecting { get; private set; }
        public float ReconnectRemaining=>Mathf.Max(0,reconnectUntil-Time.realtimeSinceStartup);
        public Func<ulong,bool> CanResume;
        readonly Dictionary<string,ulong> identities=new Dictionary<string,ulong>();
        readonly Dictionary<ulong,ulong> peers=new Dictionary<ulong,ulong>();
        string reconnectToken=Guid.NewGuid().ToString("N"),lanAddress;
        ushort lanPort;
        bool startedAsHost,cloud;
        ulong? stableLocalId;
        int recoveryGeneration;
        float reconnectUntil;
        public void ClosePlayer(ulong id)
        {
            foreach(var networkId in peers.Where(x=>x.Value==id).Select(x=>x.Key).ToArray())Manager.DisconnectClient(networkId);
        }
        public void ForgetPlayer(ulong id)
        {
            foreach(var key in identities.Where(x=>x.Value==id).Select(x=>x.Key).ToArray())identities.Remove(key);
        }
        void SetIdentityPayload()=>Manager.NetworkConfig.ConnectionData=Encoding.UTF8.GetBytes("LastCall/state-15/"+Application.version+"/"+reconnectToken);
        bool disconnectRequested;
        float lastFrameAt;
        public bool Busy { get; private set; }
        public bool Accepting = true;
        public event Action<ulong> Joined;
        public event Action<ulong> Left;
        public event Action LostConnection;
        public event Action<ulong, Command> CommandReceived;
        public event Action<Snapshot> SnapshotReceived;
        ISession session;
        bool registered;
        readonly SaloonSnapshotWire.Receiver snapshotReceiver=new SaloonSnapshotWire.Receiver();
        uint snapshotSequence;
        float lastSnapshotAt=-1,nextStats;
        long sentBytes,legacyBytes,receivedBytes;
        int sentSnapshots,receivedSnapshots;
        public float SnapshotAge=>lastSnapshotAt<0?0:Time.unscaledTime-lastSnapshotAt;
        public bool WaitingForSnapshot=>Connected&&!Hosting&&SnapshotAge>2;
        static Task servicesInitialization,playerSignIn;

        public void Initialize()
        {
            if (Manager != null) return;
            var go = new GameObject("Saloon Network");
            lastFrameAt=Time.realtimeSinceStartup;
            Manager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
            transport.MaxPacketQueueSize=1024;
            Debug.Log("SALOON_BUILD version="+Application.version+" guid="+Application.buildGUID+" platform="+Application.platform+" protocol=state-15 utc="+DateTime.UtcNow.ToString("O"));
            Manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
                ConnectionApproval = true,
                ConnectionData=Encoding.UTF8.GetBytes("LastCall/state-15/"+Application.version),
                TickRate = 30,
                ClientConnectionBufferTimeout = 15
            };
            Manager.ConnectionApprovalCallback = (request, response) =>
            {
                string payload=Encoding.UTF8.GetString(request.Payload);
                string prefix="LastCall/state-15/"+Application.version+"/";
                string token=payload.StartsWith(prefix)?payload.Substring(prefix.Length):"";
                bool matching=Guid.TryParseExact(token,"N",out _);
                bool known=identities.TryGetValue(token,out ulong stable);
                bool resume=known&&!peers.Values.Contains(stable)&&CanResume?.Invoke(stable)==true;
                bool room=Accepting&&(!known||resume)&&identities.Count<SaloonRules.LobbyCapacity;
                response.Approved=matching&&(resume||room);
                response.CreatePlayerObject=false;response.Pending=false;
                response.Reason=!matching?"Different game version. Use "+Application.version+" on both devices.":"This place is unavailable or the reconnect window has expired.";
                if(response.Approved)
                {
                    if(!resume){stable=request.ClientNetworkId;identities[token]=stable;}
                    peers[request.ClientNetworkId]=stable;
                }
            };
            Manager.OnClientConnectedCallback += id =>
            {
                if(Solo)return;
                RegisterMessages();
                if(id==LocalId&&receivedSnapshots==0)lastSnapshotAt=Time.unscaledTime;
                Debug.Log("SALOON_NET joined="+id+" host="+Hosting+" time="+Time.realtimeSinceStartup.ToString("F2"));
                Status = Hosting ? "LAN host is ready." : "Connected to the saloon.";
                if(Hosting&&!peers.ContainsKey(id))peers[id]=id;
                if(id==Manager.LocalClientId)Reconnecting=false;
                Joined?.Invoke(peers.TryGetValue(id,out var stable)?stable:id);
            };
            Manager.OnClientDisconnectCallback += id =>
            {
                if(Solo)return;
                // Capture this before game cleanup can replace the transport's reason.
                string reason=Manager.DisconnectReason;
                Debug.LogWarning("SALOON_NET disconnect utc="+DateTime.UtcNow.ToString("O")+" peer="+id+" host="+Hosting+" event="+Manager.DisconnectEvent+
                    " age="+SnapshotAge.ToString("F2")+" lastState="+snapshotReceiver.LastSequence+" reason="+reason);
                bool localLost=id==LocalId||!Manager.IsListening;
                if(localLost&&!disconnectRequested&&string.IsNullOrEmpty(LastDisconnectDetails))
                {
                    LastDisconnectDetails="Last Call "+Application.version+" / "+Application.platform+" / build "+Application.buildGUID+
                        "\nUTC: "+DateTime.UtcNow.ToString("O")+"\nTransport: "+Manager.DisconnectEvent+"\nReason: "+reason+
                        "\nLast state: "+snapshotReceiver.LastSequence+" / age "+SnapshotAge.ToString("F2")+"s"+
                        "\nMain-thread gap: "+Mathf.Max(0,Time.realtimeSinceStartup-lastFrameAt).ToString("F2")+"s / focused "+Application.isFocused;
                    Status=DisconnectSummary(Manager.DisconnectEvent,reason);
                    Debug.LogWarning("SALOON_DISCONNECT_DETAILS "+LastDisconnectDetails);
                }
                ulong stable=peers.TryGetValue(id,out var saved)?saved:id;
                peers.Remove(id);Left?.Invoke(stable);
                if(localLost)
                {
                    registered=false;
                    if(AutoReconnect&&!disconnectRequested&&!startedAsHost&&receivedSnapshots>0)
                    {if(!Reconnecting)_=Recover();}
                    else if(!Reconnecting)LostConnection?.Invoke();
                }
            };
            Manager.OnTransportFailure+=()=>Debug.LogError("SALOON_NET transport failure host="+Hosting+" reason="+Manager.DisconnectReason);
        }

        void Update()
        {
            float now=Time.realtimeSinceStartup;
            if(Manager!=null&&Manager.IsListening&&now-lastFrameAt>2)
                Debug.LogWarning("SALOON_FRAME_GAP seconds="+(now-lastFrameAt).ToString("F2")+" host="+Hosting+" focused="+Application.isFocused+" stateAge="+SnapshotAge.ToString("F2"));
            lastFrameAt=now;
            if (Manager != null && Manager.IsListening && !registered) RegisterMessages();
            if(Manager!=null&&Manager.IsListening&&Time.unscaledTime>=nextStats)
            {
                nextStats=Time.unscaledTime+10;
                Debug.Log("SALOON_NET stats host="+Hosting+" sent="+sentSnapshots+" bytes="+sentBytes+" legacyBytes="+legacyBytes+
                    " received="+receivedSnapshots+" rxBytes="+receivedBytes+" stateAge="+SnapshotAge.ToString("F2")+" lastState="+snapshotReceiver.LastSequence);
            }
        }

        internal static string DisconnectSummary(NetworkTransport.DisconnectEvents cause,string reason)
        {
            if(!string.IsNullOrEmpty(reason)&&!reason.StartsWith("[Disconnect Event]"))return reason;
            if(cause==NetworkTransport.DisconnectEvents.ProtocolTimeout)return "Disconnected: the host stopped responding.";
            if(cause==NetworkTransport.DisconnectEvents.ClosedByRemote)return "Disconnected: the host closed this connection.";
            if(cause==NetworkTransport.DisconnectEvents.MaxConnectionAttempts)return "Could not reach the host. Check its address and port.";
            return "The connection ended. Copy disconnect details below for the exact reason.";
        }

        void RegisterMessages()
        {
            if (registered || Manager.CustomMessagingManager == null) return;
            Manager.CustomMessagingManager.RegisterNamedMessageHandler("saloon.command", (sender, reader) =>
            {
                if (!Hosting || reader.Length > 2048) return;
                try
                {
                    reader.ReadValueSafe(out string json);
                    var cmd = JsonUtility.FromJson<Command>(json);
                    if (cmd != null) CommandReceived?.Invoke(peers.TryGetValue(sender,out var stable)?stable:sender, cmd);
                }
                catch (Exception e) { Debug.LogWarning("Ignored malformed command: " + e.Message); }
            });
            Manager.CustomMessagingManager.RegisterNamedMessageHandler("saloon.snapshot",ReceiveSnapshotMessage);
            registered = true;
        }

        internal void ReceiveSnapshotMessage(ulong sender,FastBufferReader reader)
        {
            // NGO already read its named-message hash. Length includes that prefix;
            // only the unread bytes are our header plus this chunk's payload.
            int remaining=reader.Length-reader.Position;
            if(sender!=NetworkManager.ServerClientId||Hosting||remaining<SaloonSnapshotWire.HeaderBytes||remaining>SaloonSnapshotWire.ChunkBytes+SaloonSnapshotWire.HeaderBytes)return;
            try
            {
                reader.ReadValueSafe(out uint sequence);reader.ReadValueSafe(out ushort part);reader.ReadValueSafe(out ushort count);reader.ReadValueSafe(out int total);
                var payload=new byte[reader.Length-reader.Position];reader.ReadBytesSafe(ref payload,payload.Length);
                ReceiveState(sequence,part,count,total,payload);
            }
            catch(Exception e){Debug.LogWarning("Ignored malformed snapshot: "+e.Message);}
        }

        public void StartSolo()
        {
            if(Busy||Connected||(Manager!=null&&Manager.IsListening))return;
            Initialize();ResetStateStream();
            // Use the same host commands and serialized snapshots, without opening a socket.
            Solo=true;Accepting=false;Status="Solo practice is ready.";
            Joined?.Invoke(LocalId);
        }

        public void StartLan(bool host, string address, ushort port)
        {
            if (Busy || Solo || (Manager != null && Manager.IsListening)) return;
            Initialize();
            cloud=false;startedAsHost=host;lanAddress=address;lanPort=port;SetIdentityPayload();
            Accepting=true;
            ResetStateStream();
            Manager.GetComponent<UnityTransport>().SetConnectionData(address, port, "0.0.0.0");
            Status = host ? "Starting LAN host..." : "Connecting to " + address + "...";
            bool started = host ? Manager.StartHost() : Manager.StartClient();
            if (!started) Status = "Could not start the connection; check the port and try again.";
        }

        public async Task StartCloud(bool host, string code)
        {
            if (Busy || Solo || (Manager != null && Manager.IsListening)) return;
            Busy = true;
            Initialize();cloud=true;startedAsHost=host;SetIdentityPayload();
            Accepting=true;
            ResetStateStream();
            try
            {
                if (string.IsNullOrEmpty(Application.cloudProjectId))
                    throw new InvalidOperationException("Link this project in Unity: Edit > Project Settings > Services; enable Authentication, Relay/Lobby and Vivox in its dashboard first.");
                Status = "Signing in to Unity Gaming Services...";
                await EnsureServices();
                if (host)
                {
                    var options = new SessionOptions { MaxPlayers = SaloonRules.LobbyCapacity, IsPrivate = true, Name = "Saloon playtest" }.WithRelayNetwork();
                    session = await MultiplayerService.Instance.CreateSessionAsync(options);
                }
                else session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant());
                RoomCode = session.Code;
                Status = "Online room " + RoomCode;
            }
            catch (Exception e)
            {
                Status = "Online setup: " + e.Message;
                Debug.LogWarning(Status);
                if (Manager.IsListening) Manager.Shutdown();
                registered = false;
            }
            finally { Busy = false; }
        }

        public static async Task EnsureServices()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                if(servicesInitialization==null||servicesInitialization.IsFaulted||servicesInitialization.IsCanceled)
                {
                    // Public routing identifiers only. UAS issues access tokens; no signing key is shipped.
                    var options=new InitializationOptions().SetVivoxCredentials(
                        "https://unity.vivox.com/appconfig/79718-last_-75603","mtu1xp.vivox.com","79718-last_-75603");
                    servicesInitialization=UnityServices.InitializeAsync(options);
                }
                await servicesInitialization;
            }
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                if(playerSignIn==null||playerSignIn.IsFaulted||playerSignIn.IsCanceled)
                {
                    // Separate editor/build processes must NOT share an anonymous player ID.
                    AuthenticationService.Instance.SwitchProfile("saloon-" + Guid.NewGuid().ToString("N").Substring(0, 16));
                    playerSignIn=AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                await playerSignIn;
            }
        }

        public void Send(Command command)
        {
            if (Hosting) { CommandReceived?.Invoke(LocalId, command); return; }
            if (!Connected) return;
            Write(NetworkManager.ServerClientId, "saloon.command", JsonUtility.ToJson(command),
                command.kind == "input" ? NetworkDelivery.UnreliableSequenced : NetworkDelivery.ReliableSequenced);
        }

        public void SendSnapshot(ulong target, Snapshot snapshot)
        {
            if(Solo&&target!=LocalId)return;
            if(!Solo&&target!=LocalId)
            {
                var transportPeer=peers.FirstOrDefault(x=>x.Value==target);
                if(!peers.ContainsKey(transportPeer.Key)||transportPeer.Value!=target)return;
                target=transportPeer.Key;
            }
            var packed=SaloonSnapshotWire.Encode(snapshot,out int oldBytes);uint sequence=++snapshotSequence;
            int count=(packed.Length+SaloonSnapshotWire.ChunkBytes-1)/SaloonSnapshotWire.ChunkBytes;
            if(target!=LocalId){sentSnapshots++;legacyBytes+=oldBytes;}
            for(int part=0;part<count;part++)
            {
                int offset=part*SaloonSnapshotWire.ChunkBytes,length=Math.Min(SaloonSnapshotWire.ChunkBytes,packed.Length-offset);
                if(target==LocalId)
                {
                    var local=new byte[length];Buffer.BlockCopy(packed,offset,local,0,length);
                    ReceiveState(sequence,part,count,packed.Length,local);continue;
                }
                if(Manager.CustomMessagingManager==null)return;
                using(var writer=new FastBufferWriter(length+SaloonSnapshotWire.HeaderBytes,Allocator.Temp))
                {
                    writer.WriteValueSafe(sequence);writer.WriteValueSafe((ushort)part);writer.WriteValueSafe((ushort)count);writer.WriteValueSafe(packed.Length);
                    writer.WriteBytesSafe(packed,length,offset);
                    Manager.CustomMessagingManager.SendNamedMessage("saloon.snapshot",target,writer,NetworkDelivery.Unreliable);
                    sentBytes+=writer.Length;
                }
            }
        }

        void ReceiveState(uint sequence,int part,int count,int total,byte[] payload)
        {
            var snapshot=snapshotReceiver.Accept(sequence,part,count,total,payload);
            if(!Hosting)receivedBytes+=payload.Length+SaloonSnapshotWire.HeaderBytes;
            if(snapshot==null)return;
            if(!Hosting&&ulong.TryParse(snapshot.ownId,out var own))stableLocalId=own;
            lastSnapshotAt=Time.unscaledTime;receivedSnapshots++;SnapshotReceived?.Invoke(snapshot);
        }

        void ResetStateStream()
        {
            // The first state can arrive before OnClientConnectedCallback. Reset before
            // starting the transport, so that callback cannot forget an already applied state.
            registered=false;disconnectRequested=false;LastDisconnectDetails="";lastFrameAt=Time.realtimeSinceStartup;
            snapshotReceiver.Reset();snapshotSequence=0;lastSnapshotAt=-1;
            sentBytes=legacyBytes=receivedBytes=0;sentSnapshots=receivedSnapshots=0;nextStats=Time.unscaledTime+10;
        }

        void Write(ulong target, string type, string json, NetworkDelivery delivery)
        {
            if (Manager.CustomMessagingManager == null) return;
            using (var writer = new FastBufferWriter(Encoding.Unicode.GetByteCount(json) + 16, Allocator.Temp))
            {
                writer.WriteValueSafe(json);
                Manager.CustomMessagingManager.SendNamedMessage(type, target, writer, delivery);
            }
        }

        async Task Recover()
        {
            Reconnecting=true;int generation=++recoveryGeneration;
            reconnectUntil=Time.realtimeSinceStartup+ReconnectGraceSeconds;
            string code=RoomCode;
            try
            {
                while(generation==recoveryGeneration&&ReconnectRemaining>0)
                {
                    Status="Reconnecting — your place is reserved ("+Mathf.CeilToInt(ReconnectRemaining)+"s).";
                    Manager.Shutdown();registered=false;
                    while(Manager.ShutdownInProgress&&generation==recoveryGeneration)await Task.Yield();
                    if(generation!=recoveryGeneration)return;
                    if(cloud)
                    {
                        if(session!=null){try{await session.LeaveAsync();}catch(Exception e){Debug.LogWarning(e.Message);}session=null;}
                        if(generation!=recoveryGeneration)return;
                        await StartCloud(false,code);
                    }
                    else StartLan(false,lanAddress,lanPort);
                    float attemptUntil=Time.realtimeSinceStartup+4;
                    while(generation==recoveryGeneration&&!Connected&&Time.realtimeSinceStartup<attemptUntil&&ReconnectRemaining>0)await Task.Yield();
                    if(generation!=recoveryGeneration){if(Connected)await Disconnect();return;}
                    if(Connected){Reconnecting=false;Status="Reconnected to the saloon.";return;}
                }
            }
            catch(Exception e){Debug.LogWarning("Reconnect failed: "+e.Message);}
            if(generation!=recoveryGeneration)return;
            Reconnecting=false;Status="Could not reconnect before the reserved place expired.";LostConnection?.Invoke();
        }

        public async Task Disconnect()
        {
            disconnectRequested=true;recoveryGeneration++;Reconnecting=false;Solo=false;
            stableLocalId=null;identities.Clear();peers.Clear();reconnectToken=Guid.NewGuid().ToString("N");
            try { if (session != null) await session.LeaveAsync(); }
            catch (Exception e) { Debug.LogWarning(e.Message); }
            session = null;
            if (Manager != null)
            {
                Manager.Shutdown();
                if (Manager.CustomMessagingManager != null)
                {
                    Manager.CustomMessagingManager.UnregisterNamedMessageHandler("saloon.command");
                    Manager.CustomMessagingManager.UnregisterNamedMessageHandler("saloon.snapshot");
                }
            }
            registered = false;snapshotReceiver.Reset();lastSnapshotAt=-1;
            RoomCode = "";
            Status = "Choose solo practice, LAN, or an online room.";
        }
    }
}
