// Procedural starfield skybox. Shares bh_starField with the black hole shader
// so the background seen through the lensing quad matches the sky around it.
Shader "BlackHole/StarfieldSkybox"
{
    Properties
    {
        _StarDensity("Star Density", Range(0.0, 2.0)) = 0.8
        _NebulaIntensity("Nebula Haze", Range(0.0, 1.0)) = 0.3
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "StarfieldSkybox"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "StarFunctions.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _StarDensity, _NebulaIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 dir : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 rd = normalize(i.dir);
                return half4(bh_starField(rd, _StarDensity, _NebulaIntensity), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
