using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    // Local presentation only: no shared-material edits or replicated role markers.
    public sealed class SaloonSafeHighlight : MonoBehaviour
    {
        SaloonGame game;
        Transform safe;
        Collider safeCollider;
        Material glow;
        readonly List<MeshRenderer> overlays=new List<MeshRenderer>();
        public bool Highlighted { get; private set; }
        public string Key { get; private set; }="";
        public string Action { get; private set; }="";
        public string Status { get; private set; }="";
        public Vector3 Anchor=>safeCollider!=null?new Vector3(safeCollider.bounds.center.x,safeCollider.bounds.max.y+.16f,safeCollider.bounds.center.z):Vector3.zero;
        public int OverlayCount=>overlays.Count;

        public void Initialize(SaloonGame owner){game=owner;}
        void LateUpdate(){Refresh();}

        bool Bind()
        {
            if(safe!=null)return overlays.Count>0;
            safe=GameObject.Find("Safe visual")?.transform;if(safe==null)return false;
            safeCollider=safe.GetComponent<Collider>();
            var shader=Resources.Load<Shader>("Shaders/SaloonSafeHighlight");
            if(shader==null)return false;
            glow=new Material(shader){name="Local safe proximity glow",hideFlags=HideFlags.HideAndDontSave};
            // Snapshot the sources before adding any overlay children.
            foreach(var source in safe.GetComponentsInChildren<MeshFilter>())
            {
                var original=source.GetComponent<MeshRenderer>();
                if(original==null||!original.enabled||source.sharedMesh==null)continue;
                var overlay=new GameObject("Safe proximity highlight");overlay.transform.SetParent(source.transform,false);
                overlay.layer=source.gameObject.layer;
                overlay.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
                var renderer=overlay.AddComponent<MeshRenderer>();
                var materials=new Material[source.sharedMesh.subMeshCount];
                for(int i=0;i<materials.Length;i++)materials[i]=glow;
                renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;
                renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;renderer.enabled=false;
                overlays.Add(renderer);
            }
            return overlays.Count>0;
        }

        public void Refresh()
        {
            var view=game?.View;var self=game?.Self;
            string key=view?.ownRole=="Sheriff"?"C":view?.ownRole=="Outlaw"&&view.moneyBag==false?"H":"";
            bool visible=view?.phase==Phase.Turns&&self?.alive==true&&key!=""&&
                !game.MenuOpen&&!game.GraphicsMenuOpen&&!game.ShowRole&&!game.IsSpectating&&
                !game.Connection.Reconnecting&&!game.PlayingTableGame&&game.NearSafe;
            Highlighted=visible&&Bind();
            Key=Highlighted?key:"";
            Action=Highlighted?(key=="C"?"INSPECT SAFE":"WORK SAFE / TAKE MONEY"):"";
            Status=!Highlighted?"":self.robbing?"Working… stay beside the safe":view.busyRemaining>0?"Finish your current action":
                view.actionAvailable?"Press "+key+" to interact":view.actionBlockReason==""||view.actionBlockReason==null?"Action spent · available next round":view.actionBlockReason;
            foreach(var renderer in overlays)if(renderer!=null)renderer.enabled=Highlighted;
            if(Highlighted)glow.SetColor("_Color",new Color(1,.67f,.20f,.68f+.12f*Mathf.Sin(Time.unscaledTime*2.4f)));
        }

        void OnDisable()
        {Highlighted=false;Key=Action=Status="";foreach(var renderer in overlays)if(renderer!=null)renderer.enabled=false;}
        void OnDestroy()
        {foreach(var renderer in overlays)if(renderer!=null)Destroy(renderer.gameObject);if(glow!=null)Destroy(glow);}
    }
}
