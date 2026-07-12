// Additive glow trail for the photon-geodesic demo (and tidal streams).
// Works with a LineRenderer in Stretch texture mode: uv.x runs 0 (launch)
// → 1 (photon head), uv.y runs across the width. Soft gaussian core, faded
// tail, boosted head, and a subtle energy pulse flowing along the line.
Shader "BlackHole/PhotonTrail"
{
    Properties
    {
        [HDR] _Tint("Tint (HDR)", Color) = (0.9, 1.8, 2.6, 1)
        _CoreSharpness("Core Sharpness", Range(1, 12)) = 6
        _TailFade("Tail Fade", Range(0.0, 0.6)) = 0.18
        _HeadBoost("Head Boost", Range(0, 6)) = 2.5
        _PulseSpeed("Pulse Speed", Range(0, 12)) = 5
        _PulseAmount("Pulse Amount", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "PhotonTrail"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _CoreSharpness, _TailFade, _HeadBoost, _PulseSpeed, _PulseAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float u = i.uv.x;
                float across = i.uv.y * 2.0 - 1.0;

                // Soft gaussian beam with a hot thin core.
                float core = exp(-across * across * _CoreSharpness);
                core += exp(-across * across * _CoreSharpness * 7.0) * 0.8;

                // Fade in from the tail; glow brighter toward the head.
                // TailFade 0 disables the fade entirely: on looped lines
                // (GW rings) the uv wrap 1 -> 0 would otherwise draw a dark
                // radial cut across the stroke at the seam.
                float tail = _TailFade <= 0.0015 ? 1.0 : smoothstep(0.0, _TailFade, u);
                float head = 1.0 + _HeadBoost * smoothstep(0.72, 1.0, u) * smoothstep(1.02, 0.98, u);

                // A faint pulse of energy flowing along the trajectory.
                float pulse = 1.0 + _PulseAmount * sin(u * 34.0 - _Time.y * _PulseSpeed * 3.0);

                float3 col = _Tint.rgb * i.color.rgb * (core * tail * head * pulse) * i.color.a;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
