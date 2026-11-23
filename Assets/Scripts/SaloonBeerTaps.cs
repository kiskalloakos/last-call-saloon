using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    // Retain the component name for existing scenes; the counter now has one shared tray.
    public sealed class SaloonBeerTaps : MonoBehaviour
    {
        public static readonly Vector3 Position=new Vector3(-4.6f,1.445f,3.48f);
        readonly List<GameObject> mugs=new List<GameObject>();
        readonly List<Material> materials=new List<Material>();
        Transform tray;
        SaloonGame game;
        Snapshot view;
        PlayerView self;
        public bool Visible=>tray!=null&&tray.gameObject.activeSelf;
        public static bool WithinReach(Vector3 position,float distance)
            =>Vector3.ProjectOnPlane(Position-position,Vector3.up).sqrMagnitude<=distance*distance;

        Material Mat(string name,Color color)
        {var material=SaloonWorld.Material(name,color,.15f);materials.Add(material);return material;}

        static void Disc(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var shape=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shape.name=name;shape.transform.SetParent(parent,false);
            shape.transform.localPosition=position;shape.transform.localScale=scale;
            shape.GetComponent<Collider>().enabled=false;Destroy(shape.GetComponent<Collider>());
            shape.GetComponent<Renderer>().sharedMaterial=material;
        }

        public void Initialize(SaloonGame owner)
        {
            game=owner;
            tray=new GameObject("Shared beer tray").transform;tray.SetParent(transform,false);tray.position=Position;
            var wood=Mat("Beer tray walnut",new Color(.25f,.14f,.075f));
            Disc(tray,"Serving tray",Vector3.zero,new Vector3(1.28f,.012f,.68f),wood);
            for(int i=0;i<owner.tuning.sharedBeers;i++)
            {
                var mug=SaloonArt.Model("BeerMug",tray,new Vector3(.18f,.20f,.16f));
                mug.name="Ready beer "+(i+1);mug.transform.localPosition=new Vector3((i%4-1.5f)*.26f,.014f,(i/4-.5f)*.25f);
                foreach(var collider in mug.GetComponentsInChildren<Collider>())collider.enabled=false;
                mugs.Add(mug);
            }
            SaloonSmokingArea.SetLayer(tray,SaloonSmokingArea.IndoorLayer);
            tray.gameObject.SetActive(false);
        }

        public void Clear(){tray.gameObject.SetActive(false);view=null;self=null;}

        public void Apply(Snapshot snapshot)
        {
            view=snapshot;self=snapshot.players.FirstOrDefault(p=>p.id==snapshot.ownId);
            // Keep the counter dressed while the completed round settles.
            tray.gameObject.SetActive((snapshot.phase==Phase.Turns||snapshot.phase==Phase.Aftermath)&&snapshot.players.Any(p=>p.room==0));
            for(int i=0;i<mugs.Count;i++)mugs[i].SetActive(i<snapshot.beers);
        }

        public string HintForRay(Ray ray)
        {
            if(view?.phase!=Phase.Turns||self?.alive!=true||self.room!=0||
                !WithinReach(new Vector3(self.x,self.y,self.z),game.tuning.interactionDistance))return "";
            if(view.beers<=0)return "Beer tray empty.";
            if(self.health>=self.maxHealth)return "Full health.";
            if(!view.actionAvailable)return "";
            return "E · Drink beer (+"+SaloonHealth.BeerHealingAmount(SaloonHealth.BeerHealing,view.cigaretteBuff)+"%)";
        }

        void OnDestroy(){foreach(var material in materials)Destroy(material);}
    }
}
