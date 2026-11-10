using UnityEngine;

namespace Saloon
{
    public static class SaloonRoomSigns
    {
        public static void Apply()
        {
            var world=GameObject.Find("Saloon World");if(world==null)return;
            var existing=world.transform.Find("Minigame room signs");
            if(existing!=null)
            {
                if(existing.childCount>0)existing.position+=Vector3.up*(3.12f-existing.GetChild(0).position.y);
                foreach(Transform child in existing)
                {
                    var position=child.position;
                    if(child.name.StartsWith(SaloonStations.Name(1)))position.x=-5;
                    if(child.name.StartsWith(SaloonStations.Name(2)))position.x=5;
                    child.position=position;
                }
                foreach(var text in existing.GetComponentsInChildren<TextMesh>(true)){text.gameObject.SetActive(true);SaloonWorldText.Attach(text,true);}return;
            }
            var root=new GameObject("Minigame room signs").transform;root.SetParent(world.transform,false);
            var wood=SaloonWorld.Material("Room sign walnut",new Color(.17f,.08f,.04f));
            var trim=SaloonWorld.Material("Room sign brass",new Color(.63f,.43f,.17f));
            for(int room=1;room<=2;room++)
            {
                var position=new Vector3(room==1?-5:5,3.12f,-1.55f);
                SaloonWorld.Box(SaloonStations.Name(room)+" sign frame",position,new Vector3(1.84f,.48f,.045f),trim,root);
                SaloonWorld.Box(SaloonStations.Name(room)+" sign",position+Vector3.forward*.026f,new Vector3(1.78f,.42f,.02f),wood,root);
                var label=new GameObject(SaloonStations.Name(room)+" label");label.transform.SetParent(root,false);
                label.transform.position=position+Vector3.forward*.045f;label.transform.rotation=Quaternion.Euler(0,180,0);
                var text=label.AddComponent<TextMesh>();text.text=SaloonStations.Name(room).ToUpperInvariant()+"\n2 seats · Ready to reserve";
                text.characterSize=.012f;text.fontSize=64;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.94f,.83f,.59f);SaloonWorldText.Attach(text,true);
            }
        }
    }
}
