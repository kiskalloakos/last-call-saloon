using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
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

        public void Initialize()
        {
            if (Manager != null) return;
            var go = new GameObject("Saloon Network");
            Manager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
            Manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
                ConnectionApproval = true,
                TickRate = 30,
                ClientConnectionBufferTimeout = 15
            };
            Manager.ConnectionApprovalCallback = (request, response) =>
            {
                response.Approved = Accepting && Manager.ConnectedClientsIds.Count < 5;
                response.CreatePlayerObject = false;
                response.Pending = false;
                response.Reason = Accepting ? "The saloon already has five players." : "A match is in progress; join after it ends.";
            };
            Manager.OnClientConnectedCallback += id =>
            {
                RegisterMessages();
                Status = Hosting ? "LAN host is ready." : "Connected to the saloon.";
                Joined?.Invoke(id);
            };
            Manager.OnClientDisconnectCallback += id =>
            {
                Left?.Invoke(id);
                if (id == LocalId || !Manager.IsListening)
                {
                    Status = string.IsNullOrEmpty(Manager.DisconnectReason) ? "Disconnected. Return to menu to reconnect." : Manager.DisconnectReason;
                    registered=false;
                    LostConnection?.Invoke();
                }
            };
        }

        void Update()
        {
            if (Manager != null && Manager.IsListening && !registered) RegisterMessages();
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
            Manager.CustomMessagingManager.RegisterNamedMessageHandler("saloon.snapshot", (sender, reader) =>
            {
                if (sender != NetworkManager.ServerClientId || Hosting || reader.Length > 32768) return;
                try
                {
                    reader.ReadValueSafe(out string json);
                    SnapshotReceived?.Invoke(JsonUtility.FromJson<Snapshot>(json));
                }
                catch (Exception e) { Debug.LogWarning("Ignored malformed snapshot: " + e.Message); }
            });
            registered = true;
        }

        public void StartLan(bool host, string address, ushort port)
        {
            if (Busy || (Manager != null && Manager.IsListening)) return;
            Initialize();
            Accepting=true;
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
            try
            {
                if (string.IsNullOrEmpty(Application.cloudProjectId))
                    throw new InvalidOperationException("Link this project in Unity: Edit > Project Settings > Services; enable Authentication, Relay/Lobby and Vivox in its dashboard first.");
                Status = "Signing in to Unity Gaming Services...";
                await EnsureServices();
                if (host)
                {
                    var options = new SessionOptions { MaxPlayers = 5, IsPrivate = true, Name = "Saloon playtest" }.WithRelayNetwork();
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
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                // Separate editor/build processes must NOT share an anonymous player ID.
                AuthenticationService.Instance.SwitchProfile("saloon-" + Guid.NewGuid().ToString("N").Substring(0, 16));
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
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
            if (target == LocalId) { SnapshotReceived?.Invoke(snapshot); return; }
            Write(target, "saloon.snapshot", JsonUtility.ToJson(snapshot), NetworkDelivery.ReliableFragmentedSequenced);
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
            registered = false;
            RoomCode = "";
            Status = "Choose solo practice, LAN, or an online room.";
        }
    }
}
