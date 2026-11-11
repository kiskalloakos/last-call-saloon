#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public void StartCoinTablePractice()
        {
            HostLan(true);if(!Hosting)return;
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            foreach(var p in players.Values){p.role=Role.Deputy;p.chosenRoom=0;p.companion=null;}
            host.role=Role.Sheriff;peer.role=Role.Renegade;
            host.chosenRoom=peer.chosenRoom=1;host.companion=peer.id;peer.companion=host.id;
            foreach(var id in players.Keys)rounds.SetReady(id,true);
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            MenuOpen=false;AutomatedInput=false;Broadcast();
        }
        public IEnumerator VerifyTabletopCoin()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string text)
            {
                if(!ok){VerificationFinished=true;throw new InvalidOperationException("Tabletop coin: "+text);}
                Debug.Log("SALOON_COIN_CHECK "+text);
            }
            yield return new WaitForSeconds(.1f);
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            var viewer=players.Values.First(p=>p!=host&&p!=peer);var smoker=players.Values.Last(p=>p.bot);
            void Setup()
            {
                pendingOutcome="";
                foreach(var p in players.Values){p.hp=60;p.role=Role.Deputy;p.chosenRoom=0;p.companion=null;p.drawn=false;p.lastAction=-100;p.nextShot=0;p.ammo=6;}
                host.role=Role.Sheriff;peer.role=Role.Renegade;
                host.chosenRoom=peer.chosenRoom=1;host.companion=peer.id;peer.companion=host.id;smoker.chosenRoom=3;
                rounds.Start(players.Keys,Time.time,30,30,6);foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);MenuOpen=false;Broadcast();
            }
            bool InFrame(Vector3 point){var v=ViewCamera.WorldToViewportPoint(point);return v.z>0&&v.x>.01f&&v.x<.99f&&v.y>.01f&&v.y<.99f;}
            Setup();yield return new WaitForSeconds(.4f);Broadcast();yield return null;
            Check(CoinTable.Visible&&PlayingCoin&&NeedsPointer,"physical coin replaces the room HUD and frees the pointer");
            Check(host.seat>=0&&peer.seat>=0&&Vector3.Dot(Forward(host),Forward(peer))<-.5f,"both players sit face to face");
            Check(InFrame(Eye(peer))&&InFrame(SaloonCoinTable.Center),"opponent and coin are visible together");
            Check(CoinTable.transform.Find("Coin toss").GetComponentsInChildren<Collider>().All(c=>!c.enabled),"clean coin table has no physical choice controls");
            Check(CoinTable.transform.Find("Coin toss").GetComponentsInChildren<TextMesh>().All(t=>t.transform.IsChildOf(CoinTable.transform.Find("Coin toss/Gold coin — Heads and Tails"))),"only the coin engravings remain on the table");
            Check(PrivateDuel(host).canChooseCoin&&PrivateDuel(peer).canChooseCoin,"either participant can use the centered Heads or Tails buttons");
            CaptureCoin("call");
            if(!Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"SaloonCoin-screen.png"));yield return null;
            }
            var savedPosition=ViewCamera.transform.position;var savedRotation=ViewCamera.transform.rotation;
            ViewCamera.transform.SetPositionAndRotation(Eye(peer),Quaternion.Euler(26,peer.yaw,0));
            CoinTable.Apply(PrivateSnapshot(peer.id),PrivateSnapshot(peer.id).players.First(p=>p.id==peer.id.ToString()));
            ViewCamera.transform.SetPositionAndRotation(savedPosition,savedRotation);Broadcast();
            var d=duels[1];int shots=host.shots;
            OnCommand(host.id,new Command{kind="draw"});Stand(host);TickBotMovement(peer);
            Check(!host.drawn&&host.shots==shots&&host.seat>=0&&peer.seat>=0&&rounds.CanAct(host.id,Time.time),"combat and standing stay locked before the toss");
            OnCommand(host.id,new Command{kind="coin",x=0});OnCommand(peer.id,new Command{kind="coin",x=0});
            Check(d.heads==host.id&&d.claimed&&d.flipping,"first screen choice locks opposite sides");Broadcast();
            Check(PrivateDuel(host).result==""&&PrivateDuel(host).firstId==""&&PublicCoinTable().result=="","result stays unknown while the coin is in the air");
            yield return new WaitForSeconds(d.coinFlipSeconds*.48f);Broadcast();yield return null;
            Check(CoinTable.TossHeight>.30f,"gold coin physically flips above the felt");CaptureCoin("toss");
            var watching=Snapshot.FromWireJson(PrivateSnapshot(viewer.id).ToWireJson());
            Check(watching.coinTable?.stage=="flipping"&&!watching.coinTable.canChooseCoin&&!watching.coinTable.yourTurn,"indoor spectator receives a read-only toss");
            CoinTable.Apply(watching,watching.players.First(p=>p.id==viewer.id.ToString()));
            Check(CoinTable.Visible&&CoinTable.transform.Find("Coin toss").GetComponentsInChildren<Collider>().All(c=>!c.enabled),"spectators see the coin without clickable controls");
            var outside=Snapshot.FromWireJson(PrivateSnapshot(smoker.id).ToWireJson());
            Check(outside.coinTable==null,"smoking yard receives no coin table state");
            CoinTable.Apply(outside,outside.players.First(p=>p.id==smoker.id.ToString()));Check(!CoinTable.Visible,"outdoor snapshot clears the coin presentation");Broadcast();
            yield return new WaitForSeconds(d.coinFlipSeconds*.6f+.2f);Broadcast();yield return new WaitForSeconds(.16f);
            Check(d.revealed&&CoinTable.FaceUp==d.result,"landed face matches the authoritative result");CaptureCoin("result");
            Check(!PlayingCoin&&PrivateDuel(host).stage=="result"&&!DuelAllows(host)&&!DuelAllows(peer),"landed result has a reaction pause before either player can draw");
            Check(!TableLocksSeat(host)&&!TableLocksSeat(peer)&&d.firstUntil-d.combatAt>=9.99f,"both players may stand and dodge before the full opening window");
            host.lastAction=-100;OnCommand(host.id,new Command{kind="stance"});
            Check(host.seat<0&&CanWalk(host)&&rounds.CanAct(host.id,Time.time),"a human can stand and dodge during the reaction pause without spending an action");
            d.combatAt=Time.time-1;Broadcast();
            Check(DuelAllows(players[d.first])&&!DuelAllows(players[d.first==d.a?d.b:d.a]),"winner receives the exclusive opening after the reaction pause");
            var first=players[d.first];var second=players[d.first==d.a?d.b:d.a];
            OnCommand(first.id,new Command{kind="pass"});Check(!rounds.CanAct(first.id,Time.time)&&DuelAllows(second),"winner may pass immediately");

            Setup();d=duels[1];OnCommand(host.id,new Command{kind="coin",x=1});
            Check(d.heads==peer.id,"Tails call assigns Heads to the opponent");
            d.revealAt=Time.time-1;TickDuels();
            d.combatAt=Time.time-1;
            first=players[d.first];second=players[d.first==d.a?d.b:d.a];
            first.role=Role.Sheriff;second.role=Role.Renegade;
            var direction=Eye(second)-Eye(first);first.yaw=Quaternion.LookRotation(direction).eulerAngles.y;first.pitch=0;
            shots=first.shots;int hp=second.hp;
            OnCommand(first.id,new Command{kind="draw"});OnCommand(first.id,new Command{kind="fire"});
            Check(first.drawn&&first.fireAfterDraw&&first.shots==shots,"Q and click queue the opening through the normal draw");
            first.busyUntil=Time.time-.01f;TickServer();
            Check(first.shots==shots+1&&second.hp==hp-SaloonHealth.RevolverDamage&&DuelAllows(second),"opening shot lands across the table and releases the response");

            Setup();d=duels[1];d.choiceUntil=Time.time-1;TickDuels();Check(d.flipping&&!d.claimed,"idle table automatically tosses the assigned sides");
            d.revealAt=Time.time-1;TickDuels();d.combatAt=Time.time-2;d.firstUntil=Time.time-1;TickDuels();
            Check(!rounds.CanAct(d.first,Time.time)&&DuelAllows(players[d.first==d.a?d.b:d.a]),"idle opening passes so the room cannot stall");
            foreach(string result in new[]{"HEADS","TAILS"})
            {
                var test=PrivateSnapshot(host.id);test.duel.result=result;CoinTable.Apply(test,test.players.First(p=>p.id==host.id.ToString()));
                // Both orientations must work even if the two real random tosses picked the same side.
                yield return new WaitForSeconds(.16f);
                CoinTable.Apply(test,test.players.First(p=>p.id==host.id.ToString()));
                Check(CoinTable.FaceUp==result,"physical face supports "+result);
            }
            rounds.BeginAftermath(Time.time);phase=rounds.Phase;Broadcast();yield return null;
            Check(!CoinTable.Visible&&View.coinTable==null,"phase change removes the coin and table controls");
            VerificationFinished=VerificationPassed=true;Debug.Log("SALOON_COIN_TABLE PASS");
        }
        void CaptureCoin(string name)
        {
            var previous=RenderTexture.active;var old=ViewCamera.targetTexture;
            var target=new RenderTexture(1280,900,24);var texture=new Texture2D(1280,900,TextureFormat.RGB24,false);
            try
            {
                ViewCamera.targetTexture=target;ViewCamera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1280,900),0,0);texture.Apply();
                string path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"SaloonCoin-"+name+".png");System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());Debug.Log("SALOON_COIN_IMAGE "+path);
            }
            finally{ViewCamera.targetTexture=old;RenderTexture.active=previous;Destroy(texture);target.Release();Destroy(target);}
        }
    }
}
#endif
