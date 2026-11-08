#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public IEnumerator VerifyTalkingMouths()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=VerificationPassed=false;
            void Check(bool ok,string message)
            {
                if(!ok){Voice.PreviewSpeech=null;VerificationFinished=true;Debug.LogError("SALOON_SPEECH FAIL "+message);throw new InvalidOperationException(message);}
                Debug.Log("SALOON_SPEECH_CHECK "+message);
            }
            string directory=Path.Combine(Path.GetTempPath(),"SaloonPrototype-EditorPreview");Directory.CreateDirectory(directory);
            Finish("SPEECH PREVIEW");ReopenLobby();var hero=players.Values.First(p=>p.bot);var other=players.Values.First(p=>p.bot&&p!=hero);
            hero.controller.enabled=false;hero.body.transform.position=new Vector3(0,.08f,-5.5f);hero.controller.enabled=true;hero.yaw=0;hero.pitch=0;hero.poseVersion++;
            hero.name=other.name="Same display name";
            OnCommand(hero.id,new Command{kind="voice",name="editor-speaker-a"});OnCommand(other.id,new Command{kind="voice",name="editor-speaker-b"});
            var wire=SaloonSnapshotWire.Decode(SaloonSnapshotWire.Encode(PrivateSnapshot(Connection.LocalId),out _));
            Check(wire.players.Single(p=>p.id==hero.id.ToString()).voiceId=="editor-speaker-a","voice identity survives the compressed snapshot");
            Check(SaloonVoice.SpeechStrength(false,1,false)==0&&SaloonVoice.SpeechStrength(true,1,true)==0,"silence and mute suppress speech even with energy present");
            Check(SaloonVoice.SpeechStrength(true,0,false)>0&&SaloonVoice.SpeechStrength(true,.05,false)>SaloonVoice.SpeechStrength(true,.001,false),"voice activity has a fallback and responds to louder speech");
            Voice.PreviewSpeech=new Dictionary<string,float>{{hero.voiceId,0}};Broadcast();yield return new WaitForSeconds(.3f);
            var mouth=avatars[hero.id.ToString()].GetComponentInChildren<SaloonSpeechMouth>(true);
            var otherMouth=avatars[other.id.ToString()].GetComponentInChildren<SaloonSpeechMouth>(true);
            int shape=mouth.FaceRenderer.sharedMesh.GetBlendShapeIndex(SaloonSpeechMouth.Shape);
            Check(shape>=0&&mouth.OpenAmount==0&&!mouth.MouthVisible,"the prepared face has a talking jaw and is closed in silence");
            externalCamera=true;MenuOpen=false;RefreshViewpoint();ViewCamera.fieldOfView=28;
            var center=mouth.HeadBone.TransformPoint(mouth.MouthPosition)+Vector3.up*.06f;
            externalPosition=center+new Vector3(.14f,.04f,.65f);externalRotation=Quaternion.LookRotation(center-externalPosition);
            IEnumerator Capture(string name)
            {yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"speech-"+name+".png"));yield return null;}
            yield return Capture("01-quiet");float low=1,high=0;
            for(int i=0;i<35;i++)
            {
                Voice.PreviewSpeech[hero.voiceId]=SaloonVoice.SpeechStrength(true,(i%7)*.025,false);
                yield return new WaitForSeconds(.025f);low=Mathf.Min(low,mouth.OpenAmount);high=Mathf.Max(high,mouth.OpenAmount);
            }
            Check(high>.5f&&high-low>.2f&&mouth.FaceRenderer.GetBlendShapeWeight(shape)>0,"speech volume drives a visibly changing jaw and uneven flap");
            Check(otherMouth.OpenAmount==0&&!otherMouth.MouthVisible,"matching display names do not animate the wrong player");
            Voice.PreviewSpeech[hero.voiceId]=1;
            for(int i=0;i<30&&mouth.OpenAmount<.65f;i++)yield return null;
            yield return Capture("02-talking");
            Voice.PreviewSpeech[hero.voiceId]=0;yield return new WaitForSeconds(.2f);
            Check(mouth.OpenAmount==0&&!mouth.MouthVisible&&mouth.FaceRenderer.GetBlendShapeWeight(shape)==0,"speech stops promptly and restores the original closed face");
            yield return Capture("03-stopped");
            Voice.PreviewSpeech[hero.voiceId]=1;hero.busyAction="drink";hero.busyUntil=Time.time+1;Broadcast();yield return new WaitForSeconds(.15f);
            Check(mouth.OpenAmount==0&&!mouth.MouthVisible,"drinking takes priority over mouth flapping");
            hero.busyUntil=0;hero.busyAction="";Broadcast();yield return new WaitForSeconds(.15f);
            Check(mouth.OpenAmount>0,"speech resumes after drinking");
            hero.hp=0;hero.deathAt=Time.time;hero.deathDirection=Vector3.back;Broadcast();yield return null;
            Check(mouth.OpenAmount==0&&!mouth.MouthVisible,"elimination closes the mouth before ragdoll takeover");
            Voice.PreviewSpeech=null;Check(Voice.SpeechLevel(hero.voiceId)==0,"disconnected voice has no lingering speech activity");
            externalCamera=false;ViewCamera.fieldOfView=78;RefreshViewpoint();
            Debug.Log("SALOON_SPEECH PASS simulated Vivox activity/energy, identity mapping, jaw deformation, flapping, silence/mute, drinking and death; previews="+directory);
            VerificationPassed=VerificationFinished=true;
        }
    }
}
#endif
