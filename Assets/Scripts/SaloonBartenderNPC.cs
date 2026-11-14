using System.Collections.Generic;
using UnityEngine;

namespace Saloon
{
    // CC0 bartender by CDmir / TinyWorlds. See ArtSource/Bartender/SOURCE.txt.
    public sealed class SaloonBartenderNPC : MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();
        Transform model;
        TextMesh sign;
        SaloonGame game;
        public bool Visible=>model!=null&&model.gameObject.activeInHierarchy;

        void Start(){game=GetComponent<SaloonGame>();Refresh();}
        void BuildCharacter()
        {
            var prefab=Resources.Load<GameObject>("Art/Bartender/Bartender");
            if(prefab==null)throw new System.InvalidOperationException("Missing licensed bartender model.");
            model=new GameObject("Bartender").transform;model.SetParent(transform,false);
            model.position=SaloonGame.BartenderPosition;model.rotation=Quaternion.Euler(0,180,0);
            var character=Instantiate(prefab,model);character.name="CDmir Bartender";
            foreach(var animator in character.GetComponentsInChildren<Animator>())animator.enabled=false;
            var renderers=character.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float scale=1.88f/bounds.size.y;character.transform.localScale*=scale;
            character.transform.position+=Vector3.up*(model.position.y-bounds.min.y)*scale;
            foreach(var renderer in renderers)
            {
                var assigned=renderer.sharedMaterials;
                for(int i=0;i<assigned.Length;i++)
                {
                    string key=assigned[i].name.Replace(" (Instance)","");
                    var texture=Resources.Load<Texture2D>("Art/Bartender/"+key);
                    if(texture==null)throw new System.InvalidOperationException("Missing bartender texture: "+key);
                    var material=new Material(Shader.Find("Standard")){name="Bartender / "+key,mainTexture=texture};
                    material.SetFloat("_Glossiness",.12f);materials.Add(material);assigned[i]=material;
                }
                renderer.sharedMaterials=assigned;
                if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
            }
            foreach(var collider in character.GetComponentsInChildren<Collider>())collider.enabled=false;
            var label=new GameObject("Bartender sign");label.transform.SetParent(transform,false);
            label.transform.position=SaloonGame.BartenderPosition+Vector3.up*2.32f;
            sign=label.AddComponent<TextMesh>();sign.anchor=TextAnchor.MiddleCenter;sign.alignment=TextAlignment.Center;
            sign.characterSize=.014f;sign.fontSize=42;SaloonWorldText.Attach(sign);
        }
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(game==null)game=GetComponent<SaloonGame>();
            bool present=game.View?.bartenderArrived==true;
            if(!present)
            {
                if(model!=null)model.gameObject.SetActive(false);
                if(sign!=null)sign.gameObject.SetActive(false);
                return;
            }
            if(model==null)BuildCharacter();
            model.gameObject.SetActive(true);
            int mood=game.View?.bartenderMood??100;
            float grump=1-Mathf.Clamp01(mood/100f),time=Time.time;
            model.localScale=new Vector3(1,1+Mathf.Sin(time*1.6f)*.002f,1);
            sign.gameObject.SetActive(mood>=0);
            sign.text="BARTENDER\n"+SaloonBartenderMood.Label(mood)+" · "+mood+" / 100";
            sign.color=Color.Lerp(new Color(.95f,.33f,.20f),new Color(.97f,.75f,.34f),Mathf.Clamp01(mood/100f));
        }
        void OnDestroy(){foreach(var material in materials)if(material!=null)Destroy(material);}
    }
}
