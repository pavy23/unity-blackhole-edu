Shader "MilkyWay/PlanetSurface"
{
    // One procedural shader for every solar-system body — rocky worlds, gas
    // giants, and Earth — selected by parameter strengths rather than
    // variants, so the SolarSystemRig can author each planet as a material.
    //
    // Lighting is computed here (Lambert toward _SunPos with a wrap term and
    // an ambient floor) because the galaxy scene has no Unity light: the Sun
    // in the rig is an emissive prop, not a Light component.
    Properties
    {
        _BaseColor("Base Colour", Color) = (0.5, 0.5, 0.5, 1)
        _SecondColor("Second Colour (bands / land)", Color) = (0.7, 0.6, 0.45, 1)
        _OceanColor("Ocean Colour", Color) = (0.06, 0.18, 0.42, 1)
        _PoleColor("Pole / Ice Colour", Color) = (0.92, 0.96, 1.0, 1)
        _RimColor("Atmosphere Rim (HDR)", Color) = (0, 0, 0, 0)

        [Header(Surface pattern)]
        _NoiseScale("Noise Scale", Float) = 6.0
        _Mottle("Mottling", Range(0, 1)) = 0.35
        _BandFreq("Band Frequency (gas giants)", Float) = 0.0
        _BandWarp("Band Turbulence", Range(0, 1)) = 0.35
        _Continents("Continents", Range(0, 1)) = 0.0
        _SeaLevel("Sea Level", Range(0, 1)) = 0.55
        _IceCap("Ice Cap Latitude (1.2 = off)", Range(0, 1.2)) = 1.2
        _Clouds("Cloud Cover", Range(0, 1)) = 0.0
        _Spot("Storm Spot", Range(0, 1)) = 0.0
        _SpotColor("Storm Spot Colour", Color) = (0.75, 0.35, 0.2, 1)

        [Header(Lighting)]
        _SunPos("Sun Position (world)", Vector) = (0, 0, 0, 0)
        _Ambient("Ambient Floor", Range(0, 1)) = 0.12
        _Glow("Self Glow", Range(0, 2)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "PlanetSurface"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _SecondColor, _OceanColor, _PoleColor, _RimColor, _SpotColor;
                float4 _SunPos;
                float _NoiseScale, _Mottle, _BandFreq, _BandWarp;
                float _Continents, _SeaLevel, _IceCap, _Clouds, _Spot;
                float _Ambient, _Glow;
            CBUFFER_END

            float pl_hash(float3 p)
            {
                // NOT the product hash the galaxy shaders use: that one is
                // symmetric under coordinate permutation, which on a sphere
                // becomes a visible mirror plane across x = z (kaleidoscope
                // bands on Jupiter). The dot() breaks the symmetry.
                return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453);
            }

            float pl_vnoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(pl_hash(i), pl_hash(i + float3(1, 0, 0)), f.x),
                         lerp(pl_hash(i + float3(0, 1, 0)), pl_hash(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(pl_hash(i + float3(0, 0, 1)), pl_hash(i + float3(1, 0, 1)), f.x),
                         lerp(pl_hash(i + float3(0, 1, 1)), pl_hash(i + float3(1, 1, 1)), f.x), f.y),
                    f.z);
            }

            float pl_fbm(float3 p)
            {
                float v = 0.0, a = 0.5;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    v += a * pl_vnoise(p);
                    p = p * 2.13 + 11.7;
                    a *= 0.5;
                }
                return v * 1.067; // renormalise the 4-octave sum toward [0,1]
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Texture in OBJECT space so the pattern rides the planet's
                // spin (the rig rotates the transform); a unit sphere's
                // object position doubles as its surface normal.
                float3 os = normalize(i.positionOS);
                float lat = os.y;

                float3 col = _BaseColor.rgb;

                // Gas-giant bands: latitude stripes, turbulence-warped so the
                // edges curl instead of running ruler-straight.
                if (_BandFreq > 0.01)
                {
                    float warp = (pl_fbm(os * _NoiseScale) - 0.5) * _BandWarp;
                    float band = sin((lat + warp) * _BandFreq * 3.14159);
                    col = lerp(col, _SecondColor.rgb, band * 0.5 + 0.5);
                }

                // Rocky mottling — maria, craters-at-a-distance, dust regions.
                float mottle = pl_fbm(os * _NoiseScale * 1.7);
                col = lerp(col, col * (0.55 + 0.9 * mottle), _Mottle);

                // Continents rise out of the ocean above the sea level.
                if (_Continents > 0.01)
                {
                    float h = pl_fbm(os * _NoiseScale);
                    float land = smoothstep(_SeaLevel, _SeaLevel + 0.05, h);
                    float3 landCol = lerp(_SecondColor.rgb * 0.85, _SecondColor.rgb * 1.15,
                                          pl_fbm(os * _NoiseScale * 3.1));
                    col = lerp(_OceanColor.rgb, landCol, land * _Continents);
                }

                // Polar ice.
                float ice = smoothstep(_IceCap, _IceCap + 0.08, abs(lat));
                col = lerp(col, _PoleColor.rgb, ice);

                // A great oval storm, fixed in the planet's own frame.
                if (_Spot > 0.01)
                {
                    float2 spotUV = float2(atan2(os.z, os.x) * 0.6, (lat + 0.32) * 2.4);
                    float d = length(spotUV - float2(0.55, 0.0));
                    col = lerp(col, _SpotColor.rgb, _Spot * smoothstep(0.16, 0.05, d));
                }

                // Clouds drift slowly relative to the surface.
                if (_Clouds > 0.01)
                {
                    float cl = pl_fbm(os * _NoiseScale * 0.9 + float3(_Time.y * 0.015, 0, 0));
                    // Threshold above the fbm mean, or a thin haze veils the
                    // whole sphere and washes the surface colours pastel.
                    col = lerp(col, float3(0.95, 0.96, 0.98),
                               _Clouds * smoothstep(0.57, 0.75, cl));
                }

                // Sun-lit Lambert with a wrap term; ambient floor keeps the
                // night side readable against a dark galaxy backdrop.
                float3 N = normalize(i.normalWS);
                float3 L = normalize(_SunPos.xyz - i.positionWS);
                float ndl = saturate((dot(N, L) + 0.18) / 1.18);
                float light = _Ambient + (1.0 - _Ambient) * ndl;

                // Atmosphere: a fresnel rim, only on the lit side.
                float3 V = normalize(_WorldSpaceCameraPos - i.positionWS);
                float fres = pow(1.0 - saturate(dot(N, V)), 3.0);
                float3 rim = _RimColor.rgb * fres * (0.25 + 0.75 * ndl);

                return half4(col * light * (1.0 + _Glow) + rim, 1.0);
            }
            ENDHLSL
        }
    }
}
