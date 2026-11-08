#if UNITY_EDITOR
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyDisconnectDetails()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            void Check(bool ok,string text){if(!ok){VerificationFinished=true;Debug.LogError("SALOON_DISCONNECT_TEST FAIL "+text);throw new InvalidOperationException(text);}Debug.Log("SALOON_DISCONNECT_CHECK "+text);}
            Check(SaloonConnection.DisconnectSummary(NetworkTransport.DisconnectEvents.ProtocolTimeout,"").Contains("stopped responding"),"timeout is distinct from a closed connection");
            Check(SaloonConnection.DisconnectSummary(NetworkTransport.DisconnectEvents.ClosedByRemote,"[Disconnect Event] ClosedByRemote").Contains("host closed"),"remote close has a clear menu explanation");
            Check(SaloonConnection.DisconnectSummary(NetworkTransport.DisconnectEvents.AuthenticationFailure,"Different game version.")=="Different game version.","explicit rejection reasons remain intact");
            var obj=new GameObject("Disconnect test peer");var peer=obj.AddComponent<SaloonConnection>();peer.Initialize();
            try
            {
                peer.StartLan(false,"127.0.0.1",ushort.Parse(Port));float until=Time.realtimeSinceStartup+8;
                while(!peer.Connected&&Time.realtimeSinceStartup<until)yield return null;
                Check(peer.Connected,"real UDP client connects");
                bool lost=false;peer.LostConnection+=()=>lost=true;
                Connection.Manager.DisconnectClient(peer.LocalId,"Verification host closed this connection.");
                until=Time.realtimeSinceStartup+8;while((!lost||peer.Connected)&&Time.realtimeSinceStartup<until)yield return null;
                Check(lost&&!peer.Connected,"real host-side disconnect reaches the client");
                Check(peer.LastDisconnectDetails.Contains("Transport:")&&peer.LastDisconnectDetails.Contains("Last state:")&&peer.LastDisconnectDetails.Contains("Main-thread gap:"),"copyable report retains transport and last-state context");
                var report=peer.LastDisconnectDetails;var cleanup=peer.Disconnect();while(!cleanup.IsCompleted)yield return null;
                Check(!cleanup.IsFaulted,"test client cleanup completes");
                Check(peer.LastDisconnectDetails==report,"cleanup does not erase the captured disconnect details");
                Debug.Log("SALOON_DISCONNECT_TEST PASS");VerificationPassed=VerificationFinished=true;
            }
            finally{if(peer.Manager!=null){peer.Manager.Shutdown();Destroy(peer.Manager.gameObject);}Destroy(obj);}
        }
    }
}
#endif
