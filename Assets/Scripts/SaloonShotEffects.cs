using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    public enum SaloonImpactSurface { None,Wood,Metal,Glass,Cloth,Head,Stone }

    public sealed class SaloonShotEffects : MonoBehaviour
    {
        static Material particleMaterial;
        LineRenderer trail;
        float born;
        Vector3 anchor;

        public static SaloonImpactSurface Surface(Collider collider)
        {
            if(collider.GetComponentInParent<SaloonBreakable>()!=null)return SaloonImpactSurface.Glass;
            if(collider.GetComponentInParent<SaloonBody>()!=null)return SaloonImpactSurface.Cloth;
            if(collider.GetComponent<SaloonRoomWall>()!=null)return SaloonImpactSurface.Stone;
            if(collider.GetComponent<SaloonRoomWindow>()!=null)return SaloonImpactSurface.Glass;
            var renderer=collider.GetComponent<Renderer>()??collider.GetComponentInParent<Renderer>();
            string name=(collider.name+" "+(renderer!=null&&renderer.sharedMaterial!=null?renderer.sharedMaterial.name:"")).ToLowerInvariant();
            if(name.Contains("glass")||name.Contains("window"))return SaloonImpactSurface.Glass;
            if(name.Contains("metal")||name.Contains("iron")||name.Contains("safe")||name.Contains("brass"))return SaloonImpactSurface.Metal;
            if(name.Contains("stone")||name.Contains("brick")||name.Contains("plaster"))return SaloonImpactSurface.Stone;
            return SaloonImpactSurface.Wood;
        }

        public static void Play(Transform owner,Vector3 muzzle,Vector3 end,Vector3 normal,SaloonImpactSurface surface,bool local)
        {
            var go=new GameObject("Shot feedback");go.layer=SaloonSmokingArea.IndoorLayer;
            go.transform.SetParent(owner);go.transform.position=muzzle;
            var effect=go.AddComponent<SaloonShotEffects>();effect.born=Time.time;effect.anchor=muzzle;
            effect.trail=go.AddComponent<LineRenderer>();effect.trail.sharedMaterial=ParticleMaterial;
            effect.trail.startWidth=.024f;effect.trail.endWidth=.005f;effect.trail.positionCount=2;
            effect.trail.SetPosition(0,muzzle);effect.trail.SetPosition(1,end);
            effect.trail.shadowCastingMode=ShadowCastingMode.Off;
            Burst(go.transform,muzzle,Vector3.up,new Color(.65f,.62f,.55f,.42f),8,.13f,.30f,.65f,0,local?SaloonViewmodel.Layer:SaloonSmokingArea.IndoorLayer);
            if(surface!=SaloonImpactSurface.None)
            {
                if(normal.sqrMagnitude<.1f)normal=(muzzle-end).normalized;
                bool flesh=surface==SaloonImpactSurface.Cloth||surface==SaloonImpactSurface.Head;
                bool hard=surface==SaloonImpactSurface.Metal||surface==SaloonImpactSurface.Glass;
                Color color=flesh?new Color(.64f,.48f,.34f,.8f):hard?new Color(1,.83f,.49f,.95f):new Color(.71f,.48f,.25f,.9f);
                Burst(go.transform,end+normal*.015f,normal,color,hard?16:14,hard?.035f:.065f,hard?3.8f:2.2f,.34f,1.1f);
                if(surface==SaloonImpactSurface.Head)
                    Burst(go.transform,end,normal,new Color(.66f,.12f,.07f,.85f),12,.09f,1.2f,.32f,.5f);
                if(!flesh)
                {
                    Burst(go.transform,end,normal,new Color(.65f,.54f,.40f,.48f),8,.18f,.65f,.55f,0);
                    if(surface!=SaloonImpactSurface.Glass)ImpactMark(go.transform,end,normal);
                }
                if(flesh)SaloonCombatAudio.Play(go.transform,end,SaloonCombatAudio.Thud,.32f,.8f);
            }
            Destroy(go,1.8f);
        }

        static void ImpactMark(Transform owner,Vector3 position,Vector3 normal)
        {
            var mark=new GameObject("Brief bullet mark");mark.layer=SaloonSmokingArea.IndoorLayer;
            mark.transform.SetParent(owner);mark.transform.SetPositionAndRotation(position+normal*.006f,Quaternion.LookRotation(-normal));
            mark.transform.localScale=Vector3.one*.11f;
            var filter=mark.AddComponent<MeshFilter>();
            filter.sharedMesh=MarkMesh;
            var renderer=mark.AddComponent<MeshRenderer>();renderer.sharedMaterial=ParticleMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            var tint=new MaterialPropertyBlock();tint.SetColor("_Color",new Color(.12f,.07f,.035f,.75f));renderer.SetPropertyBlock(tint);
        }

        static Mesh markMesh;
        static Mesh MarkMesh
        {
            get
            {
                if(markMesh!=null)return markMesh;
                markMesh=new Mesh{name="Bullet mark quad"};
                markMesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
                markMesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
                markMesh.triangles=new[]{0,2,1,0,3,2};markMesh.RecalculateNormals();return markMesh;
            }
        }

        static Material ParticleMaterial
        {
            get
            {
                if(particleMaterial!=null)return particleMaterial;
                var texture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Soft combat particle",wrapMode=TextureWrapMode.Clamp};
                for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                {
                    float radius=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f;
                    texture.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-radius),1.5f)));
                }
                texture.Apply();particleMaterial=new Material(Shader.Find("Sprites/Default")){name="Combat particles",mainTexture=texture};
                return particleMaterial;
            }
        }

        static void Burst(Transform owner,Vector3 position,Vector3 direction,Color color,int count,float size,float speed,float lifetime,float gravity,int layer=SaloonSmokingArea.IndoorLayer)
        {
            var go=new GameObject("Impact particles");go.layer=layer;go.transform.SetParent(owner);
            go.transform.SetPositionAndRotation(position,Quaternion.LookRotation(direction.sqrMagnitude>.01f?direction:Vector3.up));
            var particles=go.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.loop=false;main.playOnAwake=false;main.duration=lifetime;
            main.startLifetime=new ParticleSystem.MinMaxCurve(lifetime*.6f,lifetime);main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.3f,speed);
            main.startSize=new ParticleSystem.MinMaxCurve(size*.5f,size);main.startColor=color;main.gravityModifier=gravity;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=count;
            var emission=particles.emission;emission.enabled=false;
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=38;shape.radius=.015f;
            var fade=particles.colorOverLifetime;fade.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.6f,.35f),new GradientAlphaKey(0,1)});fade.color=gradient;
            var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=ParticleMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
            if(gravity>0)
            {
                renderer.renderMode=ParticleSystemRenderMode.Stretch;
                renderer.lengthScale=2;renderer.velocityScale=.06f;
            }
            else
            {
                var growth=particles.sizeOverLifetime;growth.enabled=true;
                growth.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,2.2f));
            }
            particles.Play();particles.Emit(count);Destroy(go,lifetime+.1f);
        }

        void LateUpdate()
        {
            // Keep the impact and delayed sounds fixed as the shooter moves.
            transform.SetPositionAndRotation(anchor,Quaternion.identity);
            if(trail==null)return;
            float alpha=1-Mathf.Clamp01((Time.time-born)/.055f);
            trail.startColor=new Color(1,.82f,.48f,alpha*.8f);trail.endColor=new Color(1,.65f,.28f,alpha*.3f);
            if(alpha<=0){Destroy(trail);trail=null;}
        }
    }
}
