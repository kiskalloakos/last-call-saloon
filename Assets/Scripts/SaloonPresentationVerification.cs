using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        bool externalCamera;
        Vector3 externalPosition;
        Quaternion externalRotation;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public System.Collections.IEnumerator VerifyPresentation()
        {
            verifying=true;AutomatedInput=true;yield return null;
            void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Local presentation: "+message);Debug.Log("SALOON_PRESENTATION_CHECK "+message);}
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../Preview"));
            // Command line output is explicit so local previews stay beside the local builds.
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-saloonPreview");
            if(at>=0&&at+1<args.Length)directory=args[at+1];Directory.CreateDirectory(directory);
            System.Collections.IEnumerator Capture(string name)
            {
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));yield return null;
            }
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            var others=players.Values.Where(p=>p!=host&&p!=peer).ToArray();
            foreach(var p in players.Values){p.hp=3;p.deathAt=-1;p.hitbox.enabled=true;p.drawn=false;p.beer=0;p.busyUntil=0;p.lastAction=-100;}
            host.role=Role.Sheriff;peer.role=Role.Outlaw;others[0].role=Role.Outlaw;others[1].role=Role.Deputy;others[2].role=Role.Renegade;
            rounds.Start(players.Keys,Time.time,1,120,6);phase=rounds.Phase;PositionEveryone(true);
            OnCommand(host.id,new Command{kind="ready",x=1});Check(rounds.IsReady(host.id),"Ready accepted during discussion");
            OnCommand(host.id,new Command{kind="room",x=0,name=peer.id.ToString()});
            Check(!rounds.IsReady(host.id),"editing a plan clears Ready");
            peer.chosenRoom=0;peer.companion=host.id;others[0].chosenRoom=others[1].chosenRoom=1;others[2].chosenRoom=2;
            Broadcast();yield return new WaitForSeconds(.3f);yield return Capture("01-discussion-ready");
            int oldPose=host.poseVersion;
            foreach(var p in players.Values)OnCommand(p.id,new Command{kind="ready",x=1});TickServer();Broadcast();
            Check(phase==Phase.Turns,"all Ready starts room actions without a choosing timer");
            var direction=(peer.body.transform.position-host.body.transform.position).normalized;
            Check(Vector3.Dot(Quaternion.Euler(0,host.yaw,0)*Vector3.forward,direction)>.99f&&Vector3.Dot(Quaternion.Euler(0,peer.yaw,0)*Vector3.forward,-direction)>.99f,"two players at the bar face one another");
            float facing=host.yaw;OnCommand(host.id,new Command{kind="input",yaw=facing+180,poseVersion=oldPose});
            Check(host.yaw==facing,"old look packets cannot overwrite the new room facing");
            yield return new WaitForSeconds(.35f);yield return Capture("02-facing-coin");
            var duel=duels[0];duel.claimed=duel.flipping=duel.revealed=true;duel.first=host.id;duel.result="HEADS";duel.firstUntil=Time.time+30;
            host.drawn=peer.drawn=true;Broadcast();yield return new WaitForSeconds(.25f);
            var weapon=ViewCamera.transform.Find("Low-poly revolver");
            Check(weapon!=null&&weapon.Find("First-person right arm")==null,"first-person revolver has no visible arm");
            yield return Capture("03-first-person-revolver");
            externalCamera=true;externalPosition=host.body.transform.position+new Vector3(0,1.12f,-1.2f);
            externalRotation=Quaternion.LookRotation(host.body.transform.position+new Vector3(.22f,.98f,0)-externalPosition);
            RefreshViewpoint();yield return new WaitForSeconds(.2f);yield return Capture("04-third-person-grip");
            externalCamera=false;RefreshViewpoint();yield return new WaitForSeconds(.15f);
            host.nextShot=0;host.ammo=6;host.busyUntil=0;Fire(host);Broadcast();
            yield return new WaitForSeconds(.05f);yield return Capture("05-shot-recoil");
            yield return new WaitForSeconds(.25f);yield return Capture("06-revolver-roll-start");
            yield return new WaitForSeconds(.30f);yield return Capture("07-revolver-roll-middle");
            yield return new WaitForSeconds(.60f);yield return Capture("08-return-to-rest");
            Check(SaloonWeaponPose.Flip(.4f)<0&&Mathf.Abs(SaloonWeaponPose.Flip(1.2f)+360)<.01f,"revolver rolls backward and returns to rest");
            Check(Quaternion.Angle(weapon.localRotation,Quaternion.identity)<2,"revolver settles after the flourish");
            Check(SaloonWeaponPose.Kick(0)==0&&SaloonWeaponPose.Kick(.035f)>.99f&&SaloonWeaponPose.Kick(.3f)==0,"recoil rises into a short kick and fully recovers");
            for(float t=0;t<1.2f;t+=1f/60)
                Check(Mathf.Abs(SaloonWeaponPose.Flip(t+1f/60)-SaloonWeaponPose.Flip(t))<12,"flip has no angular jump at 60 Hz");
            host.hp=1;TakeDamage(host,-Forward(host));Broadcast();yield return new WaitForSeconds(SaloonRagdoll.ObserverDelay+.2f);
            Check(IsSpectating&&CameraPlayer.alive&&CameraPlayer.id==peer.id.ToString(),"eliminated player enters a living player's POV");
            Check(View.ownRole==host.role.ToString()&&View.players.All(p=>string.IsNullOrEmpty(p.revealedRole)),"spectating does not reveal another player's hidden role");
            yield return Capture("09-spectator-pov");
            string previous=CameraPlayer.id;CycleSpectator(1);yield return new WaitForSeconds(.2f);
            Check(CameraPlayer.alive&&CameraPlayer.id!=previous,"spectator cycles to another living POV");
            yield return Capture("10-next-player-pov");
            Debug.Log("SALOON_PRESENTATION PASS previews="+directory);yield return new WaitForSeconds(.4f);Application.Quit(0);
        }
#endif
    }
}
