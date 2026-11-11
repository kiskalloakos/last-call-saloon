using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    // Draw the held revolver after the room, with its own depth buffer. World
    // weapons, window barriers and the host's shot ray retain normal occlusion.
    [RequireComponent(typeof(Camera))]
    public sealed class SaloonViewmodel : MonoBehaviour
    {
        public const int Layer=14;
        Camera world,overlay;
        public Camera Overlay=>overlay;

        public static void Attach(Camera camera,GameObject weapon)
        {
            var pass=camera.GetComponent<SaloonViewmodel>();
            if(pass==null)pass=camera.gameObject.AddComponent<SaloonViewmodel>();
            foreach(var child in weapon.GetComponentsInChildren<Transform>(true))child.gameObject.layer=Layer;
            foreach(var renderer in weapon.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
            }
        }

        void Awake()
        {
            world=GetComponent<Camera>();world.cullingMask&=~(1<<Layer);
            var go=new GameObject("First person weapon camera");go.transform.SetParent(transform,false);
            overlay=go.AddComponent<Camera>();Configure();
        }

        void Configure()
        {
            if(overlay==null)return;
            overlay.CopyFrom(world);overlay.targetTexture=null;
            overlay.enabled=world.enabled;overlay.depth=world.depth+1;
            overlay.clearFlags=CameraClearFlags.Depth;overlay.cullingMask=1<<Layer;
            overlay.nearClipPlane=.01f;overlay.farClipPlane=4;
            overlay.useOcclusionCulling=false;
        }

        void OnPreCull(){Configure();}

        // Camera.Render() only renders one camera. Offscreen previews explicitly
        // compose the same second camera that Unity stacks during ordinary play.
        public void RenderOverlay()
        {
            Configure();overlay.targetTexture=world.targetTexture;
            try{overlay.Render();}
            finally{overlay.targetTexture=null;}
        }

        void OnDestroy(){if(overlay!=null)Destroy(overlay.gameObject);}
    }
}
