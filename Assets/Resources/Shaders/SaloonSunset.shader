Shader "Saloon/Desert Sunset"
{
    Properties
    {
        _Zenith ("Evening violet", Color) = (0.19,0.15,0.32,1)
        _Horizon ("Apricot horizon", Color) = (1.0,0.48,0.22,1)
        _SunDirection ("Sun direction", Vector) = (1,0.065,0.18,0)
        _Center ("Sky center", Vector) = (28,0,-12,0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "IgnoreProjector"="True" }
        Cull Front ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 vertex:SV_POSITION; float3 direction:TEXCOORD0; };
            float4 _Zenith,_Horizon,_SunDirection,_Center;
            v2f vert(float4 vertex:POSITION)
            {
                v2f o;o.vertex=UnityObjectToClipPos(vertex);
                o.direction=mul(unity_ObjectToWorld,vertex).xyz-_Center.xyz;return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float3 ray=normalize(i.direction),sun=normalize(_SunDirection.xyz);
                float height=saturate(ray.y);
                float3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(saturate(height*1.5),0.55));
                float facing=pow(saturate(dot(normalize(float3(ray.x,0,ray.z)),normalize(float3(sun.x,0,sun.z)))),5);
                float glow=exp(-height*9)*facing;
                color=lerp(color,float3(1,0.68,0.32),glow*0.52);
                float angle=acos(clamp(dot(ray,sun),-1,1));
                color+=float3(0.19,0.075,0.025)*exp(-angle*9);
                float disk=1-smoothstep(0.041,0.044,angle);
                color=lerp(color,float3(1,0.9,0.61),disk);
                // Still, elongated cloud ribbons catch the last pink light.
                float azimuth=atan2(ray.z,ray.x);
                float ribbon=sin(ray.y*118+sin(azimuth*6)*1.8+sin(azimuth*17)*0.65);
                float broken=smoothstep(-0.1,0.7,sin(azimuth*9+ray.y*16));
                float clouds=smoothstep(0.55,0.94,ribbon)*broken*smoothstep(0.10,0.17,ray.y)*(1-smoothstep(0.32,0.43,ray.y));
                color=lerp(color,float3(0.82,0.41,0.43),clouds*0.35);
                color=lerp(float3(0.33,0.19,0.23),color,smoothstep(-0.09,0.005,ray.y));
                return float4(color,1);
            }
            ENDCG
        }
    }
}
