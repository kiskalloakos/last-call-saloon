using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    // World labels obey the same depth buffer as privacy glass and room geometry.
    public sealed class SaloonWorldText : MonoBehaviour
    {
        [SerializeField] bool singleSided;
        Font font;
        Material material;
        public static void Attach(TextMesh text,bool singleSided=false)
        {
            var depth=text.GetComponent<SaloonWorldText>()??text.gameObject.AddComponent<SaloonWorldText>();
            depth.singleSided=singleSided;depth.EnsureMaterial();
        }
        void OnEnable(){EnsureMaterial();}
        void EnsureMaterial()
        {
            var text=GetComponent<TextMesh>();if(text==null)return;
            font=text.font!=null?text.font:Resources.Load<Font>("Fonts/BarlowSemiCondensed-Medium")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.font=font;
            if(material==null)material=new Material(Resources.Load<Shader>("Shaders/SaloonWorldText")){name="Depth-tested world label"};
            material.mainTexture=font.material.mainTexture;material.SetInt("_Cull",(int)(singleSided?CullMode.Back:CullMode.Off));
            text.GetComponent<Renderer>().sharedMaterial=material;
            Font.textureRebuilt-=UpdateAtlas;Font.textureRebuilt+=UpdateAtlas;
        }
        void UpdateAtlas(Font rebuilt){if(rebuilt==font&&material!=null)material.mainTexture=font.material.mainTexture;}
        void OnDisable(){Font.textureRebuilt-=UpdateAtlas;}
        void OnDestroy()
        {
            Font.textureRebuilt-=UpdateAtlas;
            if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
        }
    }
}
