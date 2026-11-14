Shader "Saloon/Scanned Surface"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Color", 2D) = "white" {}
        _NormalMap ("OpenGL normal (linear)", 2D) = "bump" {}
        _Roughness ("Roughness (linear)", 2D) = "white" {}
        _Scale ("Repeats per metre", Float) = 0.5
        _NormalStrength ("Relief", Range(0,1)) = 0.4
        _Polish ("Polish", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _NormalMap, _Roughness;
        fixed4 _Color;
        half _Scale, _NormalStrength, _Polish;
        struct Input { float3 worldPos; float3 worldNormal; INTERNAL_DATA };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = normalize(WorldNormalVector(IN,float3(0,0,1)));
            float3 weights = pow(abs(n), 6);
            weights /= max(dot(weights, float3(1,1,1)), 0.0001);
            float3 p = IN.worldPos * _Scale;
            float3 s = float3(n.x < 0 ? -1 : 1, n.y < 0 ? -1 : 1, n.z < 0 ? -1 : 1);
            float2 uvX = float2(p.z * s.x, p.y);
            float2 uvY = float2(p.x * s.y, p.z);
            float2 uvZ = float2(-p.x * s.z, p.y);
            fixed3 color = tex2D(_MainTex,uvX).rgb * weights.x + tex2D(_MainTex,uvY).rgb * weights.y + tex2D(_MainTex,uvZ).rgb * weights.z;
            half rough = tex2D(_Roughness,uvX).r * weights.x + tex2D(_Roughness,uvY).r * weights.y + tex2D(_Roughness,uvZ).r * weights.z;
            // Project only the normal-map slopes onto each face, preserving the mesh normal.
            float3 nx = tex2D(_NormalMap,uvX).xyz * 2 - 1;
            float3 ny = tex2D(_NormalMap,uvY).xyz * 2 - 1;
            float3 nz = tex2D(_NormalMap,uvZ).xyz * 2 - 1;
            float3 slopes = float3(0,nx.y,nx.x*s.x)*weights.x + float3(ny.x*s.y,0,ny.y)*weights.y + float3(-nz.x*s.z,nz.y,0)*weights.z;
            float3 detail = normalize(n + (slopes - n*dot(slopes,n))*_NormalStrength);
            float3 tangent = WorldNormalVector(IN,float3(1,0,0));
            float3 bitangent = WorldNormalVector(IN,float3(0,1,0));
            float3 normal = WorldNormalVector(IN,float3(0,0,1));
            o.Normal = normalize(float3(dot(detail,tangent),dot(detail,bitangent),dot(detail,normal)));
            o.Albedo = color * _Color.rgb;
            o.Metallic = 0;
            o.Smoothness = saturate((1-rough)*0.65 + _Polish);
            o.Occlusion = 1;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
