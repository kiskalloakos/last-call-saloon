using UnityEngine;

namespace Saloon
{
    public sealed class SaloonHitReaction
    {
        readonly Renderer[] renderers;
        readonly MaterialPropertyBlock flash=new MaterialPropertyBlock();
        float hitAt=-10;
        bool tinted;
        public float LastHitAt=>hitAt;
        public bool FlashVisible=>tinted;
        public SaloonHitReaction(GameObject model){renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);}
        public void Trigger(float age){hitAt=Time.time-Mathf.Max(0,age);}
        public void Clear()
        {
            if(tinted)foreach(var renderer in renderers)renderer.SetPropertyBlock(null);
            tinted=false;hitAt=-10;
        }
        public void Tick()
        {
            float elapsed=Time.time-hitAt;
            if(elapsed<0||elapsed>.20f){if(tinted)ClearTint();return;}
            float strength=1-Mathf.SmoothStep(0,1,elapsed/.20f);
            flash.SetColor("_Color",Color.Lerp(Color.white,new Color(1.9f,.28f,.16f),strength));
            foreach(var renderer in renderers)renderer.SetPropertyBlock(flash);
            tinted=true;
        }
        void ClearTint(){foreach(var renderer in renderers)renderer.SetPropertyBlock(null);tinted=false;}
    }
}
