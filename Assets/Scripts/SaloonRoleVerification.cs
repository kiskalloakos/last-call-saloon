using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public System.Collections.IEnumerator VerifyRenegadeKnowledge()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;throw new InvalidOperationException("Renegade knowledge: "+message);}
                Debug.Log("SALOON_KNOWLEDGE_CHECK "+message);
            }
            foreach(int size in new[]{4,5})
            {
                phase=Phase.Finished;BotMatchSize=size;BotsEnabled=true;StartMatch();
                var roster=players.Values.ToArray();
                roster[0].room=3;foreach(var other in roster.Skip(1))other.room=0;
                foreach(var stage in new[]{Phase.Discussion,Phase.Choosing,Phase.Turns,Phase.Aftermath,Phase.Regroup,Phase.Finished})
                {
                    phase=stage;
                    foreach(var viewer in roster)
                    {
                        var snapshot=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(viewer.id),out _));
                        bool renegade=viewer.role==Role.Renegade;
                        Check(snapshot.players.All(p=>stage==Phase.Finished||renegade?p.revealedRole==players[ulong.Parse(p.id)].role.ToString():string.IsNullOrEmpty(p.revealedRole)),"role labels obey recipient permissions in "+stage+" / "+viewer.role);
                        if(renegade)
                            Check(snapshot.knownRoles.Length==size&&snapshot.knownRoles.All(k=>players.TryGetValue(ulong.Parse(k.id),out var p)&&k.role==p.role.ToString()&&k.name==p.name),"Renegade knows the complete accurate "+size+"-player roster in "+stage);
                        else Check(snapshot.knownRoles==null||snapshot.knownRoles.Length==0,"private role roster is absent for "+viewer.role);
                        if(SaloonPrivacy.Separated(stage))
                            Check(snapshot.players.All(p=>(p.room==3)==(viewer.room==3)),"role knowledge does not reveal other-area activity");
                    }
                }
                phase=Phase.Turns;
                foreach(var viewer in roster)
                {
                    viewer.hp=0;
                    var deadSnapshot=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(viewer.id),out _));
                    Check(deadSnapshot.ownRole==viewer.role.ToString(),"elimination preserves the recipient's own role");
                    if(viewer.role==Role.Renegade)
                        Check(deadSnapshot.knownRoles.Length==size,"Renegade retains role knowledge while spectating");
                    else
                        Check((deadSnapshot.knownRoles==null||deadSnapshot.knownRoles.Length==0)&&deadSnapshot.players.All(p=>string.IsNullOrEmpty(p.revealedRole)),"eliminated "+viewer.role+" cannot receive hidden roles");
                    viewer.hp=tuning.health;
                }
                // A spectator watching the Renegade must still receive their own permissions.
                var host=players[Connection.LocalId];var renegadePlayer=roster.Single(p=>p.role==Role.Renegade);
                if(host==renegadePlayer)
                {
                    var deputy=roster.Single(p=>p.role==Role.Deputy);
                    host.role=Role.Deputy;deputy.role=Role.Renegade;renegadePlayer=deputy;
                }
                foreach(var player in roster)player.room=0;
                phase=Phase.Discussion;host.hp=0;host.deathAt=Time.time;
                yield return new WaitForSeconds(SaloonRagdoll.ObserverDelay+.1f);
                Broadcast();spectateId=renegadePlayer.id.ToString();RefreshViewpoint();
                Check(IsSpectating&&CameraPlayer.id==renegadePlayer.id.ToString(),"non-Renegade spectator can watch the Renegade");
                Check(View.ownRole==host.role.ToString()&&View.knownRoles.Length==0&&View.players.All(p=>string.IsNullOrEmpty(p.revealedRole)),"watching the Renegade never inherits role knowledge");
                Check(avatars.Values.SelectMany(a=>a.GetComponentsInChildren<TextMesh>(true)).All(t=>!t.text.Contains("\nRenegade")&&!t.text.Contains("\nDeputy")&&!t.text.Contains("\nOutlaw")),"spectator avatar labels contain no hidden roles");
                ReopenLobby();
                Check(players.Values.All(p=>PrivateSnapshot(p.id).knownRoles.Length==0),"returning to lobby clears role knowledge");
            }
            Broadcast();yield return null;
            Debug.Log("SALOON_KNOWLEDGE PASS");VerificationFinished=VerificationPassed=true;
        }

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
                    Check(snapshot.ownRole==p.role.ToString()&&snapshot.players.All(x=>p.role==Role.Renegade?x.revealedRole==players[ulong.Parse(x.id)].role.ToString():string.IsNullOrEmpty(x.revealedRole)),"own role remains accurate and hidden roles private");
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
