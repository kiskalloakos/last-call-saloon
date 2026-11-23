using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    public static class SaloonRoomSigns
    {
        public const string RootName="Minigame wall stencils";

        public static void Apply()
        {
            var world=GameObject.Find("Saloon World");if(world==null)return;
            // Remove the plaques from saved scenes as well as freshly built worlds.
            var signs=world.transform.Find("Minigame room signs");
            if(signs!=null)
            {
                signs.gameObject.SetActive(false);
                if(Application.isPlaying)Object.Destroy(signs.gameObject);else Object.DestroyImmediate(signs.gameObject);
            }
            var existing=world.transform.Find(RootName);
            if(existing!=null){Place(existing);return;}
            var root=new GameObject(RootName).transform;root.SetParent(world.transform,false);
            for(int room=1;room<=2;room++)
            {
                var ink=SaloonWorld.Material(room==1?"Coin flip wall ink":"Blackjack wall ink",
                    room==1?new Color(.43f,.25f,.085f):new Color(.13f,.23f,.20f),0);
                ink.mainTexture=Resources.Load<Texture2D>("Art/RoomStencils/"+(room==1?"CoinFlip":"Blackjack"));
                ink.SetFloat("_Mode",1);ink.SetFloat("_Cutoff",.35f);
                ink.EnableKeyword("_ALPHATEST_ON");ink.SetOverrideTag("RenderType","TransparentCutout");ink.renderQueue=(int)RenderQueue.AlphaTest;
                var graphic=GameObject.CreatePrimitive(PrimitiveType.Quad);graphic.name=SaloonStations.Name(room)+" wall stencil";
                graphic.transform.SetParent(root,false);
                var collider=graphic.GetComponent<Collider>();collider.enabled=false;
                if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);
                var renderer=graphic.GetComponent<MeshRenderer>();renderer.sharedMaterial=ink;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            Place(root);
            SaloonSmokingArea.SetLayer(root,SaloonSmokingArea.IndoorLayer);
        }
        static void Place(Transform root)
        {
            foreach(Transform graphic in root)
            {
                graphic.localPosition=new Vector3(graphic.name.StartsWith("Coin Flip")?-5:5,2.37f,SaloonRoomWindows.FrontZ+.096f);
                graphic.localRotation=Quaternion.Euler(0,180,0);graphic.localScale=new Vector3(3.1f,2.81f,1);
            }
        }
    }
}
