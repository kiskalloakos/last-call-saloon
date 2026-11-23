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
                if(!ok){VerificationFinished=true;throw new InvalidOperationException("Role privacy: "+message);}
                Debug.Log("SALOON_KNOWLEDGE_CHECK "+message);
            }
            // These checks exercise server rules, shots and serialized private
            // snapshots. Do not render repeated synthetic deaths and rematches.
            Connection.SnapshotReceived-=ApplySnapshot;
            try
            {
                foreach(int size in Enumerable.Range(4,5))
                foreach(var independent in new[]{Role.Renegade,Role.Swindler})
                {
                    phase=Phase.Finished;BotMatchSize=size;BotsEnabled=true;StartMatch();
                    var roster=players.Values.ToArray();
                    roster.Single(p=>SaloonRules.IsIndependent(p.role)).role=independent;
                    roster[0].room=3;foreach(var other in roster.Skip(1))other.room=0;
                    foreach(var stage in new[]{Phase.Discussion,Phase.Choosing,Phase.Turns,Phase.Aftermath,Phase.Regroup,Phase.Finished})
                    {
                        phase=stage;
                        foreach(var viewer in roster)
                        {
                            var snapshot=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(viewer.id),out _));
                            Check(snapshot.players.All(p=>(stage==Phase.Finished||SaloonRules.KnowsRole(viewer.role,players[ulong.Parse(p.id)].role))?
                                p.revealedRole==players[ulong.Parse(p.id)].role.ToString():string.IsNullOrEmpty(p.revealedRole)),"labels preserve hidden Deputies and independents in "+stage+" / "+viewer.role);
                            var expected=roster.Where(p=>viewer.role==Role.Outlaw&&p.role==Role.Outlaw).ToArray();
                            Check(snapshot.knownRoles.Length==expected.Length&&snapshot.knownRoles.All(k=>expected.Any(p=>k.id==p.id.ToString()&&k.name==p.name&&k.role==p.role.ToString())),"only Outlaws receive teammate identities");
                            Check(snapshot.players.Count(p=>p.sheriff)==(snapshot.players.Any(p=>players[ulong.Parse(p.id)].role==Role.Sheriff)?1:0),"Sheriff badge remains public");
                            if(SaloonPrivacy.Separated(stage))Check(snapshot.players.All(p=>(p.room==3)==(viewer.room==3)),"privacy preserves area separation");
                        }
                    }
                    phase=Phase.Turns;
                    foreach(var viewer in roster)
                    {
                        viewer.hp=0;viewer.deathAt=Time.time-SaloonRagdoll.ObserverDelay-.1f;
                        var snapshot=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(viewer.id),out _));
                        Check(snapshot.ownRole==viewer.role.ToString()&&snapshot.knownRoles.All(k=>viewer.role==Role.Outlaw&&k.role=="Outlaw"),"spectating cannot grant hidden role knowledge");
                        viewer.hp=tuning.health;viewer.deathAt=-1;
                    }
                    ReopenLobby();
                    Check(players.Values.All(p=>PrivateSnapshot(p.id).knownRoles.Length==0),"lobby clears role knowledge");
                    yield return null;
                }
                phase=Phase.Finished;BotMatchSize=5;StartMatch();
                var shooter=players[Connection.LocalId];var victim=players.Values.First(p=>p!=shooter);
                void Window(Role attacker,int victimHealth=20)
                {
                    ResetHeist();pendingOutcome=outcome="";duels.Clear();roomTurns.Clear();
                    foreach(var p in players.Values)
                    {p.hp=100;p.role=Role.Outlaw;p.deathAt=-1;p.chosenRoom=p.room=0;p.companion=null;p.busyUntil=0;p.busyAction="";p.botThink=Time.time+100;p.drawn=false;p.ammo=6;p.nextShot=0;p.lastAction=-100;}
                    shooter.role=attacker;victim.role=Role.Swindler;victim.hp=victimHealth;
                    if(attacker!=Role.Sheriff)players.Values.Last(p=>p!=shooter&&p!=victim).role=Role.Sheriff;
                    rounds.Start(players.Keys,Time.time,30,120,0);foreach(var id in players.Keys)rounds.SetReady(id,true);
                    rounds.Tick(Time.time,id=>players[id].Alive);phase=rounds.Phase;PositionEveryone(false);
                }
                void Shoot()
                {
                    var d=Eye(victim)-Eye(shooter);shooter.yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;shooter.pitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;
                    shooter.drawn=true;Physics.SyncTransforms();OnCommand(shooter.id,new Command{kind="fire"});
                }
                foreach(var attacker in new[]{Role.Sheriff,Role.Deputy,Role.Outlaw,Role.Renegade})
                {
                    Window(attacker);Broadcast();yield return null;
                    Shoot();bool earned=attacker==Role.Sheriff||attacker==Role.Deputy;
                    Check(!victim.Alive&&victim.personalVictory==earned,"real lethal shot awards Swindler victory only to Law-caused kills");
                    Check(phase==Phase.Turns&&pendingOutcome=="","Swindler death does not end the heist");
                    var privateView=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(victim.id),out _));
                    Check(privateView.personalVictory==earned&&players.Values.Where(p=>p!=victim).All(p=>!PrivateSnapshot(p.id).personalVictory),"earned victory is recipient-private over the wire");
                    Finish("OUTLAWS WIN — MONEY ESCAPED");
                    Check(matchResults.Single(p=>p.id==victim.id.ToString()).winner==earned,"results preserve the personal win alongside faction victory");
                    phase=Phase.Finished;StartMatch();Check(players.Values.All(p=>!p.personalVictory),"rematch clears personal victories");
                    // Let avatar resurrection destroy the previous ragdoll components
                    // before another synthetic death of the same player.
                    yield return null;
                }
                Window(Role.Sheriff,40);Broadcast();yield return null;
                Shoot();Check(victim.Alive&&!victim.personalVictory,"a nonlethal Law shot cannot award a Swindler win");
                foreach(var roles in new[]{(Role.Sheriff,Role.Deputy),(Role.Sheriff,Role.Outlaw),(Role.Outlaw,Role.Outlaw),(Role.Outlaw,Role.Deputy)})
                foreach(bool boosted in new[]{false,true})
                {
                    Window(roles.Item1,100);victim.role=roles.Item2;shooter.cigaretteBuff=boosted;Shoot();
                    var feedback=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(shooter.id),out _));
                    Check(shooter.hp==100&&victim.hp==(boosted?60:80)&&!feedback.confirmedFriendly&&
                        feedback.storyProgress.HasFlag(StoryProgress.EnemyHit)&&!feedback.players.Single(p=>p.id==shooter.id.ToString()).hitReflected,
                        "ally and enemy hits have identical damage, confirmation and story progress: "+roles+" boosted="+boosted);
                }
                foreach(var attacker in new[]{Role.Outlaw,Role.Renegade,Role.Deputy,Role.Swindler})
                {
                    Window(attacker);
                    foreach(var p in players.Values.Where(p=>p!=shooter&&p!=victim))p.role=Role.Outlaw;
                    victim.role=Role.Sheriff;var deadOutlaw=players.Values.Last(p=>p!=shooter&&p!=victim);deadOutlaw.hp=0;
                    Shoot();
                    Check(!victim.Alive&&phase==Phase.Aftermath&&pendingOutcome=="OUTLAWS WIN — SHERIFF DEAD",attacker+" killing the Sheriff immediately ends actions for Outlaws");
                    Check(players.Values.All(p=>!PrivateSnapshot(p.id).actionAvailable),"Sheriff death blocks every remaining action");
                    Finish(pendingOutcome);
                    Check(matchResults.Where(p=>p.role==Role.Outlaw).All(p=>p.winner)&&matchResults.Single(p=>p.id==deadOutlaw.id.ToString()).winner,"all Outlaws win on Sheriff death, including eliminated teammates");
                    yield return null;
                }
                Window(Role.Renegade);victim.role=Role.Sheriff;
                foreach(var p in players.Values.Where(p=>p!=shooter&&p!=victim)){p.role=Role.Outlaw;p.hp=0;}
                Shoot();Check(pendingOutcome=="RENEGADE WINS — LAST STANDING","Renegade killing Sheriff last wins alone instead of awarding Outlaws");
                Finish(pendingOutcome);Check(matchResults.Single(p=>p.id==shooter.id.ToString()).winner&&matchResults.Where(p=>p.role==Role.Outlaw).All(p=>!p.winner),"last-survivor results award only the Renegade");
                yield return null;
                Window(Role.Deputy);victim.role=Role.Sheriff;foreach(var p in players.Values.Where(p=>p!=shooter&&p!=victim))p.role=Role.Outlaw;TakeDamage(victim,Vector3.back,SaloonHealth.Maximum);
                Check(pendingOutcome=="OUTLAWS WIN — SHERIFF DEAD"&&phase==Phase.Aftermath,"Sheriff death resolves even outside the shot-specific code");
                ReopenLobby();
            }
            finally { Connection.SnapshotReceived+=ApplySnapshot; }
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
                Check(players.Values.Count(p=>p.role==Role.Deputy)==1&&players.Values.Count(p=>p.role==Role.Outlaw)==2&&players.Values.Count(p=>SaloonRules.IsIndependent(p.role))==1,"full five-role deck preserved");
                foreach(var p in players.Values)
                {
                    var wire=SaloonSnapshotWire.Encode(PrivateSnapshot(p.id),out _);
                    var snapshot=SaloonSnapshotWire.Decode(wire);
                    Check(snapshot.players.Count(x=>x.sheriff)==1,"every recipient receives the public Sheriff");
                    Check(snapshot.ownRole==p.role.ToString()&&snapshot.players.All(x=>SaloonRules.KnowsRole(p.role,players[ulong.Parse(x.id)].role)?x.revealedRole==players[ulong.Parse(x.id)].role.ToString():string.IsNullOrEmpty(x.revealedRole)),"own role remains accurate and hidden roles private");
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
