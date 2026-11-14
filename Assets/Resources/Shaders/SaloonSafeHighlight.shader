Shader "Saloon/Safe Proximity Highlight"
{
    Properties
    {
        _Color ("Highlight", Color) = (1, .67, .2, .8)
        _Width ("World outline width", Float) = .014
    }
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float _Width;
            float4 vert(appdata_base input) : SV_POSITION
            {
                float4 world=mul(unity_ObjectToWorld,input.vertex);
                world.xyz+=UnityObjectToWorldNormal(input.normal)*_Width;
                return UnityWorldToClipPos(world.xyz);
            }
            fixed4 frag() : SV_Target { return _Color; }
            ENDCG
        }
        Pass
        {
            Cull Back
            Offset -1, -1
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            struct Output { float4 vertex : SV_POSITION; float3 normal : TEXCOORD0; float3 view : TEXCOORD1; };
            Output vert(appdata_base input)
            {
                Output output;
                output.vertex=UnityObjectToClipPos(input.vertex);
                output.normal=UnityObjectToWorldNormal(input.normal);
                output.view=WorldSpaceViewDir(input.vertex);
                return output;
            }
            fixed4 frag(Output input) : SV_Target
            {
                float rim=pow(1-saturate(abs(dot(normalize(input.normal),normalize(input.view)))),2);
                return fixed4(_Color.rgb,_Color.a*(.12+.4*rim));
            }
            ENDCG
        }
    }
}
