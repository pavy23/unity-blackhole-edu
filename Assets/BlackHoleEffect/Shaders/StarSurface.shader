// Star surface for the intro sequence and demos: limb darkening, two-scale
// animated convection granulation with hot cell cores, slow starspots, a
// chromosphere-tinted rim — plus a second additive corona pass (front-culled
// expanded shell) so every star gets a soft animated halo for free.
Shader "BlackHole/StarSurface"
{
    Properties
    {
        [HDR] _StarColor("Star Color (HDR)", Color) = (2.6, 2.3, 1.7, 1)
        _Granulation("Granulation Strength", Range(0, 1)) = 0.35
        _GranScale("Granule Scale", Range(1, 14)) = 6
        _LimbPower("Limb Darkening", Range(0.1, 2)) = 0.55
        _RimBoost("Rim Glow", Range(0, 2)) = 0.5
        _SpotStrength("Starspots", Range(0, 1)) = 0.35
        _CoronaBoost("Corona Intensity", Range(0, 3)) = 0.8
        _CoronaExtent("Corona Extent", Range(1.1, 3)) = 1.45
        // Optional observed photosphere map (equirectangular). Off by default
        // so every existing black-hole star keeps its procedural look.
        _SurfaceTex("Surface Map", 2D) = "black" {}
        _SurfaceTexStrength("Surface Map Strength", Range(0, 1)) = 0.0
    }

    SubShader
    {
        // Transparent queue so the additive corona composites over the skybox
        // (which URP draws after opaques). The surface pass itself stays fully
        // opaque and depth-writing, so occlusion by/of the hole still works.
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "StarSurface"
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "StarFunctions.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _StarColor;
                float _Granulation, _GranScale, _LimbPower, _RimBoost;
                float _SpotStrength, _CoronaBoost, _CoronaExtent;
                float _SurfaceTexStrength;
            CBUFFER_END

            TEXTURE2D(_SurfaceTex); SAMPLER(sampler_SurfaceTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 normalOS : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.normalOS = v.normalOS;
                o.viewWS = GetWorldSpaceViewDir(posWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);
                float ndv = saturate(dot(n, v));

                // Classical limb darkening I(μ) ≈ 1 − u(1 − μ).
                float limb = lerp(1.0, max(pow(ndv, _LimbPower), 0.12), 0.85);

                // Two-scale convection cells drifting slowly; cell cores run
                // hotter (sharper, brighter) than the cool inter-granule lanes.
                float g1 = bh_fbm3(i.normalOS * _GranScale + _Time.y * 0.12);
                float g2 = bh_vnoise3(i.normalOS * _GranScale * 3.1 - _Time.y * 0.2);
                float gran = 1.0 + _Granulation * ((g1 * 2.0 - 1.0) * 0.9 + (g2 * 2.0 - 1.0) * 0.45);
                float hotCores = pow(saturate(g1), 3.0) * _Granulation * 1.4;

                // Slow dark starspot groups (very low frequency, drifting).
                float spotN = bh_fbm3(i.normalOS * 1.7 + float3(_Time.y * 0.015, 0.0, 4.2));
                float spots = 1.0 - _SpotStrength * smoothstep(0.60, 0.78, spotN);

                // Hot cores are whiter than the mean photosphere color.
                float3 hotTint = normalize(_StarColor.rgb + 0.6) * length(_StarColor.rgb);

                // Chromosphere rim: deeper, redder glow melting into the bloom.
                float rim = pow(1.0 - ndv, 3.0) * _RimBoost;
                float3 rimTint = _StarColor.rgb * float3(1.05, 0.72, 0.52);

                // Gentle global flicker so the star feels alive.
                float flicker = 1.0 + 0.025 * sin(_Time.y * 6.3) + 0.018 * sin(_Time.y * 2.31 + 1.3);

                float3 col = (_StarColor.rgb * (limb * gran) * spots
                            + hotTint * hotCores * limb
                            + rimTint * rim) * flicker;

                // Observed photosphere (e.g. the SDO sun map): replaces the
                // procedural granulation/spots but keeps limb darkening, the
                // chromosphere rim and the flicker. The map drifts slowly in
                // longitude — a stand-in for the ~25-day solar rotation.
                if (_SurfaceTexStrength > 0.001)
                {
                    float3 os = normalize(i.normalOS);
                    float vTex = asin(clamp(os.y, -1.0, 1.0)) / 3.14159265 + 0.5;
                    float lon = atan2(os.z, os.x) / 6.2831853 + _Time.y * 0.002;
                    float uA = frac(lon);
                    float uB = frac(lon + 0.5) - 0.5;
                    float2 uv = fwidth(uA) <= fwidth(uB) ? float2(uA, vTex) : float2(uB, vTex);
                    float3 photo = SAMPLE_TEXTURE2D(_SurfaceTex, sampler_SurfaceTex, uv).rgb;
                    float3 mapped = (photo * _StarColor.rgb * limb + rimTint * rim) * flicker;
                    col = lerp(col, mapped, _SurfaceTexStrength);
                }
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // Corona: the same sphere expanded outward, front faces culled so only
        // the far shell renders — an additive halo hugging the silhouette,
        // streaked by slowly rotating coronal rays.
        // NOTE: URP draws only the first pass per ShaderTagId, so this pass
        // must use a different LightMode than the (implicitly SRPDefaultUnlit)
        // surface pass above to render in the same frame.
        Pass
        {
            Name "StarCorona"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "StarFunctions.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _StarColor;
                float _Granulation, _GranScale, _LimbPower, _RimBoost;
                float _SpotStrength, _CoronaBoost, _CoronaExtent;
                float _SurfaceTexStrength; // unused here; must mirror pass 1 for the SRP batcher
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 normalOS : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz * _CoronaExtent);
                o.positionHCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.normalOS = v.normalOS;
                o.viewWS = GetWorldSpaceViewDir(posWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);
                // Back faces of the expanded shell: |n·v| ≈ 1 behind the star,
                // ≈ 0 at the halo's outer silhouette. A steep power keeps the
                // glow hugging the photosphere so it reads as light, not dust.
                float ndv = abs(dot(n, v));
                float halo = pow(ndv, 4.0);

                // Coronal rays: streaks in object space, drifting slowly.
                float rays = bh_fbm3(i.normalOS * 5.0 + float3(0.0, _Time.y * 0.05, 0.0));
                rays = 0.7 + 0.5 * pow(saturate(rays), 2.0);

                // Whiten slightly so the halo feels hotter than the surface.
                float3 glowTint = lerp(_StarColor.rgb, normalize(_StarColor.rgb + 0.8) * length(_StarColor.rgb), 0.35);

                float breathe = 1.0 + 0.12 * sin(_Time.y * 0.9);
                float3 col = glowTint * (halo * rays * 0.30 * _CoronaBoost * breathe);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
