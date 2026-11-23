using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Saloon
{
    public sealed class SaloonBreakable : MonoBehaviour
    {
        public int id;
        public bool window;
        public bool Broken { get; private set; }
        GameObject fragments;
        static Mesh shard;
        static Material glass;
        static AudioClip breaking;

        public void SetBroken(bool value,bool effects=false)
        {
            if(Broken==value)return;
            var renderers=GetComponentsInChildren<Renderer>();
            if(value&&effects&&renderers.Length>0)Shatter(renderers);
            if(!value&&fragments!=null)Destroy(fragments);
            foreach(var renderer in renderers)renderer.enabled=!value;
            foreach(var collider in GetComponents<Collider>())collider.enabled=!value;
            Broken=value;
        }

        void Shatter(Renderer[] renderers)
        {
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
            if(shard==null)
            {
                shard=new Mesh{name="Triangular glass shard"};
                shard.vertices=new[]{new Vector3(-.5f,-.4f,0),new Vector3(.5f,-.3f,0),new Vector3(-.15f,.6f,0)};
                shard.triangles=new[]{0,1,2,2,1,0};shard.RecalculateNormals();shard.RecalculateBounds();
                glass=new Material(Shader.Find("Sprites/Default")){name="Shattered glass"};
            }
            fragments=new GameObject("Shattered "+(window?"window":"bottle"));fragments.layer=SaloonSmokingArea.IndoorLayer;
            fragments.transform.position=bounds.center;
            var particles=fragments.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.loop=false;main.playOnAwake=false;main.duration=2.4f;main.startLifetime=new ParticleSystem.MinMaxCurve(1.1f,2.4f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(.6f,2.1f);main.startSize=new ParticleSystem.MinMaxCurve(window?.045f:.025f,window?.20f:.09f);
            main.startRotation3D=true;main.startRotationX=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.startRotationY=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            main.startColor=window?new Color(.68f,.87f,.91f,.8f):renderers[0].sharedMaterial.color;
            main.gravityModifier=1;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=window?48:18;
            var emission=particles.emission;emission.enabled=false;
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=bounds.size*.9f;
            var rotation=particles.rotationOverLifetime;rotation.enabled=true;rotation.separateAxes=true;rotation.x=3;rotation.y=5;rotation.z=2;
            var collision=particles.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;collision.mode=ParticleSystemCollisionMode.Collision3D;
            collision.collidesWith=~(1<<8);collision.bounce=.25f;collision.dampen=.35f;collision.lifetimeLoss=.15f;
            var fade=particles.colorOverLifetime;fade.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,.55f),new GradientAlphaKey(0,1)});fade.color=gradient;
            var particleRenderer=particles.GetComponent<ParticleSystemRenderer>();particleRenderer.renderMode=ParticleSystemRenderMode.Mesh;
            particleRenderer.mesh=shard;particleRenderer.sharedMaterial=glass;particleRenderer.shadowCastingMode=ShadowCastingMode.Off;
            particles.Play();particles.Emit(window?48:18);
            SaloonCombatAudio.Play(fragments.transform,bounds.center,Breaking,window?.52f:.38f,window?.85f:1.15f);
            Destroy(fragments,3);
        }

        static AudioClip Breaking
        {
            get
            {
                if(breaking!=null)return breaking;
                const int rate=22050;var samples=new float[rate];var random=new System.Random(7481);float previous=0;
                for(int i=0;i<samples.Length;i++)
                {
                    float t=(float)i/rate,noise=(float)random.NextDouble()*2-1;
                    float crack=(noise-previous)*Mathf.Exp(-t*19)*.32f;previous=noise;
                    float clinks=0;
                    for(int j=0;j<6;j++)
                    {
                        float age=t-(.035f+j*.052f);
                        if(age>=0)clinks+=Mathf.Sin(age*(3100+j*637)*Mathf.PI*2)*Mathf.Exp(-age*(22+j*3))*.11f;
                    }
                    samples[i]=Mathf.Clamp(crack+clinks,-1,1);
                }
                breaking=AudioClip.Create("Glass shatter and falling fragments",samples.Length,1,rate,false);breaking.SetData(samples,0);return breaking;
            }
        }
    }

    public partial class SaloonGame
    {
        SaloonBreakable[] breakableProps=Array.Empty<SaloonBreakable>();
        readonly HashSet<int> brokenProps=new HashSet<int>();

        void InitializeBreakables()
        {breakableProps=GameObject.Find("Saloon World").GetComponentsInChildren<SaloonBreakable>().OrderBy(p=>p.id).ToArray();}

        void ResetBreakables()
        {
            brokenProps.Clear();
            foreach(var prop in breakableProps)if(prop!=null)prop.SetBroken(false);
        }

        void BreakProp(Collider collider)
        {
            var prop=collider.GetComponentInParent<SaloonBreakable>();
            if(prop==null||!brokenProps.Add(prop.id))return;
            prop.SetBroken(true,true);
        }

        void ApplyBreakables(Snapshot snapshot)
        {
            if(Hosting)return;
            var broken=new HashSet<int>(snapshot.brokenProps??Array.Empty<int>());
            foreach(var prop in breakableProps)
                if(prop!=null)prop.SetBroken(broken.Contains(prop.id),View!=null&&snapshot.phase==Phase.Turns);
        }
    }
}
