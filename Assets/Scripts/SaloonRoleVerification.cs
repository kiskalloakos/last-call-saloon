using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public System.Collections.IEnumerator VerifyRoleDeals()
        {
            verifying=true;AutomatedInput=true;yield return null;
            void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Role deal: "+message);}
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-saloonPreview");
            string directory=at>=0&&at+1<args.Length?args[at+1]:null;
            if(directory!=null)Directory.CreateDirectory(directory);
            bool ownSheriff=false,otherSheriff=false;
            for(int deal=0;deal<100;deal++)
            {
                // Exercise the production shuffle, deal and private snapshot for every seat.
                phase=Phase.Finished;StartMatch();
                Check(players.Values.Count(p=>p.role==Role.Sheriff)==1,"exactly one Sheriff in every normal deal");
                Check(players.Values.Count(p=>p.role==Role.Deputy)==1&&players.Values.Count(p=>p.role==Role.Outlaw)==2&&players.Values.Count(p=>p.role==Role.Renegade)==1,"full five-role deck preserved");
                foreach(var p in players.Values)
                {
                    var wire=SaloonSnapshotWire.Encode(PrivateSnapshot(p.id),out _);
                    var snapshot=SaloonSnapshotWire.Decode(wire);
                    Check(snapshot.players.Count(x=>x.sheriff)==1,"every recipient receives the public Sheriff");
                    Check(snapshot.ownRole==p.role.ToString()&&snapshot.players.All(x=>string.IsNullOrEmpty(x.revealedRole)),"own role remains accurate and hidden roles private");
                }
                bool local=players[Connection.LocalId].role==Role.Sheriff;
                if((local&&!ownSheriff)||(!local&&!otherSheriff))
                {
                    yield return new WaitForEndOfFrame();
                    if(directory!=null)ScreenCapture.CaptureScreenshot(Path.Combine(directory,local?"11-you-are-sheriff.png":"12-public-sheriff.png"));
                    if(local)ownSheriff=true;else otherSheriff=true;
                    yield return null;
                }
            }
            Check(ownSheriff&&otherSheriff,"local and remote Sheriff cases exercised");
            Debug.Log("SALOON_ROLE_DEAL PASS 100 normal deals, all five recipient snapshots, own and remote Sheriff");
            yield return new WaitForSeconds(.3f);Application.Quit(0);
        }
#endif
    }
}
