// Purpose: the one material every solid thing in the 3D scenes is drawn with: snow, mountains,
// trees, the snowman and the stand-in characters. Its colour comes from the mesh itself (each
// corner of the mesh carries a colour, called a "vertex colour"), so one material serves every
// object and the code that builds a mesh decides how it looks.
//
// What it adds on top of plain lighting, each for a reason from docs/unity_art_bible.md:
//   - "Wrap" lighting: light wraps a little way round the dark side, so curls, snow and cloth
//     look soft instead of plastic.
//   - A rim light: a thin cold glow along edges facing away from the camera, which keeps both
//     characters' outlines readable against the night (the art bible's moon_rim rule).
//   - Snow sparkle: a few tiny glints that come and go as the camera moves.
//   - An emission colour for things that glow themselves (the lantern's glass, a lit window).
// It also casts and receives shadows and fades into the scene's fog.
//
// This is written in HLSL, the language graphics chips run. Unity's Universal Render Pipeline
// (URP) supplies the lighting helpers through the #include lines.
Shader "Bodhi/Lit"
{
    Properties
    {
        _BaseColor ("Tint (multiplies the mesh colours)", Color) = (1, 1, 1, 1)
        _RimColor ("Rim light colour", Color) = (0.62, 0.76, 0.85, 1)
        _RimStrength ("Rim light strength", Range(0, 2)) = 0.35
        _Wrap ("Light wrap (softness)", Range(0, 1)) = 0.35
        _Gloss ("Shine", Range(0, 1)) = 0
        _Sparkle ("Snow sparkle", Range(0, 1)) = 0
        [HDR] _EmissionColor ("Glow colour", Color) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        // Shared by every pass below. Every pass must see the same material values in the same
        // order, or Unity can't batch draws together (the "SRP Batcher" speed-up).
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _RimColor;
            half _RimStrength;
            half _Wrap;
            half _Gloss;
            half _Sparkle;
            half4 _EmissionColor;
        CBUFFER_END
        ENDHLSL

        // The pass that draws the colour you see.
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex lit_vertex
            #pragma fragment lit_fragment
            // Versions of the shader for each lighting setup URP might be using.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // What the mesh hands in for each corner.
            struct MeshCorner
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                half4 colour : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // What each corner hands on to the pixels between corners.
            struct ToPixel
            {
                float4 screen_position : SV_POSITION;
                float3 world_position : TEXCOORD0;
                half3 world_normal : TEXCOORD1;
                half4 colour : TEXCOORD2;
                half fog : TEXCOORD3;
            };

            // Places each mesh corner on screen and passes its colour and facing along.
            ToPixel lit_vertex(MeshCorner corner)
            {
                UNITY_SETUP_INSTANCE_ID(corner);
                ToPixel result;
                VertexPositionInputs positions = GetVertexPositionInputs(corner.position.xyz);
                result.screen_position = positions.positionCS;
                result.world_position = positions.positionWS;
                result.world_normal = TransformObjectToWorldNormal(corner.normal);
                result.colour = corner.colour * _BaseColor;
                result.fog = ComputeFogFactor(positions.positionCS.z);
                return result;
            }

            // How much light reaches a surface turned `facing` (the dot product of the surface's
            // direction and the light's), softened by wrapping round the dark side.
            half wrapped_light(half facing)
            {
                return saturate((facing + _Wrap) / (1.0 + _Wrap));
            }

            // A repeatable "random" number from 0 to 1 for a place in space, used for sparkles.
            float hash_of(float3 place)
            {
                return frac(sin(dot(place, float3(12.9898, 78.233, 37.719))) * 43758.5453);
            }

            // Works out the colour of one pixel.
            half4 lit_fragment(ToPixel pixel) : SV_Target
            {
                half3 normal = normalize(pixel.world_normal);
                half3 to_camera = normalize(GetWorldSpaceViewDir(pixel.world_position));
                half3 albedo = pixel.colour.rgb;

                // The main light (the moon), with its shadow.
                float4 shadow_coord = TransformWorldToShadowCoord(pixel.world_position);
                Light moon = GetMainLight(shadow_coord);
                half moon_shadow = moon.shadowAttenuation * moon.distanceAttenuation;
                half3 light = moon.color * wrapped_light(dot(normal, moon.direction)) * moon_shadow;
                half3 halfway = normalize(moon.direction + to_camera);
                half shine = pow(saturate(dot(normal, halfway)), 8.0 + 56.0 * _Gloss) * _Gloss * moon_shadow;

                // Every other light (the lantern, a window), each with its own fall-off.
                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = pixel.world_position;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(pixel.screen_position);
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light lamp = GetAdditionalLight(lightIndex, pixel.world_position, half4(1, 1, 1, 1));
                    light += lamp.color * wrapped_light(dot(normal, lamp.direction)) * lamp.distanceAttenuation * lamp.shadowAttenuation;
                LIGHT_LOOP_END
                #endif

                // Light from the whole sky (set by WorldLook as a sky, horizon and ground colour).
                half3 sky_light = SampleSH(normal);

                // The cold rim along edges turned away from the camera.
                half edge = pow(1.0 - saturate(dot(normal, to_camera)), 3.0);
                half3 rim = _RimColor.rgb * edge * _RimStrength * (0.35 + 0.65 * moon_shadow);

                // Snow sparkle: a rare glint in a grid of tiny cells, flickering as the view moves.
                half sparkle = 0;
                if (_Sparkle > 0)
                {
                    float3 cell = floor(pixel.world_position * 120.0);
                    float chance = hash_of(cell + floor(to_camera * 6.0));
                    float near = saturate(1.0 - distance(pixel.world_position, _WorldSpaceCameraPos) / 30.0);
                    sparkle = step(1.0 - 0.006 * _Sparkle, chance) * moon_shadow * 2.2 * near;
                }

                half3 colour = albedo * (light + sky_light) + rim + shine * moon.color + sparkle + _EmissionColor.rgb;
                colour = MixFog(colour, pixel.fog);
                return half4(colour, 1);
            }
            ENDHLSL
        }

        // The pass that draws this object into the moon's shadow map, so it casts a shadow.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex shadow_vertex
            #pragma fragment shadow_fragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;  // Set by URP: which way the light that's casting shines.
            float3 _LightPosition;   // Set by URP for lamps, which shine outward from one spot.

            struct MeshCorner
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Places a corner as the light sees it, nudged a little to stop "shadow acne"
            // (speckles where a surface shadows itself).
            float4 shadow_vertex(MeshCorner corner) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(corner);
                float3 world_position = TransformObjectToWorld(corner.position.xyz);
                float3 world_normal = TransformObjectToWorldNormal(corner.normal);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 light_direction = normalize(_LightPosition - world_position);
                #else
                float3 light_direction = _LightDirection;
                #endif
                float4 position = TransformWorldToHClip(ApplyShadowBias(world_position, world_normal, light_direction));
                position = ApplyShadowClamping(position);
                return position;
            }

            // Shadow maps only need depth, so the colour is never used.
            half4 shadow_fragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // The pass that writes only depth, which URP uses for some screen effects.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex depth_vertex
            #pragma fragment depth_fragment
            #pragma multi_compile_instancing

            struct MeshCorner
            {
                float4 position : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Places a corner on screen; only its depth matters here.
            float4 depth_vertex(MeshCorner corner) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(corner);
                return TransformObjectToHClip(corner.position.xyz);
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
