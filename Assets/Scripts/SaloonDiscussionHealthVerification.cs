#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        IEnumerator VerifyDiscussionHealth()
        {
            void Check(bool ok,string text)
            {if(!ok){VerificationFinished=true;throw new InvalidOperationException("Discussion health: "+text);}Debug.Log("SALOON_DISCUSSION_HEALTH_CHECK "+text);}
            var host=players[Connection.LocalId];var peer=players.Values.First(p=>p.bot);
            var originalRole=host.role;
            host.role=Role.Renegade;
            foreach(var p in players.Values){p.hp=100;p.deathAt=-1;p.chosenRoom=0;p.companion=null;}
            peer.hp=40;rounds.Start(players.Keys,Time.time,30,30,6);phase=rounds.Phase;PositionEveryone(true);Broadcast();MenuOpen=false;
            yield return new WaitForSeconds(.5f);
            var text=avatars[peer.id.ToString()].transform.Find("Health dots").GetComponent<TextMesh>();
            var direction=text.transform.position-Eye(host);
            yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
            yield return new WaitForSeconds(.2f);
            var name=avatars[peer.id.ToString()].transform.Find("Name").GetComponent<TextMesh>();
            Check(name.alignment==TextAlignment.Center&&text.alignment==TextAlignment.Center&&name.text.Contains("\n"),"name, revealed role and health are center-aligned");
            Check(name.transform.localScale==text.transform.localScale,"name and health share the same distance scale");
            var nameView=ViewCamera.WorldToViewportPoint(name.GetComponent<Renderer>().bounds.center);
            var healthView=ViewCamera.WorldToViewportPoint(text.GetComponent<Renderer>().bounds.center);
            Check(Mathf.Abs(nameView.x-healthView.x)<.003f,"multiline role and health share a visual center above the player");
            Check(text.gameObject.activeInHierarchy&&text.text.Contains("40%")&&!text.text.Contains("shot"),"seated players show compact health markers and percentage without shot-count text");
            var label=text.GetComponent<Renderer>();var view=ViewCamera.WorldToViewportPoint(label.bounds.center);
            Check(view.z>0&&view.x>0&&view.x<1&&view.y>0&&view.y<1,"seated health stays in the discussion camera view");
            label.enabled=false;var without=HealthPixels();label.enabled=true;var visible=HealthPixels();
            int changed=0;for(int i=0;i<visible.Length;i++)
                if(Mathf.Abs(visible[i].r-without[i].r)+Mathf.Abs(visible[i].g-without[i].g)+Mathf.Abs(visible[i].b-without[i].b)>80)changed++;
            Check(changed>50,"health markers actually render above the seated character and hat ("+changed+" pixels)");
            WindowPreview(ViewCamera.transform.position,text.transform.position,78,"discussion-health");
            foreach(int hp in new[]{100,80,60,40,20,1})
            {
                peer.hp=hp;Broadcast();yield return null;
                string activeColor=hp<=20?"#FF6757":"#F1E4CB";
                int filled=text.text.Split(new[]{"<color="+activeColor+">●</color>"},StringSplitOptions.None).Length-1;
                Check(text.text.Contains(SaloonHealth.Percentage(hp))&&!text.text.Contains("shot")&&filled==SaloonHealth.ShotsToKill(hp),"visible "+hp+"% label has exactly "+SaloonHealth.ShotsToKill(hp)+" filled markers");
            }
            peer.hp=40;Broadcast();yield return null;
            host.role=originalRole;Broadcast();
            Debug.Log("SALOON_DISCUSSION_HEALTH PASS");
        }
        Color32[] HealthPixels()
        {
            var old=ViewCamera.targetTexture;var previous=RenderTexture.active;
            var target=new RenderTexture(1280,900,24);var image=new Texture2D(1280,900,TextureFormat.RGB24,false);
            try
            {
                ViewCamera.targetTexture=target;ViewCamera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1280,900),0,0);image.Apply();return image.GetPixels32();
            }
            finally{ViewCamera.targetTexture=old;RenderTexture.active=previous;Destroy(image);target.Release();Destroy(target);}
        }
    }
}
#endif
