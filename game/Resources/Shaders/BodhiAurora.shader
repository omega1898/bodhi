// Purpose: the green aurora (the northern lights) over the title screen. Each curtain of light is
// a long ribbon of mesh hanging in the sky (NightSky builds them). This shader makes a ribbon look
// like an aurora: a bright green lower edge that fades upward into violet, fine vertical rays
// that drift sideways, and folds that slowly ripple along its length. Its light is added on top
// of the sky ("additive" blending), so curtains overlap and brighten like real ones.
Shader "Bodhi/Aurora"
{
    Properties
    {
        [HDR] _LowColor ("Lower edge colour", Color) = (0.25, 1.6, 0.65, 1)
        [HDR] _HighColor ("Upper colour", Color) = (0.55, 0.25, 0.9, 1)
        _Brightness ("Brightness", Range(0, 3)) = 1
        _Speed ("How fast it drifts", Range(0, 3)) = 1
        _Seed ("Pattern seed (each curtain differs)", Float) = 0
        _Fade ("How much shows (0 hidden, 1 full)", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-50" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex aurora_vertex
            #pragma fragment aurora_fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LowColor;
                half4 _HighColor;
                half _Brightness;
                half _Speed;
                float _Seed;
                half _Fade;
            CBUFFER_END

            struct MeshCorner
            {
                float4 position : POSITION;
                float3 normal : NORMAL;   // Sideways across the ribbon: the way its folds sway.
                float2 along_and_up : TEXCOORD0;  // x: metres along the ribbon; y: 0 bottom, 1 top.
                float4 extra : TEXCOORD1;  // x: the ribbon's whole length in metres.
            };

            struct ToPixel
            {
                float4 screen_position : SV_POSITION;
                float2 along_and_up : TEXCOORD0;
                float length_of_ribbon : TEXCOORD1;
                float fold : TEXCOORD2;   // How sharply the curtain is folding here (folds glow brighter).
            };

            // A repeatable "random" number from 0 to 1.
            float hash_of(float value)
            {
                return frac(sin(value * 127.1 + _Seed * 17.3) * 43758.5453);
            }

            // Smooth noise along a line: random heights at whole numbers, blended smoothly between.
            float line_noise(float value)
            {
                float whole = floor(value);
                float part = frac(value);
                part = part * part * (3.0 - 2.0 * part);
                return lerp(hash_of(whole), hash_of(whole + 1.0), part);
            }

            // Sways each corner sideways in slow waves, so the curtain folds and ripples.
            ToPixel aurora_vertex(MeshCorner corner)
            {
                ToPixel result;
                float time = _Time.y * _Speed;
                float along = corner.along_and_up.x;
                float sway = sin(along * 0.012 + time * 0.13 + _Seed) * 60.0
                           + sin(along * 0.031 - time * 0.21 + _Seed * 2.0) * 22.0;
                // Where the curtain folds, we look along it through more glowing air, so it's
                // brighter: the slope of the sway says how sharply it folds.
                float slope = cos(along * 0.012 + time * 0.13 + _Seed) * 60.0 * 0.012
                            + cos(along * 0.031 - time * 0.21 + _Seed * 2.0) * 22.0 * 0.031;
                float3 position = corner.position.xyz + corner.normal * sway * (0.6 + 0.4 * corner.along_and_up.y);
                result.screen_position = TransformObjectToHClip(position);
                result.along_and_up = corner.along_and_up;
                result.length_of_ribbon = corner.extra.x;
                result.fold = saturate(abs(slope));
                return result;
            }

            // Works out how bright and what colour the curtain is at one spot.
            half4 aurora_fragment(ToPixel pixel) : SV_Target
            {
                float time = _Time.y * _Speed;
                float along = pixel.along_and_up.x;
                float up = pixel.along_and_up.y;

                // Fine vertical rays drifting sideways: a few bright ones over fainter ones.
                float fine = line_noise(along * 0.45 + time * 0.8);
                float rays = fine * fine * fine * 0.8 + line_noise(along * 0.11 - time * 0.3) * 0.45;
                // Broad patches along the curtain: some stretches blaze, some fade right out, so
                // the sky and stars show between them.
                float patches = smoothstep(0.42, 0.85, line_noise(along * 0.0035 + time * 0.025 + _Seed * 3.1));

                // The lower edge rises and falls along the curtain, and is the brightest part.
                float edge = 0.05 + 0.12 * line_noise(along * 0.015 + time * 0.07);
                float above_edge = smoothstep(edge - 0.015, edge + 0.03, up);
                float fade_up = pow(saturate(1.0 - (up - edge) / (1.0 - edge)), 3.4);
                float edge_line = exp(-abs(up - edge - 0.02) * 40.0);

                // The two ends of the ribbon fade out, so it never shows a hard cut.
                float ends = smoothstep(0.0, 200.0, along) * smoothstep(0.0, 200.0, pixel.length_of_ribbon - along);

                float folds = 0.45 + 1.4 * pixel.fold;
                float brightness = (above_edge * fade_up * (0.25 + 1.2 * rays) + edge_line * 0.9) * patches * folds * ends;
                half3 colour = lerp(_LowColor.rgb, _HighColor.rgb, smoothstep(0.3, 0.9, up));
                colour = lerp(colour, half3(0.7, 1.6, 0.8), edge_line * 0.5);  // A paler, whiter-green hem.
                return half4(colour * brightness * _Brightness * _Fade, 1);
            }
            ENDHLSL
        }
    }
}
