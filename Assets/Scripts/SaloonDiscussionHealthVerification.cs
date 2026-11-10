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
            foreach(var p in players.Values){p.hp=3;p.deathAt=-1;p.chosenRoom=0;p.companion=null;}
            peer.hp=2;rounds.Start(players.Keys,Time.time,30,30,6);phase=rounds.Phase;PositionEveryone(true);Broadcast();MenuOpen=false;
            yield return new WaitForSeconds(.5f);
            var text=avatars[peer.id.ToString()].transform.Find("Health dots").GetComponent<TextMesh>();
            var direction=text.transform.position-Eye(host);
            yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
            yield return new WaitForSeconds(.2f);
            Check(text.gameObject.activeInHierarchy&&text.text.Contains("2 / 3 HP"),"seated players show their current numeric health and health markers");
            var label=text.GetComponent<Renderer>();var view=ViewCamera.WorldToViewportPoint(label.bounds.center);
            Check(view.z>0&&view.x>0&&view.x<1&&view.y>0&&view.y<1,"seated health stays in the discussion camera view");
            label.enabled=false;var without=HealthPixels();label.enabled=true;var visible=HealthPixels();
            int changed=0;for(int i=0;i<visible.Length;i++)
                if(Mathf.Abs(visible[i].r-without[i].r)+Mathf.Abs(visible[i].g-without[i].g)+Mathf.Abs(visible[i].b-without[i].b)>80)changed++;
            Check(changed>50,"health markers actually render above the seated character and hat ("+changed+" pixels)");
            WindowPreview(ViewCamera.transform.position,text.transform.position,78,"discussion-health");
            peer.hp=3;Broadcast();yield return null;
            Check(text.text.Contains("3 / 3 HP"),"discussion health updates when the snapshot health changes");
            Debug.Log("SALOON_DISCUSSION_HEALTH PASS");
        }
        Color32[] HealthPixels()
        {
            var old=ViewCamera.targetTexture;var previous=RenderTexture.active;
            var target=new RenderTexture(640,450,24);var image=new Texture2D(640,450,TextureFormat.RGB24,false);
            try
            {
                ViewCamera.targetTexture=target;ViewCamera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,640,450),0,0);image.Apply();return image.GetPixels32();
            }
            finally{ViewCamera.targetTexture=old;RenderTexture.active=previous;Destroy(image);target.Release();Destroy(target);}
        }
    }
}
#endif
