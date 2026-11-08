#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        public System.Collections.IEnumerator VerifyEditorRoleCardsAndDraw()
        {
            verifying=true;AutomatedInput=true;VerificationFinished=false;VerificationPassed=false;
            void Check(bool ok,string message)
            {
                if(ok)return;
                VerificationFinished=true;Debug.LogError("SALOON_EDITOR_PRESENTATION FAIL "+message);throw new InvalidOperationException(message);
            }
            string directory=Path.Combine(Path.GetTempPath(),"SaloonPrototype-EditorPreview");Directory.CreateDirectory(directory);
            var host=players[Connection.LocalId];
            foreach(var role in new[]{Role.Sheriff,Role.Deputy,Role.Outlaw,Role.Renegade})
            {
                var remaining=SaloonRules.Deck.ToList();remaining.Remove(role);int index=0;
                foreach(var p in players.Values)p.role=p==host?role:remaining[index++];
                Broadcast();yield return null;
                ShowRole=true;yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"role-"+role+".png"));
                var guide=SaloonRules.Guide(role);
                Check(Self!=null&&View.ownRole==role.ToString()&&!string.IsNullOrEmpty(guide.Goal)&&!string.IsNullOrEmpty(guide.Actions)&&!string.IsNullOrEmpty(guide.Win)&&!string.IsNullOrEmpty(guide.Warning),"complete private role guide for "+role);
                Check(!string.IsNullOrEmpty(guide.PlanTitle)&&!string.IsNullOrEmpty(guide.Plan),"role-specific plan for "+role);
                if(role!=Role.Outlaw)Check(!guide.PlanTitle.Contains("ESCAPE")&&!guide.Plan.Contains("<b>H</b>")&&!guide.Plan.Contains("<b>X</b>"),"only the Outlaw receives the robbery walkthrough");
                yield return null;
            }
            ShowRole=false;host.drawn=false;Broadcast();yield return new WaitForSeconds(.5f);
            var weapon=ViewCamera.transform.Find("Low-poly revolver");Check(weapon!=null,"local revolver exists");
            for(int cycle=0;cycle<3;cycle++)
            {
                bool drawing=cycle!=1;host.lastAction=-100;OnCommand(host.id,new Command{kind="draw"});Broadcast();
                Check(host.drawn==drawing,"draw/holster command accepted");
                float started=Time.time,previousY=weapon.localPosition.y;int frames=0;bool captured=false;
                while(Time.time-started<.75f)
                {
                    yield return null;var position=weapon.localPosition;frames++;
                    Check(Mathf.Abs(position.x-.22f)<.0001f,"draw keeps a fixed horizontal position");
                    Check(Vector3.Dot(weapon.localRotation*Vector3.right,Vector3.right)>.9999f,"draw has no sideways roll or yaw");
                    Check(drawing?position.y>=previousY-.0001f:position.y<=previousY+.0001f,"draw/holster follows one smooth vertical direction");
                    previousY=position.y;
                    if(!captured&&Time.time-started>.14f)
                    {
                        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"draw-cycle-"+cycle+".png"));captured=true;
                    }
                }
                Check(frames>3&&weapon.gameObject.activeSelf==drawing,"draw settles and holster hides the gun");
            }
            Debug.Log("SALOON_EDITOR_PRESENTATION PASS four role cards and three draw/holster cycles, no lateral jump; previews="+directory);
            VerificationPassed=true;VerificationFinished=true;
        }
    }
}
#endif
