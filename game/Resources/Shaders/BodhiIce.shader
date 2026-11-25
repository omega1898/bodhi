// Purpose: the frozen lake on the title screen. Ice seen from low down acts like a dark mirror,
// so this shader works out what the sky would look like reflected in it: the night's colours, a
// green glow where the aurora hangs, and a scatter of stars. How much it mirrors depends on the
// angle you look at it (the "Fresnel" effect: shallow angles reflect most, as real water and ice
// do). Under the reflection is dark blue-green ice with faint white cracks, and frost and snow
// dust it toward its edge, so it beds into the snowbank instead of ending in a hard line.
Shader "Bodhi/Ice"
{
    Properties
    {
        _IceColour ("Deep ice colour", Color) = (0.03, 0.07, 0.1, 1)
        _FrostColour ("Frost at the edge", Color) = (0.72, 0.8, 0.92, 1)
        _SkyTop ("Reflected sky, overhead", Color) = (0.02, 0.035, 0.09, 1)
        _SkyLow ("Reflected sky, near the horizon", Color) = (0.08, 0.15, 0.24, 1)
        [HDR] _AuroraGlow ("Reflected aurora glow", Color) = (0.05, 0.32, 0.16, 1)
        _LakeMiddle ("Middle of the lake (scene space)", Vector) = (9, 0, 21, 0)
        _LakeSize ("Half-width across (x) and away (y)", Vector) = (10, 4.5, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        // Shared by both passes, so Unity can batch the lake's draws (every pass must see the same
        // material values in the same order).
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _IceColour;
            half4 _FrostColour;
            half4 _SkyTop;
            half4 _SkyLow;
            half4 _AuroraGlow;
            float4 _LakeMiddle;
            float4 _LakeSize;
        CBUFFER_END

        TEXTURE2D(_BodhiGrain);
        SAMPLER(sampler_BodhiGrain);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex ice_vertex
            #pragma fragment ice_fragment
            #pragma multi_compile_fog



            struct MeshCorner
            {
                float4 position : POSITION;
            };

            struct ToPixel
            {
                float4 screen_position : SV_POSITION;
                float3 world_position : TEXCOORD0;
                float3 lake_position : TEXCOORD1;  // Where on the lake, in the scene's own space.
                half fog : TEXCOORD2;
            };

            // Places each corner of the lake on screen.
            ToPixel ice_vertex(MeshCorner corner)
            {
                ToPixel result;
                VertexPositionInputs positions = GetVertexPositionInputs(corner.position.xyz);
                result.screen_position = positions.positionCS;
                result.world_position = positions.positionWS;
                result.lake_position = corner.position.xyz;
                result.fog = ComputeFogFactor(positions.positionCS.z);
                return result;
            }

            // A repeatable "random" number from 0 to 1 for a grid cell (for the reflected stars).
            float hash_of(float3 cell)
            {
                return frac(sin(dot(cell, float3(127.1, 311.7, 74.7))) * 43758.5453);
            }

            // Works out the colour of the ice at one spot.
            half4 ice_fragment(ToPixel pixel) : SV_Target
            {
                float3 from_camera = normalize(pixel.world_position - _WorldSpaceCameraPos);
                float3 reflected = float3(from_camera.x, -from_camera.y, from_camera.z);  // Mirrored in flat ice.

                // The reflected sky: night colours, the aurora's green glow low down, and stars.
                half height = saturate(reflected.y);
                half3 sky = lerp(_SkyLow.rgb, _SkyTop.rgb, saturate(height * 2.5));
                float across = atan2(reflected.x, reflected.z);
                float ripple = 0.55 + 0.45 * sin(across * 7.0 + _Time.y * 0.15) * sin(across * 3.0 - _Time.y * 0.08);
                sky += _AuroraGlow.rgb * exp(-height * 9.0) * ripple * 0.7;
                float3 star_cell = floor(reflected * 140.0);
                sky += step(0.985, hash_of(star_cell)) * 0.6 * saturate(height * 6.0);

                // Fresnel: looking across the ice at a shallow angle, it reflects nearly everything.
                half facing = saturate(-from_camera.y);
                half mirror = (0.04 + 0.96 * pow(1.0 - facing, 5.0)) * 0.75;  // Ice is a dull mirror, not glass.

                // The ice itself, with faint cracks where the grain picture crosses its middle value.
                half grain = SAMPLE_TEXTURE2D(_BodhiGrain, sampler_BodhiGrain, pixel.lake_position.xz * 0.35).r;
                half cracks = 1.0 - smoothstep(0.0, 0.012, abs(grain - 0.5));
                half3 colour = lerp(_IceColour.rgb + cracks * 0.12, sky, mirror);

                // Frost and snow toward the edge, blending into the bank.
                float2 offset = (pixel.lake_position.xz - _LakeMiddle.xz) / _LakeSize.xy;
                half edge = smoothstep(0.7, 1.0, length(offset) + (grain - 0.5) * 0.25);
                colour = lerp(colour, _FrostColour.rgb, edge);

                colour = MixFog(colour, pixel.fog);
                return half4(colour, 1);
            }
            ENDHLSL
        }

        // Writes only depth, which URP uses for some screen effects.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex depth_vertex
            #pragma fragment depth_fragment

            // Places a corner on screen; only its depth matters here.
            float4 depth_vertex(float4 position : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(position.xyz);
            }

            // Depth is written automatically; the colour is ignored.
            half depth_fragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
