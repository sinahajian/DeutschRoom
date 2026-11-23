Shader "DeutschRoom/HouseMirror"
{
    Properties { _ReflectionTex ("Reflection", 2D) = "black" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_ReflectionTex); SAMPLER(sampler_ReflectionTex);
            float4x4 _ReflectionVP;
            struct Varyings { float4 positionCS : SV_POSITION; float4 reflection : TEXCOORD0; };
            Varyings Vert(float4 positionOS : POSITION)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(positionOS.xyz);
                o.positionCS = TransformWorldToHClip(world);
                o.reflection = mul(_ReflectionVP, float4(world, 1));
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.reflection.xy / i.reflection.w * .5 + .5;
                // _ReflectionVP uses the render-texture GPU projection. Convert NDC
                // to the texture origin on Metal/D3D; OpenGL starts at the bottom.
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1.0 - uv.y;
                #endif
                return half4(SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, uv).rgb, 1);
            }
            ENDHLSL
        }
    }
}
