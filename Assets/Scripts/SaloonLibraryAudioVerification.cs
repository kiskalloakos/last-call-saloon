#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyLibraryAudio()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string text)
            {if(!ok){VerificationFinished=true;throw new InvalidOperationException("Library audio: "+text);}Debug.Log("SALOON_LIBRARY_AUDIO_CHECK "+text);}
            yield return null;
            foreach(var clip in new[]{SaloonLibraryAudio.Reload,SaloonLibraryAudio.Drinking,SaloonLibraryAudio.Hoofbeats,SaloonLibraryAudio.DistantShot,SaloonLibraryAudio.RevolverShot})
            {
                var samples=new float[clip.samples*clip.channels];bool loaded=clip.GetData(samples,0);
                Check(loaded&&clip.channels==1&&clip.frequency==44100&&samples.Any(v=>Mathf.Abs(v)>.05f)&&samples.All(v=>!float.IsNaN(v)&&Mathf.Abs(v)<.75f),clip.name+" is a bundled, non-silent recording with bounded peaks");
            }
            Check(SaloonGunAudio.Reload==Resources.Load<AudioClip>("Audio/Recorded/Reload")&&SaloonBeerAudio.Drinking==Resources.Load<AudioClip>("Audio/Recorded/Drinking"),"gameplay resolves the downloaded reload and drinking assets");
            Check(SaloonGunAudio.Shot==Resources.Load<AudioClip>("Audio/Recorded/RevolverShot")&&SaloonGunAudio.Shot.length>1.8f,"revolver resolves the selected Heathers recording with its full tail");
            Check(Mathf.Abs(SaloonBeerAudio.Drinking.length-2.5f)<.01f&&SaloonGunAudio.Reload.length>3.3f&&SaloonGunAudio.Reload.length<3.5f,"recordings fit normal drink and reload timing");
            Check(Resources.Load<TextAsset>("Audio/Recorded/License").text.Contains("CC0 1.0"),"distribution includes source credits and CC0 license information");
            var host=players[Connection.LocalId];
            void Window()
            {
                pendingOutcome=outcome="";ResetHeist();
                foreach(var p in players.Values)
                {p.hp=100;p.deathAt=-1;p.cigaretteBuff=false;p.chosenRoom=0;p.companion=null;p.busyAction="";p.busyUntil=0;p.lastAction=-100;p.drawn=false;p.beer=0;}
                rounds.Start(players.Keys,Time.time,30,120,0);foreach(var id in players.Keys)rounds.SetReady(id,true);
                rounds.Tick(Time.time,id=>players[id].Alive);phase=rounds.Phase;PositionEveryone(false);duels.Clear();roomTurns.Clear();Broadcast();
            }
            Window();yield return new WaitForSeconds(.15f);
            var avatar=avatars[host.id.ToString()];
            var drink=avatar.GetComponents<AudioSource>().Single(a=>a.clip==SaloonBeerAudio.Drinking);
            Check(!drink.playOnAwake&&!drink.isPlaying,"avatars do not drink audibly when they spawn");
            host.hp=80;host.beer=1;host.beerHealing=SaloonHealth.BeerHealing;OnCommand(host.id,new Command{kind="drink"});Broadcast();yield return new WaitForSeconds(.15f);
            Check(drink.isPlaying&&Mathf.Abs(drink.pitch-1)<.01f&&drink.spatialBlend==0,"real drink plays the recording at natural pitch for the local player");
            yield return new WaitForSeconds(tuning.drinkSeconds+.1f);
            Check(host.hp==100&&!drink.isPlaying,"beer heals once when the recorded drinking action finishes");
            Window();host.drawn=true;host.ammo=2;OnCommand(host.id,new Command{kind="reload"});Broadcast();yield return new WaitForSeconds(.15f);
            var reload=avatar.GetComponents<AudioSource>().Single(a=>a.clip==SaloonGunAudio.Reload);
            Check(reload.isPlaying&&!reload.playOnAwake&&Mathf.Abs(reload.pitch-SaloonGunAudio.Reload.length/tuning.reloadSeconds)<.01f,"real reload plays the downloaded cylinder recording in sync with the action");
            yield return new WaitForSeconds(tuning.reloadSeconds+.1f);
            Check(host.ammo==tuning.cylinder&&!reload.isPlaying,"reload completion refills ammo and stops the recording");
            Window();host.hp=20;host.beer=1;host.beerHealing=SaloonHealth.BeerHealing;OnCommand(host.id,new Command{kind="drink"});Broadcast();yield return new WaitForSeconds(.15f);TakeDamage(host);Broadcast();yield return null;
            Check(!drink.isPlaying&&host.hp==0,"lethal interruption stops the drinking recording without revival");
            Window();nextBroadcast=Time.unscaledTime+30;View.posseStage=1;MenuOpen=false;
            yield return new WaitForSeconds(.35f);
            var horses=GetComponents<AudioSource>().Single(a=>a.clip==SaloonLibraryAudio.Hoofbeats);
            Check(horses.isPlaying&&horses.loop&&horses.pitch==1&&horses.volume>0,"backup warning uses the recorded gallop at its original pitch with a fade-in");
            float position=horses.time;View.posseStage=2;yield return new WaitForSeconds(.15f);
            Check(horses.isPlaying&&horses.time>position,"warning progression does not restart the gallop loop");
            View.posseStage=4;yield return new WaitForSeconds(2.2f);
            Check(GetComponents<AudioSource>().Any(a=>!a.loop&&a.isPlaying),"arrival plays a recorded distant gunshot alongside the horses");
            View.phase=Phase.Finished;yield return null;yield return null;
            Check(GetComponents<AudioSource>().All(a=>!a.isPlaying),"results stop all backup arrival audio");
            Check(SaloonLibraryAudio.Hoofbeats!=null&&SaloonLibraryAudio.DistantShot!=null,"stopping arrival keeps shared imported clips available");
            Debug.Log("SALOON_LIBRARY_AUDIO PASS recorded resources, peaks, startup silence, real drink/reload timing, lethal interruption and staged backup playback");
            VerificationFinished=VerificationPassed=true;
        }
    }
}
#endif
