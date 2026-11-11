#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Services.Vivox;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public void StartSmokingPractice()
        {
            HostLan(true);if(!Hosting)return;
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            foreach(var p in players.Values){p.chosenRoom=0;p.companion=null;}
            host.chosenRoom=peer.chosenRoom=3;
            foreach(var id in players.Keys)rounds.SetReady(id,true);
            rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);
            MenuOpen=false;AutomatedInput=false;Broadcast();
        }
        public IEnumerator VerifySmoking()
        {
            verifying=true;AutomatedInput=true;VerificationPassed=VerificationFinished=false;
            void Check(bool ok,string message)
            {if(!ok){VerificationFinished=true;throw new InvalidOperationException("Smoking: "+message);}Debug.Log("SALOON_SMOKING_CHECK "+message);}
            yield return new WaitForSeconds(.1f);
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);var indoor=players.Values.First(p=>p!=host&&p!=peer);
            void Plans()
            {
                foreach(var p in players.Values){p.hp=60;p.chosenRoom=0;p.companion=null;p.puffs=0;p.smokeAt=-100;p.busyUntil=0;p.drawn=false;}
                rounds.Start(players.Keys,Time.time,30,30,6);phase=rounds.Phase;PositionEveryone(true);
                OnCommand(host.id,new Command{kind="room",x=3,name=peer.id.ToString()});
            }
            Plans();peer.chosenRoom=2;peer.companion=host.id;
            foreach(var recipient in players.Values)
            {
                var snapshot=Snapshot.FromWireJson(PrivateSnapshot(recipient.id).ToWireJson());
                Check(snapshot.players.Where(p=>p.id!=recipient.id.ToString()).All(p=>p.chosenRoom==-1&&p.companion==""&&p.planVersion==0),"room and partner selections are redacted for "+recipient.name);
            }
            peer.chosenRoom=indoor.chosenRoom=3;
            OnCommand(host.id,new Command{kind="ready",x=1});OnCommand(peer.id,new Command{kind="ready",x=1});OnCommand(indoor.id,new Command{kind="ready",x=1});
            Check(rounds.IsReady(host.id)&&rounds.IsReady(peer.id)&&!rounds.IsReady(indoor.id),"outdoor seats obey first-two-Ready reservations");
            Check(PrivateSnapshot(host.id).players.First(p=>p.id==host.id.ToString()).chosenRoom==3,"owner keeps their private outdoor choice");
            void Go(bool together)
            {
                Plans();peer.chosenRoom=together?3:0;
                foreach(var id in players.Keys)rounds.SetReady(id,true);rounds.Tick(Time.time,id=>true);phase=rounds.Phase;PositionEveryone(false);Broadcast();
            }
            Go(false);yield return null;
            Check(host.room==3&&host.seat<0&&host.controller.enabled&&Vector3.Distance(host.body.transform.position,SaloonSmokingArea.Center)<2,"solo smoker is placed standing in the outdoor placeholder");
            var outside=PrivateSnapshot(host.id);var inside=PrivateSnapshot(indoor.id);
            Check(outside.players.Length==1&&outside.players[0].id==host.id.ToString()&&outside.blackjackTable==null,"solo outside snapshot contains no indoor activities or table state");
            Check(inside.players.All(p=>p.id!=host.id.ToString()),"indoor clients receive no outdoor avatar or smoking updates");
            Check(!outside.actionAvailable&&outside.duel==null&&outside.roomTurn==null,"outside does not start a minigame or combat turn");
            int ammo=host.ammo,shots=host.shots;
            foreach(string action in new[]{"draw","fire","reload","drink","give","rob","escape","inspect","pass","interact","stance"})OnCommand(host.id,new Command{kind=action});
            host.drawn=true;Fire(host);host.drawn=false;
            Check(host.ammo==ammo&&host.shots==shots&&host.busyUntil==0&&!rounds.HasSpent(host.id),"host rejects weapons and saloon actions outside without spending a turn");
            indoor.shots+=2;indoor.hp=20;
            var quiet=Snapshot.FromWireJson(PrivateSnapshot(host.id).ToWireJson());
            Check(quiet.players.All(p=>p.id!=indoor.id.ToString()),"indoor gunshots and damage cannot enter the outdoor snapshot");
            rounds.Tick(Time.time,id=>true);Check(rounds.Phase==Phase.Turns,"smokers can relax while indoor players still have actions");
            OnCommand(host.id,new Command{kind="smoke"});OnCommand(host.id,new Command{kind="smoke"});
            Check(host.puffs==1&&!rounds.HasSpent(host.id),"left-click starts one drag and repeated clicks respect the animation cooldown");
            Go(true);yield return null;
            Check(View.players.Length==2&&View.players.All(p=>p.room==3)&&!avatars.ContainsKey(indoor.id.ToString()),"two smokers see each other without indoor audiovisual objects");
            TrySmoke(host);TrySmoke(peer);host.smokeAt=peer.smokeAt=Time.time-1;Broadcast();yield return new WaitForSeconds(.1f);
            var local=avatars[host.id.ToString()].SmokingVisual;var other=avatars[peer.id.ToString()].SmokingVisual;
            Check(local.FirstPersonVisible&&!local.ThirdPersonVisible&&other.ThirdPersonVisible,"first-person hand and third-person cigarette use the correct perspectives");
            Check(local.Lift>.9f&&other.Lift>.9f&&local.EmberHeat>1,"drag raises the cigarette and brightens the ember");
            Check((ViewCamera.cullingMask&(1<<SaloonSmokingArea.IndoorLayer))==0&&(ViewCamera.cullingMask&(1<<SaloonSmokingArea.OutdoorLayer))!=0,"outside camera excludes all saloon scenery");
            SmokingImage("first-person");
            externalCamera=true;var center=host.body.transform.position;var rot=Quaternion.Euler(0,host.yaw,0);
            externalPosition=center+rot*new Vector3(.5f,1.55f,1.0f);externalRotation=Quaternion.LookRotation(center+Vector3.up*1.5f-externalPosition);ViewCamera.fieldOfView=48;RefreshViewpoint();
            host.smokeAt=Time.time-1;Broadcast();yield return new WaitForSeconds(.1f);
            Check(local.ThirdPersonVisible&&Vector3.Distance(local.CigarettePosition,avatars[host.id.ToString()].GetComponentInChildren<SaloonCharacterMotion>().Mouth)<.08f,"third-person hand brings the cigarette to the mouth");
            SmokingImage("third-person");
            host.smokeAt=Time.time-2.3f;Broadcast();yield return new WaitForSeconds(.3f);SmokingImage("exhale");
            Check(FindObjectsByType<ParticleSystem>().Where(p=>p.name.Contains("exhaled smoke")).Any(p=>p.particleCount>0),"exhaled smoke is visibly animated");
            externalCamera=false;ViewCamera.fieldOfView=78;RefreshViewpoint();

            string expected=SaloonPrivacy.VoiceChannel(VoiceRoom,phase,3,true);
            Check(expected.EndsWith("-smoke")&&expected!=SaloonPrivacy.VoiceChannel(VoiceRoom,phase,0,true)&&expected!=SaloonPrivacy.VoiceChannel(VoiceRoom,phase,3,false),"outside voice uses a distinct private channel from living indoor and dead chat");
            var connect=Voice.ConnectForVerification();float until=Time.realtimeSinceStartup+35;
            while(!connect.IsCompleted&&Time.realtimeSinceStartup<until)yield return null;
            Check(connect.IsCompleted&&Voice.Connected&&Voice.Channel==expected,"real Vivox session joins the private outdoor voice channel");
            Check(VivoxService.Instance.ActiveChannels.Count==1&&!Voice.Transmitting,"only the outdoor channel is active; verification keeps microphone muted");
            rounds.Start(players.Keys,Time.time,30,30,6);phase=rounds.Phase;PositionEveryone(true);Broadcast();until=Time.realtimeSinceStartup+25;
            while((Voice.Channel!=VoiceRoom+"-alive"||Voice.Busy)&&Time.realtimeSinceStartup<until)yield return null;
            Check(Voice.Connected&&Voice.Channel==VoiceRoom+"-alive"&&VivoxService.Instance.ActiveChannels.Count==1,"regroup leaves outside voice and restores indoor voice without mixed channels");
            yield return null;
            Check(View.players.Length==players.Count&&!local.FirstPersonVisible&&!local.ThirdPersonVisible,"returning inside restores the roster and hides smoking props");
            Check((ViewCamera.cullingMask&(1<<SaloonSmokingArea.OutdoorLayer))==0,"indoor camera excludes the outdoor yard");
            var leave=Voice.Leave();while(!leave.IsCompleted)yield return null;
            VerificationPassed=VerificationFinished=true;Debug.Log("SALOON_SMOKING PASS");
        }
        void SmokingImage(string name)
        {
            var previous=RenderTexture.active;var old=ViewCamera.targetTexture;
            var target=new RenderTexture(1280,900,24);var image=new Texture2D(1280,900,TextureFormat.RGB24,false);
            try
            {
                ViewCamera.targetTexture=target;ViewCamera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1280,900),0,0);image.Apply();
                string path=Path.Combine(Path.GetTempPath(),"SaloonSmoking-"+name+".png");File.WriteAllBytes(path,image.EncodeToPNG());Debug.Log("SALOON_SMOKING_IMAGE "+path);
            }
            finally{ViewCamera.targetTexture=old;RenderTexture.active=previous;Destroy(image);target.Release();Destroy(target);}
        }
    }
}
#endif
