using UnityEngine;

namespace Saloon
{
    public partial class SaloonGame
    {
        Vector3 damageCameraImpulse;
        public Vector2 DamageSide { get; private set; }
        public float ConfirmedHitPulse=>Mathf.Clamp01((confirmedUntil-Time.unscaledTime)/.28f);
        public bool ConfirmedKill { get; private set; }
        public bool ConfirmedFriendly { get; private set; }
        float confirmedUntil;
        int shownConfirmation=-1,shownEmpty=-1;

        void BeginDamageFeedback(PlayerView self,int damage)
        {
            var incoming=Quaternion.Euler(0,-self.yaw,0)*-new Vector3(self.hitX,self.hitY,self.hitZ);
            DamageSide=self.hitReflected?Vector2.zero:new Vector2(incoming.x,incoming.z);
            float strength=damage>SaloonHealth.RevolverDamage?1.35f:1;
            damageCameraImpulse=new Vector3(incoming.z*3.2f,-incoming.x*1.1f,-incoming.x*3.4f)*strength;
            if(self.hitReflected)damageCameraImpulse=new Vector3(-1.3f,0,0);
            hitKick=1;HitFlash=.52f*strength;
            SaloonCombatAudio.Play(transform,ViewCamera.transform.position,self.hitReflected?SaloonCombatAudio.Mechanism:SaloonCombatAudio.Thud,.46f,.72f,0,true);
        }

        void TickCombatFeedback()
        {
            shotKick=Mathf.MoveTowards(shotKick,0,Time.unscaledDeltaTime*4.5f);
            hitKick=Mathf.MoveTowards(hitKick,0,Time.unscaledDeltaTime*6.5f);
            HitFlash=Mathf.MoveTowards(HitFlash,0,Time.unscaledDeltaTime*1.6f);
        }

        void ApplyCombatFeedback(PlayerView self)
        {
            if(self==null)return;
            if(shownConfirmation>=0&&View.hitSequence>shownConfirmation&&View.confirmedHitAge<.5f)
            {
                ConfirmedKill=View.confirmedKill;ConfirmedFriendly=View.confirmedFriendly;
                confirmedUntil=Time.unscaledTime+(ConfirmedKill?.42f:.28f);
                SaloonCombatAudio.Play(transform,ViewCamera.transform.position,SaloonCombatAudio.Mechanism,
                    ConfirmedKill?.29f:.19f,ConfirmedKill?.85f:1.75f,0,true);
            }
            if(shownEmpty>=0&&View.emptyClicks>shownEmpty&&View.emptyClickAge<.4f)
                SaloonCombatAudio.Play(transform,ViewCamera.transform.position,SaloonCombatAudio.Mechanism,.28f,.85f,0,true);
            shownConfirmation=View.hitSequence;shownEmpty=View.emptyClicks;
        }

        void ResetCombatFeedback()
        {
            shownConfirmation=shownEmpty=-1;confirmedUntil=0;DamageSide=Vector2.zero;damageCameraImpulse=Vector3.zero;
            ConfirmedKill=ConfirmedFriendly=false;
            foreach(var source in GetComponentsInChildren<AudioSource>())
                if(source.gameObject.name=="Combat sound")Destroy(source.gameObject);
        }
    }

    public partial class SaloonHUD
    {
        static Texture2D damageEdge;
        void CombatOverlay()
        {
            if(Game.HitFlash<=0||Game.IsSpectating)return;
            if(damageEdge==null)
            {
                damageEdge=new Texture2D(64,1,TextureFormat.RGBA32,false){name="Damage edge",wrapMode=TextureWrapMode.Clamp};
                for(int x=0;x<64;x++)damageEdge.SetPixel(x,0,new Color(1,1,1,Mathf.Pow(1-x/63f,2)));
                damageEdge.Apply();
            }
            var old=GUI.color;var side=Game.DamageSide;float flash=Game.HitFlash;
            GUI.color=new Color(.65f,.07f,.035f,flash*(.35f+Mathf.Max(0,-side.x)*.65f));
            GUI.DrawTexture(new Rect(0,0,190,720),damageEdge);
            GUI.color=new Color(.65f,.07f,.035f,flash*(.35f+Mathf.Max(0,side.x)*.65f));
            GUI.DrawTextureWithTexCoords(new Rect(1090,0,190,720),damageEdge,new Rect(1,0,-1,1));
            // Rotate the same soft edge for front/back feedback.
            var matrix=GUI.matrix;
            GUIUtility.RotateAroundPivot(90,new Vector2(640,360));
            GUI.color=new Color(.65f,.07f,.035f,flash*(.35f+Mathf.Max(0,side.y)*.65f));
            GUI.DrawTexture(new Rect(280,-280,140,1280),damageEdge);
            GUI.color=new Color(.65f,.07f,.035f,flash*(.35f+Mathf.Max(0,-side.y)*.65f));
            GUI.DrawTextureWithTexCoords(new Rect(860,-280,140,1280),damageEdge,new Rect(1,0,-1,1));
            GUI.matrix=matrix;GUI.color=old;
        }

        void CombatHitMarker()
        {
            float pulse=Game.ConfirmedHitPulse;
            if(pulse<=0||Game.IsSpectating||Game.MenuOpen)return;
            var color=Game.ConfirmedFriendly?new Color(1,.40f,.20f,pulse):Game.ConfirmedKill?new Color(1,.75f,.30f,pulse):new Color(1,.95f,.82f,pulse);
            float gap=(Game.ConfirmedKill?11:8)+pulse*3;
            var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(45,new Vector2(640,360));
            Fill(new Rect(640-gap-6,359,6,2),color);Fill(new Rect(640+gap,359,6,2),color);
            Fill(new Rect(639,360-gap-6,2,6),color);Fill(new Rect(639,360+gap,2,6),color);
            GUI.matrix=matrix;
        }
    }
}
