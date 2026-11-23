using UnityEngine;

namespace Saloon
{
    public sealed class SaloonEntranceDoor : MonoBehaviour
    {
        public static readonly Vector3 Position=new Vector3(0,.06f,-6.98f);
        public static SaloonEntranceDoor Build()
        {
            var world=GameObject.Find("Saloon World");
            var existing=world.GetComponentInChildren<SaloonEntranceDoor>();
            if(existing!=null){existing.PlaceLeaves();return existing;}
            var root=new GameObject("Saloon entrance doors");root.transform.SetParent(world.transform,false);root.transform.position=Position;
            var door=root.AddComponent<SaloonEntranceDoor>();
            Transform Leaf(string name,float hinge,float center)
            {
                var pivot=new GameObject(name).transform;pivot.SetParent(root.transform,false);pivot.localPosition=Vector3.right*hinge;
                var model=SaloonArt.Model("Door",pivot,new Vector3(1.5f,2.94f,.14f));model.transform.localPosition=Vector3.right*center;
                var solid=model.AddComponent<BoxCollider>();solid.center=Vector3.up*1.47f;solid.size=new Vector3(1.52f,2.94f,.16f);
                return pivot;
            }
            Leaf("Left entrance hinge",-1.5f,.75f);Leaf("Right entrance hinge",1.5f,-.75f);
            door.PlaceLeaves();
            // Half-height doors frame the sunset; full-height collision keeps room selection
            // as the only route to the private back deck.
            SaloonSmokingArea.SetLayer(root.transform,SaloonSmokingArea.IndoorLayer);return door;
        }
        void PlaceLeaves()
        {
            foreach(var solid in GetComponentsInChildren<BoxCollider>())
            {
                var model=solid.transform;model.localPosition=new Vector3(model.localPosition.x,.32f,0);
                model.localScale=new Vector3(1,1.05f/2.94f,1);
                // Collider dimensions compensate for the shorter visual model.
                solid.center=Vector3.up*((1.47f-.32f)/model.localScale.y);
                solid.size=new Vector3(1.52f,2.94f/model.localScale.y,.16f);
            }
        }
    }
}
