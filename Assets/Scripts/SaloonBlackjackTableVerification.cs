#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        // Editor shortcut for a playable hand, using the regular host command path.
        public void StartBlackjackTablePractice()
        {
            HostLan(true);if(!Hosting)return;
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            foreach(var p in players.Values){p.role=Role.Deputy;p.chosenRoom=0;p.companion=null;}
            host.role=Role.Sheriff;peer.role=Role.Renegade;
            host.chosenRoom=peer.chosenRoom=2;host.companion=peer.id;peer.companion=host.id;
            foreach(var id in players.Keys)rounds.SetReady(id,true);
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            MenuOpen=false;AutomatedInput=false;Broadcast();
        }

        public IEnumerator VerifyTabletopBlackjack()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string message)
            {
                if(!ok){VerificationFinished=true;throw new InvalidOperationException("Tabletop blackjack: "+message);}
                Debug.Log("SALOON_TABLETOP_CHECK "+message);
            }
            yield return new WaitForSeconds(.1f);
            VerifyRoomReservations();
            VerifyBlackjackFlow();
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            foreach(var p in players.Values){p.hp=3;p.chosenRoom=0;p.companion=null;p.drawn=false;p.lastAction=-100;}
            host.chosenRoom=peer.chosenRoom=2;host.companion=peer.id;peer.companion=host.id;
            rounds.Start(players.Keys,Time.time,30,30,6);foreach(var id in players.Keys)rounds.SetReady(id,true);
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            var d=duels[2];d.dealer=peer.id;
            SaloonBlackjack Hand()
            {
                for(int i=0;i<10000;i++){var h=new SaloonBlackjack(new System.Random(i));if(!h.Complete&&h.PlayerTotal<17&&h.DealerTotal<17)return h;}
                throw new InvalidOperationException("No fixture");
            }
            // The synchronous flow checks replace many hands without a phase transition.
            // Start this visual fixture clean, as the real discussion/aftermath transition does.
            d.blackjack=Hand();BlackjackTable.ClearHand();Broadcast();MenuOpen=false;
            yield return new WaitForSeconds(1.8f);Broadcast();yield return null;
            Check(BlackjackTable.Visible&&BlackjackTable.CardCount==4&&BlackjackTable.HiddenCount==1,"four physical cards dealt with one hidden hole card (visible="+BlackjackTable.Visible+", cards="+BlackjackTable.CardCount+", hidden="+BlackjackTable.HiddenCount+")");
            Check(host.seat>=0&&peer.seat>=0&&Mathf.Abs(host.body.transform.position.x-peer.body.transform.position.x)>2,"players sit across a clear table");
            Check(Vector3.Dot(Forward(host),Forward(peer))<-.5f,"seated players face one another");
            bool InFrame(Vector3 point){var v=ViewCamera.WorldToViewportPoint(point);return v.z>0&&v.x>.01f&&v.x<.99f&&v.y>.01f&&v.y<.99f;}
            Check(InFrame(Eye(peer))&&InFrame(SaloonBlackjackTable.Center),"opponent and tabletop visible from seated camera");
            void RayCheck(string action)
            {
                var target=BlackjackTable.GetComponentsInChildren<SaloonBlackjackTarget>().First(t=>t.action==action&&t.GetComponent<Collider>().enabled);
                var position=target.GetComponent<Collider>().bounds.center;Physics.SyncTransforms();
                Check(InFrame(position),action+" target fits the seated view");
                var ray=new Ray(ViewCamera.transform.position,position-ViewCamera.transform.position);
                Check(BlackjackTable.ActionForRay(ray)==action,action+" is selected by raycasting the actual table");
            }
            RayCheck("hit");RayCheck("stand");
            CaptureBlackjack("player");
            OnCommand(host.id,new Command{kind="blackjack",name="stand"});Broadcast();yield return new WaitForSeconds(.7f);
            Check(BlackjackTable.HiddenCount==0,"dealer hole card flips face up after stand");
            d.nextInputAt=0;int before=d.blackjack.Dealer.Count;
            OnCommand(host.id,new Command{kind="blackjack",name="deal"});
            Check(d.blackjack.Dealer.Count==before,"player cannot deal the dealer hand");
            OnCommand(peer.id,new Command{kind="blackjack",name="deal"});
            Check(d.blackjack.Dealer.Count==before+1,"dealer can deal the next physical card");
            d.dealer=host.id;DealBlackjack(d);d.blackjack=Hand();d.blackjack.Stand();d.readyAt=0;d.nextInputAt=0;d.nextCardAt=Time.time+30;
            Broadcast();yield return new WaitForSeconds(1.6f);Broadcast();yield return null;
            RayCheck("deal");CaptureBlackjack("dealer");
            Check(PrivateDuel(host).canDeal&&!PrivateDuel(peer).canDeal,"dealer controls are private to the correct seat");
            rounds.BeginAftermath(Time.time);phase=rounds.Phase;Broadcast();yield return null;
            Check(!BlackjackTable.Visible&&BlackjackTable.CardCount==0,"phase change clears cards and table input");
            VerificationFinished=VerificationPassed=true;Debug.Log("SALOON_TABLETOP PASS");
        }
        void CaptureBlackjack(string name)
        {
            var previous=RenderTexture.active;var target=ViewCamera.targetTexture;
            var image=new RenderTexture(1280,900,24);var texture=new Texture2D(1280,900,TextureFormat.RGB24,false);
            try
            {
                ViewCamera.targetTexture=image;ViewCamera.Render();RenderTexture.active=image;
                texture.ReadPixels(new Rect(0,0,1280,900),0,0);texture.Apply();
                string path=Path.Combine(Path.GetTempPath(),"SaloonBlackjack-"+name+".png");File.WriteAllBytes(path,texture.EncodeToPNG());Debug.Log("SALOON_TABLETOP_IMAGE "+path);
            }
            finally{ViewCamera.targetTexture=target;RenderTexture.active=previous;Destroy(texture);image.Release();Destroy(image);}
        }
    }
}
#endif
