// Purpose: the night sky drawn behind everything in the 3D scenes (Unity calls this the
// "skybox"). Every pixel looks out in some direction, and this works out what you'd see that
// way: a fade from deep blue overhead to a softer blue at the horizon, thousands of stars that
// twinkle, a faint band of the Milky Way, and the moon with its glow. Nothing is painted: it is
// all worked out from the direction, so it's sharp on any screen and costs no texture memory.
// The aurora is not drawn here: it hangs in front of the sky as its own curtains (BodhiAurora).
Shader "Bodhi/Night Sky"
{
    Properties
    {
        _TopColor ("Overhead colour", Color) = (0.015, 0.025, 0.07, 1)
        _MiddleColor ("Middle of the sky", Color) = (0.04, 0.08, 0.17, 1)
        _HorizonColor ("Horizon colour", Color) = (0.12, 0.2, 0.3, 1)
        _StarBrightness ("Star brightness", Range(0, 4)) = 1.6
        _MilkyWay ("Milky Way strength", Range(0, 1)) = 0.35
        _MoonDirection ("Direction of the moon", Vector) = (-0.5, 0.45, 0.7, 0)
        [HDR] _MoonColor ("Moon colour", Color) = (2.2, 2.15, 1.9, 1)
        _Fade ("How much of the sky shows (0 black, 1 full)", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex sky_vertex
            #pragma fragment sky_fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _MiddleColor;
                half4 _HorizonColor;
                half _StarBrightness;
                half _MilkyWay;
                float4 _MoonDirection;
                half4 _MoonColor;
                half _Fade;
            CBUFFER_END

            struct MeshCorner
            {
                float4 position : POSITION;
            };

            struct ToPixel
            {
                float4 screen_position : SV_POSITION;
                float3 direction : TEXCOORD0;  // Which way this part of the sky is, from the camera.
            };

            // The skybox is a big sphere around the camera; each corner's position is its direction.
            ToPixel sky_vertex(MeshCorner corner)
            {
                ToPixel result;
                result.screen_position = TransformObjectToHClip(corner.position.xyz);
                result.direction = corner.position.xyz;
                return result;
            }

            // A repeatable "random" number from 0 to 1 for each grid cell.
            float hash_of(float3 cell)
            {
                return frac(sin(dot(cell, float3(127.1, 311.7, 74.7))) * 43758.5453);
            }

            // Smooth "value noise": random numbers at grid corners, blended smoothly between.
            // Used for the Milky Way's cloudy band.
            float smooth_noise(float3 place)
            {
                float3 cell = floor(place);
                float3 inside = frac(place);
                inside = inside * inside * (3.0 - 2.0 * inside);
                float bottom = lerp(lerp(hash_of(cell), hash_of(cell + float3(1, 0, 0)), inside.x),
                                    lerp(hash_of(cell + float3(0, 1, 0)), hash_of(cell + float3(1, 1, 0)), inside.x), inside.y);
                float top = lerp(lerp(hash_of(cell + float3(0, 0, 1)), hash_of(cell + float3(1, 0, 1)), inside.x),
                                 lerp(hash_of(cell + float3(0, 1, 1)), hash_of(cell + float3(1, 1, 1)), inside.x), inside.y);
                return lerp(bottom, top, inside.z);
            }

            // One layer of stars. The sky is cut into a grid of little cells; some cells hold a
            // star at a random spot inside, with its own size, colour and twinkle speed.
            half3 star_layer(float3 direction, float cells_across, float how_many, float sharpness)
            {
                float3 place = direction * cells_across;
                float3 cell = floor(place);
                float chance = hash_of(cell);
                if (chance > how_many)
                {
                    return 0;
                }
                float3 star_spot = cell + float3(hash_of(cell + 1.3), hash_of(cell + 2.7), hash_of(cell + 4.1));
                float distance_to_star = length(place - star_spot);
                float glow = saturate(1.0 - distance_to_star * sharpness);
                glow = glow * glow * glow;
                float twinkle = 0.65 + 0.35 * sin(_Time.y * (1.5 + 3.0 * hash_of(cell + 7.7)) + chance * 40.0);
                // Most stars are white; some lean blue, a few lean warm, like the real sky.
                float tint = hash_of(cell + 9.1);
                half3 colour = tint < 0.2 ? half3(1.0, 0.85, 0.7) : (tint > 0.75 ? half3(0.75, 0.85, 1.0) : half3(1, 1, 1));
                return colour * glow * twinkle;
            }

            // Works out the colour of the sky in one direction.
            half4 sky_fragment(ToPixel pixel) : SV_Target
            {
                float3 direction = normalize(pixel.direction);
                float height = direction.y;  // 1 straight up, 0 at the horizon, below 0 under it.

                // The fade from overhead to the horizon.
                half3 colour = lerp(_MiddleColor.rgb, _TopColor.rgb, saturate(height * 1.6));
                colour = lerp(_HorizonColor.rgb, colour, saturate(height * 4.0 + 0.05));

                // The Milky Way: a cloudy glow along one tilted band across the sky.
                float3 band_axis = normalize(float3(0.35, 0.3, -0.89));
                float band = 1.0 - abs(dot(direction, band_axis));
                band = pow(saturate(band), 12.0);
                float clouds = smooth_noise(direction * 7.0) * 0.6 + smooth_noise(direction * 19.0) * 0.4;
                colour += half3(0.55, 0.62, 0.8) * band * clouds * _MilkyWay * 0.25;

                // Stars: three layers, from many faint ones to a few bright ones. Fewer show near the
                // horizon, where real air dims them.
                float above = saturate(height * 5.0);
                half3 stars = star_layer(direction, 260.0, 0.25, 2.4) * 0.45
                            + star_layer(direction, 120.0, 0.18, 2.6) * 0.8
                            + star_layer(direction, 55.0, 0.08, 3.2) * 1.4;
                stars *= 1.0 + band * clouds * 2.0;  // More stars crowd into the Milky Way.
                colour += stars * _StarBrightness * above;

                // The moon: a bright disc with a soft halo of light around it.
                float3 moon_direction = normalize(_MoonDirection.xyz);
                float toward_moon = dot(direction, moon_direction);
                float disc = smoothstep(0.99955, 0.99975, toward_moon);
                float halo = pow(saturate(toward_moon), 900.0) * 0.6 + pow(saturate(toward_moon), 40.0) * 0.08;
                colour += _MoonColor.rgb * (disc + halo);

                // Below the horizon is hidden by the snow; keep it the horizon colour.
                colour = height < 0 ? _HorizonColor.rgb : colour;
                return half4(colour * _Fade, 1);
            }
            ENDHLSL
        }
    }
}
