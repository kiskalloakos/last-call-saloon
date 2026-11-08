using System;
using System.Text;
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
        public bool Hosting => Manager != null && Manager.IsServer;
        public bool Connected => Manager != null && Manager.IsConnectedClient;
        public ulong LocalId => Manager != null ? Manager.LocalClientId : 0;
        public string Status = "Choose solo practice, LAN, or an online room.";
        public string RoomCode = "";
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
            Manager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
            transport.MaxPacketQueueSize=1024;
            Debug.Log("SALOON_BUILD version="+Application.version+" platform="+Application.platform+" protocol=state-4 utc="+DateTime.UtcNow.ToString("O"));
            Manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
                ConnectionApproval = true,
                ConnectionData=Encoding.UTF8.GetBytes("LastCall/state-4/"+Application.version),
                TickRate = 30,
                ClientConnectionBufferTimeout = 15
            };
            Manager.ConnectionApprovalCallback = (request, response) =>
            {
                bool matching=Encoding.UTF8.GetString(request.Payload)=="LastCall/state-4/"+Application.version;
                response.Approved = matching && Accepting && Manager.ConnectedClientsIds.Count < SaloonRules.LobbyCapacity;
                response.CreatePlayerObject = false;
                response.Pending = false;
                response.Reason = !matching?"Different game version. Use "+Application.version+" on both devices.":Accepting ? "The saloon lobby is full (8 players)." : "A match is in progress; join after it ends.";
            };
            Manager.OnClientConnectedCallback += id =>
            {
                RegisterMessages();
                if(id==LocalId&&receivedSnapshots==0)lastSnapshotAt=Time.unscaledTime;
                Debug.Log("SALOON_NET joined="+id+" host="+Hosting+" time="+Time.realtimeSinceStartup.ToString("F2"));
                Status = Hosting ? "LAN host is ready." : "Connected to the saloon.";
                Joined?.Invoke(id);
            };
            Manager.OnClientDisconnectCallback += id =>
            {
                // Capture this before game cleanup can replace the transport's reason.
                string reason=Manager.DisconnectReason;
                Debug.LogWarning("SALOON_NET disconnect utc="+DateTime.UtcNow.ToString("O")+" peer="+id+" host="+Hosting+" event="+Manager.DisconnectEvent+
                    " age="+SnapshotAge.ToString("F2")+" lastState="+snapshotReceiver.LastSequence+" reason="+reason);
                Left?.Invoke(id);
                if (id == LocalId || !Manager.IsListening)
                {
                    Status = string.IsNullOrEmpty(reason) ? "LAN connection lost or timed out. Check Wi-Fi/firewall and matching versions, then rejoin." : reason;
                    registered=false;
                    LostConnection?.Invoke();
                }
            };
            Manager.OnTransportFailure+=()=>Debug.LogError("SALOON_NET transport failure host="+Hosting+" reason="+Manager.DisconnectReason);
        }

        void Update()
        {
            if (Manager != null && Manager.IsListening && !registered) RegisterMessages();
            if(Manager!=null&&Manager.IsListening&&Time.unscaledTime>=nextStats)
            {
                nextStats=Time.unscaledTime+10;
                Debug.Log("SALOON_NET stats host="+Hosting+" sent="+sentSnapshots+" bytes="+sentBytes+" legacyBytes="+legacyBytes+
                    " received="+receivedSnapshots+" rxBytes="+receivedBytes+" stateAge="+SnapshotAge.ToString("F2")+" lastState="+snapshotReceiver.LastSequence);
            }
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
                    if (cmd != null) CommandReceived?.Invoke(sender, cmd);
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

        public void StartLan(bool host, string address, ushort port)
        {
            if (Busy || (Manager != null && Manager.IsListening)) return;
            Initialize();
            Accepting=true;
            ResetStateStream();
            Manager.GetComponent<UnityTransport>().SetConnectionData(address, port, "0.0.0.0");
            Status = host ? "Starting LAN host..." : "Connecting to " + address + "...";
            bool started = host ? Manager.StartHost() : Manager.StartClient();
            if (!started) Status = "Could not start the connection; check the port and try again.";
        }

        public async Task StartCloud(bool host, string code)
        {
            if (Busy || (Manager != null && Manager.IsListening)) return;
            Busy = true;
            Initialize();
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
            lastSnapshotAt=Time.unscaledTime;receivedSnapshots++;SnapshotReceived?.Invoke(snapshot);
        }

        void ResetStateStream()
        {
            // The first state can arrive before OnClientConnectedCallback. Reset before
            // starting the transport, so that callback cannot forget an already applied state.
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

        public async Task Disconnect()
        {
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
