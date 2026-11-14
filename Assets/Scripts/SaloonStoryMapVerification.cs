#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public System.Collections.IEnumerator VerifyStoryMaps()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            int checks=0;
            void Check(bool ok,string message)
            {
                checks++;
                if(ok)return;
                VerificationFinished=true;Debug.LogError("SALOON_STORY_MAP FAIL "+message);throw new InvalidOperationException(message);
            }
            var host=players[Connection.LocalId];
            string directory=Path.Combine(Path.GetTempPath(),"LastCall-StoryMaps");Directory.CreateDirectory(directory);
            void Deal(Role role)
            {
                var deck=SaloonRules.Deck.ToList();deck.Remove(role);int i=0;
                foreach(var p in players.Values)
                {p.role=p==host?role:deck[i++];p.hp=100;p.room=p.chosenRoom=0;p.busyUntil=0;p.busyAction="";p.deathAt=-1;}
                ResetHeist();phase=Phase.Discussion;outcome="";
                rounds.Start(players.Keys,Time.time,300,300,0);roomTurns.Clear();duels.Clear();
            }
            void Actions()
            {
                rounds.Start(players.Keys,Time.time,300,300,0);
                foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>players[id].Alive);phase=Phase.Turns;roomTurns.Clear();
                host.lastAction=-100;host.busyUntil=0;host.busyAction="";
                host.controller.enabled=false;host.body.transform.position=SafeApproach;Physics.SyncTransforms();
            }
            Snapshot Snapshot()=>PrivateSnapshot(host.id);
            StoryMilestone Node(Snapshot snapshot,string title)=>SaloonStoryMap.Build(snapshot).Nodes.Single(n=>n.Title==title);
            var atlas=Resources.Load<Texture2D>("UI/StoryMilestones");
            Check(atlas!=null&&atlas.isReadable&&atlas.width==atlas.height*2,"readable illustrated atlas preserves its 4 x 2 grid");
            foreach(var role in new[]{Role.Sheriff,Role.Deputy,Role.Outlaw,Role.Renegade})
            {
                Deal(role);var initial=Snapshot();var map=SaloonStoryMap.Build(initial);
                Check(map.Completed==1&&map.Nodes.Any(n=>n.State==StoryState.Next),"fresh "+role+" map starts with role known and a next move");
                Check(map.Nodes.SelectMany(n=>n.From).All(index=>index>=0&&index<map.Nodes.Count),"valid "+role+" trail endpoints");
                ApplySnapshot(initial);roleIntroStarted=float.NegativeInfinity;ShowRole=true;yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,role+"-start.png"));yield return null;
            }
            Deal(Role.Sheriff);Actions();
            Check(Node(Snapshot(),"Guard the Bar").State==StoryState.Complete,"actual Bar arrival completes the milestone");
            InspectSafe(host);var sheriffView=Snapshot();
            Check(Node(sheriffView,"Inspect the safe").State==StoryState.Complete&&sheriffView.evidence.Length>0,"successful inspection completes the clue milestone");
            ApplySnapshot(sheriffView);ShowRole=true;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"Sheriff-progress.png"));yield return null;
            Deal(Role.Outlaw);Actions();BeginRobbery(host);
            Check(!Snapshot().storyProgress.HasFlag(StoryProgress.SafeWork),"starting safe work does not falsely complete it");
            var partner=players.Values.Single(p=>p!=host&&p.role==Role.Outlaw);
            partner.room=3;
            CompleteRobbery(host);
            Check(Node(Snapshot(),"Work the safe").State==StoryState.Complete&&!host.bag,"a finished safe job completes work but not money collection");
            var partnerView=PrivateSnapshot(partner.id);
            Check(partnerView.safeProgress==1&&Node(partnerView,"Work the safe").State==StoryState.Complete&&!partnerView.moneyBag,
                "partner in another area shares safe progress without duplicating the money");
            var partnerWire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(partnerView,out _));
            Check(partnerWire.safeProgress==1&&partnerWire.storyProgress.HasFlag(StoryProgress.SafeWork),"shared safe progress survives network serialization");
            CompleteRobbery(host);CompleteRobbery(host);
            Check(Node(Snapshot(),"Take the money").State==StoryState.Complete,"third completed job gives money and advances the map");
            partnerView=PrivateSnapshot(partner.id);
            Check(Node(partnerView,"Take the money").State==StoryState.Complete&&!partnerView.moneyBag&&partnerView.outlawTeamMoneyBag,"money milestone is shared without creating a second carrier");
            Actions();DeclareEscape(host);
            Check(Node(Snapshot(),"Declare escape").State==StoryState.Complete&&SaloonStoryMap.Build(Snapshot()).NextTitle=="Ride out alive","accepted escape advances to the response round");
            partnerView=PrivateSnapshot(partner.id);
            Check(Node(partnerView,"Declare escape").State==StoryState.Complete&&!partnerView.escapePending&&partnerView.outlawTeamEscapePending,"partner shares the escape milestone without declaring their own escape");
            Check(players.Values.Where(p=>p.role!=Role.Outlaw).All(p=>!PrivateSnapshot(p.id).outlawTeamMoneyBag&&!PrivateSnapshot(p.id).outlawTeamEscapePending),"team money and escape state stay private to Outlaws");
            var sharedWire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(partnerView,out _));
            Check(sharedWire.outlawTeamMoneyBag&&sharedWire.outlawTeamEscapePending,"shared money and escape state survive network serialization");
            var pending=Snapshot();var decoded=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(pending,out _));
            Check(decoded.storyProgress==pending.storyProgress&&decoded.moneyBag&&decoded.escapePending,"private progress survives compressed network snapshots");
            ApplySnapshot(pending);ShowRole=true;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"Outlaw-progress.png"));yield return null;
            host.escapeRound=0;
            Check(Node(Snapshot(),"Declare escape").State==StoryState.Next,"cancelled escape becomes the next step again");
            host.bag=false;
            Check(Node(Snapshot(),"Take the money").State==StoryState.Next,"losing the bag restores the recovery step");
            host.disconnectedUntil=Time.realtimeSinceStartup+5;
            Check(Snapshot().storyProgress.HasFlag(StoryProgress.SafeWork),"participant progress survives a temporary disconnect");host.disconnectedUntil=0;
            Check(PrivateSnapshot(partner.id).storyProgress.HasFlag(StoryProgress.SafeWork),"partner retains the shared safe-work milestone");
            Check(players.Values.Where(p=>p.role!=Role.Outlaw).All(p=>!PrivateSnapshot(p.id).storyProgress.HasFlag(StoryProgress.SafeWork)),"shared heist progress stays private to Outlaws");
            Deal(Role.Renegade);Actions();host.room=3;
            foreach(var p in players.Values.Where(p=>p.role==Role.Outlaw))p.hp=0;
            Check(!Snapshot().storyProgress.HasFlag(StoryProgress.OutlawsGone),"smoking separation does not reveal unseen indoor deaths");
            host.room=0;
            Check(Snapshot().storyProgress.HasFlag(StoryProgress.OutlawsGone)&&SaloonStoryMap.Build(Snapshot()).NextTitle=="Turn on the Law","observed Outlaw deaths advance the Renegade's route");
            host.room=3;
            Check(Snapshot().storyProgress.HasFlag(StoryProgress.OutlawsGone),"already observed deaths remain complete across area changes");
            host.room=0;
            ApplySnapshot(Snapshot());ShowRole=true;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"Renegade-progress.png"));yield return null;
            Deal(Role.Deputy);phase=Phase.Finished;outcome="LAW WINS — POSSE ARRIVED";
            Check(Node(Snapshot(),"Hold for the posse").State==StoryState.Complete&&Node(Snapshot(),"Stop the criminals").State!=StoryState.Complete,"alternative victory routes are not falsely both completed");
            ResetHeist();
            Check(players.Values.All(p=>p.storyProgress==StoryProgress.None&&p.storySeenDead.Count==0),"rematch resets every private milestone and observed death");
            ShowRole=false;Debug.Log("SALOON_STORY_MAP PASS "+checks+" checks; screenshots="+directory);
            VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
